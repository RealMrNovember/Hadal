using System.Collections.Generic;
using HADAL.Shared.Enums;
using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>
    /// Absolute authoritative balances — never a difference. Replicating totals makes a
    /// dropped or duplicated frame harmless; <see cref="ServerTick"/> lets the receiver
    /// discard a frame that arrives after a newer one.
    /// </summary>
    [ProtoContract]
    public sealed class ResourceStateDto
    {
        public Dictionary<ResourceType, long> Amounts { get; set; } = new();

        /// <summary>Wire form of <see cref="Amounts"/>; see <see cref="ResourceAmountEntry"/>.</summary>
        [ProtoMember(1)] public ResourceAmountEntry[] AmountEntries { get; set; } = System.Array.Empty<ResourceAmountEntry>();

        [ProtoBeforeSerialization]
        private void BeforeSerialize() => AmountEntries = ResourceAmountEntry.FromMap(Amounts);

        [ProtoAfterDeserialization]
        private void AfterDeserialize() => Amounts = ResourceAmountEntry.ToMap(AmountEntries);

        /// <summary>Tick this balance set was computed at. 0 means "unversioned, always apply".</summary>
        [ProtoMember(2)] public ulong ServerTick { get; set; }
    }
}
