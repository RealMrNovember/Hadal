using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Invites a player without an alliance (R3 and above).</summary>
    [ProtoContract]
    public sealed class InviteToAllianceCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string PlayerId { get; set; } = string.Empty;
    }
}
