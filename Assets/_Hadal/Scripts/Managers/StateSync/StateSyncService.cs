using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Serialization;
using Hadal.Core.Contracts;
using Hadal.Core.Events;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using Hadal.Managers;
using Hadal.Managers.Base;
// HADAL.Shared also defines a NetworkSerializationLayer; DI registers the Core one.
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Managers.StateSync
{
    /// <summary>
    /// Server-authoritative state replication into <see cref="ClientStateView"/> (RAM only).
    /// </summary>
    public sealed class StateSyncService : IStateSyncService
    {
        private readonly ClientStateView _view;
        private readonly NetworkSerializationLayer _serialization;
        private readonly CircularGridManager _gridManager;
        private readonly ILocalEventBus _localBus;

        public StateSyncService(
            ClientStateView view,
            NetworkSerializationLayer serialization,
            CircularGridManager gridManager,
            ILocalEventBus localBus)
        {
            _view = view;
            _serialization = serialization;
            _gridManager = gridManager;
            _localBus = localBus;
        }

        public ClientStateView View => _view;

        public uint ActiveSchemaVersion { get; private set; }

        public void Initialize()
        {
            ActiveSchemaVersion = SchemaVersion.Current;
            _view.ResetSession();
        }

        public void ApplySnapshot(StateSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            ClientStateViewMutator.ApplySnapshot(_view, snapshot, _serialization);
            SyncGridFromView();
            PublishResourceState();
        }

        public IReadOnlyList<CommandAckDto> ApplyDelta(StateDelta delta)
        {
            if (delta == null)
                return System.Array.Empty<CommandAckDto>();

            var commandAcks = new List<CommandAckDto>();
            ClientStateViewMutator.ApplyDelta(_view, delta, _serialization, commandAcks);
            SyncGridFromView();

            // Production ticks arrive several times a minute; only wake the HUD when the
            // frame actually carried economy state.
            if (CarriesEconomyState(delta))
                PublishResourceState();

            return commandAcks;
        }

        private void SyncGridFromView()
        {
            _gridManager.SyncOccupancyFromBuildings(_view.Data.buildings);
            _localBus.Publish(new BuildingStateChangedEvent());
        }

        private void PublishResourceState()
        {
            _localBus.Publish(new ResourceStateChangedEvent { ServerTick = _view.LastResourceTick });
        }

        private static bool CarriesEconomyState(StateDelta delta)
        {
            if (delta.Changes == null)
                return false;

            foreach (var change in delta.Changes)
            {
                if (change == null || string.IsNullOrEmpty(change.EntityId))
                    continue;

                if (change.EntityId.StartsWith(EntityIdPrefixes.Resource, StringComparison.Ordinal) ||
                    change.EntityId.StartsWith(EntityIdPrefixes.Production, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public void CaptureAll(IEnumerable<ISaveParticipant> participants)
        {
            foreach (var participant in participants)
                participant.CaptureSave(_view.Data);

            _view.MarkCaptured();
        }

        public void RestoreAll(IEnumerable<ISaveParticipant> participants)
        {
            if (!_view.HasPersistedData)
                return;

            foreach (var participant in OrderParticipants(participants))
                participant.RestoreSave(_view.Data);
        }

        public void Shutdown() { }

        private static IEnumerable<ISaveParticipant> OrderParticipants(IEnumerable<ISaveParticipant> participants)
        {
            return participants
                .OfType<ManagerBase>()
                .OrderBy(m => m.Priority)
                .Cast<ISaveParticipant>()
                .Concat(participants.Where(p => p is not ManagerBase));
        }
    }
}
