using System;

namespace HADAL.Shared.Catalog
{
    /// <summary>Heroes, the Cryo-Pod and the command crew (docs/25_Heroes_Alliance_Chat.md).</summary>
    [Serializable]
    public sealed class HeroCatalogData
    {
        public int maxStars = 5;

        /// <summary>Highest level per star: stars x levelCapPerStar.</summary>
        public int levelCapPerStar = 10;

        /// <summary>Every base starts with this hero on crew slot 0.</summary>
        public string starterHeroId = string.Empty;

        /// <summary>Training cost from level L to L+1 = levelCost x levelCostGrowth^(L-1) x the rarity's costMultiplier.</summary>
        public ResourceAmountData[] levelCost = Array.Empty<ResourceAmountData>();

        public double levelCostGrowth = 1.08;

        /// <summary>Shards to reach star 2, 3, 4, 5.</summary>
        public int[] shardsPerStar = Array.Empty<int>();

        /// <summary>Reactor level that opens each command crew slot; the slot count is the array length.</summary>
        public int[] crewSlotReactorLevels = Array.Empty<int>();

        public HeroRarityData[] rarities = Array.Empty<HeroRarityData>();
        public CryoPodData pod = new CryoPodData();
        public HeroData[] list = Array.Empty<HeroData>();
    }

    [Serializable]
    public sealed class HeroRarityData
    {
        public string id = string.Empty;

        /// <summary>Chance weight in the Cryo-Pod.</summary>
        public int weight;

        /// <summary>Shards a duplicate pull of this rarity gives.</summary>
        public int duplicateShards = 10;

        /// <summary>Shards (hero cards) that recruit a hero of this rarity the player does not have yet.</summary>
        public int recruitShards = 10;

        public double costMultiplier = 1;
        public int basePower = 100;
    }

    /// <summary>The Cryo-Pod Salvage pull (docs/15_Monetization.md).</summary>
    [Serializable]
    public sealed class CryoPodData
    {
        public ResourceAmountData[] cost = Array.Empty<ResourceAmountData>();
        public ResourceAmountData[] tenCost = Array.Empty<ResourceAmountData>();

        /// <summary>One free single pull per this many seconds; 0 = none.</summary>
        public int freeSeconds = 86400;

        /// <summary>Pulls without a <see cref="pityRarity"/> or better before one is guaranteed.</summary>
        public int pity = 30;

        public string pityRarity = "epic";
    }

    [Serializable]
    public sealed class HeroData
    {
        public string id = string.Empty;

        /// <summary>Commander, Scientist, Engineer, Hunter, Medic, Pilot (docs/10).</summary>
        public string heroClass = string.Empty;

        /// <summary>AtlanticUnion, PacificOrder, Deepborn, ForgottenOnes, AbyssCult (docs/10).</summary>
        public string faction = string.Empty;

        public string rarity = string.Empty;

        /// <summary>Same effect kinds as research: buildTime, researchTime, production:&lt;Resource&gt;, capacity:All.</summary>
        public string effect = string.Empty;

        /// <summary>Effect at level 1, 1 star (percent; negative = faster for times).</summary>
        public int effectBase;

        /// <summary>Added per level above 1, in tenths of a percent.</summary>
        public int effectPerLevelTenths;

        /// <summary>Added per star above 1, in percent.</summary>
        public int effectPerStar;

        /// <summary>Accent colour (hex, no #) for the hero's frame and placeholder portrait.</summary>
        public string color = "4FC3F7";

        /// <summary>Painted portrait id (Assets/_Hadal/Art/Portraits/&lt;id&gt;.png).</summary>
        public string portrait = string.Empty;

        /// <summary>Animated 3D character shown on the hero screen (Assets/_Hadal/Art/Characters/&lt;id&gt;.fbx).</summary>
        public string model = string.Empty;
    }

    /// <summary>Alliances (docs/25).</summary>
    [Serializable]
    public sealed class AllianceCatalogData
    {
        public ResourceAmountData[] createCost = Array.Empty<ResourceAmountData>();
        public int maxMembers = 30;
        public int nameMin = 3;
        public int nameMax = 20;
        public int tagMin = 3;
        public int tagMax = 4;

        /// <summary>Helps one construction can receive.</summary>
        public int maxHelps = 10;

        /// <summary>Each help cuts the remaining time by this percent of the total, at least <see cref="helpMinSeconds"/>.</summary>
        public int helpPercent = 1;

        public int helpMinSeconds = 60;
    }

    /// <summary>World and alliance chat (docs/25).</summary>
    [Serializable]
    public sealed class ChatCatalogData
    {
        public int maxLength = 200;
        public double minIntervalSeconds = 2;

        /// <summary>Messages kept per channel and sent on login.</summary>
        public int history = 50;
    }
}
