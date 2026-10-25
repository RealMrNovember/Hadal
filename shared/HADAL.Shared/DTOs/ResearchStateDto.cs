using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>The player's research: finished levels and running researches. Singleton entity <c>EntityIds.Research</c>.</summary>
    [ProtoContract]
    public sealed class ResearchStateDto
    {
        [ProtoMember(1)] public ResearchLevelEntry[] Levels { get; set; } = System.Array.Empty<ResearchLevelEntry>();
        [ProtoMember(2)] public ActiveResearchEntry[] Active { get; set; } = System.Array.Empty<ActiveResearchEntry>();
    }

    [ProtoContract]
    public sealed class ResearchLevelEntry
    {
        [ProtoMember(1)] public string NodeId { get; set; } = string.Empty;
        [ProtoMember(2)] public int Level { get; set; }
    }

    [ProtoContract]
    public sealed class ActiveResearchEntry
    {
        [ProtoMember(1)] public string NodeId { get; set; } = string.Empty;
        [ProtoMember(2)] public int TargetLevel { get; set; }

        /// <summary>Remaining time rather than an end timestamp, so client clock skew does not matter.</summary>
        [ProtoMember(3)] public long RemainingMs { get; set; }
        [ProtoMember(4)] public long TotalMs { get; set; }
    }
}
