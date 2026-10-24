using System;
using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Hadal.Core.StateSync;
using Hadal.Data.Config;
using Hadal.Data.Models;
using UnityEngine;
using VContainer.Unity;

namespace Hadal.Core.Network
{
    /// <summary>
    /// Stub game server for Phase 1 Sprint 3 — runs the economy tick and emits resource
    /// <see cref="StateDelta"/> frames until the real server lands.
    /// <para>
    /// All arithmetic is delegated to <see cref="ProductionSimulator"/> in HADAL.Shared so the
    /// real server can drop this class and keep identical behaviour. The only thing that dies
    /// with this file is the scheduling, not the economy rules.
    /// </para>
    /// <para>
    /// It stands down the moment a real gateway socket is connected — a client and a server
    /// both minting resources would double every balance.
    /// </para>
    /// </summary>
    public sealed class LocalProductionTickSimulator : IProductionTickSimulator, IStartable, ITickable
    {
        private readonly IStateSyncService _stateSync;
        private readonly ICommandReconciliationSystem _reconciliation;
        private readonly NetworkSerializationLayer _serialization;
        private readonly IWebSocketClient _webSocket;
        private readonly GameConfigSO _config;

        private readonly Dictionary<ResourceType, long> _totals = new();
        private readonly Dictionary<ResourceType, long> _capacities = new();
        private readonly Dictionary<ResourceType, long> _netPerTick = new();
        private readonly List<BuildingProductionInput> _buildingScratch = new();

        private ProductionCatalog _catalog = ProductionCatalog.Empty;
        private DateTime _lastTickUtc;
        private DateTime _lastReplicationUtc;
        private ulong _serverTick;
        private ulong _unreplicatedFromTick;
        private bool _walletDirty;
        private bool _configured;
        private bool _seeded;

        public LocalProductionTickSimulator(
            IStateSyncService stateSync,
            ICommandReconciliationSystem reconciliation,
            NetworkSerializationLayer serialization,
            IWebSocketClient webSocket,
            GameConfigSO config)
        {
            _stateSync = stateSync;
            _reconciliation = reconciliation;
            _serialization = serialization;
            _webSocket = webSocket;
            _config = config;
        }

        public bool IsActive => _configured && !IsLiveGatewayConnected;

        public ulong SimulatedTick => _serverTick;

        private bool IsLiveGatewayConnected => _webSocket != null && _webSocket.IsConnected;

        public void Start()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            // Hard stop. This class derives accrual from the device clock, which a player can
            // set forward. That is acceptable in a dev loop standing in for the server and
            // never acceptable in a shipped build — see Forbidden Pattern #8.
            Debug.LogError(
                "[LocalProductionTickSimulator] Stub economy refused to start in a release build — the server owns production.");
            return;
#else
            if (_config == null || _config.BuildingDatabase == null)
            {
                Debug.LogWarning(
                    "[LocalProductionTickSimulator] GameConfigSO/BuildingDatabase missing — economy tick disabled.");
                return;
            }

            BuildCatalog();
            BuildCapacities();

            _lastTickUtc = DateTime.UtcNow;
            _lastReplicationUtc = _lastTickUtc;
            _configured = true;
#endif
        }

        public void Tick()
        {
            if (!IsActive)
                return;

            // Seeding waits for the first tick on purpose. GameBootstrapEntryPoint is
            // registered last, so its Start() — which calls StateSync.Initialize() and resets
            // the view — runs after ours. Seeding in Start() would be wiped immediately.
            if (!_seeded)
            {
                _seeded = true;
                SeedWallet();
                _lastTickUtc = DateTime.UtcNow;
                _lastReplicationUtc = _lastTickUtc;
                EmitSeedSnapshot();
                return;
            }

            var now = DateTime.UtcNow;
            var elapsedSeconds = (now - _lastTickUtc).TotalSeconds;

            // Device clock moved backwards (manual change, NTP correction). Never award
            // negative time; just re-anchor and wait for the next honest interval.
            if (elapsedSeconds < 0d)
            {
                _lastTickUtc = now;
                return;
            }

            var elapsedTicks = SimulationTick.TicksFromSeconds(elapsedSeconds);
            if (elapsedTicks == 0UL)
                return;

            var grantedTicks = SimulationTick.ClampOfflineTicks(elapsedTicks);

            // When the gap exceeds the offline cap the remainder is forfeited rather than
            // banked, so a long absence (or a forward clock jump) cannot be replayed later.
            _lastTickUtc = grantedTicks < elapsedTicks
                ? now
                : _lastTickUtc.AddSeconds(grantedTicks * (double)SimulationTick.IntervalSeconds);

            var isCatchUp = grantedTicks >= (ulong)SimulationTick.TicksPerReplication;
            Advance(grantedTicks);

            // Simulation runs every tick; replication is throttled separately so the economy
            // rate never changes when the network cadence is tuned. A catch-up after a long
            // gap always publishes immediately.
            if (!isCatchUp && (now - _lastReplicationUtc).TotalSeconds < SimulationTick.ReplicationIntervalSeconds)
                return;

            _lastReplicationUtc = now;
            PublishState();
        }

        public bool TryChargeBuildCost(string buildingDefinitionId, out EntityChangeDto walletChange)
        {
            walletChange = null;

            if (!IsActive)
                return true; // Stub economy is off; placement validation is somebody else's job.

            var definition = _config.BuildingDatabase.GetById(buildingDefinitionId);
            if (definition == null || definition.BuildCosts == null || definition.BuildCosts.Count == 0)
                return true;

            var charges = new List<KeyValuePair<ResourceType, long>>(definition.BuildCosts.Count);

            foreach (var cost in definition.BuildCosts)
            {
                if (cost.Amount <= 0L || cost.Type == ResourceType.Unknown)
                    continue;

                var resource = cost.Type;
                _totals.TryGetValue(resource, out var balance);
                if (balance < cost.Amount)
                    return false;

                charges.Add(new KeyValuePair<ResourceType, long>(resource, cost.Amount));
            }

            foreach (var charge in charges)
                _totals[charge.Key] = _totals[charge.Key] - charge.Value;

            if (charges.Count == 0)
                return true;

            _serverTick++;
            walletChange = BuildWalletChange();
            return true;
        }

        private void Advance(ulong ticks)
        {
            var buildings = CollectBuildings();
            var result = ProductionSimulator.Advance(_totals, buildings, _catalog, _capacities, ticks);

            _serverTick += ticks;
            _unreplicatedFromTick = _unreplicatedFromTick == 0UL ? _serverTick - ticks : _unreplicatedFromTick;

            _netPerTick.Clear();
            foreach (var pair in result.NetPerTick)
                _netPerTick[pair.Key] = pair.Value;

            if (!result.HasChanges)
                return;

            _totals.Clear();
            foreach (var pair in result.Totals)
                _totals[pair.Key] = pair.Value;

            _walletDirty = true;
        }

        private void PublishState()
        {
            var delta = new StateDelta
            {
                ServerTick = _serverTick,
                BaselineTick = _unreplicatedFromTick,
                Changes = new List<EntityChangeDto>()
            };

            if (_walletDirty)
            {
                delta.Changes.Add(BuildWalletChange());
                _walletDirty = false;
            }

            delta.Changes.Add(BuildProductionChange(_netPerTick));
            _unreplicatedFromTick = 0UL;

            Enqueue(NetworkInboundKind.StateDelta, delta, null);
        }

        /// <summary>
        /// First frame of the session: a full snapshot, the way a server answers a fresh login.
        /// Seeding through a snapshot keeps starting balances off the client's own books.
        /// </summary>
        private void EmitSeedSnapshot()
        {
            var buildings = _stateSync.View.Data.buildings ?? Array.Empty<BuildingSaveEntry>();
            var snapshot = new StateSnapshot
            {
                ServerTick = _serverTick,
                PlayerId = string.Empty,
                Resources = BuildResourceState(),
                Buildings = new List<BuildingStateDto>(buildings.Length),
                Production = BuildProductionState(
                    ProductionSimulator.ComputeNetPerTick(CollectBuildings(), _catalog))
            };

            foreach (var building in buildings)
            {
                snapshot.Buildings.Add(new BuildingStateDto
                {
                    EntityId = building.instanceKey,
                    DefinitionId = building.definitionId,
                    RingIndex = building.cellRing,
                    SectorIndex = building.cellSector,
                    Level = building.level,
                    RotationSteps = Mathf.RoundToInt(building.rotation / 90f)
                });
            }

            Enqueue(NetworkInboundKind.StateSnapshot, null, snapshot);
        }

        private void Enqueue(NetworkInboundKind kind, StateDelta delta, StateSnapshot snapshot)
        {
            _reconciliation.EnqueueInbound(new NetworkInboundPacket
            {
                Kind = kind,
                SchemaVersion = SchemaVersion.Current,
                ServerTick = _serverTick,
                BaselineTick = delta?.BaselineTick ?? 0UL,
                Delta = delta,
                Snapshot = snapshot
            });
        }

        private EntityChangeDto BuildWalletChange()
        {
            return new EntityChangeDto
            {
                EntityId = EntityIdPrefixes.WalletEntityId,
                Kind = EntityChangeKind.Update,
                Payload = _serialization.Serialize(BuildResourceState())
            };
        }

        private EntityChangeDto BuildProductionChange(IReadOnlyDictionary<ResourceType, long> netPerTick)
        {
            return new EntityChangeDto
            {
                EntityId = EntityIdPrefixes.ProductionEntityId,
                Kind = EntityChangeKind.Update,
                Payload = _serialization.Serialize(BuildProductionState(netPerTick))
            };
        }

        private ResourceStateDto BuildResourceState()
        {
            var dto = new ResourceStateDto { ServerTick = _serverTick };
            foreach (var pair in _totals)
                dto.Amounts[pair.Key] = pair.Value;

            return dto;
        }

        private ProductionStateDto BuildProductionState(IReadOnlyDictionary<ResourceType, long> netPerTick)
        {
            var dto = new ProductionStateDto
            {
                ServerTick = _serverTick,
                TickIntervalSeconds = SimulationTick.IntervalSeconds
            };

            if (netPerTick != null)
            {
                foreach (var pair in netPerTick)
                    dto.NetPerTick[pair.Key] = pair.Value;
            }

            return dto;
        }

        private IReadOnlyList<BuildingProductionInput> CollectBuildings()
        {
            _buildingScratch.Clear();

            var buildings = _stateSync.View.Data.buildings;
            if (buildings == null)
                return _buildingScratch;

            foreach (var building in buildings)
            {
                if (string.IsNullOrEmpty(building.definitionId))
                    continue;

                _buildingScratch.Add(new BuildingProductionInput(building.definitionId, building.level));
            }

            return _buildingScratch;
        }

        private void BuildCatalog()
        {
            var rules = new List<ProductionRuleDto>();

            foreach (var definition in _config.BuildingDatabase.Buildings)
            {
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                    continue;

                var perResource = new Dictionary<ResourceType, ProductionRuleDto>();

                Accumulate(perResource, definition.Id, definition.ProductionPerTick, isConsumption: false);
                Accumulate(perResource, definition.Id, definition.ConsumptionPerTick, isConsumption: true);

                rules.AddRange(perResource.Values);
            }

            _catalog = new ProductionCatalog(rules);
        }

        private static void Accumulate(
            IDictionary<ResourceType, ProductionRuleDto> target,
            string definitionId,
            IReadOnlyList<ResourceAmount> amounts,
            bool isConsumption)
        {
            if (amounts == null)
                return;

            foreach (var amount in amounts)
            {
                if (amount.Amount == 0L || amount.Type == ResourceType.Unknown)
                    continue;

                var resource = amount.Type;
                if (!target.TryGetValue(resource, out var rule))
                {
                    rule = new ProductionRuleDto
                    {
                        BuildingDefinitionId = definitionId,
                        Resource = resource
                    };
                    target[resource] = rule;
                }

                if (isConsumption)
                    rule.ConsumptionPerTick += amount.Amount;
                else
                    rule.ProductionPerTick += amount.Amount;
            }
        }

        private void BuildCapacities()
        {
            _capacities.Clear();

            var database = _config.ResourceDatabase;
            if (database == null)
                return;

            foreach (var definition in database.Resources)
            {
                if (definition == null || definition.Type == ResourceType.Unknown)
                    continue;

                _capacities[definition.Type] = definition.MaxCapacity;
            }
        }

        private void SeedWallet()
        {
            _totals.Clear();

            // Adopt whatever the view already holds (a real snapshot may have arrived first),
            // otherwise seed the starting grant the way a server would on first login.
            var existing = _stateSync.View.Data.resources;
            if (existing != null && existing.Length > 0)
            {
                foreach (var entry in existing)
                {
                    var resource = ResourceTypeMap.FromStoredValue(entry.resourceType);
                    if (resource != ResourceType.Unknown)
                        _totals[resource] = entry.amount;
                }

                return;
            }

            var database = _config.ResourceDatabase;
            if (database == null)
                return;

            foreach (var definition in database.Resources)
            {
                if (definition == null || definition.StartingAmount <= 0L)
                    continue;

                if (definition.Type != ResourceType.Unknown)
                    _totals[definition.Type] = definition.StartingAmount;
            }
        }
    }
}
