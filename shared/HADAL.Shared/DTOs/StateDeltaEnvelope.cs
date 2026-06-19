using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    [ProtoContract]
    public sealed class StateDeltaEnvelope
    {
        [ProtoMember(1)] public uint SchemaVersion { get; set; }
        [ProtoMember(2)] public ulong ServerTick { get; set; }
        [ProtoMember(3)] public ulong BaselineTick { get; set; }
        [ProtoMember(4)] public byte[] Payload { get; set; } = System.Array.Empty<byte>();
    }
}
