using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.DTOs;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Alliance polls and event time-slot votes (docs/29 §5), shared by server and client.
    /// <list type="bullet">
    /// <item>A <b>poll</b> (diplomacy officers, the leader): a question, 2–6 answers, 1–72 h, single or multiple choice,
    /// anonymous or not.</item>
    /// <item>A <b>time-slot vote</b> (war officers, the leader): 2–5 starts in server time on the quarter hour, each after
    /// the vote ends and at most 14 days ahead; 24 h by default (1–72 h); the winner is fixed for the event it names.</item>
    /// </list>
    /// Every member has one ballot and may change it while the vote is open (the latest counts). Results are live. A vote
    /// closes when its time is up or early by hand; the summary goes to alliance chat. Ties go to the earlier option
    /// (for time slots the earlier time); a vote nobody answered has no winner.
    /// </summary>
    public static class AlliancePollRules
    {
        public const string KindPoll = "poll";
        public const string KindSlots = "slots";

        public const int MinOptions = 2;
        public const int MaxOptions = 6;
        public const int MinSlots = 2;
        public const int MaxSlots = 5;
        public const int MinHours = 1;
        public const int MaxHours = 72;
        public const int PollHours = 24;
        public const int SlotVoteHours = 24;
        public const int MaxQuestion = 120;
        public const int MaxOption = 60;

        /// <summary>Open votes an alliance may run at once (both kinds together).</summary>
        public const int MaxOpen = 3;

        /// <summary>Closed votes an alliance keeps to look back on.</summary>
        public const int History = 5;

        public const int SlotStepMinutes = 15;
        public const int MaxSlotDaysAhead = 14;

        public const string ClosedByTime = "time";
        public const string ClosedByOfficer = "officer";
        public const string ClosedByOverseer = "overseer";

        /// <summary>
        /// What a time-slot vote schedules. "meeting" is a plain "when do we meet"; the event ones are read by the event
        /// steps that come later (Lure Trap, Trench Conquest) through the stored result.
        /// </summary>
        public static readonly string[] Purposes = { "meeting", "lure_trap", "trench_conquest", "alliance_event" };

        public static bool IsKind(string? kind) => kind == KindPoll || kind == KindSlots;

        /// <summary>The voting time asked for, or the kind's default when 0.</summary>
        public static int HoursOrDefault(string kind, int hours) => hours > 0 ? hours : kind == KindSlots ? SlotVoteHours : PollHours;

        /// <summary>Trimmed, one line, cut to <paramref name="max"/>; "|" (the chat's argument separator) becomes "/".</summary>
        public static string Clean(string? text, int max) =>
            SocialRules.CleanMessage(text, new ChatCatalogData { maxLength = max }).Replace('|', '/');

        /// <summary>Why a new vote is refused, or None. Texts must already be cleaned (<see cref="Clean"/>).</summary>
        public static SocialRejectReason Check(string kind, string question, IReadOnlyList<string> options, IReadOnlyList<long> slotsMs, int hours,
            string purpose, long nowMs)
        {
            if (!IsKind(kind) || hours < MinHours || hours > MaxHours)
                return SocialRejectReason.InvalidPoll;
            if (kind == KindPoll)
            {
                if (string.IsNullOrEmpty(question) || options.Count < MinOptions || options.Count > MaxOptions)
                    return SocialRejectReason.InvalidPoll;
                if (options.Any(string.IsNullOrEmpty) || options.Select(o => o.ToUpperInvariant()).Distinct().Count() != options.Count)
                    return SocialRejectReason.InvalidPoll;
                return SocialRejectReason.None;
            }

            if (Array.IndexOf(Purposes, purpose) < 0 || slotsMs.Count < MinSlots || slotsMs.Count > MaxSlots || slotsMs.Distinct().Count() != slotsMs.Count)
                return SocialRejectReason.InvalidPoll;
            var closes = nowMs + hours * 3_600_000L;
            var latest = nowMs + MaxSlotDaysAhead * 86_400_000L;
            foreach (var slot in slotsMs)
            {
                if (slot % (SlotStepMinutes * 60_000L) != 0 || slot < closes || slot > latest)
                    return SocialRejectReason.InvalidPoll;
            }

            return SocialRejectReason.None;
        }

        /// <summary>The next quarter hour at or after <paramref name="ms"/>.</summary>
        public static long RoundSlot(long ms)
        {
            var step = SlotStepMinutes * 60_000L;
            return (ms + step - 1) / step * step;
        }

        /// <summary>A ballot: at least one option, each known and once; exactly one unless multiple choice.</summary>
        public static bool ValidBallot(IReadOnlyList<int>? choices, int optionCount, bool multiple) =>
            choices != null && choices.Count >= 1 && (multiple || choices.Count == 1)
            && choices.All(c => c >= 0 && c < optionCount) && choices.Distinct().Count() == choices.Count;

        /// <summary>Votes per option.</summary>
        public static int[] Tally(int optionCount, IEnumerable<IReadOnlyList<int>> ballots)
        {
            var counts = new int[Math.Max(0, optionCount)];
            foreach (var ballot in ballots)
            foreach (var c in ballot)
            {
                if (c >= 0 && c < counts.Length)
                    counts[c]++;
            }

            return counts;
        }

        /// <summary>The winning option, 1-based; 0 when nobody voted. A tie goes to the earlier option.</summary>
        public static int Winner(IReadOnlyList<int> counts)
        {
            var best = 0;
            for (var i = 0; i < counts.Count; i++)
            {
                if (counts[i] > 0 && (best == 0 || counts[i] > counts[best - 1]))
                    best = i + 1;
            }

            return best;
        }

        /// <summary>Share of the voters who chose an option, 0–100.</summary>
        public static int Percent(int votes, int voters) => voters <= 0 ? 0 : (int)Math.Round(100.0 * Math.Max(0, votes) / voters);

        public static bool IsOpen(AlliancePollDto poll, long nowMs) => poll.ClosedAtMs <= 0 && poll.ClosesAtMs > nowMs;

        /// <summary>An open vote the viewer has not answered yet (the hub tile's red dot).</summary>
        public static bool AwaitsVote(AlliancePollDto poll, long nowMs) => IsOpen(poll, nowMs) && (poll.MyVotes == null || poll.MyVotes.Length == 0);
    }
}
