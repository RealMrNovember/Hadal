using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Hadal.Gateway.Admin;
using HADAL.Shared.Auth;
using HADAL.Shared.Telemetry;

namespace Hadal.Game.Tests
{
    /// <summary>Client crash/error ingest and the admin read path (docs/22).</summary>
    public sealed class ClientLogTests : IClassFixture<ClientLogTests.Factory>
    {
        private const string ProxyKey = "test-proxy-key-test-proxy-key-0123456789";
        private static readonly JsonSerializerOptions Json = GatewayEndToEndTests.Json;
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
        private readonly Factory _factory;

        public ClientLogTests(Factory factory)
        {
            _factory = factory;
        }

        public sealed class Factory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseSetting("Game:CatalogPath", Path.Combine(AppContext.BaseDirectory, "catalog", "catalog.json"));
                builder.UseSetting("ConnectionStrings:Hadal", string.Empty);
                builder.UseSetting("Auth:JwtSecret", "test-secret-test-secret-test-secret-0123");
                builder.UseSetting("Auth:GuestLoginsPerHour", "1000");
                builder.UseSetting("Auth:TransferLoginsPerHour", "1000");
                builder.UseSetting("Admin:ProxyKey", ProxyKey);
                builder.UseSetting("Admin:JwtSecret", "admin-secret-admin-secret-admin-secret-01");
                builder.UseSetting("Admin:LoginAttemptsPer15Minutes", "1000");
            }
        }

        private async Task<AuthResponse> GuestAsync()
        {
            var response = await _factory.CreateClient().PostAsJsonAsync(AuthRoutes.Guest, new GuestLoginRequest(), Json);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        }

        private HttpClient Player(string? accessToken = null)
        {
            var client = _factory.CreateClient();
            if (accessToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }

        private HttpClient Proxied(string? token = null)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(AdminAuth.ProxyHeader, ProxyKey);
            if (token != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<string> AdminLoginAsync()
        {
            var store = _factory.Services.GetRequiredService<IAdminStore>();
            var (admin, password) = AdminAuth.NewAdmin("op-" + Guid.NewGuid().ToString("N")[..8], AdminRoles.Viewer);
            await store.CreateAdminAsync(admin with { PasswordHash = Hadal.Gateway.Auth.PasswordHasher.Hash("op-pass-" + admin.Username) }, default);
            var code = Totp.Code(Totp.FromBase32(admin.TotpSecret), DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
            var response = await Proxied().PostAsJsonAsync("/admin/api/login",
                new { username = admin.Username, password = "op-pass-" + admin.Username, code });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<JsonObject>())!["token"]!.GetValue<string>();
        }

        private static ClientLogBatch Batch(params ClientLogEntry[] entries) =>
            new() { entries = entries, clientVersion = "0.9.10", platform = "Android", device = "Test Phone" };

        private static ClientLogEntry Entry(string level = ClientLogLevels.Error, string message = "boom") =>
            new() { level = level, message = message, stack = "stack-trace", at = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };

        [Fact]
        public async Task Post_RequiresAValidToken()
        {
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await Player().PostAsJsonAsync(TelemetryRoutes.ClientLog, Batch(Entry()), Json)).StatusCode);
        }

        [Fact]
        public async Task Post_WritesErrorAndException_Entries()
        {
            var login = await GuestAsync();
            var token = login.accessToken!;
            var post = await Player(token).PostAsJsonAsync(TelemetryRoutes.ClientLog,
                Batch(Entry(ClientLogLevels.Error, "error-one"), Entry(ClientLogLevels.Exception, "exception-two")), Json);
            Assert.Equal(HttpStatusCode.Accepted, post.StatusCode);
            var ack = (await post.Content.ReadFromJsonAsync<ClientLogAck>(Json))!;
            Assert.Equal(2, ack.accepted);

            var admin = await AdminLoginAsync();
            var logs = await Proxied(admin)
                .GetFromJsonAsync<JsonArray>("/admin/api/logs/client?days=1", Web);
            var messages = logs!.Select(n => n!["message"]!.GetValue<string>()).ToHashSet();
            Assert.Contains("error-one", messages);
            Assert.Contains("exception-two", messages);
        }

        [Fact]
        public async Task Post_IgnoresUnknownLevels_AndCapsBatch()
        {
            var login = await GuestAsync();
            var entries = Enumerable.Range(0, 25).Select(i => Entry(ClientLogLevels.Error, "cap-" + i)).ToList();
            entries.Add(Entry("debug", "should-drop"));
            var post = await Player(login.accessToken).PostAsJsonAsync(TelemetryRoutes.ClientLog,
                Batch(entries.ToArray()), Json);
            Assert.Equal(HttpStatusCode.Accepted, post.StatusCode);
            var ack = (await post.Content.ReadFromJsonAsync<ClientLogAck>(Json))!;
            Assert.Equal(20, ack.accepted);
        }

        [Fact]
        public async Task Post_TruncatesLongMessagesAndStacks()
        {
            var login = await GuestAsync();
            var longMessage = new string('m', 1000);
            var longStack = new string('s', 9000);
            var entry = Entry(ClientLogLevels.Error, longMessage);
            entry.stack = longStack;
            var post = await Player(login.accessToken).PostAsJsonAsync(TelemetryRoutes.ClientLog, Batch(entry), Json);
            Assert.Equal(HttpStatusCode.Accepted, post.StatusCode);

            var admin = await AdminLoginAsync();
            var logs = await Proxied(admin)
                .GetFromJsonAsync<JsonArray>("/admin/api/logs/client?days=1&q=mmmm", Web);
            var first = logs!.First(n => n!["message"]!.GetValue<string>().StartsWith("mmm"))!;
            Assert.Equal(300, first["message"]!.GetValue<string>().Length);
            Assert.Equal(4000, first["stack"]!.GetValue<string>().Length);
        }

        [Fact]
        public async Task AdminRead_RequiresAdminSession()
        {
            var login = await GuestAsync();
            // Without the proxy key the panel is hidden entirely (404).
            Assert.Equal(HttpStatusCode.NotFound, (await Player(login.accessToken).GetAsync("/admin/api/logs/client?days=1")).StatusCode);
            // Through the proxy with a player token, admin auth refuses it (401).
            Assert.Equal(HttpStatusCode.Unauthorized, (await Proxied(login.accessToken).GetAsync("/admin/api/logs/client?days=1")).StatusCode);
        }
    }
}
