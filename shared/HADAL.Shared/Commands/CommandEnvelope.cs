using HADAL.Shared.Enums;
using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// Transport envelope for all client → server commands.
    /// </summary>
    [ProtoContract]
    public sealed class CommandEnvelope
    {
        [ProtoMember(1)] public uint SchemaVersion { get; set; }
        [ProtoMember(2)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(3)] public ulong ClientSequence { get; set; }
        [ProtoMember(4)] public ulong ServerTick { get; set; }
        [ProtoMember(5)] public CommandType Type { get; set; }
        [ProtoMember(6)] public byte[] Payload { get; set; } = System.Array.Empty<byte>();
    }
}
