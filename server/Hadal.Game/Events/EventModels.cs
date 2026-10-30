using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hadal.Game.Events
{
    /// <summary>
    /// An event as the Overseer defines it (docs/29 §2.2). Stored as one JSON document, so new settings need no
    /// migration; the admin panel edits exactly this shape.
    /// </summary>
    public sealed class EventDefinition
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>EventTemplates: "points", "alliance" or "login".</summary>
        public string Template { get; set; } = "points";

        /// <summary>"draft" (hidden), "published" or "cancelled" (hidden, no claims).</summary>
        public string Status { get; set; } = EventStatus.Draft;

        /// <summary>Text per language code ("tr", "en", ...).</summary>
        public Dictionary<string, string> Title { get; set; } = new();
        public Dictionary<string, string> Description { get; set; } = new();
        public string Banner { get; set; } = "abyss";
        public DateTimeOffset StartsAt { get; set; }
        public DateTimeOffset EndsAt { get; set; }

        /// <summary>Days after the end during which rank rewards can still be claimed.</summary>
        public int ClaimDays { get; set; } = 3;

        /// <summary>Core Reactor range of the players who see it (0 = no bound).</summary>
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }

        /// <summary>Account ids that alone see the event (a test run); empty = everyone in the level range.</summary>
        public List<string> TestAccounts { get; set; } = new();

        /// <summary>Points a player needs for a rank reward.</summary>
        public long MinRankPoints { get; set; }

        /// <summary>
        /// Kingdom ids that see the event (docs/39); empty = every kingdom. Rankings are per kingdom; event bosses spawn on
        /// the targeted kingdom's map (Kingdom #1 when none is named), so a boss event names at most one.
        /// </summary>
        public List<string> Kingdoms { get; set; } = new();

        public List<EventActionConfig> Actions { get; set; } = new();
        public List<EventPhaseConfig> Phases { get; set; } = new();

        /// <summary>Phases repeat back to back until the event ends (a theme rotation).</summary>
        public bool CyclePhases { get; set; }

        /// <summary>Points thresholds with rewards; for login events, one entry per day.</summary>
        public List<EventTierConfig> Stages { get; set; } = new();
        public List<EventTierConfig> RankBands { get; set; } = new();

        /// <summary>Event bosses (MonsterKind.EventOnly) the map shows while the event is live (WorldService.Bosses).</summary>
        public List<EventBossConfig> Bosses { get; set; } = new();

        /// <summary>
        /// What a boss pays when it dies or the event ends (EventBossRewards): every member who hit it is ranked by damage
        /// inside their alliance and gets the band of their rank (From/To are ranks); everyone else who did damage, and
        /// players without an alliance, get <see cref="BossParticipation"/>. Both empty = the built-in tiers.
        /// </summary>
        public List<EventTierConfig> BossRanks { get; set; } = new();
        public List<EventRewardConfig> BossParticipation { get; set; } = new();

        /// <summary>Boss indices already paid out (each boss pays once, also across restarts).</summary>
        public List<int> PaidBosses { get; set; } = new();

        /// <summary>What each live boss has left and who hit it how hard, written behind so a restart mid-event keeps it.</summary>
        public List<EventBossProgress> BossProgress { get; set; } = new();

        /// <summary>The weekly schedule that created this event (EventSchedule), empty for a one-off event.</summary>
        public string ScheduleId { get; set; } = string.Empty;

        /// <summary>Mobilization settings (template "mobilization", docs/29 §4.5).</summary>
        public MobilizationConfig Mobilization { get; set; } = new();

        /// <summary>Alliance Duel settings (template "alliance_duel", docs/29 §4.3).</summary>
        public DuelConfig Duel { get; set; } = new();

        /// <summary>The duel's pairings and final results, written by the server (never by the Overseer's form).</summary>
        public DuelState DuelState { get; set; } = new();

        /// <summary>
        /// When the end-of-event payout was mailed (rank rewards, unclaimed stages, duel results; EventService.Payout), and
        /// how many mails it sent. Null until then; a paid event never pays again.
        /// </summary>
        public DateTimeOffset? PaidOutAt { get; set; }
        public int PaidOutMails { get; set; }

        public string Note { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset ClaimUntil => EndsAt.AddDays(Math.Max(0, ClaimDays));

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public string ToJson() => JsonSerializer.Serialize(this, Json);

        public static EventDefinition FromJson(string json) => JsonSerializer.Deserialize<EventDefinition>(json, Json) ?? new EventDefinition();

        public EventDefinition Clone() => FromJson(ToJson());
    }

    public static class EventStatus
    {
        public const string Draft = "draft";
        public const string Published = "published";
        public const string Cancelled = "cancelled";
    }

    public sealed class EventActionConfig
    {
        public string Action { get; set; } = string.Empty;
        public long Points { get; set; } = 1;
        public long Per { get; set; } = 1;
    }

    public sealed class EventPhaseConfig
    {
        public Dictionary<string, string> Title { get; set; } = new();

        /// <summary>Hours from the event start, and the length in hours.</summary>
        public double StartHour { get; set; }
        public double Hours { get; set; } = 4;
        public List<EventActionConfig> Actions { get; set; } = new();
    }

    public sealed class EventTierConfig
    {
        public long Points { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public List<EventRewardConfig> Rewards { get; set; } = new();
    }

    /// <summary>"A boss of this kind and level appears at X:Y for the event's duration."</summary>
    public sealed class EventBossConfig
    {
        /// <summary>An event-only monster kind (MonsterRules: "chasm_maw", "depth_kraken").</summary>
        public string Kind { get; set; } = string.Empty;
        public int Level { get; set; } = 20;
        public int X { get; set; }
        public int Y { get; set; }
    }

    /// <summary>
    /// A boss of a live event as the world map places it; after a restart it comes back with the body it had left
    /// (<paramref name="Left"/>, -1 = untouched) and its damage by player.
    /// </summary>
    public sealed record EventBossSpawn(string EventId, int Index, string Kind, int Level, int X, int Y, DateTimeOffset EndsAt,
        long Left = -1, IReadOnlyDictionary<string, long>? Damage = null, string Kingdom = "w1");

    /// <summary>A boss's stored progress (EventDefinition.BossProgress).</summary>
    public sealed class EventBossProgress
    {
        public int Index { get; set; }

        /// <summary>Body left; -1 = never hit.</summary>
        public long Left { get; set; } = -1;
        public Dictionary<string, long> Damage { get; set; } = new(StringComparer.Ordinal);
    }

    /// <summary>Same meaning as the catalog's RewardData: resource, item or shards (hero cards).</summary>
    public sealed class EventRewardConfig
    {
        public string Type { get; set; } = "resource";
        public string Id { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Rarity { get; set; } = string.Empty;
    }

    /// <summary>Mobilization rules an event can change (MobilizationRules has the defaults and the card table).</summary>
    public sealed class MobilizationConfig
    {
        /// <summary>Members an alliance needs before its members may take tasks.</summary>
        public int MinMembers { get; set; } = HADAL.Shared.Simulation.MobilizationRules.DefaultMinMembers;

        /// <summary>Cards in each alliance's pool.</summary>
        public int Slots { get; set; } = HADAL.Shared.Simulation.MobilizationRules.DefaultSlots;

        /// <summary>Minutes before a taken card's slot deals a new one.</summary>
        public int RefillMinutes { get; set; } = HADAL.Shared.Simulation.MobilizationRules.DefaultRefillMinutes;

        /// <summary>Tasks one member may take per UTC day.</summary>
        public int TasksPerDay { get; set; } = HADAL.Shared.Simulation.MobilizationRules.DefaultTasksPerDay;

        /// <summary>Rerolls of a held task per UTC day, and the minutes between two.</summary>
        public int RefreshesPerDay { get; set; } = HADAL.Shared.Simulation.MobilizationRules.DefaultRefreshesPerDay;
        public int RefreshCooldownMinutes { get; set; } = HADAL.Shared.Simulation.MobilizationRules.DefaultRefreshCooldownMinutes;

        /// <summary>Task kinds the pool deals (MobilizationRules.Kinds ids); empty = all.</summary>
        public List<string> Kinds { get; set; } = new();

        /// <summary>Targets in percent of the card table (50 = half as much to do).</summary>
        public int TargetPercent { get; set; } = 100;
    }

    /// <summary>Alliance Duel rules an event can change.</summary>
    public sealed class DuelConfig
    {
        /// <summary>Members an alliance needs to be paired.</summary>
        public int MinMembers { get; set; } = HADAL.Shared.Simulation.AllianceDuelRules.DefaultMinMembers;

        /// <summary>
        /// Mailed at the end to every member who scored at least the event's MinRankPoints for the alliance: the week's
        /// winners, losers, or both sides of a draw.
        /// </summary>
        public List<EventRewardConfig> Win { get; set; } = new();
        public List<EventRewardConfig> Loss { get; set; } = new();
        public List<EventRewardConfig> Draw { get; set; } = new();
    }

    /// <summary>The duel's pairing (made when the event goes live) and, once over, each pairing's result.</summary>
    public sealed class DuelState
    {
        public DateTimeOffset? PairedAt { get; set; }
        public List<DuelPairing> Pairings { get; set; } = new();
    }

    public sealed class DuelPairing
    {
        /// <summary>The kingdom both sides are in (docs/39): duels never cross kingdoms.</summary>
        public string Kingdom { get; set; } = string.Empty;

        /// <summary>The stronger alliance first; B may be AllianceDuelRules.Echo.</summary>
        public string A { get; set; } = string.Empty;
        public string B { get; set; } = string.Empty;
        public long APower { get; set; }
        public long BPower { get; set; }

        /// <summary>Tag and name at pairing time (kept for the history after an alliance is gone).</summary>
        public string ATag { get; set; } = string.Empty;
        public string AName { get; set; } = string.Empty;
        public string BTag { get; set; } = string.Empty;
        public string BName { get; set; } = string.Empty;

        /// <summary>Filled at the end: week points of both sides and A's result (AllianceDuelRules.Won/Lost/Draw).</summary>
        public int AWins { get; set; }
        public int BWins { get; set; }
        public int AResult { get; set; }
    }

    /// <summary>
    /// What an event keeps per alliance besides its total: the Mobilization pool and the duel's points per day. Stored as
    /// one JSON document per event and alliance (hadal.event_alliance_state).
    /// </summary>
    public sealed class EventAllianceState
    {
        /// <summary>Mobilization: the cards on the table, by slot (null = refilling).</summary>
        public List<MobilizationCard?> Pool { get; set; } = new();

        /// <summary>Mobilization: per slot, how many cards it has dealt, and when its next card appears.</summary>
        public List<int> Draws { get; set; } = new();
        public List<DateTimeOffset> ReadyAt { get; set; } = new();

        /// <summary>Alliance Duel: the alliance's points on each day.</summary>
        public long[] DayPoints { get; set; } = new long[HADAL.Shared.Simulation.AllianceDuelRules.Days];

        public string ToJson() => JsonSerializer.Serialize(this, EventDefinition.Json);

        public static EventAllianceState FromJson(string? json)
        {
            var state = string.IsNullOrWhiteSpace(json) ? new EventAllianceState() : JsonSerializer.Deserialize<EventAllianceState>(json, EventDefinition.Json) ?? new EventAllianceState();
            state.Pool ??= new List<MobilizationCard?>();
            state.Draws ??= new List<int>();
            state.ReadyAt ??= new List<DateTimeOffset>();
            if (state.DayPoints == null || state.DayPoints.Length < HADAL.Shared.Simulation.AllianceDuelRules.Days)
            {
                var days = new long[HADAL.Shared.Simulation.AllianceDuelRules.Days];
                Array.Copy(state.DayPoints ?? Array.Empty<long>(), days, Math.Min(days.Length, state.DayPoints?.Length ?? 0));
                state.DayPoints = days;
            }

            return state;
        }

        public EventAllianceState Clone() => FromJson(ToJson());
    }

    /// <summary>A Mobilization task card (on the table or held by a member).</summary>
    public sealed class MobilizationCard
    {
        public string Kind { get; set; } = string.Empty;
        public int Tier { get; set; } = 1;
        public long Target { get; set; }
        public long Points { get; set; }
    }

    /// <summary>One player's progress in one event.</summary>
    public sealed class EventScore
    {
        public string EventId { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>The alliance the player scored for last (alliance contests rank them by it).</summary>
        public string AllianceId { get; set; } = string.Empty;
        public long Points { get; set; }
        public HashSet<int> Claimed { get; set; } = new();
        public bool RankClaimed { get; set; }
        public int LoginDays { get; set; }

        /// <summary>UTC day number of the last counted sign-in.</summary>
        public long LastLoginDay { get; set; }

        /// <summary>Units of an action still short of a full "per" (EventRules.Score).</summary>
        public Dictionary<string, long> Carry { get; set; } = new();
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>Mobilization: the held task, its progress, today's takes and refreshes, tasks finished.</summary>
        public MobilizationCard? Task { get; set; }
        public long TaskProgress { get; set; }
        public long TaskDay { get; set; }
        public int TakenToday { get; set; }
        public int RefreshesToday { get; set; }
        public int Refreshes { get; set; }
        public DateTimeOffset? LastRefresh { get; set; }
        public int Completed { get; set; }

        /// <summary>Alliance Duel: the player's points on each day (daily chests).</summary>
        public long[] DayPoints { get; set; } = new long[HADAL.Shared.Simulation.AllianceDuelRules.Days];

        public string StateJson() => JsonSerializer.Serialize(new ScoreState(Claimed.ToList(), RankClaimed, LoginDays, LastLoginDay, Carry)
        {
            Task = Task, TaskProgress = TaskProgress, TaskDay = TaskDay, TakenToday = TakenToday, RefreshesToday = RefreshesToday,
            Refreshes = Refreshes, LastRefresh = LastRefresh, Completed = Completed,
            DayPoints = DayPoints.Any(p => p != 0) ? DayPoints : null
        }, EventDefinition.Json);

        public void ApplyState(string json)
        {
            var state = JsonSerializer.Deserialize<ScoreState>(json, EventDefinition.Json);
            if (state == null)
                return;
            Claimed = new HashSet<int>(state.Claimed ?? new List<int>());
            RankClaimed = state.RankClaimed;
            LoginDays = state.LoginDays;
            LastLoginDay = state.LastLoginDay;
            Carry = state.Carry ?? new Dictionary<string, long>();
            Task = state.Task;
            TaskProgress = state.TaskProgress;
            TaskDay = state.TaskDay;
            TakenToday = state.TakenToday;
            RefreshesToday = state.RefreshesToday;
            Refreshes = state.Refreshes;
            LastRefresh = state.LastRefresh;
            Completed = state.Completed;
            DayPoints = new long[HADAL.Shared.Simulation.AllianceDuelRules.Days];
            if (state.DayPoints != null)
                Array.Copy(state.DayPoints, DayPoints, Math.Min(DayPoints.Length, state.DayPoints.Length));
        }

        /// <summary>A detached copy for the write-behind queue.</summary>
        public EventScore Copy()
        {
            var copy = new EventScore { EventId = EventId, PlayerId = PlayerId, AllianceId = AllianceId, Points = Points, UpdatedAt = UpdatedAt };
            copy.ApplyState(StateJson());
            return copy;
        }

        private sealed record ScoreState(List<int>? Claimed, bool RankClaimed, int LoginDays, long LastLoginDay, Dictionary<string, long>? Carry)
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public MobilizationCard? Task { get; init; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public long TaskProgress { get; init; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public long TaskDay { get; init; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int TakenToday { get; init; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int RefreshesToday { get; init; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Refreshes { get; init; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public DateTimeOffset? LastRefresh { get; init; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public int Completed { get; init; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public long[]? DayPoints { get; init; }
        }
    }
}
