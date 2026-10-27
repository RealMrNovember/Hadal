using System;

namespace HADAL.Shared.Catalog
{
    /// <summary>
    /// One building level from the design guide's balance table (docs/rehber, catalog v9, Paket A).
    /// Written by tools/rehber/katalog.py; when a building has these, they win over the old
    /// buildCost x costGrowth formulas.
    /// </summary>
    [Serializable]
    public sealed class BuildingLevelData
    {
        /// <summary>Core Reactor level needed to build or upgrade to this level.</summary>
        public int reactor;

        public ResourceAmountData[] cost = Array.Empty<ResourceAmountData>();
        public int seconds;

        /// <summary>Power this level adds (docs/rehber K-14).</summary>
        public int power;

        public HourlyAmountData[] production = Array.Empty<HourlyAmountData>();
        public HourlyAmountData[] consumption = Array.Empty<HourlyAmountData>();
    }

    /// <summary>Production or consumption per hour (docs/rehber K-17); may be fractional.</summary>
    [Serializable]
    public sealed class HourlyAmountData
    {
        public string resource = string.Empty;
        public double perHour;
    }

    /// <summary>What raising the Core Reactor to <see cref="level"/> needs besides resources (docs/rehber 02).</summary>
    [Serializable]
    public sealed class ReactorRequirementData
    {
        public int level;
        public BuildingLevelRequirementData[] requires = Array.Empty<BuildingLevelRequirementData>();
    }

    [Serializable]
    public sealed class BuildingLevelRequirementData
    {
        public string building = string.Empty;
        public int level;
    }
}
