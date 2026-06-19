using System;
using System.Linq;
using Hadal.Core.Contracts;
using Hadal.Core.Events;
using Hadal.Core.State;
using Hadal.Core.StateSync;
using Hadal.Data.Config;
using Hadal.Gameplay.Bootstrap;
using Hadal.Managers.Base;
using UnityEngine;
using VContainer.Unity;

namespace Hadal.Managers.Bootstrap
{
    /// <summary>
    /// Application entry point: VContainer -> StateSync -> manager post-inject -> game loop.
    /// </summary>
    public sealed class GameBootstrapEntryPoint : IStartable, ITickable, IDisposable
    {
        private readonly GameStateMachine _stateMachine;
        private readonly IStateSyncService _stateSync;
        private readonly IEventBus _eventBus;
        private readonly IAssetProvider _assetProvider;
        private readonly IPoolService _poolService;
        private readonly GameConfigSO _config;
        private readonly ManagerBase[] _managers;
        private bool _initialized;

        public GameBootstrapEntryPoint(
            GameStateMachine stateMachine,
            IStateSyncService stateSync,
            IEventBus eventBus,
            IAssetProvider assetProvider,
            IPoolService poolService,
            GameConfigSO config,
            ManagerBase[] managers)
        {
            _stateMachine = stateMachine;
            _stateSync = stateSync;
            _eventBus = eventBus;
            _assetProvider = assetProvider;
            _poolService = poolService;
            _config = config;
            _managers = managers ?? Array.Empty<ManagerBase>();
        }

        public void Start()
        {
            if (_initialized)
                return;

            if (_config == null)
            {
                Debug.LogError("[GameBootstrap] GameConfigSO is not assigned on ProjectLifetimeScope.");
                return;
            }

            MobilePerformanceApplier.Apply(_config);
            MobilePerformanceApplier.Configure(_poolService);

            _stateSync.Initialize();

            foreach (var manager in _managers.Where(m => m != null).OrderBy(m => m.Priority))
                manager.OnPostInject();

            _stateSync.RestoreAll(_managers.OfType<ISaveParticipant>());

            RegisterGameplayStates();
            _assetProvider.PreloadCatalog();

            _stateMachine.ChangeState(GameStateType.MainMenu);
            _initialized = true;
            _eventBus.Publish(new GameInitializedEvent());
            Debug.Log("[GameBootstrap] VContainer bootstrap complete.");
        }

        public void Tick()
        {
            if (_initialized)
                _stateMachine.Tick(Time.deltaTime);
        }

        public void Dispose()
        {
            if (!_initialized)
                return;

            _stateSync.CaptureAll(_managers.OfType<ISaveParticipant>());
            _eventBus.Publish(new GameShutdownEvent());

            foreach (var manager in _managers.Where(m => m != null).OrderByDescending(m => m.Priority))
                manager.Shutdown();

            _assetProvider.Shutdown();
            _poolService.Shutdown();
            _stateSync.Shutdown();
            _eventBus.Shutdown();

            MobilePerformanceApplier.Shutdown();
            _initialized = false;
        }

        private void RegisterGameplayStates()
        {
            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IGameStateRegistration registration)
                    registration.Register(_stateMachine);
            }
        }
    }
}
