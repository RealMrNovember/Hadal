using HADAL.Shared.Enums;
using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    [ProtoContract]
    public sealed class CommandAckDto
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public CommandResultCode Result { get; set; }
    }
}
