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
        private readonly GameCatalog _catalog;
        private readonly ProductionCatalog _production;
        private readonly IReadOnlyDictionary<ResourceType, long> _capacities;
        private readonly INetworkSerializer _serializer;

        private IClientConnection? _connection;
        private Dictionary<ResourceType, long> _netPerTick = new();
        private DateTimeOffset _lastReplicatedAt;
        private ulong _lastReplicatedTick;
        private bool _walletDirty;

        internal PlayerSession(
            PlayerState state,
            GameCatalog catalog,
            ProductionCatalog production,
            IReadOnlyDictionary<ResourceType, long> capacities,
            INetworkSerializer serializer)
        {
            _state = state;
            _catalog = catalog;
            _production = production;
            _capacities = capacities;
            _serializer = serializer;
        }

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

                connection.Send(EnvelopeBuilder.WrapServerMessage(BuildSnapshotEnvelope()));
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
                AdvanceTo(now);

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
                AdvanceTo(now);

                switch (envelope.Type)
                {
                    case CommandType.PlaceBuilding when TryDeserialize(envelope.Payload, out PlaceBuildingCommand? place):
                        PlaceBuilding(place!, now);
                        break;

                    default:
                        SendDelta(now, new List<EntityChangeDto>
                        {
                            AckChange(envelope.CommandId, envelope.ClientSequence, CommandResultCode.Rejected,
                                envelope.Type == CommandType.PlaceBuilding ? "MalformedCommand" : "NotSupported")
                        });
                        break;
                }
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
                _state.IsDirty = false;
                return copy;
            }
        }

        /// <summary>Re-queues buildings from a failed save so the next attempt writes them.</summary>
        public void RestoreAfterFailedSave(PlayerState snapshot)
        {
            lock (_gate)
            {
                _state.UnsavedBuildings.InsertRange(0, snapshot.UnsavedBuildings);
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
                command.SectorIndex, _state.UnlockedRings, occupied, _state.Wallet);

            if (verdict != PlacementRejectReason.None)
            {
                SendDelta(now, new List<EntityChangeDto>
                {
                    AckChange(command.CommandId, command.ClientSequence, CommandResultCode.Rejected, verdict.ToString())
                });
                return;
            }

            _catalog.TryGetBuilding(command.BuildingDefinitionId, out var definition);
            PlacementRules.Charge(definition, _state.Wallet);

            var building = new PlacedBuilding
            {
                BuildingId = "b" + Guid.NewGuid().ToString("N"),
                DefinitionId = definition.Id,
                Ring = command.RingIndex,
                Sector = command.SectorIndex,
                Level = 1,
                RotationSteps = command.RotationSteps
            };

            _state.Buildings.Add(building);
            _state.UnsavedBuildings.Add(building);
            _state.IsDirty = true;
            _netPerTick = ComputeNetPerTick();

            SendDelta(now, new List<EntityChangeDto>
            {
                new()
                {
                    EntityId = EntityIds.Building + building.BuildingId,
                    Kind = EntityChangeKind.Create,
                    Payload = _serializer.Serialize(ToDto(building))
                },
                WalletChange(),
                ProductionChange(),
                AckChange(command.CommandId, command.ClientSequence, CommandResultCode.Accepted, string.Empty)
            });
        }

        /// <summary>
        /// Brings the economy up to <paramref name="now"/>. Gaps beyond the offline cap are
        /// forfeited, not banked, so a long absence cannot be replayed later.
        /// </summary>
        private void AdvanceTo(DateTimeOffset now)
        {
            var elapsed = (now - _state.LastSimulatedAt).TotalSeconds;
            if (elapsed < 0)
            {
                // Server clock went backwards (NTP step). Never award negative time.
                _state.LastSimulatedAt = now;
                return;
            }

            var ticks = SimulationTick.TicksFromSeconds(elapsed);
            if (ticks == 0UL)
                return;

            var granted = SimulationTick.ClampOfflineTicks(ticks);
            _state.LastSimulatedAt = granted < ticks
                ? now
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

        private StateSnapshotEnvelope BuildSnapshotEnvelope()
        {
            var snapshot = new StateSnapshot
            {
                ServerTick = _state.ServerTick,
                PlayerId = _state.PlayerId.ToString("N"),
                Resources = WalletDto(),
                Buildings = _state.Buildings.Select(ToDto).ToList(),
                Production = ProductionDto()
            };

            return EnvelopeBuilder.WrapStateSnapshot(snapshot, _serializer.Serialize(snapshot));
        }

        private EntityChangeDto WalletChange() => new()
        {
            EntityId = EntityIds.Wallet,
            Kind = EntityChangeKind.Update,
            Payload = _serializer.Serialize(WalletDto())
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
            _state.Buildings.Select(b => new BuildingProductionInput(b.DefinitionId, b.Level)).ToList();

        private static BuildingStateDto ToDto(PlacedBuilding building) => new()
        {
            EntityId = building.BuildingId,
            DefinitionId = building.DefinitionId,
            RingIndex = building.Ring,
            SectorIndex = building.Sector,
            Level = building.Level,
            RotationSteps = building.RotationSteps
        };

        private bool TryDeserialize<T>(byte[] payload, out T? value) where T : class
        {
            value = null;
            return payload is { Length: > 0 } && _serializer.TryDeserialize(payload, out value) && value != null;
        }
    }
}
