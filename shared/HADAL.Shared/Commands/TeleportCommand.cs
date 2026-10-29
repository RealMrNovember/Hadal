using ProtoBuf;

namespace HADAL.Shared.Commands
{
    /// <summary>Moves the player's base (TeleportRules): random, to a chosen tile, or next to an alliance mate.</summary>
    [ProtoContract]
    public sealed class TeleportCommand : ICommand
    {
        [ProtoMember(1)] public string CommandId { get; set; } = string.Empty;
        [ProtoMember(2)] public ulong ClientSequence { get; set; }

        /// <summary>"random", "target" or "alliance".</summary>
        [ProtoMember(3)] public string Mode { get; set; } = string.Empty;
        [ProtoMember(4)] public int X { get; set; }
        [ProtoMember(5)] public int Y { get; set; }

        /// <summary>Alliance jumps: the mate whose base to land next to.</summary>
        [ProtoMember(6)] public string MemberId { get; set; } = string.Empty;
    }
}
