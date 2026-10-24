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
        [ProtoMember(1)] public Dictionary<ResourceType, long> Amounts { get; set; } = new();

        /// <summary>Tick this balance set was computed at. 0 means "unversioned, always apply".</summary>
        [ProtoMember(2)] public ulong ServerTick { get; set; }
    }
}
