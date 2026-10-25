using System;
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

        // Keyed by command id: several placements can be in flight at once, and an ack for the
        // first must not be dropped just because a second command was sent after it.
        private readonly Dictionary<string, PendingCommand> _pendingCommands = new(StringComparer.Ordinal);

        private readonly struct PendingCommand
        {
            public readonly ulong ClientSequence;
            public readonly CommandType Type;

            public PendingCommand(ulong clientSequence, CommandType type)
            {
                ClientSequence = clientSequence;
                Type = type;
            }
        }

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
            if (string.IsNullOrEmpty(commandId))
                return;

            _pendingCommands[commandId] = new PendingCommand(clientSequence, commandType);
        }

        public void ResetForReconnect()
        {
            _pending.Clear();

            foreach (var pair in _pendingCommands)
            {
                if (pair.Value.Type != CommandType.PlaceBuilding)
                    continue;

                // No shake: this is not the player's mistake. The snapshot after the reconnect
                // shows whether the server applied the placement before the drop.
                _localBus.Publish(new BuildingPlacementResolvedEvent
                {
                    CommandId = pair.Key,
                    Result = CommandResultCode.Rejected
                });
            }

            _pendingCommands.Clear();
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

            if (!_pendingCommands.TryGetValue(ack.CommandId, out var pending))
                return;

            if (ack.ClientSequence != 0 && ack.ClientSequence != pending.ClientSequence)
                return;

            PublishPlacementResolution(ack, pending.Type);
            _localBus.Publish(new CommandResolvedEvent
            {
                CommandId = ack.CommandId,
                Type = pending.Type,
                Result = ack.Result,
                Reason = ack.Reason ?? string.Empty
            });

            if (ack.Result == CommandResultCode.Rejected)
            {
                TriggerRollbackForDelta(delta, ack.CommandId);
                _pendingCommands.Remove(ack.CommandId);
                return;
            }

            if (ack.Result == CommandResultCode.Accepted && HasAuthoritativeMismatch(delta, pending.Type))
                TriggerRollbackForDelta(delta, ack.CommandId);

            if (ack.Result == CommandResultCode.Accepted)
                _pendingCommands.Remove(ack.CommandId);
        }

        private bool HasAuthoritativeMismatch(StateDelta delta, CommandType commandType)
        {
            if (commandType != CommandType.PlaceBuilding || delta.Changes == null)
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

        private void PublishPlacementResolution(CommandAckDto ack, CommandType commandType)
        {
            if (commandType != CommandType.PlaceBuilding)
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
    }
}
