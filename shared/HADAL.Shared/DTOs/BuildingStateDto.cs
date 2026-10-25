using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    [ProtoContract]
    public sealed class BuildingStateDto
    {
        [ProtoMember(1)] public string EntityId { get; set; } = string.Empty;
        [ProtoMember(2)] public string DefinitionId { get; set; } = string.Empty;
        [ProtoMember(3)] public int RingIndex { get; set; }
        [ProtoMember(4)] public int SectorIndex { get; set; }
        [ProtoMember(5)] public int Level { get; set; }
        [ProtoMember(6)] public int RotationSteps { get; set; }

        /// <summary>
        /// Construction still running when this was sent; 0 = idle. The level stays the current
        /// (working) level: 0 while a new building is first built, n while upgrading to n + 1.
        /// Remaining time rather than an end timestamp, so client clock skew does not matter.
        /// </summary>
        [ProtoMember(7)] public long ConstructionRemainingMs { get; set; }

        /// <summary>Full duration of the running construction, for progress bars.</summary>
        [ProtoMember(8)] public long ConstructionTotalMs { get; set; }
    }
}
