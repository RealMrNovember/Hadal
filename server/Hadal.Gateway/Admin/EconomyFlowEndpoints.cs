using Hadal.Game;
using Hadal.Game.Economy;

namespace Hadal.Gateway.Admin
{
    /// <summary>
    /// Overseer view of the server wallet ledger (docs/30): one signed movement per row, split by source and resource,
    /// with a per-day trend for charts. The Overseer uses this to spot faucets out-drawing sinks.
    /// </summary>
    public static class EconomyFlowEndpoints
    {
        public static void MapEconomyFlowEndpoints(this RouteGroupBuilder api)
        {
            api.MapGet("/economy/flow", async (AdminPrincipal _, int? days, GameWorld world, IClock clock, CancellationToken ct) =>
            {
                var span = Math.Clamp(days ?? 30, 1, 365);
                var now = clock.UtcNow;
                var events = await world.Economy.Store.LoadAsync(now.AddDays(-span), ct);
                var report = EconomyFlowReportBuilder.Compute(events);
                return Results.Ok(new
                {
                    days = span,
                    rows = report.Rows.Select(r => new { r.Source, r.Resource, r.In, r.Out, r.Net, r.Events, r.Players }),
                    daysTrend = report.Days.Select(d => new { d.Day, d.Resource, d.In, d.Out, d.Net })
                });
            }).RequireAdmin(AdminRoles.Viewer);
        }
    }
}
