using System;
using System.Collections.Generic;
using HADAL.Shared.DTOs;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// What players do that events can score (docs/29 §2.2). The server reports each with an amount; an event
    /// turns them into points through its actions table.
    /// </summary>
    public static class EventActions
    {
        /// <summary>Resources brought home by gathering teams (units).</summary>
        public const string Gather = "gather";

        /// <summary>Power gained by finished constructions.</summary>
        public const string BuildPower = "build_power";

        /// <summary>Research levels finished.</summary>
        public const string Research = "research";

        /// <summary>Troops trained, by tier ("train_t1" … "train_t5"), in units.</summary>
        public const string TrainPrefix = "train_t";
        public const string Heal = "heal";

        /// <summary>Creatures defeated, and the same weighted by their level.</summary>
        public const string MonsterKill = "monster_kill";
        public const string MonsterLevels = "monster_levels";

        /// <summary>Units of an event boss's body destroyed (an alliance contest on it ranks alliances by shared damage).</summary>
        public const string BossDamage = "boss_damage";

        /// <summary>Cryo-Pod pulls, hero level-ups, heroes recruited from cards.</summary>
        public const string HeroPull = "hero_pull";
        public const string HeroLevel = "hero_level";
        public const string HeroRecruit = "hero_recruit";

        /// <summary>Speed-up minutes used from the bag.</summary>
        public const string SpeedupMinutes = "speedup_minutes";

        /// <summary>Hadalite spent anywhere.</summary>
        public const string HadaliteSpent = "hadalite_spent";

        /// <summary>Depth Map stages cleared, alliance helps given, salvage runs, daily activity points.</summary>
        public const string Dive = "dive";
        public const string Help = "help";
        public const string Salvage = "salvage";
        public const string DailyPoints = "daily_points";

        public static string Train(int tier) => TrainPrefix + tier;

        /// <summary>Every action, in the order the Overseer panel lists them.</summary>
        public static readonly string[] All =
        {
            Gather, BuildPower, Research, Train(1), Train(2), Train(3), Train(4), Train(5), Heal, MonsterKill, MonsterLevels, BossDamage,
            HeroPull, HeroLevel, HeroRecruit, SpeedupMinutes, HadaliteSpent, Dive, Help, Salvage, DailyPoints
        };

        public static bool IsKnown(string action) => Array.IndexOf(All, action) >= 0;
    }

    public enum EventPhase
    {
        Upcoming,
        Live,

        /// <summary>Over; rank rewards can still be claimed.</summary>
        Ended,

        /// <summary>Past the claim window.</summary>
        Closed
    }

    /// <summary>Event templates (docs/29): what kind of page and rules an event uses.</summary>
    public static class EventTemplates
    {
        /// <summary>A solo contest: points, stage chests, a player ranking.</summary>
        public const string Points = "points";

        /// <summary>An alliance contest: members' points add up to the alliance's, alliances are ranked.</summary>
        public const string Alliance = "alliance";

        /// <summary>Daily sign-in rewards (day 1 … n).</summary>
        public const string Login = "login";

        /// <summary>
        /// Mobilization (docs/29 §4.5): a shared task pool per alliance; members take one task at a time and finish it by
        /// playing. Task points go to the player and the alliance; alliances are ranked (MobilizationRules).
        /// </summary>
        public const string Mobilization = "mobilization";

        /// <summary>
        /// Alliance Duel (docs/29 §4.3): alliances of a kingdom paired 1-v-1 for six themed days; day wins are worth
        /// 1/2/2/2/2/4 and the first to 7 wins the week (AllianceDuelRules).
        /// </summary>
        public const string AllianceDuel = "alliance_duel";

        /// <summary>
        /// Trench Conquest (docs/29 §4.6): members register a team snapshot into one of three lanes; alliances are grouped
        /// six by six and meet in five league rounds of relay lane battles (TrenchRules). Alliances rank by league points.
        /// </summary>
        public const string TrenchConquest = "trench_conquest";

        /// <summary>
        /// Lure Trap (docs/29 §4.1, LureTrapRules): each alliance charges and schedules two traps; members' damage on the
        /// Gulper is their points, alliances rank by their members' damage, and the damage over both traps sets the
        /// alliance's tier reward.
        /// </summary>
        public const string LureTrap = "lure_trap";

        public static readonly string[] All = { Points, Alliance, Login, Mobilization, AllianceDuel, TrenchConquest, LureTrap };

        public static bool IsKnown(string template) => Array.IndexOf(All, template) >= 0;

        /// <summary>Templates ranked by alliance: members' points add up to the alliance's.</summary>
        public static bool RanksAlliances(string template) => template == Alliance || template == Mobilization || template == TrenchConquest || template == LureTrap;

        /// <summary>Templates whose points also count for the player's alliance.</summary>
        public static bool ForAlliances(string template) => template == Alliance || template == Mobilization || template == AllianceDuel || template == LureTrap;
    }

    /// <summary>Event arithmetic shared by the server (scoring, claims) and the client (what to show).</summary>
    public static class EventRules
    {
        public static EventPhase Phase(EventDto e, long nowMs) =>
            nowMs < e.StartsAtMs ? EventPhase.Upcoming
            : nowMs < e.EndsAtMs ? EventPhase.Live
            : nowMs < e.ClaimUntilMs ? EventPhase.Ended
            : EventPhase.Closed;

        /// <summary>The themed window running at <paramref name="nowMs"/>, or -1 (no phases, or between them).</summary>
        public static int CurrentPhase(EventDto e, long nowMs)
        {
            for (var i = 0; i < e.Phases.Length; i++)
            {
                if (nowMs >= e.Phases[i].StartMs && nowMs < e.Phases[i].EndMs)
                    return i;
            }

            return -1;
        }

        /// <summary>The actions that score right now: the running phase's, or the event's own when it has no phases.</summary>
        public static EventActionDto[] ActiveActions(EventDto e, long nowMs)
        {
            if (e.Phases.Length == 0)
                return e.Actions;
            var phase = CurrentPhase(e, nowMs);
            return phase < 0 ? Array.Empty<EventActionDto>() : e.Phases[phase].Actions;
        }

        /// <summary>
        /// Points for <paramref name="amount"/> more units of an action. Units below a full <c>Per</c> carry over in
        /// <paramref name="carry"/>, so many small deliveries score like one large one.
        /// </summary>
        public static long Score(EventActionDto[] actions, string action, long amount, ref long carry)
        {
            if (amount <= 0)
                return 0;
            foreach (var a in actions)
            {
                if (a == null || a.Action != action || a.Points <= 0)
                    continue;
                var per = Math.Max(1, a.Per);
                var units = carry + amount;
                var whole = units / per;
                carry = units % per;
                // Saturate instead of overflowing on absurd configurations.
                return whole > long.MaxValue / a.Points ? long.MaxValue : whole * a.Points;
            }

            return 0;
        }

        /// <summary>How many stages <paramref name="points"/> reaches (stages sorted by points).</summary>
        public static int StagesReached(EventDto e, long points)
        {
            var n = 0;
            foreach (var stage in e.Stages)
            {
                if (points >= stage.Points)
                    n++;
            }

            return n;
        }

        /// <summary>The rank band paying <paramref name="rank"/>, or null.</summary>
        public static EventTierDto? Band(EventDto e, int rank)
        {
            if (rank <= 0)
                return null;
            foreach (var band in e.RankBands)
            {
                if (rank >= band.From && rank <= band.To)
                    return band;
            }

            return null;
        }

        /// <summary>The day number (1-based) of a login event at <paramref name="nowMs"/>, counted in UTC days from its start.</summary>
        public static int LoginDay(EventDto e, long nowMs) =>
            nowMs < e.StartsAtMs ? 0 : (int)((nowMs - e.StartsAtMs) / 86_400_000L) + 1;

        /// <summary>The text for <paramref name="lang"/>, falling back to English, then Turkish, then anything.</summary>
        public static string Text(LocalTextDto[] texts, string lang)
        {
            string? en = null, tr = null, any = null;
            foreach (var t in texts)
            {
                if (t == null || string.IsNullOrEmpty(t.Text))
                    continue;
                if (t.Lang == lang)
                    return t.Text;
                if (t.Lang == "en")
                    en = t.Text;
                else if (t.Lang == "tr")
                    tr = t.Text;
                any ??= t.Text;
            }

            return en ?? tr ?? any ?? string.Empty;
        }

        /// <summary>
        /// Whether something waits to be claimed on the page (red dot): a reached stage, a login day, a duel chest. Only
        /// while the event runs: at its end every reward still waiting (rank rewards too) is mailed.
        /// </summary>
        public static bool HasClaim(EventDto e, long nowMs)
        {
            if (Phase(e, nowMs) != EventPhase.Live)
                return false;
            if (e.Template == EventTemplates.AllianceDuel)
                return e.Duel != null && AllianceDuelRules.ChestsWaiting(e.Duel, e.Stages) > 0;
            var claimed = new HashSet<int>(e.Claimed);
            if (e.Template == EventTemplates.Login)
            {
                for (var i = 0; i < Math.Min(e.LoginDays, e.Stages.Length); i++)
                {
                    if (!claimed.Contains(i))
                        return true;
                }

                return false;
            }

            var reached = StagesReached(e, e.MyPoints);
            for (var i = 0; i < reached; i++)
            {
                if (!claimed.Contains(i))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// The event calendar's day arithmetic (docs/29 §2.2), shared by the server and the client's week strip. Days are
    /// counted on a clock <c>offsetMs</c> ahead of UTC (0 = server time, the phone's offset for the player's own days).
    /// </summary>
    public static class EventCalendar
    {
        public const long DayMs = 86_400_000L;

        /// <summary>How many days the calendar shows, today first.</summary>
        public const int Days = 7;

        /// <summary>The start of the day holding <paramref name="ms"/>.</summary>
        public static long DayStart(long ms, long offsetMs) => FloorDiv(ms + offsetMs, DayMs) * DayMs - offsetMs;

        /// <summary>The day of week (0 = Sunday … 6 = Saturday, as System.DayOfWeek) of the day holding <paramref name="ms"/>.</summary>
        public static int Weekday(long ms, long offsetMs) => (int)(((FloorDiv(ms + offsetMs, DayMs) + 4) % 7 + 7) % 7); // 1970-01-01 was a Thursday

        /// <summary>The starts of the <see cref="Days"/> days from the one holding <paramref name="nowMs"/>.</summary>
        public static long[] Week(long nowMs, long offsetMs)
        {
            var today = DayStart(nowMs, offsetMs);
            var days = new long[Days];
            for (var i = 0; i < Days; i++)
                days[i] = today + i * DayMs;
            return days;
        }

        /// <summary>The entries overlapping the day starting at <paramref name="dayStartMs"/>: earliest start first, then earliest end, then id.</summary>
        public static List<EventDto> OnDay(IEnumerable<EventDto> entries, long dayStartMs)
        {
            var dayEnd = dayStartMs + DayMs;
            var list = new List<EventDto>();
            if (entries == null)
                return list;
            foreach (var e in entries)
            {
                if (e != null && e.StartsAtMs < dayEnd && e.EndsAtMs > dayStartMs)
                    list.Add(e);
            }

            list.Sort((a, b) =>
            {
                var c = a.StartsAtMs.CompareTo(b.StartsAtMs);
                if (c == 0)
                    c = a.EndsAtMs.CompareTo(b.EndsAtMs);
                return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
            });
            return list;
        }

        /// <summary>Whether the entry starts on the day starting at <paramref name="dayStartMs"/> (else it carries over from before).</summary>
        public static bool StartsOn(EventDto e, long dayStartMs) => e.StartsAtMs >= dayStartMs && e.StartsAtMs < dayStartMs + DayMs;

        private static long FloorDiv(long a, long b) => a >= 0 ? a / b : -((-a + b - 1) / b);
    }
}
