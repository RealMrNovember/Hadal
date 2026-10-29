using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>
    /// Helps every open request of the player's alliance mates once (docs/25): all of them, or only one group — "heal"
    /// (the medical-kit bubble) or "general" (construction and research, the clasped-hands bubble).
    /// </summary>
    [ProtoContract]
    public sealed class HelpAllCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>Empty = every request; HelpKinds.Heal or HelpKinds.General.</summary>
        [ProtoMember(3)] public string Kind { get; set; } = string.Empty;
    }
}
