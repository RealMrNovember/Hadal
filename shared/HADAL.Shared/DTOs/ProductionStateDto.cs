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
        [ProtoMember(2)] public Dictionary<ResourceType, long> NetPerTick { get; set; } = new();

        /// <summary>Echo of the server's tick interval so the HUD can label rates without guessing.</summary>
        [ProtoMember(3)] public int TickIntervalSeconds { get; set; }
    }
}
