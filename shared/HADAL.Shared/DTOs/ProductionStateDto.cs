using System.Collections.Generic;
using HADAL.Shared.Enums;
using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>
    /// Replicated production rates for HUD display only. Balances themselves travel as
    /// absolute totals in <see cref="ResourceStateDto"/> — never derive a balance from this.
    /// </summary>
    [ProtoContract]
    public sealed class ProductionStateDto
    {
        [ProtoMember(1)] public ulong ServerTick { get; set; }

        /// <summary>Net (production minus consumption) per simulation tick, server-computed.</summary>
        public Dictionary<ResourceType, long> NetPerTick { get; set; } = new();

        /// <summary>Wire form of <see cref="NetPerTick"/>; see <see cref="ResourceAmountEntry"/>.</summary>
        [ProtoMember(2)] public ResourceAmountEntry[] NetPerTickEntries { get; set; } = System.Array.Empty<ResourceAmountEntry>();

        [ProtoBeforeSerialization]
        private void BeforeSerialize()
        {
            NetPerTickEntries = ResourceAmountEntry.FromMap(NetPerTick);
            NetMilliPerTickEntries = ResourceAmountEntry.FromMap(NetMilliPerTick);
            CapacityEntries = ResourceAmountEntry.FromMap(Capacities);
        }

        [ProtoAfterDeserialization]
        private void AfterDeserialize()
        {
            NetPerTick = ResourceAmountEntry.ToMap(NetPerTickEntries);
            NetMilliPerTick = ResourceAmountEntry.ToMap(NetMilliPerTickEntries);
            Capacities = ResourceAmountEntry.ToMap(CapacityEntries);
        }

        /// <summary>Echo of the server's tick interval so the HUD can label rates without guessing.</summary>
        [ProtoMember(3)] public int TickIntervalSeconds { get; set; }

        /// <summary>Net per tick in thousandths (exact; <see cref="NetPerTick"/> is the rounded form for old clients).</summary>
        public Dictionary<ResourceType, long> NetMilliPerTick { get; set; } = new();

        [ProtoMember(4)] public ResourceAmountEntry[] NetMilliPerTickEntries { get; set; } = System.Array.Empty<ResourceAmountEntry>();

        /// <summary>This player's storage caps (research raises them); empty = the catalog's.</summary>
        public Dictionary<ResourceType, long> Capacities { get; set; } = new();

        [ProtoMember(5)] public ResourceAmountEntry[] CapacityEntries { get; set; } = System.Array.Empty<ResourceAmountEntry>();

        /// <summary>The survival loop's state (docs/35); null from older servers or when it is off.</summary>
        [ProtoMember(6)] public SurvivalStateDto? Survival { get; set; }
    }

    /// <summary>
    /// Energy and oxygen alarms and the running emergency (docs/35). Values of <c>SurvivalStatus</c>: 0 normal,
    /// 1 low (warning), 2 critical (penalty).
    /// </summary>
    [ProtoContract]
    public sealed class SurvivalStateDto
    {
        [ProtoMember(1)] public int Energy { get; set; }

        /// <summary>How hard energy users run, in percent (100 = full).</summary>
        [ProtoMember(2)] public int EnergyPercent { get; set; }

        [ProtoMember(3)] public int Oxygen { get; set; }

        /// <summary>While oxygen is critical: production lost and extra time on new timers, in percent.</summary>
        [ProtoMember(4)] public int OutputPenaltyPercent { get; set; }
        [ProtoMember(5)] public int TimePenaltyPercent { get; set; }

        /// <summary>The running emergency; empty id = none.</summary>
        [ProtoMember(6)] public string EmergencyId { get; set; } = string.Empty;

        /// <summary>EmergencyKinds: "leak" or "breakdown".</summary>
        [ProtoMember(7)] public string EmergencyKind { get; set; } = string.Empty;

        /// <summary>The affected building (entity id) and its definition.</summary>
        [ProtoMember(8)] public string EmergencyBuildingId { get; set; } = string.Empty;
        [ProtoMember(9)] public string EmergencyDefinitionId { get; set; } = string.Empty;

        /// <summary>Until it fixes itself if ignored.</summary>
        [ProtoMember(10)] public long ExpiresRemainingMs { get; set; }

        /// <summary>The repair crew at work: time left and the full repair time; 0 = not started.</summary>
        [ProtoMember(11)] public long RepairRemainingMs { get; set; }
        [ProtoMember(12)] public long RepairTotalMs { get; set; }

        /// <summary>What starting the repair costs.</summary>
        [ProtoMember(13)] public ResourceAmountEntry[] RepairCost { get; set; } = System.Array.Empty<ResourceAmountEntry>();

        /// <summary>A leak's oxygen loss per tick in thousandths (0 otherwise).</summary>
        [ProtoMember(14)] public long LeakMilliPerTick { get; set; }
    }
}
