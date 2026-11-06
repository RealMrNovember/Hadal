using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Hadal.Gateway.Admin;
using Hadal.Game.Economy;
using HADAL.Shared.Auth;

namespace Hadal.Game.Tests
{
    /// <summary>The economy ledger surface (docs/30): a new player records a starting wallet, and the Overseer can read the flow report.</summary>
    public sealed class EconomyFlowTests : IClassFixture<EconomyFlowTests.Factory>
    {
        private const string ProxyKey = "test-proxy-key-test-proxy-key-0123456789";
        private static readonly JsonSerializerOptions Json = GatewayEndToEndTests.Json;
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
        private readonly Factory _factory;

        public EconomyFlowTests(Factory factory)
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

        private static HttpClient Proxied(Factory factory, string? token = null)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add(AdminAuth.ProxyHeader, ProxyKey);
            if (token != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<string> AdminLoginAsync()
        {
            var store = _factory.Services.GetRequiredService<IAdminStore>();
            var (admin, _) = AdminAuth.NewAdmin("econ-" + Guid.NewGuid().ToString("N")[..8], AdminRoles.Viewer);
            await store.CreateAdminAsync(admin with { PasswordHash = Hadal.Gateway.Auth.PasswordHasher.Hash("op-pass-" + admin.Username) }, default);
            var code = Totp.Code(Totp.FromBase32(admin.TotpSecret), DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
            var response = await Proxied(_factory).PostAsJsonAsync("/admin/api/login",
                new { username = admin.Username, password = "op-pass-" + admin.Username, code });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<JsonObject>())!["token"]!.GetValue<string>();
        }

        private async Task CreatePlayerAsync(string tag)
        {
            var world = _factory.Services.GetRequiredService<GameWorld>();
            await world.ConnectAsync("econ-" + tag, new RecordingConnection(), default, "econ-" + tag);
        }

        [Fact]
        public async Task StartingWallet_IsRecorded()
        {
            await CreatePlayerAsync("start");
            var store = _factory.Services.GetRequiredService<IEconomyFlowStore>();
            var rows = await store.LoadAsync(DateTimeOffset.UtcNow.AddDays(-1), default);
            Assert.Contains(rows, e => e.Source == "starting_wallet" && e.Amount > 0);
        }

        [Fact]
        public async Task FlowEndpoint_RequiresAdminSession()
        {
            Assert.Equal(HttpStatusCode.NotFound,
                (await _factory.CreateClient().GetAsync("/admin/api/economy/flow?days=1")).StatusCode);
            var login = await _factory.CreateClient().PostAsJsonAsync(AuthRoutes.Guest, new GuestLoginRequest(), Json);
            var playerToken = (await login.Content.ReadFromJsonAsync<AuthResponse>(Json))!.accessToken!;
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await Proxied(_factory, playerToken).GetAsync("/admin/api/economy/flow?days=1")).StatusCode);
        }

        [Fact]
        public async Task FlowEndpoint_ReturnsRows_AndClampsDays()
        {
            await CreatePlayerAsync("rows");
            var admin = await AdminLoginAsync();
            var ok = await Proxied(_factory, admin).GetFromJsonAsync<JsonObject>("/admin/api/economy/flow?days=999999", Web);
            Assert.Equal(365, ok!["days"]!.GetValue<int>());
            Assert.Contains(ok["rows"]!.AsArray(), n => n!["source"]!.GetValue<string>() == "starting_wallet");
        }
    }
}
