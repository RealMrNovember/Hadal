using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Serialization;
using Hadal.Core.Contracts;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using Hadal.Managers.Base;

namespace Hadal.Managers.StateSync
{
    /// <summary>
    /// Sprint 2 stub — keeps session state in <see cref="ClientStateView"/> RAM only.
    /// Server Protobuf snapshots replace this in a later sprint.
    /// </summary>
    public sealed class StateSyncService : IStateSyncService
    {
        private readonly ClientStateView _view = new();

        public ClientStateView View => _view;

        public uint ActiveSchemaVersion { get; private set; }

        public void Initialize()
        {
            ActiveSchemaVersion = SchemaVersion.Current;
            _view.ResetSession();
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
