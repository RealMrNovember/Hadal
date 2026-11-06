namespace Hadal.Game.Economy
{
    /// <summary>One source/resource bucket of the economy window; <see cref="In"/> what was produced/granted, <see cref="Out"/> what was taken.</summary>
    public sealed record EconomyFlowRow(string Source, string Resource, long In, long Out, long Net, int Events, int Players);

    /// <summary>One resource per UTC day, for the Overseer trend chart.</summary>
    public sealed record EconomyFlowDay(string Day, string Resource, long In, long Out, long Net);

    public sealed record EconomyFlowReport(IReadOnlyList<EconomyFlowRow> Rows, IReadOnlyList<EconomyFlowDay> Days);

    public static class EconomyFlowReportBuilder
    {
        public static EconomyFlowReport Compute(IReadOnlyCollection<EconomyFlowEvent> events)
        {
            var rows = new List<EconomyFlowRow>();
            foreach (var group in events.GroupBy(e => (e.Source, e.Resource)))
            {
                rows.Add(new EconomyFlowRow(group.Key.Source, group.Key.Resource,
                    SumPositive(group), -SumNegative(group), Sum(group),
                    group.Count(), group.Select(e => e.PlayerId).Distinct(StringComparer.Ordinal).Count()));
            }

            var days = new List<EconomyFlowDay>();
            foreach (var group in events.GroupBy(e => (Day: e.At.UtcDateTime.ToString("yyyy-MM-dd"), e.Resource)))
            {
                days.Add(new EconomyFlowDay(group.Key.Day, group.Key.Resource,
                    SumPositive(group), -SumNegative(group), Sum(group)));
            }

            return new EconomyFlowReport(
                rows.OrderBy(r => r.Source, StringComparer.Ordinal).ThenBy(r => r.Resource, StringComparer.Ordinal).ToList(),
                days.OrderBy(d => d.Day, StringComparer.Ordinal).ThenBy(d => d.Resource, StringComparer.Ordinal).ToList());
        }

        private static long SumPositive(IEnumerable<EconomyFlowEvent> events) => AddWhere(events, e => e.Amount > 0);

        private static long SumNegative(IEnumerable<EconomyFlowEvent> events) => AddWhere(events, e => e.Amount < 0);

        private static long Sum(IEnumerable<EconomyFlowEvent> events) => AddWhere(events, _ => true);

        private static long AddWhere(IEnumerable<EconomyFlowEvent> events, Func<EconomyFlowEvent, bool> match)
        {
            long total = 0;
            foreach (var amount in events.Where(match).Select(e => e.Amount))
                total = SaturateAdd(total, amount);
            return total;
        }

        private static long SaturateAdd(long a, long b)
        {
            if (b > 0 && a > long.MaxValue - b)
                return long.MaxValue;
            if (b < 0 && a < long.MinValue - b)
                return long.MinValue;
            return a + b;
        }
    }
}
