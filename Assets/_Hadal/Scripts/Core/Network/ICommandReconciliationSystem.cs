using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using Hadal.Core.StateSync;

namespace Hadal.Core.Network
{
    public interface ICommandReconciliationSystem
    {
        void EnqueueInbound(NetworkInboundPacket packet);
        void RegisterPendingCommand(string commandId, ulong clientSequence, CommandType commandType);
        void ProcessQueue();

        /// <summary>
        /// Connection lost: drop queued inbound frames (the reconnect snapshot supersedes them) and
        /// resolve commands that can no longer be acked so their ghosts do not hang.
        /// </summary>
        void ResetForReconnect();
    }
}
