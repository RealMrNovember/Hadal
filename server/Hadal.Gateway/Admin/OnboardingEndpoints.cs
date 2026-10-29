using Hadal.Game;
using Hadal.Game.Onboarding;

namespace Hadal.Gateway.Admin
{
    /// <summary>
    /// Overseer view of the first-session guide (docs/34 §5): how many accounts reached each step, the median time to
    /// it from the guide's start and how many got there inside the first ten minutes.
    /// </summary>
    public static class OnboardingEndpoints
    {
        public static void MapOnboardingEndpoints(this RouteGroupBuilder api)
        {
            api.MapGet("/onboarding/funnel", async (AdminPrincipal _, int? days, string? cohort, GameWorld world, IClock clock, CancellationToken ct) =>
            {
                var span = Math.Clamp(days ?? 30, 1, 365);
                var group = cohort is "new" or "existing" ? cohort : cohort == "all" ? string.Empty : "new";
                var now = clock.UtcNow;
                var events = await world.Onboarding.Store.LoadAsync(now.AddDays(-span), group, ct);
                var report = OnboardingFunnel.Compute(events, OnboardingFunnel.StepIds(world.Catalog.Catalog), now);
                return Results.Ok(new
                {
                    days = span,
                    cohort = group.Length == 0 ? "all" : group,
                    report.Started,
                    report.Skipped,
                    report.Finished,
                    report.StalledIn10Minutes,
                    report.OldEnough,
                    steps = report.Steps.Select(s => new { s.Step, s.StepId, s.Players, s.Percent, s.MedianSeconds, s.Within10Minutes })
                });
            }).RequireAdmin(AdminRoles.Viewer);
        }
    }
}
