using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One alliance research node with its level and progress (docs/31 §2).</summary>
    [ProtoContract]
    public sealed class AllianceResearchNodeDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public int Level { get; set; }
        [ProtoMember(3)] public long Progress { get; set; }

        /// <summary>Points to the next level (0 at the top).</summary>
        [ProtoMember(4)] public long Needed { get; set; }
    }

    /// <summary>The alliance's research and this member's donations today.</summary>
    [ProtoContract]
    public sealed class AllianceResearchDto
    {
        [ProtoMember(1)] public string Focus { get; set; } = string.Empty;
        [ProtoMember(2)] public AllianceResearchNodeDto[] Nodes { get; set; } = System.Array.Empty<AllianceResearchNodeDto>();
        [ProtoMember(3)] public int DonationsLeft { get; set; }

        /// <summary>This member's research points donated in total (the donation ranking).</summary>
        [ProtoMember(4)] public long MyDonated { get; set; }
    }
}
