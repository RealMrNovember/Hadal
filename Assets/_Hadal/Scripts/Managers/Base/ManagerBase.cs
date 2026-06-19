using UnityEngine;
using Hadal.Data.Config;
using Hadal.Core.Services;
using VContainer;

namespace Hadal.Managers.Base
{
    public interface IManager : IGameService
    {
        int Priority { get; }
        bool IsInitialized { get; }
        void OnPostInject();
    }

    public abstract class ManagerBase : MonoBehaviour, IManager
    {
        [SerializeField] private int _priority;

        protected GameConfigSO Config { get; private set; }

        public int Priority => _priority;
        public bool IsInitialized { get; private set; }

        [Inject]
        public void InjectConfig(GameConfigSO config)
        {
            if (IsInitialized)
                return;

            Config = config;
            OnInitialize(config);
            IsInitialized = true;
        }

        public virtual void OnPostInject() { }

        public void Initialize() { }

        public virtual void Shutdown()
        {
            if (!IsInitialized)
                return;

            OnShutdown();
            IsInitialized = false;
            Config = null;
        }

        protected abstract void OnInitialize(GameConfigSO config);
        protected virtual void OnShutdown() { }
    }
}
