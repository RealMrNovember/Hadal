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

        /// <summary>Troops at home plus the ones in training (both count against the army capacity).</summary>
        private long ArmyCount() => _state.Troops.Values.Sum() + _state.Training.Sum(t => (long)t.Count);

        private long ArmyCapacity() =>
            TroopRules.ArmyCapacity(_catalog.Troops, _state.Buildings.Select(b => (b.DefinitionId, b.Level)));

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
            BuildingProgression.Charge(cost, _state.Wallet);
            var seconds = TroopRules.Seconds(troops, tier, command.Count, building!.Level);
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
            foreach (var pair in training.Cost)
                _state.Wallet[pair.Key] = (_state.Wallet.TryGetValue(pair.Key, out var have) ? have : 0) + pair.Value;
            _state.IsDirty = true;
            SendDelta(now, new List<EntityChangeDto> { WalletChange(), ArmyChange(now), Accepted(command.CommandId, command.ClientSequence) });
        }

        /// <summary>A finished batch joins the army at home.</summary>
        private void CompleteTraining(TrainingState training)
        {
            _state.Training.Remove(training);
            var key = TroopRules.Key(training.Type, training.Tier);
            _state.Troops[key] = (_state.Troops.TryGetValue(key, out var have) ? have : 0) + training.Count;
            _armyDirty = true;
        }

        private ArmyDto ArmyDto()
        {
            var (food, oxygen) = TroopRules.Upkeep(_catalog.Troops, _state.Troops);
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
                UpkeepOxygen = oxygen
            };
        }

        private EntityChangeDto ArmyChange(DateTimeOffset now)
        {
            _armyDirty = false;
            return new EntityChangeDto { EntityId = EntityIds.Army, Kind = EntityChangeKind.Update, Payload = _serializer.Serialize(ArmyDto()) };
        }

        private long TroopPower() => TroopRules.Power(_catalog.Troops, _state.Troops);
    }
}
