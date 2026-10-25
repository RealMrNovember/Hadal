using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Finishes a research inside the free-finish window.</summary>
    [ProtoContract]
    public sealed class FinishResearchCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The research node id.</summary>
        [ProtoMember(3)] public string NodeId { get; set; } = string.Empty;
    }
}
