using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>The leader hands the alliance to another member and becomes an officer.</summary>
    [ProtoContract]
    public sealed class TransferLeadershipCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string MemberId { get; set; } = string.Empty;
    }
}
