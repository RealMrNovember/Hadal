using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using Hadal.Core.Events;
using Hadal.Core.StateSync;
using UnityEngine;
using VContainer.Unity;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Processes inbound server frames via <see cref="IStateSyncService"/>,
    /// and triggers rollback when command acknowledgements disagree with prediction.
    /// </summary>
    public sealed class CommandReconciliationSystem : ICommandReconciliationSystem, ITickable
    {
        private const float RejectionShakeDurationSeconds = 0.35f;

        private readonly Queue<NetworkInboundPacket> _pending = new();
        private readonly IStateSyncService _stateSync;
        private readonly NetworkSerializationLayer _serialization;
        private readonly IRollbackAnimator _rollbackAnimator;
        private readonly ILocalEventBus _localBus;

        private string _lastCommandId = string.Empty;
        private ulong _lastClientSequence;
        private CommandType _lastCommandType = CommandType.Unknown;

        public int PendingCount => _pending.Count;

        public CommandReconciliationSystem(
            IStateSyncService stateSync,
            NetworkSerializationLayer serialization,
            IRollbackAnimator rollbackAnimator,
            ILocalEventBus localBus)
        {
            _stateSync = stateSync;
            _serialization = serialization;
            _rollbackAnimator = rollbackAnimator;
            _localBus = localBus;
        }

        public void EnqueueInbound(NetworkInboundPacket packet)
        {
            if (packet == null)
                return;

            _pending.Enqueue(packet);
        }

        public void RegisterPendingCommand(string commandId, ulong clientSequence, CommandType commandType)
        {
            _lastCommandId = commandId ?? string.Empty;
            _lastClientSequence = clientSequence;
            _lastCommandType = commandType;
        }

        public void Tick()
        {
            ProcessQueue();
            _rollbackAnimator.Tick(Time.deltaTime);
        }

        public void ProcessQueue()
        {
            while (_pending.Count > 0)
            {
                var packet = _pending.Dequeue();

                switch (packet.Kind)
                {
                    case NetworkInboundKind.StateSnapshot:
                        if (packet.Snapshot != null)
                            _stateSync.ApplySnapshot(packet.Snapshot);
                        break;

                    case NetworkInboundKind.StateDelta:
                        if (packet.Delta != null)
                            ProcessDelta(packet.Delta);
                        break;
                }
            }
        }

        private void ProcessDelta(StateDelta delta)
        {
            var commandAcks = _stateSync.ApplyDelta(delta);

            foreach (var ack in commandAcks)
                ProcessCommandAck(ack, delta);
        }

        private void ProcessCommandAck(CommandAckDto ack, StateDelta delta)
        {
            if (ack == null || string.IsNullOrEmpty(ack.CommandId))
                return;

            if (!string.Equals(ack.CommandId, _lastCommandId, System.StringComparison.Ordinal))
                return;

            if (ack.ClientSequence != 0 && ack.ClientSequence != _lastClientSequence)
                return;

            PublishPlacementResolution(ack);

            if (ack.Result == CommandResultCode.Rejected)
            {
                TriggerRollbackForDelta(delta, ack.CommandId);
                ClearPendingCommand();
                return;
            }

            if (ack.Result == CommandResultCode.Accepted && HasAuthoritativeMismatch(delta))
                TriggerRollbackForDelta(delta, ack.CommandId);

            if (ack.Result == CommandResultCode.Accepted)
                ClearPendingCommand();
        }

        private bool HasAuthoritativeMismatch(StateDelta delta)
        {
            if (_lastCommandType != CommandType.PlaceBuilding || delta.Changes == null)
                return false;

            var view = _stateSync.View;

            foreach (var change in delta.Changes)
            {
                if (change == null || !change.EntityId.StartsWith(EntityIdPrefixes.Building))
                    continue;

                if (change.Kind == EntityChangeKind.Delete)
                    return true;

                if (_serialization.TryDeserialize(change.Payload, out BuildingStateDto dto))
                {
                    var buildings = view.Data.buildings;
                    if (buildings == null)
                        return true;

                    foreach (var building in buildings)
                    {
                        if (building.instanceKey != dto.EntityId)
                            continue;

                        if (building.cellRing != dto.RingIndex || building.cellSector != dto.SectorIndex)
                            return true;
                    }
                }
            }

            return false;
        }

        private void TriggerRollbackForDelta(StateDelta delta, string commandId)
        {
            if (delta.Changes == null)
                return;

            foreach (var change in delta.Changes)
            {
                if (change == null || !change.EntityId.StartsWith(EntityIdPrefixes.Building))
                    continue;

                if (!_serialization.TryDeserialize(change.Payload, out BuildingStateDto dto))
                    continue;

                var authoritative = new Hadal.Data.Models.BuildingSaveEntry
                {
                    instanceKey = dto.EntityId,
                    definitionId = dto.DefinitionId,
                    level = dto.Level,
                    cellRing = dto.RingIndex,
                    cellSector = dto.SectorIndex,
                    cellX = dto.RingIndex,
                    cellY = dto.SectorIndex,
                    rotation = dto.RotationSteps * 90f
                };

                _rollbackAnimator.RequestBuildingRollback(authoritative, commandId);
            }
        }

        private void PublishPlacementResolution(CommandAckDto ack)
        {
            if (_lastCommandType != CommandType.PlaceBuilding)
                return;

            _localBus.Publish(new BuildingPlacementResolvedEvent
            {
                CommandId = ack.CommandId,
                Result = ack.Result
            });

            if (ack.Result == CommandResultCode.Rejected)
            {
                _localBus.Publish(new GhostRejectionShakeEvent
                {
                    CommandId = ack.CommandId,
                    DurationSeconds = RejectionShakeDurationSeconds
                });
            }
        }

        private void ClearPendingCommand()
        {
            _lastCommandId = string.Empty;
            _lastClientSequence = 0;
            _lastCommandType = CommandType.Unknown;
        }
    }
}
