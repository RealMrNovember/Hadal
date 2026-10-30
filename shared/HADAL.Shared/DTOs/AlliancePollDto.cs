using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One answer of an alliance poll, or one offered time of a time-slot vote (docs/29 §5).</summary>
    [ProtoContract]
    public sealed class AlliancePollOptionDto
    {
        /// <summary>The answer's text; empty for a time slot.</summary>
        [ProtoMember(1)] public string Text { get; set; } = string.Empty;

        /// <summary>A time slot's start in server time (Unix ms); 0 for a poll answer.</summary>
        [ProtoMember(2)] public long AtMs { get; set; }

        [ProtoMember(3)] public int Votes { get; set; }

        /// <summary>Who chose it; always empty on an anonymous poll.</summary>
        [ProtoMember(4)] public string[] VoterNames { get; set; } = System.Array.Empty<string>();
    }

    /// <summary>An alliance poll or time-slot vote (AlliancePollRules), open or recently closed.</summary>
    [ProtoContract]
    public sealed class AlliancePollDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;

        /// <summary>AlliancePollRules.KindPoll or KindSlots.</summary>
        [ProtoMember(2)] public string Kind { get; set; } = string.Empty;

        [ProtoMember(3)] public string Question { get; set; } = string.Empty;
        [ProtoMember(4)] public AlliancePollOptionDto[] Options { get; set; } = System.Array.Empty<AlliancePollOptionDto>();
        [ProtoMember(5)] public bool Multiple { get; set; }
        [ProtoMember(6)] public bool Anonymous { get; set; }
        [ProtoMember(7)] public string CreatorId { get; set; } = string.Empty;
        [ProtoMember(8)] public string CreatorName { get; set; } = string.Empty;
        [ProtoMember(9)] public long CreatedAtMs { get; set; }
        [ProtoMember(10)] public long ClosesAtMs { get; set; }

        /// <summary>When it closed; 0 while open.</summary>
        [ProtoMember(11)] public long ClosedAtMs { get; set; }

        /// <summary>How many members voted.</summary>
        [ProtoMember(12)] public int Voters { get; set; }

        /// <summary>The viewer's choices (option indexes); empty before they voted.</summary>
        [ProtoMember(13)] public int[] MyVotes { get; set; } = System.Array.Empty<int>();

        /// <summary>A time-slot vote's event (AlliancePollRules.Purposes); empty for a poll.</summary>
        [ProtoMember(14)] public string Purpose { get; set; } = string.Empty;

        /// <summary>The winning option, 1-based; 0 while open or when nobody voted.</summary>
        [ProtoMember(15)] public int Winner { get; set; }

        /// <summary>A closed time-slot vote's fixed time (server time, Unix ms); 0 otherwise.</summary>
        [ProtoMember(16)] public long WinnerAtMs { get; set; }

        /// <summary>AlliancePollRules.ClosedByTime, ClosedByOfficer or ClosedByOverseer; empty while open.</summary>
        [ProtoMember(17)] public string ClosedBy { get; set; } = string.Empty;
    }

    /// <summary>The alliance's open votes and its last closed ones, newest first.</summary>
    [ProtoContract]
    public sealed class AlliancePollsDto
    {
        [ProtoMember(1)] public AlliancePollDto[] Polls { get; set; } = System.Array.Empty<AlliancePollDto>();
    }
}
