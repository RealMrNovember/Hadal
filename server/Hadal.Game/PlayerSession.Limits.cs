using HADAL.Shared.Catalog;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;

namespace Hadal.Game
{
    /// <summary>
    /// Building copy limits (catalog countUnlocks, research extras). A base built before the limits applied keeps its
    /// strongest copies; the rest are taken down and everything spent on them comes back as bag packages.
    /// </summary>
    public sealed partial class PlayerSession
    {
        /// <summary>
        /// Removes copies above the limit for the current reactor level and refunds their full cost. Returns the
        /// refunds (empty when the base was within its limits). Caller holds the lock.
        /// </summary>
        private List<ResolvedReward> EnforceBuildingLimits(DateTimeOffset at)
        {
            var refunds = new Dictionary<ResourceType, long>();
            var reactor = ReactorLevel();
            foreach (var group in _state.Buildings.GroupBy(b => b.DefinitionId).ToList())
            {
                if (group.Key == BuildingProgression.ReactorId || !_catalog.TryGetBuilding(group.Key, out var definition))
                    continue;
                if (definition.CountUnlocks.Count == 0 && definition.MaxCount <= 0)
                    continue; // unlimited
                var allowed = definition.CountAt(reactor) + (_modifiers.ExtraLimits.TryGetValue(group.Key, out var extra) ? extra : 0);
                var extras = group.OrderByDescending(b => b.Level).ThenBy(b => b.IsUnderConstruction).Skip(Math.Max(0, allowed)).ToList();
                foreach (var building in extras)
                {
                    // Everything paid: each finished level, and the level being built.
                    var paidLevels = building.Level + (building.IsUnderConstruction ? 1 : 0);
                    for (var level = 1; level <= paidLevels; level++)
                    {
                        foreach (var cost in BuildingProgression.Cost(definition, level, _catalog.Progression))
                            refunds[cost.Key] = (refunds.TryGetValue(cost.Key, out var r) ? r : 0) + cost.Value;
                    }

                    _state.Buildings.Remove(building);
                    _state.UnsavedBuildings.Remove(building);
                    _state.RemovedBuildingIds.Add(building.BuildingId);
                    _state.IsDirty = true;
                }
            }

            var walletBeforeEnforce = WalletSnapshot();
            var rewards = new List<ResolvedReward>();
            foreach (var pair in refunds.Where(p => p.Value > 0))
            {
                if (Capsules.Holds(pair.Key))
                {
                    var id = Capsules.Id(pair.Key, pair.Value);
                    _state.Items[id] = (_state.Items.TryGetValue(id, out var n) ? n : 0) + 1;
                    rewards.Add(new ResolvedReward("item", id, 1));
                }
                else
                {
                    _state.Wallet[pair.Key] = (_state.Wallet.TryGetValue(pair.Key, out var w) ? w : 0) + pair.Value;
                    _walletDirty = true;
                    rewards.Add(new ResolvedReward("resource", pair.Key.ToString(), pair.Value));
                }
            }
            LogWalletDelta("refund", walletBeforeEnforce, at);

            if (rewards.Count > 0)
                RefreshModifiers();
            return rewards;
        }

        /// <summary>Tells the player what came back for buildings over the limit (shown like any reward list).</summary>
        private EntityChangeDto LimitRefundReport(List<ResolvedReward> rewards) => new()
        {
            EntityId = EntityIds.RewardReport + "limits",
            Kind = EntityChangeKind.Create,
            Payload = _serializer.Serialize(new RewardReportDto
            {
                Rewards = rewards.Select(r => new RewardEntryDto { Type = r.Type, Id = r.Id, Amount = r.Amount }).ToArray()
            })
        };

        internal List<ResolvedReward> EnforceBuildingLimitsForTests()
        {
            lock (_gate)
                return EnforceBuildingLimits(DateTimeOffset.UtcNow);
        }
    }
}
