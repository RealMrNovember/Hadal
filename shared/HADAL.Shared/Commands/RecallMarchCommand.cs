using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Calls a march home; what it gathered so far is kept (docs/rehber 09 §4.7).</summary>
    [ProtoContract]
    public sealed class RecallMarchCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string MarchId { get; set; } = string.Empty;
    }
}
