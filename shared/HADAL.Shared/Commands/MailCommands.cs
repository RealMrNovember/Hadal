using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Marks a mail as read (opening it).</summary>
    [ProtoContract]
    public sealed class ReadMailCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string MailId { get; set; } = string.Empty;
    }

    /// <summary>Takes a mail's attachments into the bag and wallet; an empty <see cref="MailId"/> claims every mail.</summary>
    [ProtoContract]
    public sealed class ClaimMailCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string MailId { get; set; } = string.Empty;
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
    }
}
