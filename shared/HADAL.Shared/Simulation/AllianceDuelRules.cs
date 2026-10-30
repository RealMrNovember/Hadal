using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Alliance Duel (İttifak Düellosu, docs/29 §4.3), shared by the server (pairing, day and week results) and the client
    /// (the duel page).
    /// <para>
    /// Once a week the alliances of a kingdom are paired 1-v-1 by alliance power: strongest with the next strongest, and
    /// so on down the list. With an odd number the weakest alliance meets the Abyss Echo (<see cref="Echo"/>), a stand-in
    /// whose score on each day is the average of every real alliance's score in that kingdom on that day — the odd one out
    /// still has a fair race instead of a free win. The duel runs six themed days (the event's six phases):
    /// </para>
    /// <code>
    ///   day   theme                win worth
    ///   1     Gathering            1
    ///   2     Base growth          2
    ///   3     Research             2
    ///   4     Heroes               2
    ///   5     Total mobilisation   2
    ///   6     Hunt                 4
    /// </code>
    /// The alliance with more points on a day wins it (a tie, or no points at all, wins nothing). The first to
    /// <see cref="WinTarget"/> wins the week (13 points exist, so at most one side gets there); when neither does, the
    /// side with more week points wins, then the side with more duel points; otherwise the week is a draw. Members also
    /// have personal daily chests: the event's stages are thresholds of the player's own points on each day.
    /// </summary>
    public static class AllianceDuelRules
    {
        public const int Days = 6;
        public const int WinTarget = 7;
        public const int DefaultMinMembers = 3;

        /// <summary>The id of the Abyss Echo, the odd alliance's opponent.</summary>
        public const string Echo = "~echo";

        /// <summary>The day themes (localisation keys "duel.theme.{id}").</summary>
        public static readonly string[] Themes = { "gathering", "growth", "research", "heroes", "mobilisation", "hunt" };

        private static readonly int[] Worth = { 1, 2, 2, 2, 2, 4 };

        /// <summary>Week points for winning day <paramref name="day"/> (0-based).</summary>
        public static int DayWorth(int day) => day >= 0 && day < Days ? Worth[day] : 0;

        /// <summary>DuelDto.DayWinners values.</summary>
        public const int Pending = 0;
        public const int First = 1;
        public const int Second = 2;
        public const int Tie = 3;

        /// <summary>DuelDto.Result values.</summary>
        public const int Running = 0;
        public const int Won = 1;
        public const int Lost = 2;
        public const int Draw = 3;

        /// <summary>The winner of a finished day: <see cref="First"/>, <see cref="Second"/> or <see cref="Tie"/>.</summary>
        public static int DayWinner(long first, long second) => first > second ? First : second > first ? Second : Tie;

        /// <summary>Week points of both sides over the finished days (<paramref name="daysDone"/> of them).</summary>
        public static void WeekPoints(long[] first, long[] second, int daysDone, out int firstWins, out int secondWins)
        {
            firstWins = secondWins = 0;
            for (var d = 0; d < Math.Min(daysDone, Days); d++)
            {
                var winner = DayWinner(At(first, d), At(second, d));
                if (winner == First)
                    firstWins += DayWorth(d);
                else if (winner == Second)
                    secondWins += DayWorth(d);
            }
        }

        /// <summary>The week's result for the first side once all days are done: Won, Lost or Draw.</summary>
        public static int WeekResult(long[] first, long[] second)
        {
            WeekPoints(first, second, Days, out var a, out var b);
            if (a >= WinTarget || b >= WinTarget)
                return a >= WinTarget ? Won : Lost;
            if (a != b)
                return a > b ? Won : Lost;
            long sumA = 0, sumB = 0;
            for (var d = 0; d < Days; d++)
            {
                sumA += At(first, d);
                sumB += At(second, d);
            }

            return sumA > sumB ? Won : sumB > sumA ? Lost : Draw;
        }

        /// <summary>
        /// Pairs alliances by power: strongest first (ties by id), each with the next; an odd last one meets the
        /// <see cref="Echo"/>. Returns (first, second) pairs, the stronger first.
        /// </summary>
        public static List<KeyValuePair<string, string>> Pair(IEnumerable<KeyValuePair<string, long>> alliances)
        {
            var ordered = alliances.Where(a => !string.IsNullOrEmpty(a.Key)).GroupBy(a => a.Key, StringComparer.Ordinal).Select(g => g.First())
                .OrderByDescending(a => a.Value).ThenBy(a => a.Key, StringComparer.Ordinal).Select(a => a.Key).ToList();
            var pairs = new List<KeyValuePair<string, string>>();
            for (var i = 0; i < ordered.Count; i += 2)
                pairs.Add(new KeyValuePair<string, string>(ordered[i], i + 1 < ordered.Count ? ordered[i + 1] : Echo));
            return pairs;
        }

        /// <summary>The Abyss Echo's score for a day: the average of the real alliances' scores (rounded down), 0 without any.</summary>
        public static long EchoScore(IEnumerable<long> realScores)
        {
            long sum = 0;
            var n = 0;
            foreach (var s in realScores)
            {
                sum += Math.Max(0, s);
                n++;
            }

            return n == 0 ? 0 : sum / n;
        }

        /// <summary>The day (0-based) a moment falls in, by the event's phases; -1 outside them.</summary>
        public static int DayAt(EventDto e, long nowMs) => Math.Min(EventRules.CurrentPhase(e, nowMs), Days - 1);

        /// <summary>How many days are over at <paramref name="nowMs"/> (all of them once the event ended).</summary>
        public static int DaysDone(EventDto e, long nowMs)
        {
            if (nowMs >= e.EndsAtMs)
                return Days;
            var done = 0;
            for (var i = 0; i < e.Phases.Length && i < Days; i++)
            {
                if (e.Phases[i].EndMs <= nowMs)
                    done++;
            }

            return done;
        }

        /// <summary>The chest key of day <paramref name="day"/> (0-based) and stage <paramref name="stage"/> (DuelDto.ClaimedChests).</summary>
        public static int ChestKey(int day, int stage) => day * 100 + stage;

        /// <summary>Daily chests reached and not yet claimed, over every day of the week so far.</summary>
        public static int ChestsWaiting(DuelDto duel, EventTierDto[] stages)
        {
            if (duel == null || !duel.InDuel || stages == null)
                return 0;
            var claimed = new HashSet<int>(duel.ClaimedChests ?? Array.Empty<int>());
            var n = 0;
            var days = duel.MyDayPoints ?? Array.Empty<long>();
            for (var d = 0; d < days.Length && d < Days; d++)
            {
                for (var s = 0; s < stages.Length; s++)
                {
                    if (days[d] >= stages[s].Points && !claimed.Contains(ChestKey(d, s)))
                        n++;
                }
            }

            return n;
        }

        private static long At(long[] values, int i) => values != null && i >= 0 && i < values.Length ? values[i] : 0;
    }
}
