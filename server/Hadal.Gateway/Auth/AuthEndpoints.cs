using System.Threading.RateLimiting;
using HADAL.Shared.Auth;
using HADAL.Shared.Serialization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Hadal.Gateway.Auth
{
    public static class AuthEndpoints
    {
        private const string GuestPolicy = "auth-guest";
        private const string RefreshPolicy = "auth-refresh";

        public static IServiceCollection AddHadalAuth(this IServiceCollection services, IConfiguration configuration)
        {
            // Shared auth contracts use public fields (Unity's JsonUtility cannot read properties).
            services.ConfigureHttpJsonOptions(json => json.SerializerOptions.IncludeFields = true);

            services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.Section));
            services.Configure<VersionOptions>(configuration.GetSection(VersionOptions.Section));
            services.AddSingleton<TokenService>();
            services.AddHostedService<AccountStoreInitializer>();

            var guestPerHour = configuration.GetSection(AuthOptions.Section).GetValue(nameof(AuthOptions.GuestLoginsPerHour), 20);
            services.AddRateLimiter(limiter =>
            {
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Guest creation is the cheapest thing to abuse: bound it per client IP.
                limiter.AddPolicy(GuestPolicy, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = guestPerHour, Window = TimeSpan.FromHours(1) }));

                limiter.AddPolicy(RefreshPolicy, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
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

            app.MapPost(AuthRoutes.Refresh, async (RefreshRequest? request, TokenService tokens, CancellationToken ct) =>
            {
                var response = await tokens.RefreshAsync(request?.refreshToken ?? string.Empty, ct);
                return response == null ? Results.Unauthorized() : Results.Ok(response);
            }).RequireRateLimiting(RefreshPolicy);

            app.MapGet(AuthRoutes.Manifest, (IOptions<VersionOptions> version) => Results.Ok(new VersionManifest
            {
                minClientVersion = version.Value.MinClientVersion,
                latestClientVersion = version.Value.LatestClientVersion,
                schemaVersion = SchemaVersion.Current,
                maintenance = version.Value.Maintenance,
                message = version.Value.Message
            }));
        }

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
