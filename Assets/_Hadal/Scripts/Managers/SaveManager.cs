using System.Collections.Generic;
using System.Linq;
using Hadal.Core.Contracts;
using Hadal.Core.StateSync;
using Hadal.Managers.Base;
using VContainer;

namespace Hadal.Managers
{
    public class SaveManager : ManagerBase
    {
        private IStateSyncService _stateSync;
        private ManagerBase[] _managers = System.Array.Empty<ManagerBase>();

        protected override void OnInitialize(Data.Config.GameConfigSO config) { }

        [Inject]
        public void InjectStateSync(IStateSyncService stateSync, ManagerBase[] managers)
        {
            _stateSync = stateSync;
            _managers = managers ?? System.Array.Empty<ManagerBase>();
        }

        public void SyncNow()
        {
            _stateSync?.CaptureAll(_managers.OfType<ISaveParticipant>());
        }
    }
}
