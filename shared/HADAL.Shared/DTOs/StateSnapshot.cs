using System.Collections.Generic;
using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    [ProtoContract]
    public sealed class StateSnapshot
    {
        [ProtoMember(1)] public ulong ServerTick { get; set; }
        [ProtoMember(2)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(3)] public ResourceStateDto Resources { get; set; } = new();
        public List<BuildingStateDto> Buildings { get; set; } = new();

        /// <summary>Wire form of <see cref="Buildings"/> (IL2CPP: arrays only, see <see cref="ResourceAmountEntry"/>).</summary>
        [ProtoMember(4)] public BuildingStateDto[] BuildingsWire { get; set; } = System.Array.Empty<BuildingStateDto>();

        /// <summary>
        /// Production rates at snapshot time — display only. Offline accrual is already
        /// folded into <see cref="Resources"/> by the server; the client never re-applies it.
        /// </summary>
        [ProtoMember(5)] public ProductionStateDto Production { get; set; } = new();

        /// <summary>Outermost open ring (docs/23, rule 12). 0 from older servers = use the catalog default.</summary>
        [ProtoMember(6)] public int UnlockedRings { get; set; }

        /// <summary>Research levels and running researches (docs/24); null from older servers.</summary>
        [ProtoMember(7)] public ResearchStateDto? Research { get; set; }

        [ProtoBeforeSerialization]
        private void BeforeSerialize() => BuildingsWire = Buildings?.ToArray() ?? System.Array.Empty<BuildingStateDto>();

        [ProtoAfterDeserialization]
        private void AfterDeserialize() => Buildings = new List<BuildingStateDto>(BuildingsWire ?? System.Array.Empty<BuildingStateDto>());
    }
}
