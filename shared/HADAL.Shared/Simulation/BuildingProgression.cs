using System;
using System.Collections.Generic;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>Why a construction command was refused. Sent in <c>CommandAckDto.Reason</c>.</summary>
    public enum ConstructionRejectReason
    {
        None = 0,
        UnknownBuilding = 1,
        BuilderBusy = 2,
        MaxLevel = 3,
        ReactorLevelTooLow = 4,
        InsufficientResources = 5,
        UnderConstruction = 6,
        NotUnderConstruction = 7,
        NotYetFree = 8,

        /// <summary>The reactor's prerequisite buildings are not at the required level yet (docs/rehber 02).</summary>
        ReactorPrerequisite = 9,
    }

    /// <summary>
    /// Level costs, times and the upgrade rules of docs/23_Base_Management.md. Shared so the
    /// client shows exactly the numbers the server will charge.
    /// </summary>
    public static class BuildingProgression
    {
        public const string ReactorId = "core_reactor";

        /// <summary>Cost to reach <paramref name="targetLevel"/> (1 = new building): the balance table, else the formula.</summary>
        public static List<KeyValuePair<ResourceType, long>> Cost(BuildingDefinition building, int targetLevel, ProgressionData rules)
        {
            var row = building.Level(targetLevel);
            if (row != null)
                return new List<KeyValuePair<ResourceType, long>>(row.Cost);

            var factor = Math.Pow(rules.costGrowth, Math.Max(0, targetLevel - 1));
            var cost = new List<KeyValuePair<ResourceType, long>>(building.BuildCost.Count);
            foreach (var pair in building.BuildCost)
                cost.Add(new KeyValuePair<ResourceType, long>(pair.Key, (long)Math.Round(pair.Value * factor)));
            return cost;
        }

        /// <summary>Construction seconds to reach <paramref name="targetLevel"/>.</summary>
        public static int Seconds(BuildingDefinition building, int targetLevel, ProgressionData rules) =>
            building.Level(targetLevel)?.Seconds
            ?? (int)Math.Round(building.BuildSeconds * Math.Pow(rules.timeGrowth, Math.Max(0, targetLevel - 1)));

        /// <summary>Reactor level a building needs to reach <paramref name="targetLevel"/> (the reactor itself: none).</summary>
        public static int ReactorFor(BuildingDefinition building, int targetLevel)
        {
            if (building.Id == ReactorId)
                return 0;
            return building.Level(targetLevel)?.Reactor ?? targetLevel;
        }

        /// <summary>Power of one building at <paramref name="level"/>: every level's table power added up (docs/rehber K-14).</summary>
        public static long Power(BuildingDefinition building, int level)
        {
            if (building.Levels.Count == 0)
                return level * 10L; // catalogs without a table: the old "levels x 10"
            long power = 0;
            for (var l = 1; l <= Math.Min(level, building.Levels.Count); l++)
                power += building.Levels[l - 1].Power;
            return power;
        }

        /// <summary>Prerequisites of raising the reactor to <paramref name="targetLevel"/> that are not met yet.</summary>
        public static List<KeyValuePair<string, int>> MissingReactorPrerequisites(GameCatalog catalog, int targetLevel, Func<string, int> buildingLevel)
        {
            var missing = new List<KeyValuePair<string, int>>();
            foreach (var need in catalog.ReactorRequirements(targetLevel))
            {
                if (buildingLevel(need.Key) < need.Value)
                    missing.Add(need);
            }

            return missing;
        }

        public static bool CanAfford(IReadOnlyList<KeyValuePair<ResourceType, long>> cost, IReadOnlyDictionary<ResourceType, long> wallet)
        {
            foreach (var pair in cost)
            {
                if (!wallet.TryGetValue(pair.Key, out var have) || have < pair.Value)
                    return false;
            }

            return true;
        }

        public static void Charge(IReadOnlyList<KeyValuePair<ResourceType, long>> cost, IDictionary<ResourceType, long> wallet)
        {
            foreach (var pair in cost)
                wallet[pair.Key] = (wallet.TryGetValue(pair.Key, out var have) ? have : 0L) - pair.Value;
        }

        /// <summary>
        /// Upgrade rules in priority order: the building must exist and be idle, a builder must be
        /// free, the level must be below the max and (except for the reactor) not above the reactor.
        /// </summary>
        public static ConstructionRejectReason ValidateUpgrade(
            GameCatalog catalog,
            string definitionId,
            int currentLevel,
            bool underConstruction,
            int activeConstructions,
            int reactorLevel,
            IReadOnlyDictionary<ResourceType, long> wallet,
            int extraBuilders = 0,
            Func<string, int>? buildingLevel = null)
        {
            if (!catalog.TryGetBuilding(definitionId, out var building))
                return ConstructionRejectReason.UnknownBuilding;

            if (underConstruction)
                return ConstructionRejectReason.UnderConstruction;

            var rules = catalog.Progression;
            if (activeConstructions >= Math.Max(1, rules.builderSlots) + extraBuilders)
                return ConstructionRejectReason.BuilderBusy;

            if (currentLevel >= building.MaxLevel)
                return ConstructionRejectReason.MaxLevel;

            if (definitionId != ReactorId && ReactorFor(building, currentLevel + 1) > reactorLevel)
                return ConstructionRejectReason.ReactorLevelTooLow;

            if (definitionId == ReactorId && buildingLevel != null
                && MissingReactorPrerequisites(catalog, currentLevel + 1, buildingLevel).Count > 0)
                return ConstructionRejectReason.ReactorPrerequisite;

            return CanAfford(Cost(building, currentLevel + 1, rules), wallet)
                ? ConstructionRejectReason.None
                : ConstructionRejectReason.InsufficientResources;
        }
    }
}
