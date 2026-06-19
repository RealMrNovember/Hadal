using UnityEngine;
using Hadal.Core.DI;
using Hadal.Core.Services;
using Hadal.Data.Config;

namespace Hadal.Managers.Base
{
    public interface IManager : IGameService
    {
        int Priority { get; }
        bool IsInitialized { get; }
        void Initialize(GameConfigSO config, GameServiceContainer container);
        void ResolveDependencies(GameServiceContainer container);
    }

    public abstract class ManagerBase : MonoBehaviour, IManager
    {
        [SerializeField] private int _priority;

        protected GameServiceContainer Container { get; private set; }
        protected GameConfigSO Config { get; private set; }

        public int Priority => _priority;
        public bool IsInitialized { get; private set; }

        public void Initialize(GameConfigSO config, GameServiceContainer container)
        {
            if (IsInitialized)
                return;

            Config = config;
            Container = container;
            OnInitialize(config);
            IsInitialized = true;
        }

        public virtual void ResolveDependencies(GameServiceContainer container) { }

        public void Initialize()
        {
            Debug.LogWarning($"[{name}] Initialize() called without bootstrap context.");
        }

        public virtual void Shutdown()
        {
            if (!IsInitialized)
                return;

            OnShutdown();
            IsInitialized = false;
            Container = null;
            Config = null;
        }

        protected abstract void OnInitialize(GameConfigSO config);
        protected virtual void OnShutdown() { }
    }
}
