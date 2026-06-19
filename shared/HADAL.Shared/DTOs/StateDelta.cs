using System.Collections.Generic;
using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    [ProtoContract]
    public sealed class StateDelta
    {
        [ProtoMember(1)] public ulong ServerTick { get; set; }
        [ProtoMember(2)] public ulong BaselineTick { get; set; }
        [ProtoMember(3)] public List<EntityChangeDto> Changes { get; set; } = new();
    }
}
