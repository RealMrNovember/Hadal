using System.Collections.Generic;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>Why a placement was refused. Sent to the client in <c>CommandAckDto.Reason</c>.</summary>
    public enum PlacementRejectReason
    {
        None = 0,
        UnknownBuilding = 1,
        SlotDoesNotExist = 2,
        RingLocked = 3,
        SectorLocked = 4,
        SlotOccupied = 5,
        RingNotAllowed = 6,
        SlotTypeMismatch = 7,
        SectorRestricted = 8,
        InsufficientResources = 9,
        ReactorLevelTooLow = 10,
        LimitReached = 11,
    }

    /// <summary>
    /// Authoritative building placement rules. Same order and semantics as the client preview
    /// (<c>CircularGridModel.ValidatePlacement</c>) plus the cost check the client cannot own.
    /// </summary>
    public static class PlacementRules
    {
        public static PlacementRejectReason Validate(
            GameCatalog catalog,
            string buildingId,
            int ring,
            int sector,
            int unlockedRings,
            ISet<(int ring, int sector)> occupied,
            IReadOnlyDictionary<ResourceType, long> wallet,
            IReadOnlyDictionary<string, int>? ownedCounts = null,
            int reactorLevel = int.MaxValue,
            PlayerModifiers? modifiers = null)
        {
            if (!catalog.TryGetBuilding(buildingId, out var building))
                return PlacementRejectReason.UnknownBuilding;

            if (!catalog.SlotExists(ring, sector))
                return PlacementRejectReason.SlotDoesNotExist;

            if (ring > unlockedRings)
                return PlacementRejectReason.RingLocked;

            if (catalog.SectorStartsLocked(ring, sector))
                return PlacementRejectReason.SectorLocked;

            if (occupied.Contains((ring, sector)))
                return PlacementRejectReason.SlotOccupied;

            if (ring < building.MinRing || ring > building.MaxRing)
                return PlacementRejectReason.RingNotAllowed;

            if (!building.MatchesSlotType(catalog.GetSlotType(ring, sector)))
                return PlacementRejectReason.SlotTypeMismatch;

            if (!catalog.SectorAllowsBuilding(ring, sector, building.Id))
                return PlacementRejectReason.SectorRestricted;

            // Copies open with the reactor (docs/rehber 03): the next copy needs its reactor level.
            var owned = ownedCounts != null && ownedCounts.TryGetValue(building.Id, out var have) ? have : 0;
            var maxCount = (modifiers ?? PlayerModifiers.None).MaxCount(building);
            if (maxCount > 0 && owned >= maxCount)
                return PlacementRejectReason.LimitReached;

            var extra = maxCount > 0 ? maxCount - building.MaxCount : 0;
            if (reactorLevel < building.RequiredReactorLevel
                || (maxCount > 0 && owned >= building.CountAt(reactorLevel) + extra))
                return PlacementRejectReason.ReactorLevelTooLow;

            if (!BuildingProgression.CanAfford(BuildingProgression.Cost(building, 1, catalog.Progression), wallet))
                return PlacementRejectReason.InsufficientResources;

            return PlacementRejectReason.None;
        }

        /// <summary>Costs summed per resource, so duplicate lines cannot slip past the check.</summary>
        public static bool CanAfford(BuildingDefinition building, IReadOnlyDictionary<ResourceType, long> wallet)
        {
            var required = new Dictionary<ResourceType, long>();
            foreach (var cost in building.BuildCost)
            {
                required.TryGetValue(cost.Key, out var sum);
                required[cost.Key] = sum + cost.Value;
            }

            foreach (var pair in required)
            {
                wallet.TryGetValue(pair.Key, out var balance);
                if (balance < pair.Value)
                    return false;
            }

            return true;
        }

        /// <summary>Deducts the build cost. Call only after <see cref="Validate"/> returned None.</summary>
        public static void Charge(BuildingDefinition building, IDictionary<ResourceType, long> wallet)
        {
            foreach (var cost in building.BuildCost)
            {
                wallet.TryGetValue(cost.Key, out var balance);
                wallet[cost.Key] = balance - cost.Value;
            }
        }
    }
}
