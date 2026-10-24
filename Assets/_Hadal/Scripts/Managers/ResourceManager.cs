using System;
using System.Collections.Generic;
using UnityEngine;
using Hadal.Core.Contracts;
using Hadal.Core.Events;
using HADAL.Shared.Enums;
using Hadal.Core.StateSync;
using Hadal.Data.Config;
using Hadal.Data.Events;
using Hadal.Data.Models;
using Hadal.Managers.Base;
using VContainer;

namespace Hadal.Managers
{
    /// <summary>
    /// Presentation wallet. The server owns balances; this mirrors
    /// <see cref="ClientStateView"/> and exposes an optimistic <see cref="TrySpend"/> so
    /// build mode can respond before the ack arrives. Every replicated frame overwrites
    /// whatever the prediction did — by design.
    /// </summary>
    public class ResourceManager : ManagerBase, IResourceService, ISaveParticipant
    {
        [SerializeField] private ResourceChangedEventSO _resourceChangedEvent;

        private readonly ResourceWallet _wallet = new();
        private ResourceDatabaseSO _database;
        private IStateSyncService _stateSync;
        private ILocalEventBus _localBus;

        public ResourceWallet Wallet => _wallet;

        protected override void OnInitialize(GameConfigSO config)
        {
            _database = config.ResourceDatabase;
        }

        [Inject]
        public void InjectRuntime(IStateSyncService stateSync, ILocalEventBus localBus)
        {
            _stateSync = stateSync;
            _localBus = localBus;
        }

        public override void OnPostInject()
        {
            _localBus?.Subscribe<ResourceStateChangedEvent>(OnResourceStateChanged);
            SyncFromStateView();
        }

        private void OnResourceStateChanged(ResourceStateChangedEvent _) => SyncFromStateView();

        /// <summary>
        /// Rebuilds the wallet from the replicated view. Raises the UI channel per resource
        /// that actually moved so listeners are not woken every tick for nothing.
        /// </summary>
        public void SyncFromStateView()
        {
            if (_stateSync == null)
                return;

            var entries = _stateSync.View.Data.resources;
            var previous = new Dictionary<ResourceType, long>();

            foreach (var pair in _wallet.Snapshot())
                previous[pair.Key] = pair.Value;

            _wallet.Clear();

            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    var resource = ResourceTypeMap.FromStoredValue(entry.resourceType);
                    if (resource == ResourceType.Unknown)
                        continue;

                    _wallet.Set(resource, entry.amount);
                }
            }

            foreach (var pair in _wallet.Snapshot())
            {
                previous.TryGetValue(pair.Key, out var before);
                if (before != pair.Value)
                    _resourceChangedEvent?.Raise(pair.Key, before, pair.Value);

                previous.Remove(pair.Key);
            }

            // Resources that vanished from the replicated set fell to zero.
            foreach (var pair in previous)
            {
                if (pair.Value != 0L)
                    _resourceChangedEvent?.Raise(pair.Key, pair.Value, 0L);
            }
        }

        public long Get(ResourceType type) => _wallet.Get(type);

        /// <summary>
        /// Optimistic local deduction so build mode can react without a round trip.
        /// Authority still rests with the server ack; a rejected command is corrected by
        /// the next replicated balance frame.
        /// </summary>
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
            var entries = new List<ResourceSaveEntry>(snapshot.Count);

            foreach (var pair in snapshot)
            {
                if (pair.Key == ResourceType.Unknown)
                    continue;

                entries.Add(new ResourceSaveEntry
                {
                    resourceType = ResourceTypeMap.ToStoredValue(pair.Key),
                    amount = pair.Value
                });
            }

            data.resources = entries.ToArray();
        }

        public void RestoreSave(SaveGameData data)
        {
            _wallet.Clear();
            if (data.resources == null)
                return;

            foreach (var entry in data.resources)
            {
                var resource = ResourceTypeMap.FromStoredValue(entry.resourceType);
                if (resource != ResourceType.Unknown)
                    _wallet.Set(resource, entry.amount);
            }
        }

        protected override void OnShutdown()
        {
            _localBus?.Unsubscribe<ResourceStateChangedEvent>(OnResourceStateChanged);
        }
    }
}
