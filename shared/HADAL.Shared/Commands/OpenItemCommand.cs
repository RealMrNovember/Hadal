using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Opens gift chests or uses hero cards from the bag (docs/27).</summary>
    [ProtoContract]
    public sealed class OpenItemCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string ItemId { get; set; } = string.Empty;
        [ProtoMember(4)] public int Count { get; set; }
    }
}
