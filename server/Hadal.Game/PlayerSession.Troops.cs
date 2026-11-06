using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>
    /// Troops and training (docs/rehber 07 §2.3–2.5, 19 §2): each training yard trains one batch of its own troop
    /// type at a time; the price is paid up front and refunded in full on cancel; the Military Domes cap the army.
    /// </summary>
    public sealed partial class PlayerSession
    {
        private bool _armyDirty;

        /// <summary>Every troop the base keeps: at home, in training, wounded or healing, out on marches (docs/rehber 07 §2.4).</summary>
        private long ArmyCount() => _state.Troops.Values.Sum() + _state.Training.Sum(t => (long)t.Count) + _state.Wounded.Values.Sum()
                                    + (_state.Healing?.Troops.Values.Sum() ?? 0) + Marching();

        private long Marching() => _state.Marches.Sum(m => m.Troops.Values.Sum());

        /// <summary>Troops that eat: at home and on marches (the wounded do not).</summary>
        private Dictionary<string, long> FedTroops()
        {
            var fed = new Dictionary<string, long>(_state.Troops, StringComparer.Ordinal);
            foreach (var march in _state.Marches)
                foreach (var pair in march.Troops)
                    fed[pair.Key] = (fed.TryGetValue(pair.Key, out var n) ? n : 0) + pair.Value;
            return fed;
        }

        private IEnumerable<(string DefinitionId, int Level)> Levels() => _state.Buildings.Select(b => (b.DefinitionId, b.Level));

        /// <summary>Troops a hero leads (command capacity: level, stars, the best Military Dome).</summary>
        private int CommandOf(OwnedHeroState hero) =>
            TroopRules.CommandCapacity(_catalog.Troops, hero.Level, hero.Stars, TroopRules.BestDome(_catalog.Troops, Levels()));

        private string Affinity(string heroId) =>
            _catalog.TryGetHero(heroId, out var hero) ? TroopRules.Affinity(_catalog.Troops, hero.HeroClass) : string.Empty;

        private long ArmyCapacity() => TroopRules.ArmyCapacity(_catalog.Troops, Levels());

        private void TrainTroops(TrainTroopsCommand command, DateTimeOffset now)
        {
            var troops = _catalog.Troops;
            var building = FindBuilding(command.BuildingId);
            var busy = building != null && (building.ConstructionEndsAt != null || _state.Training.Any(t => t.BuildingId == building.BuildingId));
            var verdict = TroopRules.ValidateTrain(troops, building?.DefinitionId, building?.Level ?? 0, busy, command.TroopType, command.Tier,
                command.Count, ArmyCount(), ArmyCapacity(), _state.Wallet);
            if (verdict != TroopRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            var tier = TroopRules.Tier(troops, command.Tier)!;
            var cost = TroopRules.Cost(tier, command.Count);
            Spend("troop", cost, now);
            var seconds = Slowed(TroopRules.Seconds(troops, tier, command.Count, building!.Level), now);
            _state.Training.Add(new TrainingState
            {
                BuildingId = building.BuildingId, Type = command.TroopType, Tier = command.Tier, Count = command.Count,
                StartedAt = now, EndsAt = now.AddSeconds(seconds), Cost = cost.ToDictionary(p => p.Key, p => p.Value)
            });
            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto> { WalletChange(), ArmyChange(now), Accepted(command.CommandId, command.ClientSequence) });
        }

        private void CancelTraining(CancelTrainingCommand command, DateTimeOffset now)
        {
            var training = _state.Training.FirstOrDefault(t => t.BuildingId == command.BuildingId);
            if (training == null)
            {
                Reject(command.CommandId, command.ClientSequence, TroopRejectReason.NotTraining.ToString(), now);
                return;
            }

            _state.Training.Remove(training);
            var walletBefore = WalletSnapshot();
            foreach (var pair in training.Cost)
                WalletOps.Credit(_state.Wallet, pair.Key, pair.Value);
            LogWalletDelta("refund", walletBefore, now);
            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto> { WalletChange(), ArmyChange(now), Accepted(command.CommandId, command.ClientSequence) });
        }

        /// <summary>Heals wounded troops (docs/rehber 07 §2.7): a share of the training price, one batch at a time.</summary>
        private void HealTroops(HealTroopsCommand command, DateTimeOffset now)
        {
            var troops = _catalog.Troops;
            var request = new Dictionary<string, long>(StringComparer.Ordinal);
            if (command.Troops.Length == 0)
            {
                foreach (var pair in _state.Wounded.Where(p => p.Value > 0))
                    request[pair.Key] = pair.Value;
            }
            else
            {
                foreach (var stack in command.Troops)
                {
                    var key = TroopRules.Key(stack.Type, stack.Tier);
                    request[key] = (request.TryGetValue(key, out var n) ? n : 0) + stack.Count;
                }
            }

            var best = TroopRules.BestInfirmary(troops, Levels());
            var verdict = TroopRules.ValidateHeal(troops, best, _state.Healing != null, _state.Wounded, request, _state.Wallet);
            if (verdict != TroopRejectReason.None)
            {
                Reject(command.CommandId, command.ClientSequence, verdict.ToString(), now);
                return;
            }

            var cost = TroopRules.HealCost(troops, request);
            Spend("heal", cost, now);
            foreach (var pair in request)
            {
                _state.Wounded[pair.Key] -= pair.Value;
                if (_state.Wounded[pair.Key] <= 0)
                    _state.Wounded.Remove(pair.Key);
            }

            _state.Healing = new HealState
            {
                Troops = request, StartedAt = now, EndsAt = now.AddSeconds(TroopRules.HealSeconds(troops, request, best)),
                Cost = cost.ToDictionary(p => p.Key, p => p.Value)
            };
            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto> { WalletChange(), ArmyChange(now), Accepted(command.CommandId, command.ClientSequence) });
        }

        private void CancelHeal(CancelHealCommand command, DateTimeOffset now)
        {
            if (_state.Healing is not { } healing)
            {
                Reject(command.CommandId, command.ClientSequence, TroopRejectReason.NothingToHeal.ToString(), now);
                return;
            }

            _state.Healing = null;
            foreach (var pair in healing.Troops)
                _state.Wounded[pair.Key] = (_state.Wounded.TryGetValue(pair.Key, out var n) ? n : 0) + pair.Value;
            var walletBefore = WalletSnapshot();
            foreach (var pair in healing.Cost)
                WalletOps.Credit(_state.Wallet, pair.Key, pair.Value);
            LogWalletDelta("refund", walletBefore, now);
            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto> { WalletChange(), ArmyChange(now), Accepted(command.CommandId, command.ClientSequence) });
        }

        /// <summary>Healed troops go back to the army at home.</summary>
        private void CompleteHeal(HealState healing)
        {
            _state.Healing = null;
            foreach (var pair in healing.Troops)
                _state.Troops[pair.Key] = (_state.Troops.TryGetValue(pair.Key, out var n) ? n : 0) + pair.Value;
            Score(EventActions.Heal, healing.Troops.Values.Sum());
            _armyDirty = true;
        }

        /// <summary>Puts troops in the Med Bay (combat, D4); whatever does not fit is lost. Returns how many fit.</summary>
        internal long WoundForTests(string key, long count)
        {
            lock (_gate)
            {
                var room = Math.Max(0, TroopRules.InfirmaryCapacity(_catalog.Troops, Levels()) - _state.Wounded.Values.Sum()
                                          - (_state.Healing?.Troops.Values.Sum() ?? 0));
                var fit = Math.Min(room, count);
                _state.Wounded[key] = (_state.Wounded.TryGetValue(key, out var n) ? n : 0) + fit;
                return fit;
            }
        }

        /// <summary>Net milli-units per tick of one resource (upkeep included).</summary>
        internal long ProductionForTests(ResourceType type)
        {
            lock (_gate)
                return NetMilliPerTick(_state.LastSimulatedAt).TryGetValue(type, out var net) ? net : 0;
        }

        /// <summary>A finished batch joins the army at home.</summary>
        private void CompleteTraining(TrainingState training)
        {
            _state.Training.Remove(training);
            var key = TroopRules.Key(training.Type, training.Tier);
            _state.Troops[key] = (_state.Troops.TryGetValue(key, out var have) ? have : 0) + training.Count;
            Score(EventActions.Train(training.Tier), training.Count);
            _armyDirty = true;
        }

        private ArmyDto ArmyDto()
        {
            var (food, oxygen) = TroopRules.Upkeep(_catalog.Troops, FedTroops());
            return new ArmyDto
            {
                Troops = _state.Troops.Where(p => p.Value > 0)
                    .Select(p => TroopRules.TryParseKey(p.Key, out var type, out var tier) ? new TroopStackDto { Type = type, Tier = tier, Count = p.Value } : null)
                    .Where(s => s != null).Select(s => s!).OrderBy(s => s.Type, StringComparer.Ordinal).ThenByDescending(s => s.Tier).ToArray(),
                Training = _state.Training.Select(t => new TrainingDto
                {
                    BuildingId = t.BuildingId, Type = t.Type, Tier = t.Tier, Count = t.Count,
                    StartedAtMs = t.StartedAt.ToUnixTimeMilliseconds(), EndsAtMs = t.EndsAt.ToUnixTimeMilliseconds()
                }).ToArray(),
                Capacity = ArmyCapacity(),
                UpkeepFood = food,
                UpkeepOxygen = oxygen,
                Wounded = Stacks(_state.Wounded),
                Healing = _state.Healing == null ? null : new HealingDto
                {
                    Troops = Stacks(_state.Healing.Troops),
                    StartedAtMs = _state.Healing.StartedAt.ToUnixTimeMilliseconds(),
                    EndsAtMs = _state.Healing.EndsAt.ToUnixTimeMilliseconds()
                },
                InfirmaryCapacity = TroopRules.InfirmaryCapacity(_catalog.Troops, Levels()),
                Marching = Marching(),
                Command = _state.Heroes.Select(h => new HeroCommandDto { HeroId = h.HeroId, Capacity = CommandOf(h) }).ToArray()
            };
        }

        private static TroopStackDto[] Stacks(IReadOnlyDictionary<string, long> stacks) =>
            stacks.Where(p => p.Value > 0)
                .Select(p => TroopRules.TryParseKey(p.Key, out var type, out var tier) ? new TroopStackDto { Type = type, Tier = tier, Count = p.Value } : null)
                .Where(s => s != null).Select(s => s!).OrderBy(s => s.Type, StringComparer.Ordinal).ThenByDescending(s => s.Tier).ToArray();

        private EntityChangeDto ArmyChange(DateTimeOffset now)
        {
            _armyDirty = false;
            return new EntityChangeDto { EntityId = EntityIds.Army, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(ArmyDto()) };
        }

        private long TroopPower() => TroopRules.Power(_catalog.Troops, _state.Troops);
    }
}
