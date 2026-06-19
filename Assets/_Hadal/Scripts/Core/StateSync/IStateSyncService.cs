using System.Collections.Generic;
using HADAL.Shared.DTOs;
using Hadal.Core.Contracts;
using Hadal.Core.Services;

namespace Hadal.Core.StateSync
{
    public interface IStateSyncService : IGameService
    {
        ClientStateView View { get; }

        uint ActiveSchemaVersion { get; }

        void Initialize();

        void ApplySnapshot(StateSnapshot snapshot);

        IReadOnlyList<CommandAckDto> ApplyDelta(StateDelta delta);

        void CaptureAll(IEnumerable<ISaveParticipant> participants);

        void RestoreAll(IEnumerable<ISaveParticipant> participants);
    }
}
