using System;
using System.Collections.Generic;
using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Core.DI;
using Hadal.Core.Events;
using Hadal.Core.State;
using Hadal.Core.Tick;
using Hadal.Data.Config;
using Hadal.Managers.Addressables;
using Hadal.Managers.Base;
using Hadal.Managers.Services;

namespace Hadal.Managers.Bootstrap
{
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _gameConfig;
        [SerializeField] private ManagerBase[] _managers = Array.Empty<ManagerBase>();
        [SerializeField] private Transform _poolRoot;

        private GameStateMachine _stateMachine;
        private GameServiceContainer _container;
        private GameContext _context;
        private EventBusService _eventBus;
        private SaveService _saveService;
        private PoolService _poolService;
        private AddressableAssetProvider _assetProvider;
        private TickManager _tickManager;
        private bool _initialized;

        public IGameContext Context => _context;

        private void Awake()
        {
            _stateMachine = new GameStateMachine();
            _container = new GameServiceContainer();
        }

        private void Start() => InitializeGame();

        private void Update()
        {
            if (_initialized)
                _stateMachine.Tick(Time.deltaTime);
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused || !_initialized)
                return;

            _saveService?.SaveAll();
        }

        private void OnDestroy() => ShutdownGame();
        private void OnApplicationQuit() => ShutdownGame();

        public void InitializeGame()
        {
            if (_initialized)
                return;

            if (_gameConfig == null)
            {
                Debug.LogError("[GameBootstrap] GameConfigSO is not assigned.");
                return;
            }

            MobilePerformanceApplier.Apply(_gameConfig);

            _eventBus = new EventBusService();
            _eventBus.Initialize();
            _container.Register<IEventBus>(_eventBus);

            _saveService = new SaveService();
            _saveService.Initialize();
            _container.Register<ISaveService>(_saveService);

            var poolRoot = _poolRoot != null ? _poolRoot : transform;
            _poolService = new PoolService(_gameConfig.MobilePerformanceConfig, poolRoot);
            _poolService.Initialize();
            _container.Register<IPoolService>(_poolService);

            _assetProvider = new AddressableAssetProvider(
                _gameConfig.AssetCatalog,
                _gameConfig.MobilePerformanceConfig != null
                    ? _gameConfig.MobilePerformanceConfig.MaxCachedAddressables
                    : 64);
            _assetProvider.Initialize();
            _container.Register<IAssetProvider>(_assetProvider);

            _context = new GameContext(_gameConfig, _stateMachine, _container);
            GameContext.SetCurrent(_context);

            Array.Sort(_managers, (a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var manager in _managers)
            {
                if (manager == null)
                    continue;

                manager.Initialize(_gameConfig, _container);
                RegisterManagerServices(manager);
            }

            foreach (var manager in _managers)
                manager?.ResolveDependencies(_container);

            RegisterSaveParticipants();
            RegisterGameplayStates();
            _assetProvider.PreloadCatalog();

            if (_saveService.HasSave)
                _saveService.TryLoadAll();

            _stateMachine.ChangeState(GameStateType.MainMenu);
            _container.Lock();
            _initialized = true;
            _eventBus.Publish(new GameInitializedEvent());
            Debug.Log("[GameBootstrap] HADAL production bootstrap complete.");
        }

        private void RegisterManagerServices(ManagerBase manager)
        {
            _container.Register(manager.GetType(), manager);

            switch (manager)
            {
                case ResourceManager resource:
                    _container.Register<IResourceService>(resource);
                    break;
                case GridManager grid:
                    _container.Register<ICircularGridService>(grid);
                    break;
                case BuildingManager building:
                    _container.Register<IBuildingService>(building);
                    break;
                case PressureManager pressure:
                    _container.Register<IPressureService>(pressure);
                    break;
                case TickManager tick:
                    _tickManager = tick;
                    break;
            }
        }

        private void RegisterGameplayStates()
        {
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IGameStateRegistration registration)
                    registration.Register(_stateMachine);
            }
        }

        private void RegisterSaveParticipants()
        {
            foreach (var manager in _managers)
            {
                if (manager is ISaveParticipant participant)
                    _saveService.RegisterParticipant(participant);
            }
        }

        private void ShutdownGame()
        {
            if (!_initialized)
                return;

            _saveService?.SaveAll();
            _eventBus?.Publish(new GameShutdownEvent());

            for (var i = _managers.Length - 1; i >= 0; i--)
                _managers[i]?.Shutdown();

            _assetProvider?.Shutdown();
            _poolService?.Shutdown();
            _saveService?.Shutdown();
            _eventBus?.Shutdown();

            MobilePerformanceApplier.Shutdown();
            _container?.Clear();
            GameContext.ClearCurrent();

            _initialized = false;
        }
    }
}
