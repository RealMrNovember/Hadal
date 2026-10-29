using Hadal.Game.Persistence;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>
    /// One player's authoritative simulation and its replication to the attached connection.
    /// Every state change happens under <see cref="_gate"/>; the economy only ever advances
    /// against the server clock.
    /// </summary>
    public sealed partial class PlayerSession
    {
        private readonly object _gate = new();
        private readonly PlayerState _state;
        private readonly CatalogRuntime _runtime;
        private readonly INetworkSerializer _serializer;

        private IClientConnection? _connection;
        private PlayerModifiers _modifiers = PlayerModifiers.None;
        private DateTimeOffset _lastReplicatedAt;
        private ulong _lastReplicatedTick;
        private bool _walletDirty;

        internal PlayerSession(PlayerState state, CatalogRuntime runtime, INetworkSerializer serializer)
        {
            _state = state;
            _runtime = runtime;
            _serializer = serializer;
            EnsureStarterHero();
            RefreshModifiers();
        }

        // The catalog can be swapped at runtime (GameWorld.ReplaceCatalog); always read it fresh.
        private GameCatalog _catalog => _runtime.Current.Catalog;
        private ProductionCatalog _production => _runtime.Current.Production;

        /// <summary>This player's storage caps: the catalog's, raised by research.</summary>
        /// <summary>This player's storage caps: the catalog's at the current reactor level, raised by research.</summary>
        private Dictionary<ResourceType, long> Capacities() => _modifiers.Capacities(_catalog.Capacities(ReactorLevel()));

        public Guid PlayerId => _state.PlayerId;

        public string IdentityKey => _state.IdentityKey;

        public IClientConnection? Connection
        {
            get { lock (_gate) return _connection; }
        }

        /// <summary>
        /// Binds a connection, folds in offline accrual (clamped) and sends the full snapshot.
        /// Returns the previous connection, if any, so the caller can close it.
        /// </summary>
        public IClientConnection? Attach(IClientConnection connection, DateTimeOffset now)
        {
            lock (_gate)
            {
                var previous = _connection;
                _connection = connection;

                AdvanceTo(now);
                // A base built before the copy limits applied is brought within them first.
                var refunds = EnforceBuildingLimits();
                _lastReplicatedAt = now;
                _lastReplicatedTick = _state.ServerTick;
                _walletDirty = false;

                connection.Send(EnvelopeBuilder.WrapServerMessage(BuildSnapshotEnvelope(now)));
                if (refunds.Count > 0)
                    SendDelta(now, new List<EntityChangeDto> { LimitRefundReport(refunds) });
                return previous;
            }
        }

        /// <summary>Detaches only if <paramref name="connection"/> is still the current one.</summary>
        public bool Detach(IClientConnection connection, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (!ReferenceEquals(_connection, connection))
                    return false;

                AdvanceTo(now);
                _connection = null;
                return true;
            }
        }

        /// <summary>Called every simulation tick by the world.</summary>
        public void Tick(DateTimeOffset now)
        {
            lock (_gate)
            {
                RollDailyIfNeeded(now);
                var changes = AdvanceTo(now);
                if (changes.Count > 0)
                {
                    SendDelta(now, changes);
                    return;
                }

                if (_connection == null || !_walletDirty)
                    return;

                if ((now - _lastReplicatedAt).TotalSeconds < SimulationTick.ReplicationIntervalSeconds)
                    return;

                SendDelta(now, new List<EntityChangeDto> { WalletChange(), ProductionChange(now) });
            }
        }

        public void HandleCommand(CommandEnvelope envelope, DateTimeOffset now)
        {
            lock (_gate)
            {
                var changes = AdvanceTo(now);
                if (changes.Count > 0)
                    SendDelta(now, changes);
                var hadaliteBefore = _state.Wallet.TryGetValue(ResourceType.Hadalite, out var h0) ? h0 : 0;
                Dispatch(envelope, now);
                var hadaliteAfter = _state.Wallet.TryGetValue(ResourceType.Hadalite, out var h1) ? h1 : 0;
                Score(EventActions.HadaliteSpent, hadaliteBefore - hadaliteAfter);
            }
        }

        private void Dispatch(CommandEnvelope envelope, DateTimeOffset now)
        {
            {
                switch (envelope.Type)
                {
                    case CommandType.PlaceBuilding when TryDeserialize(envelope.Payload, out PlaceBuildingCommand? place):
                        PlaceBuilding(place!, now);
                        break;

                    case CommandType.UpgradeBuilding when TryDeserialize(envelope.Payload, out UpgradeBuildingCommand? upgrade):
                        UpgradeBuilding(upgrade!, now);
                        break;

                    case CommandType.FinishConstruction when TryDeserialize(envelope.Payload, out FinishConstructionCommand? finish):
                        FinishConstruction(finish!, now);
                        break;

                    case CommandType.RemoveBuilding when TryDeserialize(envelope.Payload, out RemoveBuildingCommand? remove):
                        RemoveBuilding(remove!, now);
                        break;

                    case CommandType.UnlockRing when TryDeserialize(envelope.Payload, out UnlockRingCommand? unlock):
                        UnlockRing(unlock!, now);
                        break;

                    case CommandType.StartResearch when TryDeserialize(envelope.Payload, out StartResearchCommand? research):
                        StartResearch(research!, now);
                        break;

                    case CommandType.FinishResearch when TryDeserialize(envelope.Payload, out FinishResearchCommand? finishResearch):
                        FinishResearch(finishResearch!, now);
                        break;

                    case CommandType.BoostBuilding when TryDeserialize(envelope.Payload, out BoostBuildingCommand? boost):
                        Boost(boost!, now);
                        break;

                    case CommandType.Overdrive when TryDeserialize(envelope.Payload, out OverdriveCommand? overdrive):
                        Overdrive(overdrive!, now);
                        break;

                    case CommandType.StartSalvage when TryDeserialize(envelope.Payload, out StartSalvageCommand? salvage):
                        StartSalvage(salvage!, now);
                        break;

                    case CommandType.ExecuteGacha when TryDeserialize(envelope.Payload, out ExecuteGachaCommand? gacha):
                        Pull(gacha!, now);
                        break;

                    case CommandType.LevelUpHero when TryDeserialize(envelope.Payload, out LevelUpHeroCommand? levelUp):
                        LevelUpHero(levelUp!, now);
                        break;

                    case CommandType.PromoteHero when TryDeserialize(envelope.Payload, out PromoteHeroCommand? promote):
                        PromoteHero(promote!, now);
                        break;

                    case CommandType.AssignHero when TryDeserialize(envelope.Payload, out AssignHeroCommand? assign):
                        AssignHero(assign!, now);
                        break;

                    case CommandType.ClaimQuest when TryDeserialize(envelope.Payload, out ClaimQuestCommand? claim):
                        ClaimQuest(claim!, now);
                        break;

                    case CommandType.ClaimDailyChest when TryDeserialize(envelope.Payload, out ClaimDailyChestCommand? chest):
                        ClaimDailyChest(chest!, now);
                        break;

                    case CommandType.UseShield when TryDeserialize(envelope.Payload, out UseShieldCommand? shield):
                        UseShield(shield!, now);
                        break;

                    case CommandType.OpenItem when TryDeserialize(envelope.Payload, out OpenItemCommand? open):
                        OpenItem(open!, now);
                        break;

                    case CommandType.RecruitHero when TryDeserialize(envelope.Payload, out RecruitHeroCommand? recruit):
                        RecruitHero(recruit!, now);
                        break;

                    case CommandType.TrainTroops when TryDeserialize(envelope.Payload, out TrainTroopsCommand? train):
                        TrainTroops(train!, now);
                        break;

                    case CommandType.CancelTraining when TryDeserialize(envelope.Payload, out CancelTrainingCommand? cancel):
                        CancelTraining(cancel!, now);
                        break;

                    case CommandType.UseSpeedup when TryDeserialize(envelope.Payload, out UseSpeedupCommand? speedup):
                        UseSpeedup(speedup!, now);
                        break;

                    case CommandType.ClaimVipLogin when TryDeserialize(envelope.Payload, out ClaimVipLoginCommand? vipLogin):
                        ClaimVipLogin(vipLogin!, now);
                        break;

                    case CommandType.ClaimVipChest when TryDeserialize(envelope.Payload, out ClaimVipChestCommand? vipChest):
                        ClaimVipChest(vipChest!, now);
                        break;

                    case CommandType.HealTroops when TryDeserialize(envelope.Payload, out HealTroopsCommand? heal):
                        HealTroops(heal!, now);
                        break;

                    case CommandType.CancelHeal when TryDeserialize(envelope.Payload, out CancelHealCommand? cancelHeal):
                        CancelHeal(cancelHeal!, now);
                        break;

                    case CommandType.SetProfile when TryDeserialize(envelope.Payload, out SetProfileCommand? profile):
                        SetProfile(profile!, now);
                        break;

                    case CommandType.DiveStage when TryDeserialize(envelope.Payload, out DiveStageCommand? dive):
                        DiveStage(dive!, now);
                        break;

                    default:
                        var known = envelope.Type is CommandType.PlaceBuilding or CommandType.UpgradeBuilding or CommandType.FinishConstruction
                            or CommandType.RemoveBuilding or CommandType.UnlockRing or CommandType.StartResearch or CommandType.FinishResearch
                            or CommandType.BoostBuilding or CommandType.Overdrive or CommandType.StartSalvage or CommandType.ExecuteGacha
                            or CommandType.LevelUpHero or CommandType.PromoteHero or CommandType.AssignHero
                            or CommandType.ClaimQuest or CommandType.ClaimDailyChest or CommandType.OpenItem or CommandType.RecruitHero
                            or CommandType.DiveStage or CommandType.SetProfile or CommandType.TrainTroops or CommandType.CancelTraining
                            or CommandType.HealTroops or CommandType.CancelHeal or CommandType.UseSpeedup
                            or CommandType.ClaimVipLogin or CommandType.ClaimVipChest;
                        Reject(envelope.CommandId, envelope.ClientSequence, known ? "MalformedCommand" : "NotSupported", now);
                        break;
                }
            }
        }

        /// <summary>Settles the economy up to now under the current rules (before a catalog swap).</summary>
        internal void Settle(DateTimeOffset now)
        {
            lock (_gate)
            {
                var changes = AdvanceTo(now);
                if (changes.Count > 0)
                    SendDelta(now, changes);
            }
        }

        /// <summary>Recomputes rates after a catalog swap and pushes them.</summary>
        internal void ApplyCatalogChange(DateTimeOffset now)
        {
            lock (_gate)
            {
                EnsureStarterHero();
                RefreshModifiers();
                var before = _state.Buildings.Select(b => b.BuildingId).ToList();
                var refunds = EnforceBuildingLimits();
                var changes = new List<EntityChangeDto> { WalletChange(), ProductionChange(now), ResearchChange(now), HeroesChange(now) };
                if (refunds.Count > 0)
                {
                    var kept = new HashSet<string>(_state.Buildings.Select(b => b.BuildingId));
                    changes.AddRange(before.Where(id => !kept.Contains(id))
                        .Select(id => new EntityChangeDto { EntityId = EntityIds.Building + id, Kind = EntityChangeKind.Delete }));
                    changes.Add(InventoryChange());
                    changes.Add(LimitRefundReport(refunds));
                }

                SendDelta(now, changes);
            }
        }

        /// <summary>Copy of wallet and buildings for the Overseer (taken under the session lock).</summary>
        internal PlayerState Describe(DateTimeOffset now)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                var copy = new PlayerState
                {
                    PlayerId = _state.PlayerId,
                    IdentityKey = _state.IdentityKey,
                    ServerTick = _state.ServerTick,
                    LastSimulatedAt = _state.LastSimulatedAt,
                    UnlockedRings = _state.UnlockedRings
                };
                foreach (var pair in _state.Wallet)
                    copy.Wallet[pair.Key] = pair.Value;
                foreach (var b in _state.Buildings)
                    copy.Buildings.Add(new PlacedBuilding
                    {
                        BuildingId = b.BuildingId, DefinitionId = b.DefinitionId, Ring = b.Ring, Sector = b.Sector,
                        Level = b.Level, RotationSteps = b.RotationSteps, ConstructionEndsAt = b.ConstructionEndsAt,
                        ConstructionSeconds = b.ConstructionSeconds
                    });
                foreach (var pair in _state.ResearchLevels)
                    copy.ResearchLevels[pair.Key] = pair.Value;
                foreach (var pair in _state.Items)
                    copy.Items[pair.Key] = pair.Value;
                foreach (var pair in _state.Troops)
                    copy.Troops[pair.Key] = pair.Value;
                return copy;
            }
        }

        /// <summary>
        /// Overseer grant and coupons: as packages, each resource lands in the bag as one capsule of exactly that amount
        /// (Last Asylum's resource items; opening it may pass the storage cap); otherwise it goes straight to the wallet,
        /// past the cap as well, so nothing given is ever lost. Negative amounts take away (never below zero).
        /// </summary>
        internal void Grant(IReadOnlyDictionary<ResourceType, long> amounts, DateTimeOffset now, bool asPackages = false,
            IReadOnlyDictionary<string, int>? items = null)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                ApplyGrant(_state, amounts, asPackages, items);
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { WalletChange(), InventoryChange(), ProductionChange(now) });
            }
        }

        internal static void ApplyGrant(PlayerState state, IReadOnlyDictionary<ResourceType, long> amounts, bool asPackages,
            IReadOnlyDictionary<string, int>? items = null)
        {
            foreach (var pair in items ?? new Dictionary<string, int>())
                state.Items[pair.Key] = (state.Items.TryGetValue(pair.Key, out var have) ? have : 0) + pair.Value;

            foreach (var pair in amounts)
            {
                if (pair.Value == 0)
                    continue;
                if (asPackages && pair.Value > 0 && Capsules.Holds(pair.Key))
                {
                    var id = Capsules.Id(pair.Key, pair.Value);
                    state.Items[id] = (state.Items.TryGetValue(id, out var held) ? held : 0) + 1;
                    continue;
                }

                var have = state.Wallet.TryGetValue(pair.Key, out var w) ? w : 0;
                state.Wallet[pair.Key] = Math.Max(0, have + pair.Value);
            }
        }

        /// <summary>Copies state for persistence and clears the unsaved markers. Call outside the save I/O.</summary>
        /// <summary>The operator removed this player; nothing may write it back.</summary>
        public bool IsDeleted { get; private set; }

        internal void MarkDeleted()
        {
            lock (_gate)
                IsDeleted = true;
        }

        public PlayerState? TakeSaveSnapshot()
        {
            lock (_gate)
            {
                if (!_state.IsDirty || IsDeleted)
                    return null;

                var copy = new PlayerState
                {
                    PlayerId = _state.PlayerId,
                    IdentityKey = _state.IdentityKey,
                    ServerTick = _state.ServerTick,
                    LastSimulatedAt = _state.LastSimulatedAt,
                    UnlockedRings = _state.UnlockedRings,
                    ExtrasJson = PlayerExtras.Write(_state)
                };

                foreach (var pair in _state.Wallet)
                    copy.Wallet[pair.Key] = pair.Value;

                copy.UnsavedBuildings.AddRange(_state.UnsavedBuildings);
                _state.UnsavedBuildings.Clear();
                copy.RemovedBuildingIds.AddRange(_state.RemovedBuildingIds);
                _state.RemovedBuildingIds.Clear();
                _state.IsDirty = false;
                return copy;
            }
        }

        /// <summary>Re-queues buildings from a failed save so the next attempt writes them.</summary>
        public void RestoreAfterFailedSave(PlayerState snapshot)
        {
            lock (_gate)
            {
                // A building removed after this snapshot was taken must not come back.
                _state.UnsavedBuildings.InsertRange(0, snapshot.UnsavedBuildings.Where(b => !_state.RemovedBuildingIds.Contains(b.BuildingId)));
                _state.RemovedBuildingIds.InsertRange(0, snapshot.RemovedBuildingIds);
                _state.IsDirty = true;
            }
        }

        internal PlayerState StateForTests
        {
            get { lock (_gate) return _state; }
        }

        // --- Building commands ------------------------------------------------------------------

        private void PlaceBuilding(PlaceBuildingCommand command, DateTimeOffset now)
        {
            var occupied = new HashSet<(int ring, int sector)>(_state.Buildings.Select(b => (b.Ring, b.Sector)));
            var verdict = PlacementRules.Validate(_catalog, command.BuildingDefinitionId, command.RingIndex,
                command.SectorIndex, _state.UnlockedRings, occupied, _state.Wallet, OwnedCounts(), ReactorLevel(), _modifiers);

            if (verdict != PlacementRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            if (ActiveConstructions() >= Builders())
            {
                Reject(command.CommandId, command.ClientSequence, ConstructionRejectReason.BuilderBusy.ToString(), now);
                return;
            }

            _catalog.TryGetBuilding(command.BuildingDefinitionId, out var definition);
            BuildingProgression.Charge(BuildingProgression.Cost(definition, 1, _catalog.Progression), _state.Wallet);
            var seconds = _modifiers.BuildSeconds(BuildingProgression.Seconds(definition, 1, _catalog.Progression));

            // A new building works only once its first construction completes (level 0 until then).
            var building = new PlacedBuilding
            {
                BuildingId = "b" + Guid.NewGuid().ToString("N"),
                DefinitionId = definition.Id,
                Ring = command.RingIndex,
                Sector = command.SectorIndex,
                Level = seconds > 0 ? 0 : 1,
                RotationSteps = command.RotationSteps,
                ConstructionEndsAt = seconds > 0 ? now.AddSeconds(seconds) : null,
                ConstructionSeconds = seconds
            };

            _state.Buildings.Add(building);
            MarkChanged(building);
            Record("upgrade");

            SendDelta(now, new List<EntityChangeDto>
            {
                new()
                {
                    EntityId = EntityIds.Building + building.BuildingId,
                    Kind = EntityChangeKind.Create,
                    Payload = _serializer.Serialize(ToDto(building, now))
                },
                WalletChange(),
                ProductionChange(now),
                Accepted(command.CommandId, command.ClientSequence)
            });
        }

        private void UpgradeBuilding(UpgradeBuildingCommand command, DateTimeOffset now)
        {
            var building = FindBuilding(command.BuildingEntityId);
            if (building == null)
            {
                Reject(command.CommandId, command.ClientSequence, ConstructionRejectReason.UnknownBuilding.ToString(), now);
                return;
            }

            var verdict = BuildingProgression.ValidateUpgrade(_catalog, building.DefinitionId, building.Level,
                building.IsUnderConstruction, ActiveConstructions(), ReactorLevel(), _state.Wallet, _modifiers.ExtraBuilders, HighestLevel);
            if (verdict != ConstructionRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            _catalog.TryGetBuilding(building.DefinitionId, out var definition);
            var target = building.Level + 1;
            BuildingProgression.Charge(BuildingProgression.Cost(definition, target, _catalog.Progression), _state.Wallet);
            var seconds = _modifiers.BuildSeconds(BuildingProgression.Seconds(definition, target, _catalog.Progression));

            Record("upgrade");

            // The building keeps producing at its current level while it is being upgraded.
            if (seconds > 0)
            {
                building.ConstructionEndsAt = now.AddSeconds(seconds);
                building.ConstructionSeconds = seconds;
            }
            else
            {
                building.Level = target;
            }

            MarkChanged(building);
            SendDelta(now, new List<EntityChangeDto>
            {
                BuildingChange(building, now),
                WalletChange(),
                ProductionChange(now),
                Accepted(command.CommandId, command.ClientSequence)
            });
        }

        private void FinishConstruction(FinishConstructionCommand command, DateTimeOffset now)
        {
            var building = FindBuilding(command.BuildingEntityId);
            ConstructionRejectReason verdict;
            if (building == null)
                verdict = ConstructionRejectReason.UnknownBuilding;
            else if (!building.IsUnderConstruction)
                verdict = ConstructionRejectReason.NotUnderConstruction;
            else if ((building.ConstructionEndsAt!.Value - now).TotalSeconds > _catalog.Progression.freeFinishSeconds)
                verdict = ConstructionRejectReason.NotYetFree;
            else
                verdict = ConstructionRejectReason.None;

            if (verdict != ConstructionRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            CompleteConstruction(building!);
            SendDelta(now, new List<EntityChangeDto>
            {
                BuildingChange(building!, now),
                WalletChange(),
                ProductionChange(now),
                Accepted(command.CommandId, command.ClientSequence)
            });
        }

        private void RemoveBuilding(RemoveBuildingCommand command, DateTimeOffset now)
        {
            var building = FindBuilding(command.BuildingEntityId);
            var verdict = building == null
                ? RemoveRejectReason.UnknownBuilding
                : BaseRules.ValidateRemove(building.DefinitionId, building.Level, building.IsUnderConstruction);
            if (verdict != RemoveRejectReason.None || !_catalog.TryGetBuilding(building!.DefinitionId, out var definition))
            {
                Reject(command.CommandId, command.ClientSequence,
                    (verdict == RemoveRejectReason.None ? RemoveRejectReason.UnknownBuilding : verdict).ToString(), now);
                return;
            }

            // Cancelling a first construction refunds it all; a finished building gives back a share.
            var refund = BaseRules.Refund(definition, building.IsUnderConstruction ? 0 : building.Level, _catalog.Progression);
            WalletOps.Add(_state.Wallet, refund.ToDictionary(p => p.Key, p => p.Value), Capacities());

            _state.Buildings.Remove(building);
            _state.UnsavedBuildings.Remove(building);
            _state.RemovedBuildingIds.Add(building.BuildingId);
            _state.IsDirty = true;

            SendDelta(now, new List<EntityChangeDto>
            {
                new() { EntityId = EntityIds.Building + building.BuildingId, Kind = EntityChangeKind.Delete },
                WalletChange(),
                ProductionChange(now),
                Accepted(command.CommandId, command.ClientSequence)
            });
        }

        private void UnlockRing(UnlockRingCommand command, DateTimeOffset now)
        {
            var finished = new HashSet<string>(_state.Buildings.Where(b => b.Level > 0).Select(b => b.DefinitionId));
            var verdict = BaseRules.ValidateUnlock(_catalog, command.Ring, _state.UnlockedRings, ReactorLevel(), finished, _state.Wallet);
            if (verdict != RingRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            _catalog.TryGetRingUnlock(command.Ring, out var unlock);
            BuildingProgression.Charge(unlock.Cost, _state.Wallet);
            _state.UnlockedRings = command.Ring;
            _state.IsDirty = true;

            SendDelta(now, new List<EntityChangeDto>
            {
                LayoutChange(),
                WalletChange(),
                Accepted(command.CommandId, command.ClientSequence)
            });
        }

        // --- Building functions (docs/24) --------------------------------------------------------

        private void StartResearch(StartResearchCommand command, DateTimeOffset now)
        {
            var inProgress = new HashSet<string>(_state.ActiveResearch.Select(a => a.NodeId), StringComparer.Ordinal);
            var verdict = ResearchRules.Validate(_catalog, command.NodeId, _state.ResearchLevels, inProgress,
                CenterLevel(), _modifiers, _state.Wallet);
            if (verdict != ResearchRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            _catalog.TryGetResearch(command.NodeId, out var node);
            var target = (_state.ResearchLevels.TryGetValue(node.Id, out var level) ? level : 0) + 1;
            BuildingProgression.Charge(ResearchRules.Cost(node, target, _catalog.Research), _state.Wallet);
            var seconds = _modifiers.ResearchSeconds(ResearchRules.BaseSeconds(node, target, _catalog.Research));

            Record("research");
            if (seconds > 0)
                _state.ActiveResearch.Add(new ActiveResearch { NodeId = node.Id, TargetLevel = target, EndsAt = now.AddSeconds(seconds), Seconds = seconds });
            else
                CompleteResearch(node.Id, target);

            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto>
            {
                ResearchChange(now),
                WalletChange(),
                ProductionChange(now),
                Accepted(command.CommandId, command.ClientSequence)
            });
        }

        private void FinishResearch(FinishResearchCommand command, DateTimeOffset now)
        {
            var active = _state.ActiveResearch.FirstOrDefault(a => a.NodeId == command.NodeId);
            if (active == null || (active.EndsAt - now).TotalSeconds > _catalog.Progression.freeFinishSeconds)
            {
                Reject(command.CommandId, command.ClientSequence,
                    (active == null ? ResearchRejectReason.NotResearching : ResearchRejectReason.NotYetFree).ToString(), now);
                return;
            }

            _state.ActiveResearch.Remove(active);
            CompleteResearch(active.NodeId, active.TargetLevel);
            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto>
            {
                ResearchChange(now),
                WalletChange(),
                ProductionChange(now),
                Accepted(command.CommandId, command.ClientSequence)
            });
        }

        private void Boost(BoostBuildingCommand command, DateTimeOffset now)
        {
            var building = FindBuilding(command.BuildingEntityId);
            var definition = Definition(building);
            var verdict = BuildingFunctions.ValidateBoost(_catalog, definition, building?.Level ?? 0,
                building?.BoostEndsAt > now, _state.Wallet);
            if (verdict != FunctionRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            BuildingProgression.Charge(_catalog.BoostCost, _state.Wallet);
            building!.BoostEndsAt = now.AddSeconds(Math.Max(1, _catalog.Boost.seconds));
            Record("boost");
            _state.IsDirty = true;
            SendFunctionAccepted(command.CommandId, command.ClientSequence, building, now);
        }

        private void Overdrive(OverdriveCommand command, DateTimeOffset now)
        {
            var building = FindBuilding(command.BuildingEntityId);
            var verdict = BuildingFunctions.ValidateOverdrive(Definition(building), building?.Level ?? 0,
                building?.OverdriveEndsAt > now, building?.OverdriveReadyAt > now);
            if (verdict != FunctionRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            var rules = _catalog.Overdrive;
            building!.OverdriveEndsAt = now.AddSeconds(Math.Max(1, rules.seconds));
            building.OverdriveReadyAt = now.AddSeconds(Math.Max(rules.seconds, rules.cooldownSeconds));
            _state.IsDirty = true;
            SendFunctionAccepted(command.CommandId, command.ClientSequence, building, now);
        }

        private void StartSalvage(StartSalvageCommand command, DateTimeOffset now)
        {
            var building = FindBuilding(command.BuildingEntityId);
            var verdict = BuildingFunctions.ValidateSalvage(_catalog, Definition(building), building?.Level ?? 0,
                building?.MissionEndsAt != null, command.MissionId);
            if (verdict != FunctionRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            _catalog.TryGetSalvageMission(command.MissionId, out var mission);
            building!.MissionId = mission.Id;
            building.MissionSeconds = mission.Seconds;
            building.MissionEndsAt = now.AddSeconds(mission.Seconds);
            building.MissionSeed = Random.Shared.Next();
            Record("salvage");
            Score(EventActions.Salvage, 1);
            _state.IsDirty = true;
            SendFunctionAccepted(command.CommandId, command.ClientSequence, building, now);
        }

        private void SendFunctionAccepted(string commandId, ulong sequence, PlacedBuilding building, DateTimeOffset now) =>
            SendDelta(now, new List<EntityChangeDto>
            {
                BuildingChange(building, now),
                WalletChange(),
                ProductionChange(now),
                Accepted(commandId, sequence)
            });

        // --- Simulation ---------------------------------------------------------------------------

        /// <summary>
        /// Brings the economy up to <paramref name="now"/>. Every timed thing that ends on the way
        /// (construction, research, boost, overdrive, salvage run) splits the interval, so rates change
        /// at the exact moment — also while the player was offline. Gaps beyond the offline cap are
        /// forfeited (not banked) before splitting. Returns the changes to replicate (empty if none).
        /// </summary>
        private List<EntityChangeDto> AdvanceTo(DateTimeOffset now)
        {
            var changedBuildings = new List<PlacedBuilding>();
            var reports = new List<EntityChangeDto>();
            var researchChanged = false;

            var elapsed = (now - _state.LastSimulatedAt).TotalSeconds;
            if (elapsed < 0)
            {
                // Server clock went backwards (NTP step). Never award negative time.
                _state.LastSimulatedAt = now;
                return new List<EntityChangeDto>();
            }

            var maxSeconds = SimulationTick.SecondsFromTicks((ulong)SimulationTick.MaxOfflineAccrualTicks);
            if (elapsed > maxSeconds)
            {
                _state.LastSimulatedAt = now.AddSeconds(-maxSeconds);
                _state.IsDirty = true;
            }

            while (NextEvent(now) is { } at)
            {
                if (at > _state.LastSimulatedAt)
                    AdvanceEconomy(at);

                foreach (var building in _state.Buildings)
                {
                    var changed = false;
                    if (building.ConstructionEndsAt <= at)
                    {
                        CompleteConstruction(building);
                        changed = true;
                    }

                    if (building.BoostEndsAt <= at)
                    {
                        building.BoostEndsAt = null;
                        changed = true;
                    }

                    if (building.OverdriveEndsAt <= at)
                    {
                        building.OverdriveEndsAt = null;
                        changed = true;
                    }

                    if (building.MissionEndsAt <= at)
                    {
                        reports.Add(CompleteSalvage(building));
                        changed = true;
                    }

                    if (changed && !changedBuildings.Contains(building))
                        changedBuildings.Add(building);
                }

                foreach (var march in _state.Marches.Where(m => m.Battle is { Reported: false } && m.ArrivesAt <= at).ToList())
                    reports.Add(Fight(march));

                foreach (var march in _state.Marches.Where(m => m.HomeAt <= at && !AwaitsBattle(m)).ToList())
                    CompleteMarch(march);

                foreach (var training in _state.Training.Where(t => t.EndsAt <= at).ToList())
                    CompleteTraining(training);
                if (_state.Healing is { } healing && healing.EndsAt <= at)
                    CompleteHeal(healing);

                foreach (var research in _state.ActiveResearch.Where(a => a.EndsAt <= at).ToList())
                {
                    _state.ActiveResearch.Remove(research);
                    CompleteResearch(research.NodeId, research.TargetLevel);
                    researchChanged = true;
                }

                _state.IsDirty = true;
            }

            AdvanceEconomy(now);

            var changes = new List<EntityChangeDto>();
            if (_marchesDirty)
                changes.Add(MarchesChange(now));
            if (_armyDirty)
            {
                changes.Add(ArmyChange(now));
                changes.Add(ProductionChange(now));
            }
            if (changedBuildings.Count == 0 && !researchChanged && changes.Count == 0)
                return changes;

            changes.AddRange(changedBuildings.Select(b => BuildingChange(b, now)));
            changes.AddRange(reports);
            if (researchChanged)
                changes.Add(ResearchChange(now));
            changes.Add(WalletChange());
            changes.Add(ProductionChange(now));
            return changes;
        }

        /// <summary>The earliest timed end at or before <paramref name="now"/>, if any.</summary>
        private DateTimeOffset? NextEvent(DateTimeOffset now)
        {
            DateTimeOffset? next = null;
            void Consider(DateTimeOffset? at)
            {
                if (at.HasValue && at.Value <= now && (next == null || at.Value < next.Value))
                    next = at;
            }

            foreach (var b in _state.Buildings)
            {
                Consider(b.ConstructionEndsAt);
                Consider(b.BoostEndsAt);
                Consider(b.OverdriveEndsAt);
                Consider(b.MissionEndsAt);
            }

            foreach (var research in _state.ActiveResearch)
                Consider(research.EndsAt);
            foreach (var march in _state.Marches)
            {
                if (!AwaitsBattle(march))
                    Consider(march.HomeAt);
                if (march.Battle is { Reported: false })
                    Consider(march.ArrivesAt);
            }
            foreach (var training in _state.Training)
                Consider(training.EndsAt);
            Consider(_state.Healing?.EndsAt);
            return next;
        }

        private void CompleteConstruction(PlacedBuilding building)
        {
            // A bigger Military Dome holds more troops; a new yard level opens tiers and batch sizes.
            if (building.DefinitionId == _catalog.Troops.capacityBuilding || TroopRules.IsTrainingBuilding(_catalog.Troops, building.DefinitionId))
                _armyDirty = true;
            building.Level += 1;
            building.ConstructionEndsAt = null;
            building.ConstructionSeconds = 0;
            MarkChanged(building);
            if (_catalog.TryGetBuilding(building.DefinitionId, out var definition))
                Score(EventActions.BuildPower, BuildingProgression.Power(definition, building.Level) - BuildingProgression.Power(definition, building.Level - 1));
        }

        private void CompleteResearch(string nodeId, int level)
        {
            var before = _state.ResearchLevels.TryGetValue(nodeId, out var have) ? have : 0;
            _state.ResearchLevels[nodeId] = Math.Max(level, before);
            Score(EventActions.Research, Math.Max(0, level - before));
            RefreshModifiers();
        }

        /// <summary>Delivers a finished salvage run (capped at storage) and returns its report.</summary>
        private EntityChangeDto CompleteSalvage(PlacedBuilding building)
        {
            var rewards = new Dictionary<ResourceType, long>();
            if (_catalog.TryGetSalvageMission(building.MissionId, out var mission))
            {
                foreach (var pair in BuildingFunctions.SalvageRewards(mission, building.Level))
                    rewards[pair.Key] = pair.Value;
                var hadalite = BuildingFunctions.SalvageHadalite(mission, building.MissionSeed);
                if (hadalite > 0)
                    rewards[ResourceType.Hadalite] = (rewards.TryGetValue(ResourceType.Hadalite, out var h) ? h : 0) + hadalite;
                WalletOps.Add(_state.Wallet, rewards, Capacities());
                _walletDirty = true;
            }

            var report = new SalvageReportDto
            {
                BuildingEntityId = building.BuildingId,
                MissionId = building.MissionId,
                Rewards = ResourceAmountEntry.FromMap(rewards)
            };
            building.MissionId = string.Empty;
            building.MissionEndsAt = null;
            building.MissionSeconds = 0;
            return new EntityChangeDto
            {
                EntityId = EntityIds.SalvageReport + building.BuildingId,
                Kind = EntityChangeKind.Create,
                Payload = _serializer.Serialize(report)
            };
        }

        private void AdvanceEconomy(DateTimeOffset until)
        {
            var elapsed = (until - _state.LastSimulatedAt).TotalSeconds;
            if (elapsed <= 0)
                return;

            var ticks = SimulationTick.TicksFromSeconds(elapsed);
            if (ticks == 0UL)
                return;

            // Rates as they were during this interval: events split the interval at every change.
            var net = NetMilliPerTick(_state.LastSimulatedAt);
            var granted = SimulationTick.ClampOfflineTicks(ticks);
            _state.LastSimulatedAt = granted < ticks
                ? until
                : _state.LastSimulatedAt.AddSeconds(SimulationTick.SecondsFromTicks(granted));

            var result = ProductionSimulator.AdvanceMilli(_state.Wallet, net, Capacities(), granted, _state.CarryMilli);
            _state.ServerTick += granted;
            _state.IsDirty = true;

            if (!result.HasChanges)
                return;

            _state.Wallet.Clear();
            foreach (var pair in result.Totals)
                _state.Wallet[pair.Key] = pair.Value;

            _walletDirty = true;
        }

        private Dictionary<ResourceType, long> NetMilliPerTick(DateTimeOffset at)
        {
            var net = ProductionSimulator.ComputeNetMilliPerTick(Inputs(at), _production, _modifiers.ProductionPercent);

            // The army eats (docs/rehber 07 §2.5): troops at home and out on marches; the wounded do not.
            var (food, oxygen) = TroopRules.Upkeep(_catalog.Troops, FedTroops());
            var perTick = SimulationTick.SecondsFromTicks(1) / 3600.0 * 1000.0;
            void Eat(ResourceType type, double perHour)
            {
                var milli = (long)Math.Round(perHour * perTick);
                if (milli > 0)
                    net[type] = (net.TryGetValue(type, out var have) ? have : 0) - milli;
            }

            Eat(ResourceType.Food, food);
            Eat(ResourceType.Oxygen, oxygen);
            return net;
        }

        private List<BuildingProductionInput> Inputs(DateTimeOffset at) =>
            _state.Buildings.Where(b => b.Level > 0).Select(b => new BuildingProductionInput(b.DefinitionId, b.Level, BonusPercent(b, at))).ToList();

        /// <summary>Boost and Overdrive add to one building's output while they run.</summary>
        private int BonusPercent(PlacedBuilding building, DateTimeOffset at)
        {
            var bonus = 0;
            if (building.BoostEndsAt > at)
                bonus += _catalog.Boost.percent;
            if (building.OverdriveEndsAt > at)
                bonus += _catalog.Overdrive.percent;
            return bonus;
        }

        private void RefreshModifiers()
        {
            var modifiers = ResearchRules.Modifiers(_catalog, _state.ResearchLevels);
            HeroRules.AddCrew(_catalog, modifiers, Crew());
            VipRules.Apply(modifiers, VipLevel);
            _modifiers = modifiers;
        }

        // --- Helpers ------------------------------------------------------------------------------

        private PlacedBuilding? FindBuilding(string id) => _state.Buildings.FirstOrDefault(b => b.BuildingId == id);

        private BuildingDefinition? Definition(PlacedBuilding? building) =>
            building != null && _catalog.TryGetBuilding(building.DefinitionId, out var definition) ? definition : null;

        private void MarkChanged(PlacedBuilding building)
        {
            if (!_state.UnsavedBuildings.Contains(building))
                _state.UnsavedBuildings.Add(building);
            _state.IsDirty = true;
            _questsDirty = true; // building levels drive chapter tasks
        }

        private void Reject(string commandId, ulong sequence, string reason, DateTimeOffset now) =>
            SendDelta(now, new List<EntityChangeDto> { AckChange(commandId, sequence, CommandResultCode.Rejected, reason) });

        private EntityChangeDto Accepted(string commandId, ulong sequence) =>
            AckChange(commandId, sequence, CommandResultCode.Accepted, string.Empty);

        private int ActiveConstructions() => _state.Buildings.Count(b => b.IsUnderConstruction);

        private int Builders() => Math.Max(1, _catalog.Progression.builderSlots) + _modifiers.ExtraBuilders;

        private Dictionary<string, int> OwnedCounts() =>
            _state.Buildings.GroupBy(b => b.DefinitionId).ToDictionary(g => g.Key, g => g.Count());

        /// <summary>The highest level among this player's buildings of one kind (reactor prerequisites).</summary>
        private int HighestLevel(string definitionId) =>
            _state.Buildings.Where(b => b.DefinitionId == definitionId).Select(b => b.Level).DefaultIfEmpty(0).Max();

        private int ReactorLevel() =>
            _state.Buildings.Where(b => b.DefinitionId == BuildingProgression.ReactorId).Select(b => b.Level).DefaultIfEmpty(0).Max();

        private int CenterLevel() =>
            _state.Buildings.Where(b => b.DefinitionId == ResearchRules.CenterId).Select(b => b.Level).DefaultIfEmpty(0).Max();

        // --- Replication --------------------------------------------------------------------------

        private void SendDelta(DateTimeOffset now, List<EntityChangeDto> changes)
        {
            if (_connection == null)
                return;

            if (_questsDirty)
            {
                changes.Add(QuestsChange(now));
                _questsDirty = false;
            }

            if (changes.Count == 0)
                return;

            var delta = new StateDelta
            {
                ServerTick = _state.ServerTick,
                BaselineTick = _lastReplicatedTick,
                Changes = changes
            };

            _connection.Send(EnvelopeBuilder.WrapServerMessage(
                EnvelopeBuilder.WrapStateDelta(delta, _serializer.Serialize(delta))));

            if (changes.Any(c => c.EntityId == EntityIds.Wallet))
            {
                _walletDirty = false;
                _lastReplicatedAt = now;
            }

            _lastReplicatedTick = _state.ServerTick;
        }

        private StateSnapshotEnvelope BuildSnapshotEnvelope(DateTimeOffset now)
        {
            var snapshot = new StateSnapshot
            {
                ServerTick = _state.ServerTick,
                PlayerId = _state.PlayerId.ToString("N"),
                Resources = WalletDto(),
                Buildings = _state.Buildings.Select(b => ToDto(b, now)).ToList(),
                Production = ProductionDto(now),
                UnlockedRings = _state.UnlockedRings,
                Research = ResearchDto(now),
                Heroes = RosterDto(now),
                Quests = QuestDto(now),
                Inventory = InventoryDto(),
                Marches = MarchListDto(now),
                Expedition = ExpeditionDto(),
                Profile = new PlayerProfileDto { PlayerId = PublicId, Name = _displayName, AvatarId = _state.AvatarId },
                Army = ArmyDto(),
                Vip = VipDto(now)
            };
            _questsDirty = false;

            return EnvelopeBuilder.WrapStateSnapshot(snapshot, _serializer.Serialize(snapshot));
        }

        private EntityChangeDto BuildingChange(PlacedBuilding building, DateTimeOffset now) => new()
        {
            EntityId = EntityIds.Building + building.BuildingId,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(ToDto(building, now))
        };

        private EntityChangeDto WalletChange() => new()
        {
            EntityId = EntityIds.Wallet,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(WalletDto())
        };

        private EntityChangeDto LayoutChange() => new()
        {
            EntityId = EntityIds.BaseLayout,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(new BaseLayoutDto { UnlockedRings = _state.UnlockedRings })
        };

        private EntityChangeDto ResearchChange(DateTimeOffset now) => new()
        {
            EntityId = EntityIds.Research,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(ResearchDto(now))
        };

        private EntityChangeDto ProductionChange(DateTimeOffset now) => new()
        {
            EntityId = EntityIds.ProductionSummary,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(ProductionDto(now))
        };

        private EntityChangeDto AckChange(string commandId, ulong sequence, CommandResultCode result, string reason) => new()
        {
            EntityId = EntityIds.Command + commandId,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(new CommandAckDto
            {
                CommandId = commandId,
                ClientSequence = sequence,
                Result = result,
                Reason = reason
            })
        };

        private ResourceStateDto WalletDto()
        {
            var dto = new ResourceStateDto { ServerTick = _state.ServerTick };
            foreach (var pair in _state.Wallet)
                dto.Amounts[pair.Key] = pair.Value;
            return dto;
        }

        private ProductionStateDto ProductionDto(DateTimeOffset now)
        {
            var dto = new ProductionStateDto
            {
                ServerTick = _state.ServerTick,
                TickIntervalSeconds = SimulationTick.IntervalSeconds
            };
            foreach (var pair in NetMilliPerTick(now))
            {
                dto.NetMilliPerTick[pair.Key] = pair.Value;
                dto.NetPerTick[pair.Key] = pair.Value / ProductionFormulas.Milli;
            }

            foreach (var pair in Capacities())
                dto.Capacities[pair.Key] = pair.Value;
            return dto;
        }

        private ResearchStateDto ResearchDto(DateTimeOffset now) => new()
        {
            Levels = _state.ResearchLevels.Select(p => new ResearchLevelEntry { NodeId = p.Key, Level = p.Value }).ToArray(),
            Active = _state.ActiveResearch.Select(a => new ActiveResearchEntry
            {
                NodeId = a.NodeId,
                TargetLevel = a.TargetLevel,
                RemainingMs = Remaining(a.EndsAt, now),
                TotalMs = a.Seconds * 1000L
            }).ToArray()
        };

        private static long Remaining(DateTimeOffset? endsAt, DateTimeOffset now) =>
            endsAt.HasValue ? Math.Max(0L, (long)(endsAt.Value - now).TotalMilliseconds) : 0L;

        private static BuildingStateDto ToDto(PlacedBuilding building, DateTimeOffset now) => new()
        {
            EntityId = building.BuildingId,
            DefinitionId = building.DefinitionId,
            RingIndex = building.Ring,
            SectorIndex = building.Sector,
            Level = building.Level,
            RotationSteps = building.RotationSteps,
            ConstructionRemainingMs = Remaining(building.ConstructionEndsAt, now),
            ConstructionTotalMs = building.ConstructionEndsAt.HasValue ? building.ConstructionSeconds * 1000L : 0L,
            BoostRemainingMs = Remaining(building.BoostEndsAt, now),
            MissionId = building.MissionEndsAt.HasValue ? building.MissionId : string.Empty,
            MissionRemainingMs = Remaining(building.MissionEndsAt, now),
            MissionTotalMs = building.MissionEndsAt.HasValue ? building.MissionSeconds * 1000L : 0L,
            OverdriveRemainingMs = Remaining(building.OverdriveEndsAt, now),
            OverdriveCooldownMs = Remaining(building.OverdriveReadyAt, now)
        };

        private bool TryDeserialize<T>(byte[] payload, out T? value) where T : class
        {
            value = null;
            return payload is { Length: > 0 } && _serializer.TryDeserialize(payload, out value) && value != null;
        }
    }
}
