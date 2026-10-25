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
    }
}
