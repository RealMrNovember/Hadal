using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Collects an alliance gift; an empty <see cref="GiftId"/> collects every waiting one.</summary>
    [ProtoContract]
    public sealed class CollectAllianceGiftCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string GiftId { get; set; } = string.Empty;
    }
}
