using System.Collections.Generic;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using Hadal.Core.Contracts;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using UnityEngine;
using VContainer.Unity;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Stub gateway — emits CommandAck + building StateDelta after a short latency.
    /// </summary>
    public sealed class LocalPlacementAckSimulator : IPlacementAckSimulator, IStartable, ITickable
    {
        private const float AckDelaySeconds = 0.2f;

        private readonly ICommandReconciliationSystem _reconciliation;
        private readonly ICircularGridService _grid;
        private readonly NetworkSerializationLayer _serialization;
        private readonly IGatewaySessionState _session;
        private readonly IProductionTickSimulator _economy;
        private readonly IWebSocketClient _webSocket;
        private readonly Queue<PendingAck> _pending = new();

        public LocalPlacementAckSimulator(
            ICommandReconciliationSystem reconciliation,
            ICircularGridService grid,
            NetworkSerializationLayer serialization,
            IGatewaySessionState session,
            IProductionTickSimulator economy,
            IWebSocketClient webSocket)
        {
            _reconciliation = reconciliation;
            _grid = grid;
            _serialization = serialization;
            _session = session;
            _economy = economy;
            _webSocket = webSocket;
        }

        /// <summary>True when a real gateway owns acknowledgement and the stub must stay quiet.</summary>
        private bool IsLiveGatewayConnected => _webSocket != null && _webSocket.IsConnected;

        public void Start()
        {
            if (IsLiveGatewayConnected || _session.IsHandshakeComplete)
                return;

            _session.CompleteHandshake();
            Debug.Log("[LocalPlacementAckSimulator] Dev handshake completed — stub gateway ready.");
        }

        public void SchedulePlacementAck(PlaceBuildingCommand command)
        {
            // CommandDispatcher calls this even after sending to a live socket. Acking a
            // command the real server is already handling would duplicate the building and,
            // now that placement charges the wallet, double-charge the player.
            if (IsLiveGatewayConnected)
                return;

            _pending.Enqueue(new PendingAck
            {
                Command = command,
                FireAt = Time.unscaledTime + AckDelaySeconds
            });
        }

        public void Tick()
        {
            if (_pending.Count == 0)
                return;

            var now = Time.unscaledTime;
            while (_pending.Count > 0 && _pending.Peek().FireAt <= now)
            {
                var item = _pending.Dequeue();
                EmitAck(item.Command);
            }
        }

        private void EmitAck(PlaceBuildingCommand command)
        {
            var slotId = new PolarGridSlotId(command.RingIndex, command.SectorIndex);
            var accepted = _grid != null && _grid.IsSlotAvailable(slotId);

            // The stub economy owns the wallet, so the build cost has to be charged there.
            // Deducting it client-side only would be undone by the next production frame,
            // which replicates absolute totals and would silently refund the building.
            EntityChangeDto walletChange = null;
            if (accepted && _economy != null && !_economy.TryChargeBuildCost(command.BuildingDefinitionId, out walletChange))
                accepted = false;

            var ack = new CommandAckDto
            {
                CommandId = command.CommandId,
                ClientSequence = command.ClientSequence,
                Result = accepted ? CommandResultCode.Accepted : CommandResultCode.Rejected
            };

            var delta = new StateDelta
            {
                ServerTick = _economy != null ? _economy.SimulatedTick : 1UL,
                BaselineTick = 0,
                Changes = new List<EntityChangeDto>()
            };

            if (accepted)
            {
                var entityId = command.CommandId;
                var buildingDto = new BuildingStateDto
                {
                    EntityId = entityId,
                    DefinitionId = command.BuildingDefinitionId,
                    RingIndex = command.RingIndex,
                    SectorIndex = command.SectorIndex,
                    Level = 1,
                    RotationSteps = command.RotationSteps
                };

                delta.Changes.Add(new EntityChangeDto
                {
                    EntityId = EntityIdPrefixes.Building + entityId,
                    Kind = EntityChangeKind.Create,
                    Payload = _serialization.Serialize(buildingDto)
                });

                if (walletChange != null)
                    delta.Changes.Add(walletChange);
            }

            delta.Changes.Add(new EntityChangeDto
            {
                EntityId = EntityIdPrefixes.Command + command.CommandId,
                Kind = EntityChangeKind.Update,
                Payload = _serialization.Serialize(ack)
            });

            _reconciliation.EnqueueInbound(new NetworkInboundPacket
            {
                Kind = NetworkInboundKind.StateDelta,
                SchemaVersion = SchemaVersion.Current,
                ServerTick = delta.ServerTick,
                BaselineTick = 0,
                Delta = delta
            });

            Debug.Log(
                $"[LocalPlacementAckSimulator] {ack.Result} for {command.CommandId} slot R{command.RingIndex}:S{command.SectorIndex}");
        }

        private struct PendingAck
        {
            public PlaceBuildingCommand Command;
            public float FireAt;
        }
    }
}
