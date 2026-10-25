using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Starts researching the next level of a node (docs/24).</summary>
    [ProtoContract]
    public sealed class StartResearchCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The research node id.</summary>
        [ProtoMember(3)] public string NodeId { get; set; } = string.Empty;
    }
}
