using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Sends a gathering march to a resource node (docs/rehber 09 §4.7).</summary>
    [ProtoContract]
    public sealed class MarchGatherCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string NodeId { get; set; } = string.Empty;
    }
}
