using HADAL.Shared.DTOs;
using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Marks a mail as read (opening it); an empty <see cref="MailId"/> marks every mail of <see cref="Tab"/> read.</summary>
    [ProtoContract]
    public sealed class ReadMailCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string MailId { get; set; } = string.Empty;

        /// <summary>With an empty <see cref="MailId"/>: the mailbox tab (MailRules.Tabs) to act on; empty = the whole box.</summary>
        [ProtoMember(4)] public string Tab { get; set; } = string.Empty;
    }

    /// <summary>
    /// Takes a mail's attachments into the bag and wallet; an empty <see cref="MailId"/> claims every mail of
    /// <see cref="Tab"/>.
    /// </summary>
    [ProtoContract]
    public sealed class ClaimMailCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string MailId { get; set; } = string.Empty;

        /// <summary>With an empty <see cref="MailId"/>: the mailbox tab (MailRules.Tabs) to act on; empty = the whole box.</summary>
        [ProtoMember(4)] public string Tab { get; set; } = string.Empty;
    }

    /// <summary>
    /// A player's mail to one other player (MailRules). Resources may go along only to a mate in the same alliance,
    /// within the sender's daily allowance; they leave the sender's wallet at once and wait in the mail.
    /// </summary>
    [ProtoContract]
    public sealed class SendMailCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>The recipient's public player id.</summary>
        [ProtoMember(3)] public string PlayerId { get; set; } = string.Empty;
        [ProtoMember(4)] public string Title { get; set; } = string.Empty;
        [ProtoMember(5)] public string Body { get; set; } = string.Empty;
        [ProtoMember(6)] public ResourceAmountEntry[] Resources { get; set; } = System.Array.Empty<ResourceAmountEntry>();
    }

    /// <summary>
    /// Deletes a mail whose attachments are taken; an empty <see cref="MailId"/> deletes every read mail with nothing
    /// left to claim.
    /// </summary>
    [ProtoContract]
    public sealed class DeleteMailCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string MailId { get; set; } = string.Empty;

        /// <summary>With an empty <see cref="MailId"/>: the mailbox tab (MailRules.Tabs) to act on; empty = the whole box.</summary>
        [ProtoMember(4)] public string Tab { get; set; } = string.Empty;
    }
}
