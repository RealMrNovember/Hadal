using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Accepts or declines an alliance invitation.</summary>
    [ProtoContract]
    public sealed class AnswerInviteCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string AllianceId { get; set; } = string.Empty;
        [ProtoMember(4)] public bool Accept { get; set; }
    }
}
