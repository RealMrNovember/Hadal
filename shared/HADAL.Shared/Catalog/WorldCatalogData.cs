using System;

namespace HADAL.Shared.Catalog
{
    /// <summary>The outer world (docs/28_World_Map.md, docs/rehber/09).</summary>
    [Serializable]
    public sealed class WorldCatalogData
    {
        /// <summary>The map is size x size tiles.</summary>
        public int size = 200;

        /// <summary>Tiles between neighbouring new bases.</summary>
        public int spacing = 3;

        /// <summary>Tiles around the centre one view returns.</summary>
        public int viewRadius = 14;

        // Resource nodes and gathering (docs/rehber 09 §4); written by tools/rehber/katalog.py.

        /// <summary>Depth belts from the rim inwards, by Chebyshev distance to the centre tile.</summary>
        public BeltData[] belts = Array.Empty<BeltData>();

        public NodeLevelData[] nodeLevels = Array.Empty<NodeLevelData>();
        public NodeTypeData[] nodeTypes = Array.Empty<NodeTypeData>();

        /// <summary>Nodes per 100 tiles of each belt and the mix of kinds.</summary>
        public DensityData[] density = Array.Empty<DensityData>();

        /// <summary>Load one unit of a resource takes in a march (1 = one Titanium).</summary>
        public LoadWeightData[] loadWeights = Array.Empty<LoadWeightData>();

        /// <summary>A depleted node comes back elsewhere in its belt after this long.</summary>
        public int respawnMinutes = 30;

        public int hadaliteDailyCap = 60;
        public int maxVeins = 12;

        /// <summary>Travel time per tile at base speed (docs/rehber 08 §4).</summary>
        public int marchSecondsPerTile = 60;
    }

    [Serializable]
    public sealed class BeltData
    {
        public string id = string.Empty;

        /// <summary>Chebyshev distance range to the centre, inclusive.</summary>
        public int inner;
        public int outer;

        public int nodeMin = 1;
        public int nodeMax = 1;

        /// <summary>Whether bases may stand in this belt.</summary>
        public bool bases;
    }

    [Serializable]
    public sealed class NodeLevelData
    {
        public int level;

        /// <summary>Load a fresh node holds.</summary>
        public long capacity;

        /// <summary>Load per hour without bonuses.</summary>
        public long baseRate;

        /// <summary>Typical gathering bonus at this level (research, heroes, alliance); used until those systems exist.</summary>
        public double bonus = 1;
    }

    [Serializable]
    public sealed class NodeTypeData
    {
        public string id = string.Empty;
        public int minLevel = 1;
        public int maxLevel = 1;
        public double capacityFactor = 1;

        /// <summary>Hours until the node fades on its own (whale falls); 0 = never.</summary>
        public int lifeHours;

        public NodeYieldData[] yields = Array.Empty<NodeYieldData>();
    }

    [Serializable]
    public sealed class NodeYieldData
    {
        public int level;
        public HourlyAmountData[] rates = Array.Empty<HourlyAmountData>();
    }

    [Serializable]
    public sealed class DensityData
    {
        public string belt = string.Empty;
        public int per100;
        public DensityWeightData[] weights = Array.Empty<DensityWeightData>();
    }

    [Serializable]
    public sealed class DensityWeightData
    {
        public string type = string.Empty;
        public int percent;
    }

    [Serializable]
    public sealed class LoadWeightData
    {
        public string resource = string.Empty;
        public double weight = 1;
    }

    /// <summary>March queues and carrying capacity (docs/rehber 08 §2.1, 15 §3.2).</summary>
    [Serializable]
    public sealed class GatherCatalogData
    {
        /// <summary>Core Reactor level that opens each march queue (one Submarine Hangar each).</summary>
        public int[] queueReactors = Array.Empty<int>();

        /// <summary>Load one march carries, by reactor level (stands in for troops until they exist).</summary>
        public long[] loadByReactor = Array.Empty<long>();

        /// <summary>Below this reactor level the Superalloy share of a node comes as Titanium (Alloy Foundry gate).</summary>
        public int alloyReactor = 30;

        /// <summary>
        /// Scales node yields, march loads and node sizes together (trip times stay the same). 1 for the guide's
        /// v9 economy; the live v8 economy runs nodes at 0.02 so a trip pays a few upgrades, not a flood.
        /// Hadalite is never scaled (it has its own daily limit).
        /// </summary>
        public double yieldScale = 1;
    }

    /// <summary>The Depth Map campaign (docs/rehber 08 §5).</summary>
    [Serializable]
    public sealed class ExpeditionCatalogData
    {
        public int stages = 10;

        /// <summary>Won dives a day that pay resources.</summary>
        public int dailyRewarded = 3;

        /// <summary>Oxygen returned after a lost dive.</summary>
        public int refundPercent = 80;

        /// <summary>Income hours a day the rewarded dives pay, before and after <see cref="lateFromReactor"/>.</summary>
        public double gsPerDayEarly = 1.65;
        public double gsPerDayLate = 1;
        public int lateFromReactor = 20;

        public ExpeditionChapterData[] chapters = Array.Empty<ExpeditionChapterData>();
    }

    [Serializable]
    public sealed class ExpeditionChapterData
    {
        public int chapter;
        public string id = string.Empty;
        public int reactor;
        public long oxygen;
        public long powerFirst;
        public long powerLast;
        public long powerBoss;
        public long bossHadalite;
        public long bossAlloy;
        public string bossCardRarity = string.Empty;
        public int bossCards;

        /// <summary>Superalloy each rewarded dive adds in this chapter.</summary>
        public long diveAlloy;
    }
}
