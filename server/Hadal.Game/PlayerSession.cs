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
    public sealed class PlayerSession
    {
        private readonly object _gate = new();
        private readonly PlayerState _state;
        private readonly CatalogRuntime _runtime;
        private readonly INetworkSerializer _serializer;

        private IClientConnection? _connection;
        private Dictionary<ResourceType, long> _netPerTick = new();
        private DateTimeOffset _lastReplicatedAt;
        private ulong _lastReplicatedTick;
        private bool _walletDirty;

        internal PlayerSession(PlayerState state, CatalogRuntime runtime, INetworkSerializer serializer)
        {
            _state = state;
            _runtime = runtime;
            _serializer = serializer;
        }

        // The catalog can be swapped at runtime (GameWorld.ReplaceCatalog); always read it fresh.
        private GameCatalog _catalog => _runtime.Current.Catalog;
        private ProductionCatalog _production => _runtime.Current.Production;
        private IReadOnlyDictionary<ResourceType, long> _capacities => _runtime.Current.Capacities;

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
                _netPerTick = ComputeNetPerTick();
                _lastReplicatedAt = now;
                _lastReplicatedTick = _state.ServerTick;
                _walletDirty = false;

                connection.Send(EnvelopeBuilder.WrapServerMessage(BuildSnapshotEnvelope(now)));
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
                var completed = AdvanceTo(now);
                if (completed.Count > 0)
                {
                    SendDelta(now, CompletionChanges(completed, now));
                    return;
                }

                if (_connection == null || !_walletDirty)
                    return;

                if ((now - _lastReplicatedAt).TotalSeconds < SimulationTick.ReplicationIntervalSeconds)
                    return;

                SendDelta(now, new List<EntityChangeDto> { WalletChange(), ProductionChange() });
            }
        }

        public void HandleCommand(CommandEnvelope envelope, DateTimeOffset now)
        {
            lock (_gate)
            {
                var completed = AdvanceTo(now);
                if (completed.Count > 0)
                    SendDelta(now, CompletionChanges(completed, now));

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

                    default:
                        var known = envelope.Type is CommandType.PlaceBuilding or CommandType.UpgradeBuilding or CommandType.FinishConstruction
                            or CommandType.RemoveBuilding or CommandType.UnlockRing;
                        SendDelta(now, new List<EntityChangeDto>
                        {
                            AckChange(envelope.CommandId, envelope.ClientSequence, CommandResultCode.Rejected,
                                known ? "MalformedCommand" : "NotSupported")
                        });
                        break;
                }
            }
        }

        /// <summary>Settles the economy up to now under the current rules (before a catalog swap).</summary>
        internal void Settle(DateTimeOffset now)
        {
            lock (_gate)
            {
                var completed = AdvanceTo(now);
                if (completed.Count > 0)
                    SendDelta(now, CompletionChanges(completed, now));
            }
        }

        /// <summary>Recomputes rates after a catalog swap and pushes them.</summary>
        internal void ApplyCatalogChange(DateTimeOffset now)
        {
            lock (_gate)
            {
                _netPerTick = ComputeNetPerTick();
                SendDelta(now, new List<EntityChangeDto> { WalletChange(), ProductionChange() });
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
                return copy;
            }
        }

        /// <summary>Overseer grant: adds resources (capped at storage) and pushes the wallet at once.</summary>
        internal void Grant(IReadOnlyDictionary<ResourceType, long> amounts, DateTimeOffset now)
        {
            lock (_gate)
            {
                AdvanceTo(now);
                WalletOps.Add(_state.Wallet, amounts, _capacities);
                _state.IsDirty = true;
                SendDelta(now, new List<EntityChangeDto> { WalletChange(), ProductionChange() });
            }
        }

        /// <summary>Copies state for persistence and clears the unsaved markers. Call outside the save I/O.</summary>
        public PlayerState? TakeSaveSnapshot()
        {
            lock (_gate)
            {
                if (!_state.IsDirty)
                    return null;

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

        private void PlaceBuilding(PlaceBuildingCommand command, DateTimeOffset now)
        {
            var occupied = new HashSet<(int ring, int sector)>(_state.Buildings.Select(b => (b.Ring, b.Sector)));
            var verdict = PlacementRules.Validate(_catalog, command.BuildingDefinitionId, command.RingIndex,
                command.SectorIndex, _state.UnlockedRings, occupied, _state.Wallet, OwnedCounts(), ReactorLevel());

            if (verdict != PlacementRejectReason.None)
            {
                SendDelta(now, new List<EntityChangeDto>
                {
                    AckChange(command.CommandId, command.ClientSequence, CommandResultCode.Rejected, verdict.ToString())
                });
                return;
            }

            if (ActiveConstructions() >= Math.Max(1, _catalog.Progression.builderSlots))
            {
                Reject(command.CommandId, command.ClientSequence, ConstructionRejectReason.BuilderBusy.ToString(), now);
                return;
            }

            _catalog.TryGetBuilding(command.BuildingDefinitionId, out var definition);
            BuildingProgression.Charge(BuildingProgression.Cost(definition, 1, _catalog.Progression), _state.Wallet);
            var seconds = BuildingProgression.Seconds(definition, 1, _catalog.Progression);

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
            _netPerTick = ComputeNetPerTick();

            SendDelta(now, new List<EntityChangeDto>
            {
                new()
                {
                    EntityId = EntityIds.Building + building.BuildingId,
                    Kind = EntityChangeKind.Create,
                    Payload = _serializer.Serialize(ToDto(building, now))
                },
                WalletChange(),
                ProductionChange(),
                AckChange(command.CommandId, command.ClientSequence, CommandResultCode.Accepted, string.Empty)
            });
        }

        private void UpgradeBuilding(UpgradeBuildingCommand command, DateTimeOffset now)
        {
            var building = _state.Buildings.FirstOrDefault(b => b.BuildingId == command.BuildingEntityId);
            if (building == null)
            {
                Reject(command.CommandId, command.ClientSequence, ConstructionRejectReason.UnknownBuilding.ToString(), now);
                return;
            }

            var verdict = BuildingProgression.ValidateUpgrade(_catalog, building.DefinitionId, building.Level,
                building.IsUnderConstruction, ActiveConstructions(), ReactorLevel(), _state.Wallet);
            if (verdict != ConstructionRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            _catalog.TryGetBuilding(building.DefinitionId, out var definition);
            var target = building.Level + 1;
            BuildingProgression.Charge(BuildingProgression.Cost(definition, target, _catalog.Progression), _state.Wallet);
            var seconds = BuildingProgression.Seconds(definition, target, _catalog.Progression);

            // The building keeps producing at its current level while it is being upgraded.
            if (seconds > 0)
            {
                building.ConstructionEndsAt = now.AddSeconds(seconds);
                building.ConstructionSeconds = seconds;
            }
            else
            {
                building.Level = target;
                _netPerTick = ComputeNetPerTick();
            }

            MarkChanged(building);
            SendDelta(now, new List<EntityChangeDto>
            {
                BuildingChange(building, now),
                WalletChange(),
                ProductionChange(),
                AckChange(command.CommandId, command.ClientSequence, CommandResultCode.Accepted, string.Empty)
            });
        }

        private void FinishConstruction(FinishConstructionCommand command, DateTimeOffset now)
        {
            var building = _state.Buildings.FirstOrDefault(b => b.BuildingId == command.BuildingEntityId);
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

            Complete(building!);
            var changes = CompletionChanges(new List<PlacedBuilding> { building! }, now);
            changes.Add(AckChange(command.CommandId, command.ClientSequence, CommandResultCode.Accepted, string.Empty));
            SendDelta(now, changes);
        }

        private void RemoveBuilding(RemoveBuildingCommand command, DateTimeOffset now)
        {
            var building = _state.Buildings.FirstOrDefault(b => b.BuildingId == command.BuildingEntityId);
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
            WalletOps.Add(_state.Wallet, refund.ToDictionary(p => p.Key, p => p.Value), _capacities);

            _state.Buildings.Remove(building);
            _state.UnsavedBuildings.Remove(building);
            _state.RemovedBuildingIds.Add(building.BuildingId);
            _state.IsDirty = true;
            _netPerTick = ComputeNetPerTick();

            SendDelta(now, new List<EntityChangeDto>
            {
                new() { EntityId = EntityIds.Building + building.BuildingId, Kind = EntityChangeKind.Delete },
                WalletChange(),
                ProductionChange(),
                AckChange(command.CommandId, command.ClientSequence, CommandResultCode.Accepted, string.Empty)
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
                AckChange(command.CommandId, command.ClientSequence, CommandResultCode.Accepted, string.Empty)
            });
        }

        private Dictionary<string, int> OwnedCounts() =>
            _state.Buildings.GroupBy(b => b.DefinitionId).ToDictionary(g => g.Key, g => g.Count());

        private void Complete(PlacedBuilding building)
        {
            building.Level += 1;
            building.ConstructionEndsAt = null;
            building.ConstructionSeconds = 0;
            MarkChanged(building);
            _netPerTick = ComputeNetPerTick();
        }

        private List<EntityChangeDto> CompletionChanges(List<PlacedBuilding> completed, DateTimeOffset now)
        {
            var changes = completed.Select(b => BuildingChange(b, now)).ToList();
            changes.Add(WalletChange());
            changes.Add(ProductionChange());
            return changes;
        }

        private void MarkChanged(PlacedBuilding building)
        {
            if (!_state.UnsavedBuildings.Contains(building))
                _state.UnsavedBuildings.Add(building);
            _state.IsDirty = true;
        }

        private void Reject(string commandId, ulong sequence, string reason, DateTimeOffset now) =>
            SendDelta(now, new List<EntityChangeDto> { AckChange(commandId, sequence, CommandResultCode.Rejected, reason) });

        private int ActiveConstructions() => _state.Buildings.Count(b => b.IsUnderConstruction);

        private int ReactorLevel() =>
            _state.Buildings.Where(b => b.DefinitionId == BuildingProgression.ReactorId).Select(b => b.Level).DefaultIfEmpty(0).Max();

        /// <summary>
        /// Brings the economy up to <paramref name="now"/>, completing constructions on the way:
        /// the interval is split at each completion so a building produces from the moment it
        /// finished, also while the player was offline. Gaps beyond the offline cap are forfeited
        /// (not banked) before splitting, so a long absence cannot be replayed later.
        /// Returns the buildings whose construction completed.
        /// </summary>
        private List<PlacedBuilding> AdvanceTo(DateTimeOffset now)
        {
            var completed = new List<PlacedBuilding>();
            var elapsed = (now - _state.LastSimulatedAt).TotalSeconds;
            if (elapsed < 0)
            {
                // Server clock went backwards (NTP step). Never award negative time.
                _state.LastSimulatedAt = now;
                return completed;
            }

            var maxSeconds = SimulationTick.SecondsFromTicks((ulong)SimulationTick.MaxOfflineAccrualTicks);
            if (elapsed > maxSeconds)
            {
                _state.LastSimulatedAt = now.AddSeconds(-maxSeconds);
                _state.IsDirty = true;
            }

            while (true)
            {
                var next = _state.Buildings
                    .Where(b => b.ConstructionEndsAt.HasValue && b.ConstructionEndsAt.Value <= now)
                    .OrderBy(b => b.ConstructionEndsAt!.Value)
                    .FirstOrDefault();
                if (next == null)
                    break;

                var at = next.ConstructionEndsAt!.Value;
                if (at > _state.LastSimulatedAt)
                    AdvanceEconomy(at);
                Complete(next);
                if (!completed.Contains(next))
                    completed.Add(next);
            }

            AdvanceEconomy(now);
            return completed;
        }

        private void AdvanceEconomy(DateTimeOffset until)
        {
            var elapsed = (until - _state.LastSimulatedAt).TotalSeconds;
            if (elapsed <= 0)
                return;

            var ticks = SimulationTick.TicksFromSeconds(elapsed);
            if (ticks == 0UL)
                return;

            var granted = SimulationTick.ClampOfflineTicks(ticks);
            _state.LastSimulatedAt = granted < ticks
                ? until
                : _state.LastSimulatedAt.AddSeconds(SimulationTick.SecondsFromTicks(granted));

            var result = ProductionSimulator.Advance(_state.Wallet, Inputs(), _production, _capacities, granted);
            _state.ServerTick += granted;
            _state.IsDirty = true;

            if (!result.HasChanges)
                return;

            _state.Wallet.Clear();
            foreach (var pair in result.Totals)
                _state.Wallet[pair.Key] = pair.Value;

            _walletDirty = true;
        }

        private void SendDelta(DateTimeOffset now, List<EntityChangeDto> changes)
        {
            if (_connection == null)
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
                Production = ProductionDto(),
                UnlockedRings = _state.UnlockedRings
            };

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

        private EntityChangeDto ProductionChange() => new()
        {
            EntityId = EntityIds.ProductionSummary,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(ProductionDto())
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

        private ProductionStateDto ProductionDto()
        {
            var dto = new ProductionStateDto
            {
                ServerTick = _state.ServerTick,
                TickIntervalSeconds = SimulationTick.IntervalSeconds
            };
            foreach (var pair in _netPerTick)
                dto.NetPerTick[pair.Key] = pair.Value;
            return dto;
        }

        private Dictionary<ResourceType, long> ComputeNetPerTick() =>
            ProductionSimulator.ComputeNetPerTick(Inputs(), _production);

        private List<BuildingProductionInput> Inputs() =>
            _state.Buildings.Where(b => b.Level > 0).Select(b => new BuildingProductionInput(b.DefinitionId, b.Level)).ToList();

        private static BuildingStateDto ToDto(PlacedBuilding building, DateTimeOffset now) => new()
        {
            EntityId = building.BuildingId,
            DefinitionId = building.DefinitionId,
            RingIndex = building.Ring,
            SectorIndex = building.Sector,
            Level = building.Level,
            RotationSteps = building.RotationSteps,
            ConstructionRemainingMs = building.ConstructionEndsAt.HasValue
                ? Math.Max(0L, (long)(building.ConstructionEndsAt.Value - now).TotalMilliseconds)
                : 0L,
            ConstructionTotalMs = building.ConstructionEndsAt.HasValue ? building.ConstructionSeconds * 1000L : 0L
        };

        private bool TryDeserialize<T>(byte[] payload, out T? value) where T : class
        {
            value = null;
            return payload is { Length: > 0 } && _serializer.TryDeserialize(payload, out value) && value != null;
        }
    }
}
