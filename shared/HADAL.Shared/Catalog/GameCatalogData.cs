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
        public ProgressionData progression = new ProgressionData();
        public BuildingDefinitionData[] buildings = Array.Empty<BuildingDefinitionData>();
        public GridLayoutData grid = new GridLayoutData();

        /// <summary>Requirements and costs of rings beyond <see cref="GridLayoutData.startingUnlockedRings"/>.</summary>
        public RingUnlockData[] ringUnlocks = Array.Empty<RingUnlockData>();

        // Building functions (docs/24_Building_Functions.md).
        public ResearchCatalogData research = new ResearchCatalogData();
        public BoostData boost = new BoostData();
        public OverdriveData overdrive = new OverdriveData();
        public SalvageMissionData[] salvageMissions = Array.Empty<SalvageMissionData>();

        // Heroes, alliances and chat (docs/25_Heroes_Alliance_Chat.md).
        public HeroCatalogData heroes = new HeroCatalogData();
        public AllianceCatalogData alliance = new AllianceCatalogData();
        public ChatCatalogData chat = new ChatCatalogData();

        // Quests, gift chests and hero cards (docs/27_Quests_And_Items.md).
        public QuestCatalogData quests = new QuestCatalogData();
        public ItemData[] items = System.Array.Empty<ItemData>();

        public WorldCatalogData world = new WorldCatalogData();

        // Gathering and the Depth Map campaign (docs/rehber 08, 09; income sources).
        public GatherCatalogData gather = new GatherCatalogData();
        public ExpeditionCatalogData expedition = new ExpeditionCatalogData();

        // Balance infrastructure (docs/rehber, Paket A).
        public ReactorRequirementData[] reactorRequirements = Array.Empty<ReactorRequirementData>();

        /// <summary>A free player's Titanium per hour by reactor level ("Gelir Saati"; stamps resource capsules).</summary>
        public long[] incomeHour = Array.Empty<long>();

        public double incomeBiomassRatio = 0.4;
    }

    /// <summary>What opening one outer ring takes (docs/23_Base_Management.md, rule 12).</summary>
    [Serializable]
    public sealed class RingUnlockData
    {
        public int ring;
        public int reactorLevel;

        /// <summary>A building the base must own (finished), e.g. the Mega Dome; empty = none.</summary>
        public string requiresBuilding = string.Empty;

        public ResourceAmountData[] cost = Array.Empty<ResourceAmountData>();
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

        /// <summary>Cap by Core Reactor level (index = level); empty = <see cref="maxCapacity"/> at every level.</summary>
        public long[] capacityByReactor = Array.Empty<long>();

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

        /// <summary>Construction time of level 1; higher levels scale by <see cref="ProgressionData.timeGrowth"/>.</summary>
        public int buildSeconds;

        /// <summary>Balance table per level (index 0 = level 1); empty = the formula fields above.</summary>
        public BuildingLevelData[] levels = Array.Empty<BuildingLevelData>();

        /// <summary>Reactor level that unlocks each copy; empty = <see cref="maxCount"/> copies from <see cref="requiredReactorLevel"/>.</summary>
        public int[] countUnlocks = Array.Empty<int>();

        /// <summary>0 = <see cref="ProgressionData.maxLevel"/>.</summary>
        public int maxLevel;

        /// <summary>Core Reactor level needed to build it; 0 = always.</summary>
        public int requiredReactorLevel;

        /// <summary>How many one base may own (including ones under construction); 0 = unlimited.</summary>
        public int maxCount;
    }

    /// <summary>Building level progression (docs/23_Base_Management.md).</summary>
    [Serializable]
    public sealed class ProgressionData
    {
        public int maxLevel = 10;

        /// <summary>Cost multiplier per level: cost(L) = buildCost x costGrowth^(L-1).</summary>
        public double costGrowth = 1.55;

        /// <summary>Time multiplier per level: seconds(L) = buildSeconds x timeGrowth^(L-1).</summary>
        public double timeGrowth = 1.6;

        /// <summary>Constructions with this much time left (or less) can be finished for free.</summary>
        public int freeFinishSeconds = 300;

        /// <summary>Constructions that may run at the same time.</summary>
        public int builderSlots = 1;

        /// <summary>Share of everything spent on a finished building that removing it gives back.</summary>
        public int removeRefundPercent = 50;
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
