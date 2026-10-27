using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hadal.Game;
using Hadal.Gateway.Admin;
using HADAL.Shared.Auth;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Hadal.Game.Tests
{
    /// <summary>The Overseer API (docs/22) over the real HTTP pipeline with in-memory stores.</summary>
    public sealed class AdminApiTests : IClassFixture<AdminApiTests.Factory>
    {
        private const string ProxyKey = "test-proxy-key-test-proxy-key-0123456789";
        private static readonly JsonSerializerOptions Json = GatewayEndToEndTests.Json;
        private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
        private readonly Factory _factory;

        public AdminApiTests(Factory factory)
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

        private async Task<AdminUser> CreateAdminAsync(string role)
        {
            var store = _factory.Services.GetRequiredService<IAdminStore>();
            var (admin, password) = AdminAuth.NewAdmin("op-" + Guid.NewGuid().ToString("N")[..8], role);
            await store.CreateAdminAsync(admin with { PasswordHash = Hadal.Gateway.Auth.PasswordHasher.Hash("pw-" + admin.Username) }, default);
            return admin;
        }

        private HttpClient Proxied(string? token = null)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add(AdminAuth.ProxyHeader, ProxyKey);
            if (token != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<string> LoginAsync(string role)
        {
            var admin = await CreateAdminAsync(role);
            var code = Totp.Code(Totp.FromBase32(admin.TotpSecret), DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
            var response = await Proxied().PostAsJsonAsync("/admin/api/login", new { username = admin.Username, password = "pw-" + admin.Username, code });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<JsonObject>())!["token"]!.GetValue<string>();
        }

        private async Task<AuthResponse> GuestAsync()
        {
            var response = await _factory.CreateClient().PostAsJsonAsync(AuthRoutes.Guest, new GuestLoginRequest(), Json);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AuthResponse>(Json))!;
        }

        /// <summary>A guest that has entered the game once (so a player state exists).</summary>
        private async Task<AuthResponse> PlayerAsync()
        {
            var login = await GuestAsync();
            var world = _factory.Services.GetRequiredService<GameWorld>();
            var connection = new RecordingConnection();
            var session = await world.ConnectAsync(login.accountId, connection, default);
            await world.DisconnectAsync(session, connection, default);
            return login;
        }

        [Fact]
        public async Task Panel_IsInvisibleWithoutTheProxyKey()
        {
            var direct = _factory.CreateClient();
            Assert.Equal(HttpStatusCode.NotFound, (await direct.GetAsync("/admin/")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await direct.PostAsJsonAsync("/admin/api/login", new { username = "x", password = "y", code = "123456" })).StatusCode);

            direct.DefaultRequestHeaders.Add(AdminAuth.ProxyHeader, "wrong-key-wrong-key-wrong-key-wrong-key-00");
            Assert.Equal(HttpStatusCode.NotFound, (await direct.GetAsync("/admin/api/stats")).StatusCode);
        }

        [Fact]
        public async Task Panel_IsServedUnderAdminSlash_WithStrictHeaders()
        {
            var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Add(AdminAuth.ProxyHeader, ProxyKey);

            var bare = await client.GetAsync("/admin");
            Assert.Equal(HttpStatusCode.Redirect, bare.StatusCode);
            Assert.Equal("/admin/", bare.Headers.Location!.OriginalString);

            var page = await client.GetAsync("/admin/");
            page.EnsureSuccessStatusCode();
            Assert.Contains("Overseer", await page.Content.ReadAsStringAsync());
            Assert.Contains("script-src 'self'", page.Headers.GetValues("Content-Security-Policy").Single());
            Assert.Equal("DENY", page.Headers.GetValues("X-Frame-Options").Single());
            (await client.GetAsync("/admin/app.js")).EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Login_NeedsTheRightPassword_NoCode()
        {
            var admin = await CreateAdminAsync(AdminRoles.Owner);
            var passwordOnly = await Proxied().PostAsJsonAsync("/admin/api/login", new { username = admin.Username, password = "pw-" + admin.Username });
            Assert.Equal(HttpStatusCode.OK, passwordOnly.StatusCode);

            var wrongPassword = await Proxied().PostAsJsonAsync("/admin/api/login", new { username = admin.Username, password = "nope" });
            Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
            var unknown = await Proxied().PostAsJsonAsync("/admin/api/login", new { username = "nobody-" + admin.Username, password = "pw-" + admin.Username });
            Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized, (await Proxied().GetAsync("/admin/api/stats")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Proxied("not-a-token").GetAsync("/admin/api/stats")).StatusCode);
        }

        [Fact]
        public async Task PlayerTokens_CannotOpenThePanel()
        {
            var player = await GuestAsync();
            Assert.Equal(HttpStatusCode.Unauthorized, (await Proxied(player.accessToken).GetAsync("/admin/api/stats")).StatusCode);
        }

        [Fact]
        public async Task Events_AreCreatedPublishedAndListed()
        {
            var gm = await LoginAsync(AdminRoles.GameMaster);
            var viewer = await LoginAsync(AdminRoles.Viewer);
            var start = DateTimeOffset.UtcNow.AddMinutes(-5);
            var body = new
            {
                template = "points",
                title = new Dictionary<string, string> { ["tr"] = "Derinlik Yarışı" },
                banner = "abyss",
                startsAt = start,
                endsAt = start.AddDays(3),
                actions = new[] { new { action = "monster_kill", points = 10, per = 1 } },
                stages = new[] { new { points = 100, rewards = new[] { new { type = "resource", id = "Titanium", amount = 1000 } } } }
            };

            Assert.Equal(HttpStatusCode.Forbidden, (await Proxied(viewer).PostAsJsonAsync("/admin/api/events", body)).StatusCode);
            var bad = await Proxied(gm).PostAsJsonAsync("/admin/api/events", new { template = "points", title = new Dictionary<string, string>(), startsAt = start, endsAt = start.AddDays(1) });
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

            var created = await Proxied(gm).PostAsJsonAsync("/admin/api/events", body);
            created.EnsureSuccessStatusCode();
            var id = (await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetString()!;
            (await Proxied(gm).PostAsync($"/admin/api/events/{id}/publish", null)).EnsureSuccessStatusCode();

            var list = await Proxied(viewer).GetFromJsonAsync<System.Text.Json.JsonElement>("/admin/api/events");
            var row = list.EnumerateArray().Single(e => e.GetProperty("definition").GetProperty("id").GetString() == id);
            Assert.Equal("live", row.GetProperty("phase").GetString());
            Assert.Equal(HttpStatusCode.OK, (await Proxied(viewer).GetAsync($"/admin/api/events/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await Proxied(viewer).GetAsync("/admin/api/events/meta")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Proxied(gm).DeleteAsync($"/admin/api/events/{id}")).StatusCode);
        }

        [Fact]
        public async Task Roles_AreEnforced()
        {
            var viewer = await LoginAsync(AdminRoles.Viewer);
            var player = await PlayerAsync();
            Assert.Equal(HttpStatusCode.OK, (await Proxied(viewer).GetAsync("/admin/api/stats")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await Proxied(viewer).PostAsJsonAsync($"/admin/api/players/{player.accountId}/grant",
                new { amounts = new Dictionary<string, long> { ["Titanium"] = 5 } })).StatusCode);

            var gm = await LoginAsync(AdminRoles.GameMaster);
            Assert.Equal(HttpStatusCode.Forbidden, (await Proxied(gm).PutAsJsonAsync("/admin/api/catalog", new { json = "{}", note = "x" })).StatusCode);
        }

        [Fact]
        public async Task Grant_ReachesOfflineAndOnlinePlayers()
        {
            var gm = await LoginAsync(AdminRoles.GameMaster);
            var player = await PlayerAsync();

            var offline = await Proxied(gm).PostAsJsonAsync($"/admin/api/players/{player.accountId}/grant",
                new { amounts = new Dictionary<string, long> { ["Titanium"] = 500 }, note = "support ticket 42" });
            offline.EnsureSuccessStatusCode();

            var detail = await Proxied(gm).GetFromJsonAsync<JsonObject>($"/admin/api/players/{player.accountId}");
            Assert.Equal(500 + 500, // starting titanium + grant
                 detail!["wallet"]!["Titanium"]!.GetValue<long>());

            // Online: the change reaches the live session and its connection at once.
            var world = _factory.Services.GetRequiredService<GameWorld>();
            var connection = new RecordingConnection();
            var session = await world.ConnectAsync(player.accountId, connection, default);
            (await Proxied(gm).PostAsJsonAsync($"/admin/api/players/{player.accountId}/grant",
                new { amounts = new Dictionary<string, long> { ["Hadalite"] = 25 } })).EnsureSuccessStatusCode();
            Assert.Equal(125, connection.Messages.Last().Delta().Wallet()!.Amount(ResourceType.Hadalite)); // 100 to start + 25

            // Direct grants pass the storage cap: nothing given is lost.
            (await Proxied(gm).PostAsJsonAsync($"/admin/api/players/{player.accountId}/grant",
                new { amounts = new Dictionary<string, long> { ["Titanium"] = 5_000_000 } })).EnsureSuccessStatusCode();
            Assert.True(session.StateForTests.Wallet[ResourceType.Titanium] >= 5_000_000);

            // Packages: one capsule of exactly that amount in the bag.
            (await Proxied(gm).PostAsJsonAsync($"/admin/api/players/{player.accountId}/grant",
                new { amounts = new Dictionary<string, long> { ["Food"] = 250_000 }, packages = true })).EnsureSuccessStatusCode();
            Assert.Equal(1, session.StateForTests.Items[Capsules.Id(ResourceType.Food, 250_000)]);

            var audit = await Proxied(gm).GetFromJsonAsync<JsonArray>("/admin/api/audit");
            Assert.Contains(audit!, e => e!["action"]!.GetValue<string>() == "player.grant" && e["details"]!.GetValue<string>().Contains("support ticket 42"));
        }

        [Fact]
        public async Task Ban_BlocksRefreshAndKicksTheSession()
        {
            var gm = await LoginAsync(AdminRoles.GameMaster);
            var player = await PlayerAsync();
            var world = _factory.Services.GetRequiredService<GameWorld>();
            var connection = new RecordingConnection();
            await world.ConnectAsync(player.accountId, connection, default);

            (await Proxied(gm).PostAsJsonAsync($"/admin/api/players/{player.accountId}/ban", new { hours = 24, reason = "cheating" })).EnsureSuccessStatusCode();
            Assert.Equal(HandshakeRejectReasons.Banned, connection.ClosedReason);

            var refresh = await _factory.CreateClient().PostAsJsonAsync(AuthRoutes.Refresh, new RefreshRequest { refreshToken = player.refreshToken }, Json);
            Assert.Equal(HttpStatusCode.Forbidden, refresh.StatusCode);

            (await Proxied(gm).PostAsync($"/admin/api/players/{player.accountId}/unban", null)).EnsureSuccessStatusCode();
            var store = _factory.Services.GetRequiredService<IAdminStore>();
            Assert.Null(await store.GetActiveBanAsync(Guid.Parse(player.accountId), DateTimeOffset.UtcNow, default));
        }

        [Fact]
        public async Task Coupons_RedeemOncePerPlayer_AndRespectTheLimit()
        {
            var gm = await LoginAsync(AdminRoles.GameMaster);
            var code = "WELCOME" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var created = await Proxied(gm).PostAsJsonAsync("/admin/api/coupons",
                new { code, rewards = new Dictionary<string, long> { ["Titanium"] = 300, ["Hadalite"] = 10 }, maxRedemptions = 1, note = "launch" });
            created.EnsureSuccessStatusCode();

            var first = await PlayerAsync();
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.accessToken);
            var redeemed = await client.PostAsJsonAsync(AuthRoutes.CouponRedeem, new CouponRedeemRequest { code = code.ToLowerInvariant() }, Json);
            redeemed.EnsureSuccessStatusCode();
            Assert.Equal(2, (await redeemed.Content.ReadFromJsonAsync<CouponRedeemResponse>(Json))!.rewards.Length);

            var again = await client.PostAsJsonAsync(AuthRoutes.CouponRedeem, new CouponRedeemRequest { code = code }, Json);
            Assert.Equal(AccountErrors.CouponAlreadyRedeemed, (await again.Content.ReadFromJsonAsync<AccountError>(Json))!.error);

            var second = await PlayerAsync();
            var other = _factory.CreateClient();
            other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", second.accessToken);
            var exhausted = await other.PostAsJsonAsync(AuthRoutes.CouponRedeem, new CouponRedeemRequest { code = code }, Json);
            Assert.Equal(AccountErrors.CouponExhausted, (await exhausted.Content.ReadFromJsonAsync<AccountError>(Json))!.error);

            // Coupon gifts land in the bag as a package of exactly their amount; Hadalite is paid out directly.
            var world = _factory.Services.GetRequiredService<GameWorld>();
            var state = (await world.DescribeAsync(first.accountId, default))!;
            Assert.Equal(1, state.Items[Capsules.Id(ResourceType.Titanium, 300)]);
            Assert.Equal(500, state.Wallet[ResourceType.Titanium]);
            Assert.Equal(100 + 10, state.Wallet[ResourceType.Hadalite]);
        }

        private async Task<string> RedeemErrorAsync(AuthResponse player, string code)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", player.accessToken);
            var response = await client.PostAsJsonAsync(AuthRoutes.CouponRedeem, new CouponRedeemRequest { code = code }, Json);
            return response.IsSuccessStatusCode ? "ok" : (await response.Content.ReadFromJsonAsync<AccountError>(Json))!.error;
        }

        [Fact]
        public async Task Coupons_ForOnePlayer_AndDateWindows()
        {
            var gm = await LoginAsync(AdminRoles.GameMaster);
            var owner = await PlayerAsync();
            var stranger = await PlayerAsync();
            var rewards = new Dictionary<string, long> { ["Titanium"] = 50 };

            // Per-player compensation code.
            var personal = "VIP" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            (await Proxied(gm).PostAsJsonAsync("/admin/api/coupons", new { code = personal, rewards, targetAccountId = owner.accountId })).EnsureSuccessStatusCode();
            Assert.Equal(AccountErrors.CouponNotForYou, await RedeemErrorAsync(stranger, personal));
            Assert.Equal("ok", await RedeemErrorAsync(owner, personal));
            Assert.Equal(AccountErrors.CouponAlreadyRedeemed, await RedeemErrorAsync(owner, personal));

            // Event code that opens tomorrow, and one that already ended.
            var future = "EVT" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            (await Proxied(gm).PostAsJsonAsync("/admin/api/coupons", new { code = future, rewards, startsAt = DateTimeOffset.UtcNow.AddDays(1), expiresAt = DateTimeOffset.UtcNow.AddDays(2) })).EnsureSuccessStatusCode();
            Assert.Equal(AccountErrors.CouponNotStarted, await RedeemErrorAsync(owner, future));

            var past = "OLD" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            (await Proxied(gm).PostAsJsonAsync("/admin/api/coupons", new { code = past, rewards, expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) })).EnsureSuccessStatusCode();
            Assert.Equal(AccountErrors.CouponExpired, await RedeemErrorAsync(owner, past));

            // Bad input is refused.
            Assert.Equal(HttpStatusCode.BadRequest, (await Proxied(gm).PostAsJsonAsync("/admin/api/coupons",
                new { rewards, startsAt = DateTimeOffset.UtcNow.AddDays(2), expiresAt = DateTimeOffset.UtcNow.AddDays(1) })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Proxied(gm).PostAsJsonAsync("/admin/api/coupons",
                new { rewards, targetAccountId = Guid.NewGuid().ToString("N") })).StatusCode);
        }

        [Fact]
        public async Task LiveOps_DriveTheManifest()
        {
            var gm = await LoginAsync(AdminRoles.GameMaster);
            (await Proxied(gm).PutAsJsonAsync("/admin/api/liveops",
                new { maintenance = true, maintenanceMessage = "Bakım: 10 dk", announcement = "Hoş geldin, kaptan!" })).EnsureSuccessStatusCode();

            var manifest = await _factory.CreateClient().GetFromJsonAsync<VersionManifest>(AuthRoutes.Manifest, Json);
            Assert.True(manifest!.maintenance);
            Assert.Equal("Bakım: 10 dk", manifest.message);
            Assert.Equal("Hoş geldin, kaptan!", manifest.announcement);
            Assert.Equal("https://hadal.cicibyte.com/download/hadal.apk", manifest.updateUrl);

            // OTA: the panel announces a new build and can force old ones to update.
            (await Proxied(gm).PutAsJsonAsync("/admin/api/liveops",
                new { latestClientVersion = "1.2.0", minClientVersion = "1.1.0", updateUrl = "https://example.org/h.apk" })).EnsureSuccessStatusCode();
            manifest = await _factory.CreateClient().GetFromJsonAsync<VersionManifest>(AuthRoutes.Manifest, Json);
            Assert.Equal("1.2.0", manifest!.latestClientVersion);
            Assert.Equal("1.1.0", manifest.minClientVersion);
            Assert.Equal("https://example.org/h.apk", manifest.updateUrl);
            Assert.Equal(HttpStatusCode.BadRequest, (await Proxied(gm).PutAsJsonAsync("/admin/api/liveops", new { latestClientVersion = "yeni" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Proxied(gm).PutAsJsonAsync("/admin/api/liveops", new { updateUrl = "http://plain.example/h.apk" })).StatusCode);

            (await Proxied(gm).PutAsJsonAsync("/admin/api/liveops", new { maintenance = false, maintenanceMessage = "", announcement = "" })).EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Catalog_PublishValidatesAppliesLiveAndRestores()
        {
            var owner = await LoginAsync(AdminRoles.Owner);
            var current = await Proxied(owner).GetFromJsonAsync<JsonObject>("/admin/api/catalog");
            var json = current!["json"]!.GetValue<string>();

            var invalid = await Proxied(owner).PutAsJsonAsync("/admin/api/catalog", new { json = "{ \"buildings\": [ { \"id\": \"\" } ] }", note = "broken" });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

            // Make the reactor's first upgrade cheaper and publish it.
            var data = JsonNode.Parse(json)!;
            data["progression"]!["freeFinishSeconds"] = 600;
            var published = await Proxied(owner).PutAsJsonAsync("/admin/api/catalog", new { json = data.ToJsonString(), note = "longer free finish" });
            published.EnsureSuccessStatusCode();
            var version = (await published.Content.ReadFromJsonAsync<JsonObject>())!["version"]!.GetValue<int>();

            var world = _factory.Services.GetRequiredService<GameWorld>();
            Assert.Equal(600, world.Catalog.Catalog.Progression.freeFinishSeconds);
            Assert.Equal(version, world.Catalog.Version);

            var publicCatalog = await _factory.CreateClient().GetAsync(AuthRoutes.Catalog);
            Assert.Equal(version.ToString(), publicCatalog.Headers.GetValues("X-Catalog-Version").Single());
            Assert.Equal(version, (await _factory.CreateClient().GetFromJsonAsync<VersionManifest>(AuthRoutes.Manifest, Json))!.catalogVersion);

            // Restore the original as a new version.
            var original = await Proxied(owner).PutAsJsonAsync("/admin/api/catalog", new { json, note = "back to file" });
            original.EnsureSuccessStatusCode();
            Assert.Equal(300, world.Catalog.Catalog.Progression.freeFinishSeconds);
        }

        [Fact]
        public void NewerCatalogFile_SupersedesOlderPanelEdits_OnlyWhenItsDesignIsNewer()
        {
            Assert.True(CatalogBootstrap.FileSupersedes("{\"version\": 3}", "{\"version\": 1, \"x\": 2}"));
            Assert.False(CatalogBootstrap.FileSupersedes("{\"version\": 3}", "{\"version\": 3}"));
            Assert.False(CatalogBootstrap.FileSupersedes("{\"version\": 2}", "{\"version\": 3}"));
            Assert.Equal(0, CatalogBootstrap.DesignVersion("not json"));
        }

        [Fact]
        public void Totp_MatchesTheRfc6238Reference()
        {
            // RFC 6238 appendix B, SHA-1 secret "12345678901234567890", T = 59 s -> 94287082 (8 digits) -> 287082.
            var key = System.Text.Encoding.ASCII.GetBytes("12345678901234567890");
            Assert.Equal("287082", Totp.Code(key, 59 / 30));
            Assert.Equal(key, Totp.FromBase32(Totp.Base32(key)));
        }
    }
}
