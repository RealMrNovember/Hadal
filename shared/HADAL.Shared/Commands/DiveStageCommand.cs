using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Dives one Depth Map stage (docs/rehber 08 §5); the result is immediate.</summary>
    [ProtoContract]
    public sealed class DiveStageCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public int Chapter { get; set; }
        [ProtoMember(4)] public int Stage { get; set; }
    }
}
