using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Accepts or declines a player who applied to join (R3 and above).</summary>
    [ProtoContract]
    public sealed class AnswerApplicationCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(4)] public bool Accept { get; set; }
    }
}
