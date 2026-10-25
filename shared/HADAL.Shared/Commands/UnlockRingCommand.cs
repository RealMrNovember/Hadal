using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Opens the next outer ring of the base (docs/23_Base_Management.md, rule 12).</summary>
    [ProtoContract]
    public sealed class UnlockRingCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The ring to open; must be the one right after the outermost open ring.</summary>
        [ProtoMember(3)] public int Ring { get; set; }
    }
}
