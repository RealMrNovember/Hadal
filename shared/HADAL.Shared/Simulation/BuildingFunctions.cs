using System;
using System.Collections.Generic;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>Why Boost, Overdrive or a salvage run was refused. Sent in <c>CommandAckDto.Reason</c>.</summary>
    public enum FunctionRejectReason
    {
        None = 0,
        UnknownBuilding = 1,
        NotAvailableHere = 2,
        UnderConstruction = 3,
        AlreadyRunning = 4,
        OnCooldown = 5,
        UnknownMission = 6,
        InsufficientResources = 7,
    }

    /// <summary>
    /// What each building's function button does (docs/24_Building_Functions.md): Boost on
    /// producers, Overdrive on the Core Reactor, salvage runs from the Drone Factory. Shared so the
    /// client shows exactly what the server will accept.
    /// </summary>
    public static class BuildingFunctions
    {
        public const string DroneFactoryId = "drone_factory";

        /// <summary>Producers other than the reactor can be boosted.</summary>
        public static bool CanBoost(BuildingDefinition building) =>
            building.Id != BuildingProgression.ReactorId && building.Production.Count > 0;

        public static bool CanOverdrive(BuildingDefinition building) => building.Id == BuildingProgression.ReactorId;

        public static bool CanSalvage(BuildingDefinition building) => building.Id == DroneFactoryId;

        /// <summary>A building works (and can use its function) once its first construction is done.</summary>
        private static FunctionRejectReason Ready(BuildingDefinition? building, int level)
        {
            if (building == null)
                return FunctionRejectReason.UnknownBuilding;
            return level <= 0 ? FunctionRejectReason.UnderConstruction : FunctionRejectReason.None;
        }

        public static FunctionRejectReason ValidateBoost(GameCatalog catalog, BuildingDefinition? building, int level, bool boosted,
            IReadOnlyDictionary<ResourceType, long> wallet)
        {
            var ready = Ready(building, level);
            if (ready != FunctionRejectReason.None)
                return ready;
            if (!CanBoost(building!))
                return FunctionRejectReason.NotAvailableHere;
            if (boosted)
                return FunctionRejectReason.AlreadyRunning;
            return BuildingProgression.CanAfford(catalog.BoostCost, wallet)
                ? FunctionRejectReason.None
                : FunctionRejectReason.InsufficientResources;
        }

        public static FunctionRejectReason ValidateOverdrive(BuildingDefinition? building, int level, bool running, bool coolingDown)
        {
            var ready = Ready(building, level);
            if (ready != FunctionRejectReason.None)
                return ready;
            if (!CanOverdrive(building!))
                return FunctionRejectReason.NotAvailableHere;
            if (running)
                return FunctionRejectReason.AlreadyRunning;
            return coolingDown ? FunctionRejectReason.OnCooldown : FunctionRejectReason.None;
        }

        public static FunctionRejectReason ValidateSalvage(GameCatalog catalog, BuildingDefinition? building, int level, bool running, string missionId)
        {
            var ready = Ready(building, level);
            if (ready != FunctionRejectReason.None)
                return ready;
            if (!CanSalvage(building!))
                return FunctionRejectReason.NotAvailableHere;
            if (running)
                return FunctionRejectReason.AlreadyRunning;
            return catalog.TryGetSalvageMission(missionId, out _) ? FunctionRejectReason.None : FunctionRejectReason.UnknownMission;
        }

        /// <summary>Guaranteed rewards of a run from a Drone Factory of <paramref name="level"/>.</summary>
        public static List<KeyValuePair<ResourceType, long>> SalvageRewards(SalvageMission mission, int level)
        {
            var percent = 100L + mission.LevelBonusPercent * (long)Math.Max(0, level - 1);
            var rewards = new List<KeyValuePair<ResourceType, long>>(mission.Rewards.Count);
            foreach (var pair in mission.Rewards)
                rewards.Add(new KeyValuePair<ResourceType, long>(pair.Key, pair.Value * percent / 100L));
            return rewards;
        }

        /// <summary>The Hadalite a run finds, decided by the seed drawn when it started (0 = none).</summary>
        public static long SalvageHadalite(SalvageMission mission, int seed)
        {
            if (mission.HadaliteChancePercent <= 0 || mission.HadaliteMax <= 0)
                return 0;
            var random = new Random(seed);
            if (random.Next(100) >= mission.HadaliteChancePercent)
                return 0;
            return random.Next(mission.HadaliteMin, mission.HadaliteMax + 1);
        }
    }
}
