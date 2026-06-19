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
    }
}
