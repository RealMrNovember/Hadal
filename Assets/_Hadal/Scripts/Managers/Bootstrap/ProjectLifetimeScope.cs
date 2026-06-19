using System;
using Hadal.Core.Contracts;
using Hadal.Core.Events;
using Hadal.Core.Network;
using Hadal.Core.State;
using Hadal.Core.StateSync;
using Hadal.Data.Config;
using Hadal.Managers.Addressables;
using Hadal.Managers.Base;
using Hadal.Managers.StateSync;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Hadal.Managers.Bootstrap
{
    /// <summary>
    /// Root VContainer scope — registers core services and scene managers.
    /// Gameplay MonoBehaviours under this hierarchy receive [Inject] via auto-inject.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameConfigSO _gameConfig;
        [SerializeField] private ManagerBase[] _managers = Array.Empty<ManagerBase>();
        [SerializeField] private Transform _poolRoot;
        [SerializeField] private SessionLifetimeScope _sessionScopePrefab;

        private SessionLifetimeScope _sessionScope;

        protected override void Awake()
        {
            autoInjectGameObjects = true;
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ILocalEventBus, LocalEventBusStub>(Lifetime.Singleton);
            builder.Register<INetworkEventBus, NetworkEventBusStub>(Lifetime.Singleton);
            builder.Register<IEventBus, EventBusService>(Lifetime.Singleton);

            builder.RegisterInstance(_gameConfig);
            builder.Register<GameStateMachine>(Lifetime.Singleton);
            builder.Register<ClientStateView>(Lifetime.Singleton);
            builder.Register<IStateSyncService, StateSyncService>(Lifetime.Singleton);

            builder.Register<NetworkSerializationLayer>(Lifetime.Singleton);
            builder.Register<IWebSocketClient, NativeWebSocketClient>(Lifetime.Singleton);
            builder.Register<ICommandDispatcher, CommandDispatcher>(Lifetime.Singleton);

            builder.RegisterBuildCallback(_ => CreateSessionScope());

            builder.Register<IPoolService>(resolver =>
            {
                var config = resolver.Resolve<GameConfigSO>();
                var root = _poolRoot != null ? _poolRoot : transform;
                var pool = new PoolService(config.MobilePerformanceConfig, root);
                pool.Initialize();
                return pool;
            }, Lifetime.Singleton);

            builder.Register<IAssetProvider>(resolver =>
            {
                var config = resolver.Resolve<GameConfigSO>();
                var maxCached = config.MobilePerformanceConfig != null
                    ? config.MobilePerformanceConfig.MaxCachedAddressables
                    : 64;
                var provider = new AddressableAssetProvider(config.AssetCatalog, maxCached);
                provider.Initialize();
                return provider;
            }, Lifetime.Singleton);

            foreach (var manager in _managers)
            {
                if (manager == null)
                    continue;

                RegisterManager(builder, manager);
            }

            builder.RegisterInstance(_managers);
            builder.RegisterEntryPoint<GameBootstrapEntryPoint>();
        }

        private void CreateSessionScope()
        {
            _sessionScope = _sessionScopePrefab != null
                ? Instantiate(_sessionScopePrefab, transform)
                : CreateSessionScopeInstance();

            using (LifetimeScope.EnqueueParent(this))
            {
                _sessionScope.Build();
            }
        }

        private SessionLifetimeScope CreateSessionScopeInstance()
        {
            var go = new GameObject("SessionLifetimeScope");
            go.transform.SetParent(transform, false);
            return go.AddComponent<SessionLifetimeScope>();
        }

        private static void RegisterManager(IContainerBuilder builder, ManagerBase manager)
        {
            switch (manager)
            {
                case ResourceManager resourceManager:
                    builder.RegisterComponent(resourceManager).AsSelf().AsImplementedInterfaces();
                    break;
                case GridManager gridManager:
                    builder.RegisterComponent(gridManager).AsSelf().AsImplementedInterfaces();
                    break;
                case BuildingManager buildingManager:
                    builder.RegisterComponent(buildingManager).AsSelf().AsImplementedInterfaces();
                    break;
                case PressureManager pressureManager:
                    builder.RegisterComponent(pressureManager).AsSelf().AsImplementedInterfaces();
                    break;
                case MapManager mapManager:
                    builder.RegisterComponent(mapManager).AsSelf().AsImplementedInterfaces();
                    break;
                default:
                    builder.RegisterComponent(manager).AsSelf();
                    break;
            }
        }
    }
}
