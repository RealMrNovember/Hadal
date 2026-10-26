using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Leader removes a member (docs/25).</summary>
    [ProtoContract]
    public sealed class KickMemberCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The member's player id.</summary>
        [ProtoMember(3)] public string MemberId { get; set; } = string.Empty;
    }
}
