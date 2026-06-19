using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Core.DI;
using Hadal.Data.Config;
using Hadal.Managers.Base;

namespace Hadal.Managers
{
    public class SaveManager : ManagerBase
    {
        protected override void OnInitialize(GameConfigSO config) { }

        public override void ResolveDependencies(GameServiceContainer container) { }

        public void SaveNow()
        {
            if (Container != null && Container.TryResolve<ISaveService>(out var save))
                save.SaveAll();
        }
    }
}
