using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// Gateway handshake — schema negotiation and session bootstrap.
    /// </summary>
    [ProtoContract]
    public sealed class ClientAuthCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public uint ClientSchemaVersion { get; set; }
        [ProtoMember(4)] public string ClientVersion { get; set; } = string.Empty;
        [ProtoMember(5)] public string DeviceId { get; set; } = string.Empty;
    }
}
