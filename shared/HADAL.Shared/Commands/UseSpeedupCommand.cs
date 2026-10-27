using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Uses speed-up items from the bag on one timer (a building, a march, the Med Bay).</summary>
    [ProtoContract]
    public sealed class UseSpeedupCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }
        [ProtoMember(3)] public string ItemId { get; set; } = string.Empty;
        [ProtoMember(4)] public int Count { get; set; } = 1;

        /// <summary>What to speed up: "build:{instance}", "train:{instance}", "heal", "research:{node}", "march:{id}".</summary>
        [ProtoMember(5)] public string Target { get; set; } = string.Empty;
    }
}
