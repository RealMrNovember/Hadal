using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Helps every open request of the player's alliance mates once (docs/25).</summary>
    [ProtoContract]
    public sealed class HelpAllCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
    }
}
