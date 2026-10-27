using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Asks for another player's profile card (tapping an avatar in chat, a base on the map).</summary>
    [ProtoContract]
    public sealed class ViewProfileCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string PlayerId { get; set; } = string.Empty;
    }
}
