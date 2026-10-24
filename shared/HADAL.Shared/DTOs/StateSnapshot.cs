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
        [ProtoMember(4)] public List<BuildingStateDto> Buildings { get; set; } = new();

        /// <summary>
        /// Production rates at snapshot time — display only. Offline accrual is already
        /// folded into <see cref="Resources"/> by the server; the client never re-applies it.
        /// </summary>
        [ProtoMember(5)] public ProductionStateDto Production { get; set; } = new();
    }
}
