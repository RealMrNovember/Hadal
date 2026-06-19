using HADAL.Shared.Enums;
using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    [ProtoContract]
    public sealed class HandshakeAckEnvelope
    {
        [ProtoMember(1)] public uint SchemaVersion { get; set; }
        [ProtoMember(2)] public bool Accepted { get; set; }
        [ProtoMember(3)] public CommandResultCode Result { get; set; }
        [ProtoMember(4)] public string SessionToken { get; set; } = string.Empty;
    }
}
