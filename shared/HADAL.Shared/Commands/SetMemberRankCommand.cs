using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Changes a member's rank (1–4) and officer duty tag (docs/rehber 10 §3).</summary>
    [ProtoContract]
    public sealed class SetMemberRankCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string MemberId { get; set; } = string.Empty;
        [ProtoMember(4)] public int Rank { get; set; }
        [ProtoMember(5)] public string Role { get; set; } = string.Empty;
    }
}
