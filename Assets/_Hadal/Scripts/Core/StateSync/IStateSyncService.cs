using System.Collections.Generic;
using Hadal.Core.Contracts;
using Hadal.Core.Services;

namespace Hadal.Core.StateSync
{
    public interface IStateSyncService : IGameService
    {
        ClientStateView View { get; }

        uint ActiveSchemaVersion { get; }

        void Initialize();

        void CaptureAll(IEnumerable<ISaveParticipant> participants);

        void RestoreAll(IEnumerable<ISaveParticipant> participants);
    }
}
