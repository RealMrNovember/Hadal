using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One mail in the player's mailbox (docs/29 §2.1).</summary>
    [ProtoContract]
    public sealed class MailDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;

        /// <summary>"system", "admin", "event" or "alliance".</summary>
        [ProtoMember(2)] public string Kind { get; set; } = string.Empty;
        [ProtoMember(3)] public string Sender { get; set; } = string.Empty;
        [ProtoMember(4)] public string Title { get; set; } = string.Empty;
        [ProtoMember(5)] public string Body { get; set; } = string.Empty;
        [ProtoMember(6)] public long SentAtMs { get; set; }
        [ProtoMember(7)] public long ExpiresAtMs { get; set; }
        [ProtoMember(8)] public bool Read { get; set; }

        /// <summary>The attachments were taken (always false for a mail without any).</summary>
        [ProtoMember(9)] public bool Claimed { get; set; }

        /// <summary>Resources ("resource", type name) and bag items ("item", item id).</summary>
        [ProtoMember(10)] public RewardEntryDto[] Attachments { get; set; } = System.Array.Empty<RewardEntryDto>();
    }

    /// <summary>The mailbox, newest first.</summary>
    [ProtoContract]
    public sealed class MailListDto
    {
        [ProtoMember(1)] public MailDto[] Mails { get; set; } = System.Array.Empty<MailDto>();
        [ProtoMember(2)] public int Unread { get; set; }

        /// <summary>Mails with attachments still to take.</summary>
        [ProtoMember(3)] public int Unclaimed { get; set; }
    }
}
