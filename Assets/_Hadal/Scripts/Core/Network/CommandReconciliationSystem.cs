using System.Collections.Generic;
using UnityEngine;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Sprint 5 stub — buffers inbound server frames for future ack/mismatch handling.
    /// </summary>
    public sealed class CommandReconciliationSystem : ICommandReconciliationSystem
    {
        private readonly Queue<NetworkInboundPacket> _pending = new();

        public int PendingCount => _pending.Count;

        public void EnqueueInbound(NetworkInboundPacket packet)
        {
            if (packet == null)
                return;

            _pending.Enqueue(packet);

            Debug.Log(
                $"[CommandReconciliation] Queued {packet.Kind} tick={packet.ServerTick} " +
                $"schema={packet.SchemaVersion} queue={_pending.Count}");
        }
    }
}
