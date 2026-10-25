using System;

namespace HADAL.Shared.Catalog
{
    /// <summary>Research Center tree (docs/24_Building_Functions.md).</summary>
    [Serializable]
    public sealed class ResearchCatalogData
    {
        /// <summary>Cost multiplier per node level: cost(L) = baseCost x costGrowth^(L-1).</summary>
        public double costGrowth = 1.4;

        /// <summary>Time multiplier per node level.</summary>
        public double timeGrowth = 1.5;

        /// <summary>Research Center levels needed per extra node level: level L needs requiredCenterLevel + centerStep x (L-1).</summary>
        public int centerStep = 2;

        /// <summary>Researches that may run at once before any "researchSlots" node.</summary>
        public int baseSlots = 1;

        public ResearchNodeData[] nodes = Array.Empty<ResearchNodeData>();
    }

    [Serializable]
    public sealed class ResearchNodeData
    {
        public string id = string.Empty;

        /// <summary>Tab in the research screen: "development", "economy", ...</summary>
        public string tree = string.Empty;

        public int maxLevel = 1;

        /// <summary>
        /// What each level does: buildTime, researchTime (percent, negative = faster),
        /// production:&lt;Resource&gt;, capacity:All (percent), limit:&lt;buildingId&gt;,
        /// builderSlots, researchSlots (counts).
        /// </summary>
        public string effect = string.Empty;

        public int valuePerLevel;
        public int requiredCenterLevel = 1;
        public int baseSeconds;
        public ResourceAmountData[] baseCost = Array.Empty<ResourceAmountData>();
        public ResearchRequirementData[] requires = Array.Empty<ResearchRequirementData>();
    }

    [Serializable]
    public sealed class ResearchRequirementData
    {
        public string id = string.Empty;
        public int level = 1;
    }

    /// <summary>Boost: a production building works harder for a while (docs/24).</summary>
    [Serializable]
    public sealed class BoostData
    {
        public int percent = 30;
        public int seconds = 1800;
        public ResourceAmountData[] cost = Array.Empty<ResourceAmountData>();
    }

    /// <summary>Overdrive: the Core Reactor's energy burst with a cooldown (docs/24).</summary>
    [Serializable]
    public sealed class OverdriveData
    {
        public int percent = 50;
        public int seconds = 3600;
        public int cooldownSeconds = 28800;
    }

    /// <summary>A Drone Factory salvage run (docs/24).</summary>
    [Serializable]
    public sealed class SalvageMissionData
    {
        public string id = string.Empty;
        public int seconds;
        public ResourceAmountData[] rewards = Array.Empty<ResourceAmountData>();

        /// <summary>Reward bonus per Drone Factory level above 1, in percent.</summary>
        public int levelBonusPercent = 10;

        public int hadaliteChancePercent;
        public int hadaliteMin;
        public int hadaliteMax;
    }
}
