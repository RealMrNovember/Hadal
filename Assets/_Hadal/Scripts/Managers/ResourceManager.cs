using System;
using System.Collections.Generic;
using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Core.StateSync;
using Hadal.Core.Tick;
using Hadal.Data.Config;
using Hadal.Data.Enums;
using Hadal.Data.Events;
using Hadal.Data.Models;
using Hadal.Managers.Base;
using VContainer;

namespace Hadal.Managers
{
    public class ResourceManager : ManagerBase, IResourceService, ISaveParticipant, IGameTickable
    {
        [SerializeField] private ResourceChangedEventSO _resourceChangedEvent;

        private readonly ResourceWallet _wallet = new();
        private ResourceDatabaseSO _database;
        private float _tickTimer;
        private TickManager _tickManager;
        private IStateSyncService _stateSync;

        public int TickPriority => 10;
        public ResourceWallet Wallet => _wallet;

        protected override void OnInitialize(GameConfigSO config)
        {
            _database = config.ResourceDatabase;
        }

        [Inject]
        public void InjectRuntime(TickManager tickManager, IStateSyncService stateSync)
        {
            _tickManager = tickManager;
            _stateSync = stateSync;
        }

        public override void OnPostInject()
        {
            if (!_stateSync.View.HasPersistedData)
                SeedStartingResources();

            _tickManager?.Register(this);
        }

        public void Tick(float deltaTime)
        {
            _tickTimer += deltaTime;
            if (_tickTimer < Config.BaseTickIntervalSeconds)
                return;

            _tickTimer = 0f;
        }

        private void SeedStartingResources()
        {
            if (_database == null)
                return;

            foreach (var def in _database.Resources)
            {
                if (def != null)
                    _wallet.Add(def.Type, def.StartingAmount);
            }
        }

        public long Get(ResourceType type) => _wallet.Get(type);

        public bool TrySpend(IReadOnlyList<ResourceAmount> costs) => _wallet.TrySpend(costs);

        public void Add(ResourceType type, long amount)
        {
            var old = _wallet.Get(type);
            var cap = _database?.Get(type)?.MaxCapacity ?? long.MaxValue;
            var next = Math.Min(cap, old + amount);
            if (next == old)
                return;

            _wallet.Add(type, next - old);
            _resourceChangedEvent?.Raise(type, old, next);
        }

        public bool CanAfford(IReadOnlyList<ResourceAmount> costs) => _wallet.CanAfford(costs);

        public void CaptureSave(SaveGameData data)
        {
            var snapshot = _wallet.Snapshot();
            var entries = new ResourceSaveEntry[snapshot.Count];
            var index = 0;

            foreach (var pair in snapshot)
            {
                entries[index++] = new ResourceSaveEntry
                {
                    resourceType = (int)pair.Key,
                    amount = pair.Value
                };
            }

            data.resources = entries;
        }

        public void RestoreSave(SaveGameData data)
        {
            _wallet.Clear();
            if (data.resources == null)
                return;

            foreach (var entry in data.resources)
                _wallet.Set((ResourceType)entry.resourceType, entry.amount);
        }

        protected override void OnShutdown()
        {
            if (_tickManager != null)
                _tickManager.Unregister(this);
        }
    }
}
