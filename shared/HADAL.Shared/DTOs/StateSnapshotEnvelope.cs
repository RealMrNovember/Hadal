using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>
    /// Transport envelope for full server → client state snapshots.
    /// </summary>
    [ProtoContract]
    public sealed class StateSnapshotEnvelope
    {
        [ProtoMember(1)] public uint SchemaVersion { get; set; }
        [ProtoMember(2)] public ulong ServerTick { get; set; }
        [ProtoMember(3)] public byte[] Payload { get; set; } = System.Array.Empty<byte>();
    }
}
