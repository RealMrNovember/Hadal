using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>What a finished salvage run brought home (entity <c>EntityIds.SalvageReport</c> + building id).</summary>
    [ProtoContract]
    public sealed class SalvageReportDto
    {
        [ProtoMember(1)] public string BuildingEntityId { get; set; } = string.Empty;
        [ProtoMember(2)] public string MissionId { get; set; } = string.Empty;
        [ProtoMember(3)] public ResourceAmountEntry[] Rewards { get; set; } = System.Array.Empty<ResourceAmountEntry>();
    }
}
