using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Opens a daily activity chest once enough points are collected (docs/27).</summary>
    [ProtoContract]
    public sealed class ClaimDailyChestCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public int Points { get; set; }
    }
}
