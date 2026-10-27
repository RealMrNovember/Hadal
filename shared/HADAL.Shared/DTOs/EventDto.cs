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
    }

    /// <summary>Every event the player can see: upcoming, live and ended ones still paying out.</summary>
    [ProtoContract]
    public sealed class EventListDto
    {
        [ProtoMember(1)] public EventDto[] Events { get; set; } = System.Array.Empty<EventDto>();
        [ProtoMember(2)] public long ServerNowMs { get; set; }
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
