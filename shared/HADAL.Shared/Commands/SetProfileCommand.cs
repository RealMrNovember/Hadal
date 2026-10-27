using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Chooses the player's avatar from the ready-made set (hero portraits; empty = the initial).</summary>
    [ProtoContract]
    public sealed class SetProfileCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string AvatarId { get; set; } = string.Empty;
    }
}
