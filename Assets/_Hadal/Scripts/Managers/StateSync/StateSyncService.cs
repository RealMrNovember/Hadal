using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Serialization;
using Hadal.Core.Contracts;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
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

        public StateSyncService(ClientStateView view, NetworkSerializationLayer serialization)
        {
            _view = view;
            _serialization = serialization;
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
        }

        public IReadOnlyList<CommandAckDto> ApplyDelta(StateDelta delta)
        {
            if (delta == null)
                return System.Array.Empty<CommandAckDto>();

            var commandAcks = new List<CommandAckDto>();
            ClientStateViewMutator.ApplyDelta(_view, delta, _serialization, commandAcks);
            return commandAcks;
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
