using System;

namespace HADAL.Shared.Catalog
{
    /// <summary>
    /// Troops (docs/rehber 07 §2–3, 19 §2): four types, each trained in its own training building, ten tiers with
    /// the same cost for every type; the Military Dome sets how many troops the base can keep.
    /// </summary>
    [Serializable]
    public sealed class TroopCatalogData
    {
        public TroopTypeData[] types = Array.Empty<TroopTypeData>();
        public TroopTierData[] tiers = Array.Empty<TroopTierData>();

        /// <summary>Army capacity by Military Dome level (anchors, linear between them; several domes add up).</summary>
        public LevelValueData[] capacity = Array.Empty<LevelValueData>();

        /// <summary>Batch size by training building level (anchors, linear between them).</summary>
        public LevelValueData[] batch = Array.Empty<LevelValueData>();

        /// <summary>Training speed per training building level, in percent (K-13 divisor).</summary>
        public double speedPerLevel = 0.5;

        /// <summary>Building that holds the army (capacity).</summary>
        public string capacityBuilding = "military_dome";
    }

    [Serializable]
    public sealed class TroopTypeData
    {
        public string id = string.Empty;

        /// <summary>Training building of this type (one queue each).</summary>
        public string building = string.Empty;

        public double atk = 1;
        public double def = 1;
        public double hp = 1;

        /// <summary>March speed, 100 = base.</summary>
        public int speed = 100;

        /// <summary>Load multiplier on the tier's load.</summary>
        public double load = 1;
    }

    [Serializable]
    public sealed class TroopTierData
    {
        public int tier = 1;

        /// <summary>Training building level that opens this tier.</summary>
        public int buildingLevel = 1;

        public int power = 1;
        public double food;
        public double biomass;
        public double titanium;
        public double alloy;

        /// <summary>Seconds per troop before speed bonuses.</summary>
        public double seconds = 1;

        public double upkeepFood;
        public double upkeepOxygen;
        public int load = 10;
    }

    [Serializable]
    public sealed class LevelValueData
    {
        public int level;
        public long value;
    }
}
