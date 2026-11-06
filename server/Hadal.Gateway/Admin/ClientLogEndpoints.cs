using Hadal.Game;
using Hadal.Game.Telemetry;

namespace Hadal.Gateway.Admin
{
    /// <summary>Overseer view of client crash reports (docs/22): newest first, filterable.</summary>
    public static class ClientLogEndpoints
    {
        public static void MapClientLogEndpoints(this RouteGroupBuilder api)
        {
            api.MapGet("/logs/client", async (AdminPrincipal _, int? days, string? level, string? q,
                IClientLogStore store, IClock clock, CancellationToken ct) =>
            {
                var span = Math.Clamp(days ?? 7, 1, 90);
                var rows = await store.LoadAsync(clock.UtcNow.AddDays(-span), level, q, 200, ct);
                return Results.Ok(rows.Select(r => new
                {
                    r.PlayerId,
                    r.Level,
                    r.Message,
                    r.Stack,
                    at = r.At.ToString("O"),
                    r.ClientVersion,
                    r.Platform,
                    r.Device
                }));
            }).RequireAdmin(AdminRoles.Viewer);
        }
    }
}
