using System;

namespace HADAL.Shared.Catalog
{
    // Raw shape of shared/catalog/catalog.json. Public fields and arrays only, so both Unity's
    // JsonUtility (editor setup) and System.Text.Json with IncludeFields (server) can read it.
    // Enum-like values are strings for readability; GameCatalog parses and validates them.

    [Serializable]
    public sealed class GameCatalogData
    {
        public int version;
        public ResourceDefinitionData[] resources = Array.Empty<ResourceDefinitionData>();
        public BuildingDefinitionData[] buildings = Array.Empty<BuildingDefinitionData>();
        public GridLayoutData grid = new GridLayoutData();
    }

    [Serializable]
    public sealed class ResourceDefinitionData
    {
        public string type = string.Empty;
        public string displayName = string.Empty;
        public string description = string.Empty;
        public long startingAmount;

        /// <summary>Storage cap; zero or negative means uncapped.</summary>
        public long maxCapacity;

        public bool critical;
    }

    [Serializable]
    public sealed class BuildingDefinitionData
    {
        public string id = string.Empty;
        public string displayName = string.Empty;

        /// <summary>Client presentation only (Hadal.Data.Enums.BuildingCategory).</summary>
        public string category = string.Empty;

        /// <summary>Client presentation only (Hadal.Data.Enums.DomeType).</summary>
        public string dome = "None";

        public string slotType = "Universal";
        public int minRing;
        public int maxRing = 12;
        public ResourceAmountData[] production = Array.Empty<ResourceAmountData>();
        public ResourceAmountData[] consumption = Array.Empty<ResourceAmountData>();
        public ResourceAmountData[] buildCost = Array.Empty<ResourceAmountData>();
    }

    [Serializable]
    public sealed class ResourceAmountData
    {
        public string resource = string.Empty;
        public long amount;
    }

    [Serializable]
    public sealed class GridLayoutData
    {
        public int maxRings = 12;
        public int startingUnlockedRings = 4;
        public float rotationSnapDegrees = 60f;
        public float slotYOffset = 0.05f;
        public int outerRingBaseSectors = 6;
        public int outerRingSectorIncrement = 2;
        public int outerRingMaxSectors = 16;
        public RingData[] rings = Array.Empty<RingData>();
    }

    [Serializable]
    public sealed class RingData
    {
        public int ring;
        public float radius;
        public int sectorCount;
        public float startAngleOffset;
        public string defaultSlotType = "Universal";
        public bool expandable;
        public SectorData[] sectors = Array.Empty<SectorData>();
    }

    [Serializable]
    public sealed class SectorData
    {
        public int sector;
        public string slotType = "Universal";
        public string preferredBuildingId = string.Empty;
        public string[] allowedBuildingIds = Array.Empty<string>();
        public bool startsLocked;
    }
}
