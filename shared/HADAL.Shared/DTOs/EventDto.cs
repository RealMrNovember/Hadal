using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>A text in one language ("tr", "en", ...); events carry every language the Overseer wrote.</summary>
    [ProtoContract]
    public sealed class LocalTextDto
    {
        [ProtoMember(1)] public string Lang { get; set; } = string.Empty;
        [ProtoMember(2)] public string Text { get; set; } = string.Empty;
    }

    /// <summary>One way to earn points: <see cref="Points"/> for every <see cref="Per"/> units of <see cref="Action"/> (EventActions).</summary>
    [ProtoContract]
    public sealed class EventActionDto
    {
        [ProtoMember(1)] public string Action { get; set; } = string.Empty;
        [ProtoMember(2)] public long Points { get; set; }
        [ProtoMember(3)] public long Per { get; set; } = 1;
    }

    /// <summary>A themed window inside an event (genre "survival war": a new theme every few hours).</summary>
    [ProtoContract]
    public sealed class EventPhaseDto
    {
        [ProtoMember(1)] public LocalTextDto[] Title { get; set; } = System.Array.Empty<LocalTextDto>();
        [ProtoMember(2)] public long StartMs { get; set; }
        [ProtoMember(3)] public long EndMs { get; set; }
        [ProtoMember(4)] public EventActionDto[] Actions { get; set; } = System.Array.Empty<EventActionDto>();
    }

    /// <summary>A reward step: a points threshold (stages), a login day (login events) or a rank band.</summary>
    [ProtoContract]
    public sealed class EventTierDto
    {
        /// <summary>Points to reach (stages) or the day number (login).</summary>
        [ProtoMember(1)] public long Points { get; set; }
        [ProtoMember(2)] public RewardEntryDto[] Rewards { get; set; } = System.Array.Empty<RewardEntryDto>();

        /// <summary>Rank bands only: first and last rank (inclusive).</summary>
        [ProtoMember(3)] public int From { get; set; }
        [ProtoMember(4)] public int To { get; set; }
    }

    /// <summary>One event as a player sees it, with their own progress.</summary>
    [ProtoContract]
    public sealed class EventDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;

        /// <summary>"points" (solo contest), "alliance" (alliance contest) or "login" (daily sign-in rewards).</summary>
        [ProtoMember(2)] public string Template { get; set; } = string.Empty;
        [ProtoMember(3)] public LocalTextDto[] Title { get; set; } = System.Array.Empty<LocalTextDto>();
        [ProtoMember(4)] public LocalTextDto[] Description { get; set; } = System.Array.Empty<LocalTextDto>();

        /// <summary>Banner art id (Resources/Events/banner_{id}).</summary>
        [ProtoMember(5)] public string Banner { get; set; } = string.Empty;
        [ProtoMember(6)] public long StartsAtMs { get; set; }
        [ProtoMember(7)] public long EndsAtMs { get; set; }

        /// <summary>Rank rewards can be claimed until then.</summary>
        [ProtoMember(8)] public long ClaimUntilMs { get; set; }
        [ProtoMember(9)] public EventActionDto[] Actions { get; set; } = System.Array.Empty<EventActionDto>();
        [ProtoMember(10)] public EventPhaseDto[] Phases { get; set; } = System.Array.Empty<EventPhaseDto>();
        [ProtoMember(11)] public EventTierDto[] Stages { get; set; } = System.Array.Empty<EventTierDto>();
        [ProtoMember(12)] public EventTierDto[] RankBands { get; set; } = System.Array.Empty<EventTierDto>();

        [ProtoMember(13)] public long MyPoints { get; set; }

        /// <summary>0 = not ranked yet.</summary>
        [ProtoMember(14)] public int MyRank { get; set; }

        /// <summary>Stage indices (or login day indices) already claimed.</summary>
        [ProtoMember(15)] public int[] Claimed { get; set; } = System.Array.Empty<int>();
        [ProtoMember(16)] public bool RankClaimed { get; set; }

        /// <summary>Alliance events: the alliance's points and rank (0 = none).</summary>
        [ProtoMember(17)] public long AlliancePoints { get; set; }
        [ProtoMember(18)] public int AllianceRank { get; set; }

        /// <summary>Login events: days signed in so far.</summary>
        [ProtoMember(19)] public int LoginDays { get; set; }
        [ProtoMember(20)] public int Participants { get; set; }

        /// <summary>Points needed for a rank reward (0 = any score).</summary>
        [ProtoMember(21)] public long MinRankPoints { get; set; }

        /// <summary>The weekly schedule this event comes from (empty = a one-off event).</summary>
        [ProtoMember(22)] public string ScheduleId { get; set; } = string.Empty;

        /// <summary>Mobilization events: the alliance's task pool and the player's task (null for other templates).</summary>
        [ProtoMember(23)] public MobilizationDto? Mobilization { get; set; }

        /// <summary>Alliance Duel events: both sides, the days, the player's daily points and chests (null for other templates).</summary>
        [ProtoMember(24)] public DuelDto? Duel { get; set; }

        /// <summary>Set once the end-of-event payout went out: rank rewards and unclaimed stages arrive by mail.</summary>
        [ProtoMember(25)] public bool PaidOut { get; set; }
    }

    /// <summary>One Mobilization task card (MobilizationRules): kind, colour tier, target and points.</summary>
    [ProtoContract]
    public sealed class MobilizationTaskDto
    {
        /// <summary>MobilizationRules.Kinds id ("gather", "train", …).</summary>
        [ProtoMember(1)] public string Kind { get; set; } = string.Empty;

        /// <summary>1 green … 4 gold.</summary>
        [ProtoMember(2)] public int Tier { get; set; }
        [ProtoMember(3)] public long Target { get; set; }
        [ProtoMember(4)] public long Points { get; set; }

        /// <summary>The pool slot (-1 for a held task).</summary>
        [ProtoMember(5)] public int Slot { get; set; }

        /// <summary>Pool cards: while the slot refills, when its next card appears (Unix ms); 0 = ready to take.</summary>
        [ProtoMember(6)] public long ReadyAtMs { get; set; }

        /// <summary>The player's held task: how far it is.</summary>
        [ProtoMember(7)] public long Progress { get; set; }
    }

    /// <summary>What a member sees of Mobilization: the alliance's pool, their task and their limits.</summary>
    [ProtoContract]
    public sealed class MobilizationDto
    {
        [ProtoMember(1)] public MobilizationTaskDto[] Pool { get; set; } = System.Array.Empty<MobilizationTaskDto>();

        /// <summary>The task the player holds; null when none.</summary>
        [ProtoMember(2)] public MobilizationTaskDto? Mine { get; set; }

        [ProtoMember(3)] public int TasksLeftToday { get; set; }
        [ProtoMember(4)] public int TasksPerDay { get; set; }
        [ProtoMember(5)] public int RefreshesLeftToday { get; set; }

        /// <summary>When the next refresh is allowed (Unix ms; 0 = now).</summary>
        [ProtoMember(6)] public long RefreshReadyAtMs { get; set; }

        /// <summary>The alliance's members now, and how many it needs to take part.</summary>
        [ProtoMember(7)] public int Members { get; set; }
        [ProtoMember(8)] public int MinMembers { get; set; }

        /// <summary>Tasks the player finished in this event.</summary>
        [ProtoMember(9)] public int Completed { get; set; }

        /// <summary>False without an alliance.</summary>
        [ProtoMember(10)] public bool InAlliance { get; set; }
    }

    /// <summary>One side of an Alliance Duel.</summary>
    [ProtoContract]
    public sealed class DuelSideDto
    {
        /// <summary>Alliance id, or AllianceDuelRules.Echo for the stand-in.</summary>
        [ProtoMember(1)] public string AllianceId { get; set; } = string.Empty;
        [ProtoMember(2)] public string Tag { get; set; } = string.Empty;
        [ProtoMember(3)] public string Name { get; set; } = string.Empty;
        [ProtoMember(4)] public string Emblem { get; set; } = string.Empty;

        /// <summary>Alliance power when the duel was paired.</summary>
        [ProtoMember(5)] public long Power { get; set; }

        /// <summary>Points on each of the six days (the Echo's show once a day is over).</summary>
        [ProtoMember(6)] public long[] DayPoints { get; set; } = System.Array.Empty<long>();

        /// <summary>Week points from the days won so far.</summary>
        [ProtoMember(7)] public int Wins { get; set; }
    }

    /// <summary>A past week's duel, for the history list.</summary>
    [ProtoContract]
    public sealed class DuelHistoryDto
    {
        [ProtoMember(1)] public long StartsAtMs { get; set; }
        [ProtoMember(2)] public string RivalTag { get; set; } = string.Empty;
        [ProtoMember(3)] public string RivalName { get; set; } = string.Empty;
        [ProtoMember(4)] public int MyWins { get; set; }
        [ProtoMember(5)] public int RivalWins { get; set; }

        /// <summary>AllianceDuelRules.Won, Lost or Draw.</summary>
        [ProtoMember(6)] public int Result { get; set; }
    }

    /// <summary>What a member sees of the Alliance Duel (AllianceDuelRules).</summary>
    [ProtoContract]
    public sealed class DuelDto
    {
        /// <summary>False when the player's alliance is not in this week's duel (no alliance, too small, joined late).</summary>
        [ProtoMember(1)] public bool InDuel { get; set; }
        [ProtoMember(2)] public DuelSideDto? Mine { get; set; }
        [ProtoMember(3)] public DuelSideDto? Rival { get; set; }

        /// <summary>Per day: AllianceDuelRules.Pending, First (mine), Second (rival) or Tie.</summary>
        [ProtoMember(4)] public int[] DayWinners { get; set; } = System.Array.Empty<int>();

        /// <summary>The player's own points on each day (the daily chests count these).</summary>
        [ProtoMember(5)] public long[] MyDayPoints { get; set; } = System.Array.Empty<long>();

        /// <summary>Daily chests taken: AllianceDuelRules.ChestKey(day, stage).</summary>
        [ProtoMember(6)] public int[] ClaimedChests { get; set; } = System.Array.Empty<int>();

        /// <summary>AllianceDuelRules.Running until the week is over, then Won, Lost or Draw for the player's alliance.</summary>
        [ProtoMember(7)] public int Result { get; set; }

        /// <summary>The alliance's last duels, newest first.</summary>
        [ProtoMember(8)] public DuelHistoryDto[] History { get; set; } = System.Array.Empty<DuelHistoryDto>();

        /// <summary>Alliances paired this week in the kingdom (for "x duels this week").</summary>
        [ProtoMember(9)] public int Pairings { get; set; }
    }

    /// <summary>Every event the player can see: upcoming, live and ended ones still paying out.</summary>
    [ProtoContract]
    public sealed class EventListDto
    {
        [ProtoMember(1)] public EventDto[] Events { get; set; } = System.Array.Empty<EventDto>();
        [ProtoMember(2)] public long ServerNowMs { get; set; }
    }

    /// <summary>
    /// The event calendar (docs/29 §2.2): every event overlapping [<see cref="FromMs"/>, <see cref="ToMs"/>), the
    /// recurring ones too before the server creates them (same id as the event they become). Rules and rewards only,
    /// no progress; texts in the language asked for.
    /// </summary>
    [ProtoContract]
    public sealed class EventCalendarDto
    {
        [ProtoMember(1)] public EventDto[] Entries { get; set; } = System.Array.Empty<EventDto>();
        [ProtoMember(2)] public long ServerNowMs { get; set; }
        [ProtoMember(3)] public long FromMs { get; set; }
        [ProtoMember(4)] public long ToMs { get; set; }
    }

    [ProtoContract]
    public sealed class EventRankEntryDto
    {
        [ProtoMember(1)] public int Rank { get; set; }

        /// <summary>Player or alliance id.</summary>
        [ProtoMember(2)] public string Id { get; set; } = string.Empty;
        [ProtoMember(3)] public string Name { get; set; } = string.Empty;

        /// <summary>Alliance tag (players) or emblem (alliances).</summary>
        [ProtoMember(4)] public string Tag { get; set; } = string.Empty;
        [ProtoMember(5)] public string Avatar { get; set; } = string.Empty;
        [ProtoMember(6)] public long Points { get; set; }
    }

    /// <summary>The top of an event's ranking and the asker's own line.</summary>
    [ProtoContract]
    public sealed class EventRankingDto
    {
        [ProtoMember(1)] public string EventId { get; set; } = string.Empty;
        [ProtoMember(2)] public bool Alliance { get; set; }
        [ProtoMember(3)] public EventRankEntryDto[] Entries { get; set; } = System.Array.Empty<EventRankEntryDto>();
        [ProtoMember(4)] public EventRankEntryDto? Mine { get; set; }
    }
}
