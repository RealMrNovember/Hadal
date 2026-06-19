using HADAL.Shared.DTOs;

namespace Hadal.Core.Network
{
    public enum NetworkInboundKind
    {
        Unknown = 0,
        StateDelta = 1,
        StateSnapshot = 2
    }

    /// <summary>
    /// Normalized server frame handed to <see cref="ICommandReconciliationSystem"/>.
    /// </summary>
    public sealed class NetworkInboundPacket
    {
        public NetworkInboundKind Kind { get; init; }
        public uint SchemaVersion { get; init; }
        public ulong ServerTick { get; init; }
        public ulong BaselineTick { get; init; }
        public StateDelta? Delta { get; init; }
        public StateSnapshot? Snapshot { get; init; }
    }
}
