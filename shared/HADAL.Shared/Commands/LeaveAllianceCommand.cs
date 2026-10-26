using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Leaves the player's alliance; a leaving leader hands over to the oldest member (docs/25).</summary>
    [ProtoContract]
    public sealed class LeaveAllianceCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
    }
}
