using HADAL.Shared.Enums;
using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>
    /// One producer/consumer row for a building definition, expressed per simulation tick
    /// at level 1. Level scaling lives in <c>ProductionFormulas</c>, not here.
    /// </summary>
    [ProtoContract]
    public sealed class ProductionRuleDto
    {
        [ProtoMember(1)] public string BuildingDefinitionId { get; set; } = string.Empty;
        [ProtoMember(2)] public ResourceType Resource { get; set; }
        [ProtoMember(3)] public long ProductionPerTick { get; set; }
        [ProtoMember(4)] public long ConsumptionPerTick { get; set; }
    }
}
