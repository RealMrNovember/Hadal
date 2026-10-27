using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One chat line (entity <c>EntityIds.ChatMessage</c> + id).</summary>
    [ProtoContract]
    public sealed class ChatMessageDto
    {
        [ProtoMember(1)] public long Id { get; set; }

        /// <summary><c>ChatChannel</c> value.</summary>
        [ProtoMember(2)] public int Channel { get; set; }
        [ProtoMember(3)] public string SenderId { get; set; } = string.Empty;
        [ProtoMember(4)] public string SenderName { get; set; } = string.Empty;
        [ProtoMember(5)] public string AllianceTag { get; set; } = string.Empty;
        [ProtoMember(6)] public string Text { get; set; } = string.Empty;
        [ProtoMember(7)] public long SentAtUnixMs { get; set; }

        /// <summary>Server notices (someone joined the alliance, ...) carry a localization key in Text.</summary>
        [ProtoMember(8)] public bool IsSystem { get; set; }

        /// <summary>Private lines: the recipient.</summary>
        [ProtoMember(9)] public string TargetId { get; set; } = string.Empty;
        [ProtoMember(10)] public string TargetName { get; set; } = string.Empty;

        /// <summary>The sender's avatar (hero portrait id; empty = initial).</summary>
        [ProtoMember(11)] public string SenderAvatar { get; set; } = string.Empty;
    }

    /// <summary>Recent lines of every readable channel, oldest first (entity <c>EntityIds.ChatHistory</c>).</summary>
    [ProtoContract]
    public sealed class ChatHistoryDto
    {
        [ProtoMember(1)] public ChatMessageDto[] Messages { get; set; } = System.Array.Empty<ChatMessageDto>();
    }
}
