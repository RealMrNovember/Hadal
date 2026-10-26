using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Posts a chat line to the world or alliance channel (docs/25).</summary>
    [ProtoContract]
    public sealed class SendChatCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>SocialRules.ChatChannel value.</summary>
        [ProtoMember(3)] public int Channel { get; set; }

        [ProtoMember(4)] public string Text { get; set; } = string.Empty;
    }
}
