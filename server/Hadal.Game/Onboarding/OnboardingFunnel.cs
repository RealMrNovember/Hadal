using HADAL.Shared.Catalog;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.Onboarding
{
    /// <summary>One row of the guide funnel: how many players got this far and how long it took them.</summary>
    public sealed record FunnelStep(int Step, string StepId, int Players, double Percent, int MedianSeconds, int Within10Minutes);

    /// <summary>The guide funnel for the Overseer (docs/34 §5).</summary>
    /// <param name="StalledIn10Minutes">Started at least a day ago (<paramref name="OldEnough"/>), claimed nothing after their first ten minutes and did not finish.</param>
    public sealed record FunnelReport(
        int Started,
        int Skipped,
        int Finished,
        int StalledIn10Minutes,
        int OldEnough,
        IReadOnlyList<FunnelStep> Steps);

    public static class OnboardingFunnel
    {
        public const int TenMinutes = 600;

        /// <summary>Builds the funnel from the raw events; <paramref name="stepIds"/> are the catalog's guide steps in order.</summary>
        public static FunnelReport Compute(IReadOnlyCollection<OnboardingEvent> events, IReadOnlyList<string> stepIds, DateTimeOffset now)
        {
            var starts = events.Where(e => e.Step == 0).GroupBy(e => e.PlayerId).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            var byPlayer = events.Where(e => starts.ContainsKey(e.PlayerId) && e.Step > 0)
                .GroupBy(e => e.PlayerId).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            var skipped = events.Where(e => e.Step < 0).Select(e => e.PlayerId).Distinct().Count();
            var total = stepIds.Count;

            var steps = new List<FunnelStep>();
            for (var step = 1; step <= total; step++)
            {
                var times = byPlayer.Values.SelectMany(list => list.Where(e => e.Step == step)).Select(e => e.Seconds).OrderBy(s => s).ToList();
                steps.Add(new FunnelStep(step, stepIds[step - 1], times.Count,
                    starts.Count == 0 ? 0 : Math.Round(100.0 * times.Count / starts.Count, 1),
                    Median(times), times.Count(s => s <= TenMinutes)));
            }

            var finished = byPlayer.Values.Count(list => list.Any(e => e.Step == total));
            var oldEnough = starts.Values.Where(s => now - s.At >= TimeSpan.FromDays(1)).Select(s => s.PlayerId).ToList();
            var stalled = oldEnough.Count(id =>
            {
                var list = byPlayer.TryGetValue(id, out var l) ? l : new List<OnboardingEvent>();
                return !list.Any(e => e.Step == total) && list.All(e => e.Seconds <= TenMinutes);
            });
            return new FunnelReport(starts.Count, skipped, finished, stalled, oldEnough.Count, steps);
        }

        private static int Median(List<int> sorted)
        {
            if (sorted.Count == 0)
                return 0;
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
        }

        public static IReadOnlyList<string> StepIds(GameCatalog catalog) =>
            (catalog.Quests.onboarding ?? Array.Empty<QuestData>()).Select(q => q.id).ToList();
    }

    /// <summary>Writes guide events without holding up the game: a failed write is logged, never retried into the session.</summary>
    public sealed class OnboardingLog
    {
        private readonly ILogger _logger;

        public OnboardingLog(IOnboardingStore store, ILogger logger)
        {
            Store = store;
            _logger = logger;
        }

        public IOnboardingStore Store { get; }

        public Task InitializeAsync(CancellationToken cancellationToken) => Store.InitializeAsync(cancellationToken);

        public void Record(OnboardingEvent entry)
        {
            Task write;
            try
            {
                write = Store.RecordAsync(entry, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Onboarding event {Step} of {PlayerId} not written.", entry.Step, entry.PlayerId);
                return;
            }

            if (!write.IsCompleted)
                _ = write.ContinueWith(t => _logger.LogWarning(t.Exception, "Onboarding event {Step} of {PlayerId} not written.", entry.Step, entry.PlayerId),
                    TaskContinuationOptions.OnlyOnFaulted);
            else if (write.IsFaulted)
                _logger.LogWarning(write.Exception, "Onboarding event {Step} of {PlayerId} not written.", entry.Step, entry.PlayerId);
        }
    }
}
