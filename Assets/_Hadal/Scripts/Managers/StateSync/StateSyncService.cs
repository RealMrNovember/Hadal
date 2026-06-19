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
        }

        public IReadOnlyList<CommandAckDto> ApplyDelta(StateDelta delta)
        {
            if (delta == null)
                return System.Array.Empty<CommandAckDto>();

            var commandAcks = new List<CommandAckDto>();
            ClientStateViewMutator.ApplyDelta(_view, delta, _serialization, commandAcks);
            SyncGridFromView();
            return commandAcks;
        }

        private void SyncGridFromView()
        {
            _gridManager.SyncOccupancyFromBuildings(_view.Data.buildings);
            _localBus.Publish(new BuildingStateChangedEvent());
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
