using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.RateLimiting;
using Hadal.Game;
using HADAL.Shared.Auth;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;

namespace Hadal.Gateway.Admin
{
    public sealed record LoginRequest(string Username, string Password, string? Code = null);
    /// <param name="Packages">True: each resource goes to the bag as a package; false: straight to the wallet (past the cap).</param>
    public sealed record GrantRequest(Dictionary<string, long> Amounts, string Note, bool Packages = false, Dictionary<string, int>? Items = null);
    public sealed record BanRequest(int Hours, string Reason);
    public sealed record PurchaseRequest(string PackId, string Note);
    /// <param name="Level">When set, the points become that level's threshold; otherwise <paramref name="Points"/> are used.</param>
    public sealed record VipRequest(long? Points, int? Level, string Note);
    public sealed record CouponRequest(string Code, Dictionary<string, long> Rewards, int MaxRedemptions, DateTimeOffset? ExpiresAt, string Note,
        DateTimeOffset? StartsAt = null, string? TargetAccountId = null);
    public sealed record ActiveRequest(bool Active);
    public sealed record CatalogRequest(string Json, string Note);
    public sealed record AdminCreateRequest(string Username, string Role);
    public sealed record DisabledRequest(bool Disabled);

    /// <summary>Overseer REST API under /admin/api and the panel itself under /admin (docs/22).</summary>
    public static class AdminEndpoints
    {
        private const string LoginPolicy = "admin-login";
        private const string ApiPolicy = "admin-api";

        public static IServiceCollection AddHadalAdmin(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.Section));
            services.AddSingleton<AdminAuth>();
            services.AddSingleton<LiveCatalog>();
            services.AddSingleton<LiveOpsCache>();
            services.AddSingleton<CouponService>();
            services.AddHostedService<CatalogBootstrap>();

            var perWindow = configuration.GetSection(AdminOptions.Section).GetValue(nameof(AdminOptions.LoginAttemptsPer15Minutes), 10);
            services.AddRateLimiter(limiter =>
            {
                limiter.AddPolicy(LoginPolicy, context => RateLimitPartition.GetFixedWindowLimiter(AdminAuth.ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = perWindow, Window = TimeSpan.FromMinutes(15) }));
                limiter.AddPolicy(ApiPolicy, context => RateLimitPartition.GetFixedWindowLimiter(AdminAuth.ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 300, Window = TimeSpan.FromMinutes(1) }));
            });
            return services;
        }

        public static void MapHadalAdmin(this WebApplication app)
        {
            // One door: anything under /admin that did not come through the fronting proxy is a 404,
            // so hadal-api.cicibyte.com/admin does not even reveal that the panel exists.
            app.Use(async (http, next) =>
            {
                if (http.Request.Path.StartsWithSegments("/admin") && !http.RequestServices.GetRequiredService<AdminAuth>().IsFromProxy(http))
                {
                    http.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                if (http.Request.Path.StartsWithSegments("/admin"))
                {
                    // The panel resolves "api/..." relative to itself, so it must live at /admin/.
                    if (http.Request.Path.Value == "/admin")
                    {
                        http.Response.Redirect("/admin/");
                        return;
                    }

                    http.Response.Headers["Cache-Control"] = "no-store";
                    http.Response.Headers["X-Frame-Options"] = "DENY";
                    http.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    http.Response.Headers["Referrer-Policy"] = "no-referrer";
                    http.Response.Headers["Content-Security-Policy"] =
                        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
                }

                await next();
            });

            var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            if (Directory.Exists(webRoot))
            {
                var files = new PhysicalFileProvider(webRoot);
                app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files, RequestPath = "" });
                app.UseStaticFiles(new StaticFileOptions { FileProvider = files, RequestPath = "" });
            }

            var api = app.MapGroup("/admin/api").RequireRateLimiting(ApiPolicy);

            api.MapPost("/login", async (LoginRequest request, HttpContext http, AdminAuth auth, IAdminStore store, CancellationToken ct) =>
            {
                var (token, principal) = await auth.LoginAsync(request.Username, request.Password, request.Code, ct);
                await store.AuditAsync(request.Username ?? "?", principal != null ? "login" : "login.failed", string.Empty, string.Empty, AdminAuth.ClientIp(http), ct);
                return principal == null
                    ? Results.Json(new { error = "InvalidCredentials" }, statusCode: StatusCodes.Status401Unauthorized)
                    : Results.Ok(new { token, username = principal.Username, role = principal.Role, expiresIn = auth.SessionSeconds });
            }).RequireRateLimiting(LoginPolicy);

            api.MapGet("/me", (AdminPrincipal me) => Results.Ok(new { me.Username, me.Role })).RequireAdmin(AdminRoles.Viewer);

            api.MapGet("/stats", async (AdminPrincipal _, IAdminStore store, GameWorld world, LiveCatalog catalog,
                LiveOpsCache liveOps, IClock clock, CancellationToken ct) =>
            {
                var (total, last24h) = await store.CountPlayersAsync(clock.UtcNow, ct);
                var coupons = await store.ListCouponsAsync(ct);
                return Results.Ok(new
                {
                    players = total,
                    playersLast24h = last24h,
                    online = world.OnlineIdentities.Count,
                    catalogVersion = catalog.Version,
                    couponRedemptions = coupons.Sum(c => c.Redeemed),
                    liveOps = await liveOps.GetAsync(ct)
                });
            }).RequireAdmin(AdminRoles.Viewer);

            // --- Players --------------------------------------------------------------------

            api.MapGet("/players", async (AdminPrincipal _, string? q, IAdminStore store, GameWorld world, CancellationToken ct) =>
            {
                var players = await store.SearchPlayersAsync((q ?? string.Empty).Trim(), 100, ct);
                return Results.Ok(players.Select(p => PlayerJson(p, world)));
            }).RequireAdmin(AdminRoles.Viewer);

            api.MapGet("/players/{id:guid}", async (AdminPrincipal _, Guid id, IAdminStore store, GameWorld world, CancellationToken ct) =>
            {
                var player = await store.GetPlayerAsync(id, ct);
                if (player == null)
                    return Results.NotFound();

                var state = await world.DescribeAsync(id.ToString("N"), ct);
                return Results.Ok(new
                {
                    player = PlayerJson(player, world),
                    wallet = state?.Wallet.ToDictionary(p => p.Key.ToString(), p => p.Value) ?? new Dictionary<string, long>(),
                    bag = state?.Items.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value) ?? new Dictionary<string, int>(),
                    buildings = state?.Buildings.Select(b => new
                    {
                        b.BuildingId, b.DefinitionId, b.Ring, b.Sector, b.Level,
                        constructionEndsAt = b.ConstructionEndsAt
                    }) ?? Enumerable.Empty<object>(),
                    serverTick = state?.ServerTick ?? 0,
                    vip = new { points = state?.VipPoints ?? 0, level = VipRules.Level(state?.VipPoints ?? 0) }
                });
            }).RequireAdmin(AdminRoles.Viewer);

            api.MapPost("/players/{id:guid}/grant", async (AdminPrincipal me, Guid id, GrantRequest request,
                HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                var amounts = ParseAmounts(request.Amounts ?? new Dictionary<string, long>()) ?? new Dictionary<ResourceType, long>();
                var items = (request.Items ?? new Dictionary<string, int>()).Where(p => p.Value > 0 && world.IsGrantableItem(p.Key))
                    .ToDictionary(p => p.Key, p => p.Value);
                if (amounts.Count == 0 && items.Count == 0)
                    return Results.BadRequest(new { error = "InvalidAmounts" });
                if (!await world.GrantAsync(id.ToString("N"), amounts, ct, request.Packages, items))
                    return Results.NotFound(new { error = "PlayerNotStarted" });

                await store.AuditAsync(me.Username, "player.grant", id.ToString("N"),
                    $"{(request.Packages ? "[paket] " : string.Empty)}{Describe(amounts)} {string.Join(" ", items.Select(p => $"{p.Key}x{p.Value}"))} {request.Note}".Trim(),
                    AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.GameMaster);

            // A shop pack credited by hand (support, tests, until store payments exist): Hadalite plus VIP points.
            api.MapGet("/store/packs", (AdminPrincipal _) => Results.Ok(StorePacks.All.Select(p => new
            {
                id = p.Id, hadalite = p.Hadalite, bonus = p.Bonus, priceCents = p.PriceCents, vipPoints = p.VipPoints
            }))).RequireAdmin(AdminRoles.Viewer);

            api.MapPost("/players/{id:guid}/purchase", async (AdminPrincipal me, Guid id, PurchaseRequest request,
                HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                if (StorePacks.Find(request.PackId ?? string.Empty) == null)
                    return Results.BadRequest(new { error = "UnknownPack" });
                if (!await world.CreditPurchaseAsync(id.ToString("N"), request.PackId!, ct))
                    return Results.NotFound(new { error = "PlayerNotStarted" });
                await store.AuditAsync(me.Username, "player.purchase", id.ToString("N"), $"{request.PackId} {request.Note}".Trim(), AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapPost("/players/{id:guid}/vip", async (AdminPrincipal me, Guid id, VipRequest request,
                HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                long points;
                if (request.Level is { } level)
                {
                    if (level < 0 || level > VipRules.MaxLevel)
                        return Results.BadRequest(new { error = "InvalidVip" });
                    points = VipRules.Thresholds[level];
                }
                else if (request.Points is { } p && p >= 0 && p <= 100_000_000)
                {
                    points = p;
                }
                else
                {
                    return Results.BadRequest(new { error = "InvalidVip" });
                }

                if (!await world.SetVipPointsAsync(id.ToString("N"), points, ct))
                    return Results.NotFound(new { error = "PlayerNotStarted" });
                await store.AuditAsync(me.Username, "player.vip", id.ToString("N"), $"{points} (VIP {VipRules.Level(points)}) {request.Note}".Trim(),
                    AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { points, level = VipRules.Level(points) });
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapPost("/players/{id:guid}/ban", async (AdminPrincipal me, Guid id, BanRequest request,
                HttpContext http, GameWorld world, IAdminStore store, IClock clock, CancellationToken ct) =>
            {
                if (request.Hours <= 0)
                    return Results.BadRequest(new { error = "InvalidHours" });
                var until = clock.UtcNow.AddHours(Math.Min(request.Hours, 24 * 365 * 100));
                await store.SetBanAsync(id, until, request.Reason ?? string.Empty, ct);
                world.Kick(id.ToString("N"), HandshakeRejectReasons.Banned);
                await store.AuditAsync(me.Username, "player.ban", id.ToString("N"), $"{request.Hours}h {request.Reason}", AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { bannedUntil = until });
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapPost("/players/{id:guid}/unban", async (AdminPrincipal me, Guid id, HttpContext http, IAdminStore store, CancellationToken ct) =>
            {
                await store.SetBanAsync(id, null, string.Empty, ct);
                await store.AuditAsync(me.Username, "player.unban", id.ToString("N"), string.Empty, AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.GameMaster);

            // Removal (Owner): test accounts and players that no one owns. Needs Confirm = "DELETE".
            api.MapPost("/players/{id:guid}/delete", async (AdminPrincipal me, Guid id, DeleteRequest request,
                HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                if (request.Confirm != "DELETE")
                    return Results.BadRequest(new { error = "ConfirmationRequired" });
                var player = await store.GetPlayerAsync(id, ct);
                if (player == null)
                    return Results.NotFound();

                var hadBase = await world.DeletePlayerAsync(id.ToString("N"), ct);
                await store.DeleteAccountAsync(id, ct);
                await store.AuditAsync(me.Username, "player.delete", id.ToString("N"),
                    $"{player.DisplayName} {(hadBase ? "account + base" : "account only")}".Trim(), AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { deleted = true, hadBase });
            }).RequireAdmin(AdminRoles.Owner);

            api.MapGet("/orphans", async (AdminPrincipal _, GameWorld world, CancellationToken ct) =>
                Results.Ok((await world.PlayersWithoutAccountAsync(ct)).Select(o => new
                {
                    identityKey = o.IdentityKey,
                    playerId = o.PlayerId.ToString("N"),
                    createdAt = o.CreatedAt
                }))).RequireAdmin(AdminRoles.Viewer);

            api.MapPost("/orphans/delete", async (AdminPrincipal me, OrphanDeleteRequest request,
                HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                if (request.Confirm != "DELETE")
                    return Results.BadRequest(new { error = "ConfirmationRequired" });

                // Only identities that really have no account can go this way.
                var orphans = (await world.PlayersWithoutAccountAsync(ct)).Select(o => o.IdentityKey).ToHashSet(StringComparer.Ordinal);
                var deleted = 0;
                foreach (var key in (request.IdentityKeys ?? Array.Empty<string>()).Where(orphans.Contains).Distinct())
                {
                    if (await world.DeletePlayerAsync(key, ct))
                        deleted++;
                }

                await store.AuditAsync(me.Username, "orphan.delete", $"{deleted}", string.Join(",", request.IdentityKeys ?? Array.Empty<string>()), AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { deleted });
            }).RequireAdmin(AdminRoles.Owner);

            api.MapPost("/players/{id:guid}/kick", async (AdminPrincipal me, Guid id, HttpContext http, GameWorld world, IAdminStore store, CancellationToken ct) =>
            {
                var kicked = world.Kick(id.ToString("N"), "kicked by an operator");
                await store.AuditAsync(me.Username, "player.kick", id.ToString("N"), kicked ? "online" : "offline", AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { kicked });
            }).RequireAdmin(AdminRoles.GameMaster);

            // --- Chat moderation (docs/25) --------------------------------------------------

            api.MapGet("/chat", (AdminPrincipal _, string? channel, GameWorld world) =>
                Results.Ok(world.Social.RecentChat(string.IsNullOrWhiteSpace(channel) ? Hadal.Game.Social.SocialHub.WorldChannel : channel)
                    .OrderByDescending(l => l.Id)
                    .Select(l => new { id = l.Id.ToString(), l.Channel, l.SenderId, l.SenderName, l.AllianceTag, l.Text, l.SentAt, l.IsSystem })))
                .RequireAdmin(AdminRoles.Viewer);

            api.MapDelete("/chat/{id:long}", async (AdminPrincipal me, long id, HttpContext http, GameWorld world, IClock clock, IAdminStore store, CancellationToken ct) =>
            {
                var deleted = world.Social.DeleteChat(id, clock.UtcNow);
                await store.AuditAsync(me.Username, "chat.delete", id.ToString(), deleted ? string.Empty : "not found", AdminAuth.ClientIp(http), ct);
                return deleted ? Results.Ok(new { ok = true }) : Results.NotFound();
            }).RequireAdmin(AdminRoles.GameMaster);

            // --- Events (docs/29) -------------------------------------------------------------

            api.MapEventEndpoints();

            // --- Mail (docs/29 §2.1) ----------------------------------------------------------

            api.MapMailEndpoints();

            // --- First-session guide funnel (docs/34) ------------------------------------------

            api.MapOnboardingEndpoints();

            // --- Coupons --------------------------------------------------------------------

            api.MapGet("/coupons", async (AdminPrincipal _, IAdminStore store, CancellationToken ct) =>
                Results.Ok((await store.ListCouponsAsync(ct)).Select(CouponJson))).RequireAdmin(AdminRoles.Viewer);

            api.MapPost("/coupons", async (AdminPrincipal me, CouponRequest request, HttpContext http, IAdminStore store, IClock clock, CancellationToken ct) =>
            {
                var rewards = ParseAmounts(request.Rewards);
                if (rewards == null || rewards.Count == 0 || rewards.Values.Any(v => v <= 0))
                    return Results.BadRequest(new { error = "InvalidRewards" });

                var code = CouponService.Normalise(string.IsNullOrWhiteSpace(request.Code) ? CouponService.NewCode() : request.Code);
                if (code.Length is < 4 or > 24 || !code.All(char.IsLetterOrDigit))
                    return Results.BadRequest(new { error = "InvalidCode" });

                if (request.StartsAt.HasValue && request.ExpiresAt.HasValue && request.StartsAt.Value >= request.ExpiresAt.Value)
                    return Results.BadRequest(new { error = "InvalidWindow" });

                Guid? target = null;
                if (!string.IsNullOrWhiteSpace(request.TargetAccountId))
                {
                    if (!Guid.TryParse(request.TargetAccountId.Trim(), out var id) || await store.GetPlayerAsync(id, ct) == null)
                        return Results.BadRequest(new { error = "UnknownPlayer" });
                    target = id;
                }

                var coupon = new Coupon(code, rewards, Math.Max(0, request.MaxRedemptions), 0, request.ExpiresAt, true, me.Username, clock.UtcNow,
                    request.Note ?? string.Empty, request.StartsAt, target);
                if (!await store.CreateCouponAsync(coupon, ct))
                    return Results.Conflict(new { error = "CodeExists" });

                await store.AuditAsync(me.Username, "coupon.create", code,
                    $"{Describe(rewards)} max={coupon.MaxRedemptions} window={coupon.StartsAt:u}..{coupon.ExpiresAt:u} target={target?.ToString("N") ?? "-"}",
                    AdminAuth.ClientIp(http), ct);
                return Results.Ok(CouponJson(coupon));
            }).RequireAdmin(AdminRoles.GameMaster);

            api.MapPost("/coupons/{code}/active", async (AdminPrincipal me, string code, ActiveRequest request, HttpContext http, IAdminStore store, CancellationToken ct) =>
            {
                await store.SetCouponActiveAsync(CouponService.Normalise(code), request.Active, ct);
                await store.AuditAsync(me.Username, request.Active ? "coupon.enable" : "coupon.disable", code, string.Empty, AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.GameMaster);

            // --- Live ops -------------------------------------------------------------------

            api.MapGet("/liveops", async (AdminPrincipal _, LiveOpsCache liveOps, CancellationToken ct) =>
                Results.Ok(await liveOps.GetAsync(ct))).RequireAdmin(AdminRoles.Viewer);

            api.MapPut("/liveops", async (AdminPrincipal me, LiveOpsSettings settings, HttpContext http, LiveOpsCache liveOps, IAdminStore store, CancellationToken ct) =>
            {
                settings.MaintenanceMessage = Clip(settings.MaintenanceMessage, 500);
                settings.Announcement = Clip(settings.Announcement, 500);
                settings.LatestClientVersion = (settings.LatestClientVersion ?? string.Empty).Trim();
                settings.MinClientVersion = (settings.MinClientVersion ?? string.Empty).Trim();
                settings.UpdateUrl = (settings.UpdateUrl ?? string.Empty).Trim();
                if (!IsVersionOrEmpty(settings.LatestClientVersion) || !IsVersionOrEmpty(settings.MinClientVersion))
                    return Results.BadRequest(new { error = "InvalidVersion" });
                if (settings.UpdateUrl.Length > 0 && !(Uri.TryCreate(settings.UpdateUrl, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps))
                    return Results.BadRequest(new { error = "InvalidUrl" });
                await liveOps.SetAsync(settings, ct);
                await store.AuditAsync(me.Username, "liveops.update", string.Empty,
                    $"maintenance={settings.Maintenance} announcement={settings.Announcement} latest={settings.LatestClientVersion} min={settings.MinClientVersion}",
                    AdminAuth.ClientIp(http), ct);
                return Results.Ok(settings);
            }).RequireAdmin(AdminRoles.GameMaster);

            // --- Balance (catalog) ------------------------------------------------------------

            api.MapGet("/catalog", (AdminPrincipal _, LiveCatalog catalog) =>
                Results.Ok(new { version = catalog.Version, json = catalog.Json })).RequireAdmin(AdminRoles.Viewer);

            api.MapGet("/catalog/versions", async (AdminPrincipal _, IAdminStore store, CancellationToken ct) =>
                Results.Ok(await store.ListCatalogsAsync(50, ct))).RequireAdmin(AdminRoles.Viewer);

            api.MapPut("/catalog", async (AdminPrincipal me, CatalogRequest request, HttpContext http, IAdminStore store,
                GameWorld world, LiveCatalog live, CancellationToken ct) =>
                await PublishCatalogAsync(request.Json, request.Note, me, http, store, world, live, ct)).RequireAdmin(AdminRoles.Owner);

            api.MapPost("/catalog/{version:int}/restore", async (AdminPrincipal me, int version, HttpContext http,
                IAdminStore store, GameWorld world, LiveCatalog live, CancellationToken ct) =>
            {
                var old = await store.GetCatalogAsync(version, ct);
                return old == null
                    ? Results.NotFound()
                    : await PublishCatalogAsync(old.Json, $"restore of v{version}", me, http, store, world, live, ct);
            }).RequireAdmin(AdminRoles.Owner);

            // --- Audit and admins -------------------------------------------------------------

            api.MapGet("/audit", async (AdminPrincipal _, IAdminStore store, CancellationToken ct) =>
                Results.Ok(await store.ListAuditAsync(300, ct))).RequireAdmin(AdminRoles.Viewer);

            api.MapGet("/admins", async (AdminPrincipal _, IAdminStore store, CancellationToken ct) =>
                Results.Ok((await store.ListAdminsAsync(ct)).Select(a => new { a.Id, a.Username, a.Role, a.Disabled, a.CreatedAt, a.LastLoginAt }))).RequireAdmin(AdminRoles.Owner);

            api.MapPost("/admins", async (AdminPrincipal me, AdminCreateRequest request, HttpContext http, IAdminStore store, AdminAuth auth, CancellationToken ct) =>
            {
                var username = (request.Username ?? string.Empty).Trim();
                if (username.Length is < 3 or > 32 || !username.All(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-') || !AdminRoles.IsValid(request.Role))
                    return Results.BadRequest(new { error = "InvalidAdmin" });

                var (admin, password) = AdminAuth.NewAdmin(username, request.Role);
                if (!await store.CreateAdminAsync(admin, ct))
                    return Results.Conflict(new { error = "UsernameExists" });

                await store.AuditAsync(me.Username, "admin.create", username, request.Role, AdminAuth.ClientIp(http), ct);
                // Shown once; the TOTP secret matters only when Admin__RequireTotp is on.
                return Results.Ok(new { username, role = admin.Role, password, totpSecret = auth.RequireTotp ? admin.TotpSecret : null });
            }).RequireAdmin(AdminRoles.Owner);

            api.MapPost("/admins/{id:guid}/disabled", async (AdminPrincipal me, Guid id, DisabledRequest request, HttpContext http, IAdminStore store, CancellationToken ct) =>
            {
                if (id == me.Id)
                    return Results.BadRequest(new { error = "CannotDisableSelf" });
                await store.SetAdminDisabledAsync(id, request.Disabled, ct);
                await store.AuditAsync(me.Username, request.Disabled ? "admin.disable" : "admin.enable", id.ToString("N"), string.Empty, AdminAuth.ClientIp(http), ct);
                return Results.Ok(new { ok = true });
            }).RequireAdmin(AdminRoles.Owner);
        }

        private static async Task<IResult> PublishCatalogAsync(string json, string note, AdminPrincipal me, HttpContext http, IAdminStore store,
            GameWorld world, LiveCatalog live, CancellationToken ct)
        {
            HADAL.Shared.Catalog.GameCatalog catalog;
            try
            {
                catalog = CatalogLoader.Parse(json ?? string.Empty);
            }
            catch (FormatException exception)
            {
                return Results.BadRequest(new { error = "InvalidCatalog", detail = exception.Message });
            }

            var version = await store.AddCatalogAsync(json!, me.Username, Clip(note, 200), ct);
            world.ReplaceCatalog(catalog, version);
            live.Set(version, json!);
            await store.AuditAsync(me.Username, "catalog.publish", $"v{version}", Clip(note, 200), AdminAuth.ClientIp(http), ct);
            return Results.Ok(new { version });
        }

        /// <summary>Requires a signed-in admin with at least <paramref name="minimumRole"/>; the handler gets them as <see cref="AdminPrincipal"/>.</summary>
        internal static RouteHandlerBuilder RequireAdmin(this RouteHandlerBuilder builder, string minimumRole) =>
            builder.AddEndpointFilter(async (context, next) =>
            {
                var me = await AdminPrincipal.BindAsync(context.HttpContext) ?? AdminPrincipal.Anonymous;
                if (me.IsAnonymous)
                    return Results.Json(new { error = "Unauthorized" }, statusCode: StatusCodes.Status401Unauthorized);
                if (AdminRoles.Rank(me.Role) < AdminRoles.Rank(minimumRole))
                    return Results.Json(new { error = "Forbidden" }, statusCode: StatusCodes.Status403Forbidden);

                return await next(context);
            });

        private static object PlayerJson(PlayerSummary p, GameWorld world) => new
        {
            accountId = p.AccountId.ToString("N"),
            p.DisplayName,
            p.CreatedAt,
            p.LastLoginAt,
            p.BannedUntil,
            p.BanReason,
            p.ClientVersion,
            providers = p.Providers,
            online = world.IsOnline(p.AccountId.ToString("N"))
        };

        private static object CouponJson(Coupon c) => new
        {
            c.Code,
            rewards = c.Rewards.ToDictionary(p => p.Key.ToString(), p => p.Value),
            c.MaxRedemptions,
            c.Redeemed,
            c.ExpiresAt,
            c.Active,
            c.CreatedBy,
            c.CreatedAt,
            c.Note,
            c.StartsAt,
            targetAccountId = c.TargetAccountId?.ToString("N")
        };

        private const long MaxGrant = 1_000_000_000_000;

        internal static Dictionary<ResourceType, long>? ParseAmounts(Dictionary<string, long>? raw)
        {
            if (raw == null)
                return null;

            var amounts = new Dictionary<ResourceType, long>();
            foreach (var pair in raw)
            {
                if (!Enum.TryParse<ResourceType>(pair.Key, true, out var type) || type == ResourceType.Unknown || pair.Value == 0)
                    continue;
                // Very large amounts are trimmed to the limit instead of failing the whole grant.
                amounts[type] = Math.Clamp(pair.Value, -MaxGrant, MaxGrant);
            }

            return amounts;
        }

        private static string Describe(IReadOnlyDictionary<ResourceType, long> amounts) =>
            string.Join(", ", amounts.Select(a => $"{a.Key}:{a.Value:+#;-#;0}"));

        private static bool IsVersionOrEmpty(string text) => text.Length == 0 || (text.Length <= 32 && Version.TryParse(text, out _));

        private static string Clip(string? text, int max) =>
            string.IsNullOrEmpty(text) ? string.Empty : text.Length <= max ? text : text[..max];
    }
}
