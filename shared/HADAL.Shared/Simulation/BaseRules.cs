using System;
using System.Collections.Generic;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>Why removing a building was refused. Sent in <c>CommandAckDto.Reason</c>.</summary>
    public enum RemoveRejectReason
    {
        None = 0,
        UnknownBuilding = 1,
        CannotRemoveReactor = 2,
        UpgradeInProgress = 3,
    }

    /// <summary>Why opening a ring was refused. Sent in <c>CommandAckDto.Reason</c>.</summary>
    public enum RingRejectReason
    {
        None = 0,
        NotNextRing = 1,
        MaxRingReached = 2,
        ReactorLevelTooLow = 3,
        MegaDomeRequired = 4,
        InsufficientResources = 5,
    }

    /// <summary>
    /// Removal and ring rules of docs/23_Base_Management.md (rules 12 and 14). Shared so the client
    /// shows the same refund and the same requirements the server applies.
    /// </summary>
    public static class BaseRules
    {
        /// <summary>
        /// A finished building may go (not the reactor); a first construction may be cancelled; an
        /// upgrade must finish first.
        /// </summary>
        public static RemoveRejectReason ValidateRemove(string definitionId, int level, bool underConstruction)
        {
            if (string.IsNullOrEmpty(definitionId))
                return RemoveRejectReason.UnknownBuilding;
            if (definitionId == BuildingProgression.ReactorId)
                return RemoveRejectReason.CannotRemoveReactor;
            if (underConstruction && level > 0)
                return RemoveRejectReason.UpgradeInProgress;
            return RemoveRejectReason.None;
        }

        /// <summary>
        /// What removing gives back: the whole level-1 cost when the first construction is cancelled
        /// (level 0), otherwise <c>removeRefundPercent</c> of every level's cost up to <paramref name="level"/>.
        /// </summary>
        public static List<KeyValuePair<ResourceType, long>> Refund(BuildingDefinition building, int level, ProgressionData rules)
        {
            if (level <= 0)
                return BuildingProgression.Cost(building, 1, rules);

            var spent = new Dictionary<ResourceType, long>();
            for (var l = 1; l <= level; l++)
            {
                foreach (var pair in BuildingProgression.Cost(building, l, rules))
                    spent[pair.Key] = (spent.TryGetValue(pair.Key, out var sum) ? sum : 0L) + pair.Value;
            }

            var percent = Math.Max(0, Math.Min(100, rules.removeRefundPercent));
            var refund = new List<KeyValuePair<ResourceType, long>>(spent.Count);
            foreach (var pair in spent)
            {
                var amount = pair.Value * percent / 100L;
                if (amount > 0)
                    refund.Add(new KeyValuePair<ResourceType, long>(pair.Key, amount));
            }

            return refund;
        }

        /// <summary>
        /// Rings open one at a time from the inside out. <paramref name="ownedFinished"/> holds the
        /// definitions the base has finished at least once (for the Mega Dome requirement).
        /// </summary>
        public static RingRejectReason ValidateUnlock(
            GameCatalog catalog,
            int ring,
            int unlockedRings,
            int reactorLevel,
            ICollection<string> ownedFinished,
            IReadOnlyDictionary<ResourceType, long> wallet)
        {
            if (unlockedRings >= catalog.Grid.maxRings)
                return RingRejectReason.MaxRingReached;
            if (ring != unlockedRings + 1 || !catalog.TryGetRingUnlock(ring, out var unlock))
                return RingRejectReason.NotNextRing;
            if (reactorLevel < unlock.ReactorLevel)
                return RingRejectReason.ReactorLevelTooLow;
            if (!string.IsNullOrEmpty(unlock.RequiresBuilding) && !ownedFinished.Contains(unlock.RequiresBuilding))
                return RingRejectReason.MegaDomeRequired;
            return BuildingProgression.CanAfford(unlock.Cost, wallet)
                ? RingRejectReason.None
                : RingRejectReason.InsufficientResources;
        }
    }
}
