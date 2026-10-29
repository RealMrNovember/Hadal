using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One member in an alliance ranking.</summary>
    [ProtoContract]
    public sealed class AllianceRankEntryDto
    {
        [ProtoMember(1)] public int Rank { get; set; }
        [ProtoMember(2)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(3)] public string Name { get; set; } = string.Empty;
        [ProtoMember(4)] public string AvatarId { get; set; } = string.Empty;
        [ProtoMember(5)] public long Value { get; set; }
    }

    /// <summary>An alliance ranking (AllianceRankingKinds), best first, every member listed.</summary>
    [ProtoContract]
    public sealed class AllianceRankingDto
    {
        [ProtoMember(1)] public string Kind { get; set; } = string.Empty;
        [ProtoMember(2)] public AllianceRankEntryDto[] Entries { get; set; } = System.Array.Empty<AllianceRankEntryDto>();
    }
}
