using System.Threading.RateLimiting;
using Hadal.Game;
using Hadal.Gateway.Admin;
using HADAL.Shared.Auth;
using HADAL.Shared.Serialization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hadal.Gateway.Auth
{
    public static class AuthEndpoints
    {
        private const string GuestPolicy = "auth-guest";
        private const string RefreshPolicy = "auth-refresh";
        private const string TransferPolicy = "auth-transfer";
        private const string AccountPolicy = "account";
        private const string TelemetryPolicy = "telemetry";

        public static IServiceCollection AddHadalAuth(this IServiceCollection services, IConfiguration configuration)
        {
            // Shared auth contracts use public fields (Unity's JsonUtility cannot read properties).
            services.ConfigureHttpJsonOptions(json => json.SerializerOptions.IncludeFields = true);

            services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.Section));
            services.Configure<VersionOptions>(configuration.GetSection(VersionOptions.Section));
            services.AddSingleton<TokenService>();
            services.TryAddSingleton<IIdentityTokenValidator, OidcIdentityTokenValidator>();
            services.AddHostedService<AccountStoreInitializer>();

            var guestPerHour = configuration.GetSection(AuthOptions.Section).GetValue(nameof(AuthOptions.GuestLoginsPerHour), 20);
            var transferPerHour = configuration.GetSection(AuthOptions.Section).GetValue(nameof(AuthOptions.TransferLoginsPerHour), 10);
            services.AddRateLimiter(limiter =>
            {
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Guest creation is the cheapest thing to abuse: bound it per client IP.
                limiter.AddPolicy(GuestPolicy, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = guestPerHour, Window = TimeSpan.FromHours(1) }));

                limiter.AddPolicy(RefreshPolicy, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));

                // Code + password sign-in is a guessing target: few attempts per IP.
                limiter.AddPolicy(TransferPolicy, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = transferPerHour, Window = TimeSpan.FromHours(1) }));

                limiter.AddPolicy(AccountPolicy, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));

                limiter.AddPolicy(TelemetryPolicy, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
            });

            return services;
        }

        public static void MapHadalAuth(this WebApplication app)
        {
            app.UseRateLimiter();

            app.MapPost(AuthRoutes.Guest, async (GuestLoginRequest? request, IAccountStore accounts, TokenService tokens, CancellationToken ct) =>
            {
                var account = await accounts.CreateGuestAsync(request?.deviceId ?? string.Empty, request?.clientVersion ?? string.Empty, ct);
                return Results.Ok(await tokens.IssueAsync(account, ct));
            }).RequireRateLimiting(GuestPolicy);

            app.MapPost(AuthRoutes.Refresh, async (RefreshRequest? request, TokenService tokens, IAdminStore admin, IClock clock, CancellationToken ct) =>
            {
                var response = await tokens.RefreshAsync(request?.refreshToken ?? string.Empty, ct);
                if (response == null)
                    return Results.Unauthorized();
                return await IsBannedAsync(response.accountId, admin, clock, ct) ? Error(StatusCodes.Status403Forbidden, AccountErrors.Banned) : Results.Ok(response);
            }).RequireRateLimiting(RefreshPolicy);

            MapAccount(app);

            app.MapPost(AuthRoutes.CouponRedeem, async (HttpContext http, CouponRedeemRequest? request, TokenService tokens, CouponService coupons, CancellationToken ct) =>
            {
                var account = await AuthenticateAsync(http, tokens);
                if (account == null)
                    return Results.Unauthorized();

                var (error, coupon) = await coupons.RedeemAsync(request?.code ?? string.Empty, account.Value, ct);
                return coupon == null ? Error(StatusCodes.Status400BadRequest, error) : Results.Ok(CouponService.ToResponse(coupon));
            }).RequireRateLimiting(AccountPolicy);

            // Live catalog for clients: the balance the server enforces right now (Overseer edits included).
            app.MapGet(AuthRoutes.Catalog, (HttpContext http, LiveCatalog catalog) =>
            {
                http.Response.Headers["X-Catalog-Version"] = catalog.Version.ToString();
                return Results.Text(catalog.Json, "application/json");
            });

            app.MapGet(AuthRoutes.Manifest, async (IOptions<VersionOptions> version, LiveOpsCache liveOps, LiveCatalog catalog, CancellationToken ct) =>
            {
                var ops = await liveOps.GetAsync(ct);
                return Results.Ok(new VersionManifest
                {
                    minClientVersion = ops.MinClientVersion.Length > 0 ? ops.MinClientVersion : version.Value.MinClientVersion,
                    latestClientVersion = ops.LatestClientVersion.Length > 0 ? ops.LatestClientVersion : version.Value.LatestClientVersion,
                    updateUrl = ops.UpdateUrl.Length > 0 ? ops.UpdateUrl : version.Value.UpdateUrl,
                    schemaVersion = SchemaVersion.Current,
                    maintenance = version.Value.Maintenance || ops.Maintenance,
                    message = ops.Maintenance && ops.MaintenanceMessage.Length > 0 ? ops.MaintenanceMessage : version.Value.Message,
                    announcement = ops.Announcement,
                    catalogVersion = catalog.Version
                });
            });
        }

        private static void MapAccount(WebApplication app)
        {
            app.MapGet(AuthRoutes.Profile, async (HttpContext http, TokenService tokens, IAccountStore accounts, CancellationToken ct) =>
            {
                var account = await AuthenticateAsync(http, tokens);
                if (account == null)
                    return Results.Unauthorized();

                var profile = await accounts.GetProfileAsync(account.Value, ct);
                return profile == null ? Results.Unauthorized() : Results.Ok(ToResponse(profile));
            }).RequireRateLimiting(AccountPolicy);

            // A name change follows RenameRules: unique, a day apart, the first free and then a Rename Card or Hadalite.
            app.MapPut(AuthRoutes.Profile, async (HttpContext http, UpdateProfileRequest? request, TokenService tokens, IAccountStore accounts,
                Hadal.Game.GameWorld world, CancellationToken ct) =>
            {
                var account = await AuthenticateAsync(http, tokens);
                if (account == null)
                    return Results.Unauthorized();

                var name = request?.displayName?.Trim() ?? string.Empty;
                if (!AccountRules.IsValidName(name))
                    return Error(StatusCodes.Status400BadRequest, AccountErrors.NameInvalid);
                if (NameFilter.IsProfane(name))
                    return Error(StatusCodes.Status400BadRequest, AccountErrors.NameProfane);
                var current = await accounts.GetProfileAsync(account.Value, ct);
                if (current == null)
                    return Results.Unauthorized();
                if (string.Equals(current.DisplayName, name, StringComparison.Ordinal))
                    return Results.Ok(ToResponse(current));
                if (await accounts.IsNameTakenAsync(name, account.Value, ct))
                    return Error(StatusCodes.Status409Conflict, AccountErrors.NameTaken);

                var unpaid = await world.RenameAsync(account.Value.ToString("N"), name, ct);
                if (unpaid == AccountErrors.RenameCooldown || unpaid == AccountErrors.NeedRenameCard)
                    return Error(StatusCodes.Status409Conflict, unpaid);

                await accounts.SetDisplayNameAsync(account.Value, name, ct);
                var profile = await accounts.GetProfileAsync(account.Value, ct);
                return profile == null ? Results.Unauthorized() : Results.Ok(ToResponse(profile));
            }).RequireRateLimiting(AccountPolicy);

            app.MapPost(AuthRoutes.TransferCreate, async (HttpContext http, TransferCreateRequest? request, TokenService tokens, IAccountStore accounts, CancellationToken ct) =>
            {
                var account = await AuthenticateAsync(http, tokens);
                if (account == null)
                    return Results.Unauthorized();

                if (!AccountRules.IsValidPassword(request?.password))
                    return Error(StatusCodes.Status400BadRequest, AccountErrors.PasswordInvalid);

                // A 59-bit random code practically never collides; retry the rare case instead of failing.
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    var code = TransferCodes.New();
                    if (await accounts.LinkAsync(account.Value, AccountProviders.Transfer, code, PasswordHasher.Hash(request!.password), ct))
                        return Results.Ok(new TransferCodeResponse { transferCode = TransferCodes.Format(code) });
                }

                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }).RequireRateLimiting(AccountPolicy);

            app.MapPost(AuthRoutes.TransferLogin, async (TransferLoginRequest? request, IAccountStore accounts, TokenService tokens,
                IAdminStore admin, IClock clock, CancellationToken ct) =>
            {
                var code = AccountRules.NormaliseTransferCode(request?.transferCode);
                var link = code.Length is > 0 and <= 32 ? await accounts.FindLinkAsync(AccountProviders.Transfer, code, ct) : null;

                // Same answer for unknown code and wrong password: no account enumeration.
                if (link == null || !PasswordHasher.Verify(request?.password ?? string.Empty, link.Value.SecretHash))
                    return Error(StatusCodes.Status401Unauthorized, AccountErrors.InvalidCredentials);

                if (await IsBannedAsync(link.Value.AccountId.ToString("N"), admin, clock, ct))
                    return Error(StatusCodes.Status403Forbidden, AccountErrors.Banned);

                return Results.Ok(await tokens.IssueAsync(link.Value.AccountId, ct));
            }).RequireRateLimiting(TransferPolicy);

            app.MapPost(AuthRoutes.ProviderLink, async (HttpContext http, ProviderRequest? request, TokenService tokens, IAccountStore accounts,
                IIdentityTokenValidator identities, CancellationToken ct) =>
            {
                var account = await AuthenticateAsync(http, tokens);
                if (account == null)
                    return Results.Unauthorized();

                var identity = await identities.ValidateAsync(request?.provider ?? string.Empty, request?.idToken ?? string.Empty, ct);
                if (identity.Status != IdentityStatus.Valid)
                    return IdentityError(identity.Status);

                if (!await accounts.LinkAsync(account.Value, request!.provider, identity.Subject, string.Empty, ct))
                    return Error(StatusCodes.Status409Conflict, AccountErrors.ProviderLinkedElsewhere);

                var profile = await accounts.GetProfileAsync(account.Value, ct);
                return profile == null ? Results.Unauthorized() : Results.Ok(ToResponse(profile));
            }).RequireRateLimiting(AccountPolicy);

            app.MapPost(AuthRoutes.ProviderLogin, async (ProviderRequest? request, IAccountStore accounts, TokenService tokens,
                IIdentityTokenValidator identities, IAdminStore admin, IClock clock, CancellationToken ct) =>
            {
                var identity = await identities.ValidateAsync(request?.provider ?? string.Empty, request?.idToken ?? string.Empty, ct);
                if (identity.Status != IdentityStatus.Valid)
                    return IdentityError(identity.Status);

                // Signing in never creates an account: an identity is linked from inside the game first.
                var link = await accounts.FindLinkAsync(request!.provider, identity.Subject, ct);
                if (link == null)
                    return Error(StatusCodes.Status404NotFound, AccountErrors.ProviderNotLinked);
                if (await IsBannedAsync(link.Value.AccountId.ToString("N"), admin, clock, ct))
                    return Error(StatusCodes.Status403Forbidden, AccountErrors.Banned);
                return Results.Ok(await tokens.IssueAsync(link.Value.AccountId, ct));
            }).RequireRateLimiting(TransferPolicy);
        }

        private static async Task<bool> IsBannedAsync(string accountId, IAdminStore admin, IClock clock, CancellationToken ct) =>
            Guid.TryParse(accountId, out var id) && await admin.GetActiveBanAsync(id, clock.UtcNow, ct) != null;

        internal static async Task<Guid?> AuthenticateAsync(HttpContext http, TokenService tokens)
        {
            var header = http.Request.Headers.Authorization.ToString();
            const string prefix = "Bearer ";
            if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            var validation = await tokens.ValidateAccessTokenAsync(header[prefix.Length..].Trim());
            return validation.IsValid ? validation.AccountId : null;
        }

        private static ProfileResponse ToResponse(AccountProfile profile) => new()
        {
            accountId = profile.AccountId.ToString("N"),
            displayName = profile.DisplayName,
            createdAt = profile.CreatedAt.UtcDateTime.ToString("O"),
            linkedProviders = profile.Providers.ToArray()
        };

        private static IResult IdentityError(IdentityStatus status) => status switch
        {
            IdentityStatus.UnknownProvider => Error(StatusCodes.Status400BadRequest, AccountErrors.ProviderUnknown),
            IdentityStatus.NotConfigured => Error(StatusCodes.Status501NotImplemented, AccountErrors.ProviderNotConfigured),
            _ => Error(StatusCodes.Status401Unauthorized, AccountErrors.ProviderTokenInvalid)
        };

        private static IResult Error(int status, string error) => Results.Json(new AccountError { error = error }, statusCode: status);

        // Behind Cloudflare Tunnel every request comes from cloudflared on localhost; the real
        // client is in CF-Connecting-IP. The gateway port is bound to 127.0.0.1, so the header
        // cannot be injected by anyone who bypasses Cloudflare.
        private static string ClientIp(HttpContext context) =>
            context.Request.Headers.TryGetValue("CF-Connecting-IP", out var ip) && !string.IsNullOrEmpty(ip)
                ? ip.ToString()
                : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public sealed class AccountStoreInitializer : IHostedService
    {
        private readonly IAccountStore _accounts;

        public AccountStoreInitializer(IAccountStore accounts) => _accounts = accounts;

        public Task StartAsync(CancellationToken cancellationToken) => _accounts.InitializeAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
