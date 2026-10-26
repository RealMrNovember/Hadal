using System;
using System.Collections.Generic;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>Why a hero command was refused. Sent in <c>CommandAckDto.Reason</c>.</summary>
    public enum HeroRejectReason
    {
        None = 0,
        UnknownHero = 1,
        NotOwned = 2,
        LevelCapReached = 3,
        MaxStars = 4,
        NotEnoughShards = 5,
        InsufficientResources = 6,
        SlotLocked = 7,
        UnknownBanner = 8,
    }

    /// <summary>One owned hero as the rules see it.</summary>
    public readonly struct OwnedHero
    {
        public OwnedHero(string heroId, int level, int stars, int shards, int slot)
        {
            HeroId = heroId;
            Level = level;
            Stars = stars;
            Shards = shards;
            Slot = slot;
        }

        public string HeroId { get; }
        public int Level { get; }
        public int Stars { get; }
        public int Shards { get; }

        /// <summary>Command crew slot, or -1 when the hero is not on the crew.</summary>
        public int Slot { get; }
    }

    /// <summary>Training, promotion, the command crew and the Cryo-Pod (docs/25_Heroes_Alliance_Chat.md).</summary>
    public static class HeroRules
    {
        public const string PodBanner = "cryo_pod";

        public static int LevelCap(HeroCatalogData rules, int stars) => Math.Max(1, stars) * Math.Max(1, rules.levelCapPerStar);

        /// <summary>Training from <paramref name="level"/> to level + 1.</summary>
        public static List<KeyValuePair<ResourceType, long>> LevelCost(GameCatalog catalog, HeroDefinition hero, int level)
        {
            var factor = Math.Pow(catalog.HeroRules.levelCostGrowth, Math.Max(0, level - 1)) * Math.Max(0.1, hero.Rarity.costMultiplier);
            var cost = new List<KeyValuePair<ResourceType, long>>();
            foreach (var pair in catalog.HeroLevelCost)
                cost.Add(new KeyValuePair<ResourceType, long>(pair.Key, (long)Math.Round(pair.Value * factor)));
            return cost;
        }

        /// <summary>Shards to go from <paramref name="stars"/> to stars + 1; 0 when already at the top.</summary>
        public static int ShardsForNextStar(HeroCatalogData rules, int stars)
        {
            var steps = rules.shardsPerStar ?? Array.Empty<int>();
            return stars < 1 || stars - 1 >= steps.Length || stars >= rules.maxStars ? 0 : steps[stars - 1];
        }

        /// <summary>Effect strength in percent at this level and star count.</summary>
        public static int EffectValue(HeroDefinition hero, int level, int stars) =>
            hero.EffectBase + Math.Max(0, level - 1) * hero.EffectPerLevelTenths / 10 + Math.Max(0, stars - 1) * hero.EffectPerStar;

        public static int Power(HeroDefinition hero, int level, int stars) =>
            (int)Math.Round(hero.Rarity.basePower * (1 + Math.Max(0, level - 1) * 0.1) * (1 + Math.Max(0, stars - 1) * 0.2));

        /// <summary>Command crew slots open at this reactor level.</summary>
        public static int OpenSlots(HeroCatalogData rules, int reactorLevel)
        {
            var open = 0;
            foreach (var needed in rules.crewSlotReactorLevels ?? Array.Empty<int>())
            {
                if (reactorLevel >= needed)
                    open++;
            }

            return open;
        }

        public static int SlotCount(HeroCatalogData rules) => (rules.crewSlotReactorLevels ?? Array.Empty<int>()).Length;

        public static HeroRejectReason ValidateLevelUp(GameCatalog catalog, HeroDefinition? hero, OwnedHero? owned, IReadOnlyDictionary<ResourceType, long> wallet)
        {
            if (hero == null)
                return HeroRejectReason.UnknownHero;
            if (owned == null)
                return HeroRejectReason.NotOwned;
            if (owned.Value.Level >= LevelCap(catalog.HeroRules, owned.Value.Stars))
                return HeroRejectReason.LevelCapReached;
            return BuildingProgression.CanAfford(LevelCost(catalog, hero, owned.Value.Level), wallet)
                ? HeroRejectReason.None
                : HeroRejectReason.InsufficientResources;
        }

        public static HeroRejectReason ValidatePromote(GameCatalog catalog, HeroDefinition? hero, OwnedHero? owned)
        {
            if (hero == null)
                return HeroRejectReason.UnknownHero;
            if (owned == null)
                return HeroRejectReason.NotOwned;
            var needed = ShardsForNextStar(catalog.HeroRules, owned.Value.Stars);
            if (needed <= 0)
                return HeroRejectReason.MaxStars;
            return owned.Value.Shards >= needed ? HeroRejectReason.None : HeroRejectReason.NotEnoughShards;
        }

        /// <summary>Slot -1 takes the hero off the crew.</summary>
        public static HeroRejectReason ValidateAssign(GameCatalog catalog, HeroDefinition? hero, OwnedHero? owned, int slot, int reactorLevel)
        {
            if (hero == null)
                return HeroRejectReason.UnknownHero;
            if (owned == null)
                return HeroRejectReason.NotOwned;
            if (slot == -1)
                return HeroRejectReason.None;
            return slot < 0 || slot >= Math.Min(SlotCount(catalog.HeroRules), OpenSlots(catalog.HeroRules, reactorLevel))
                ? HeroRejectReason.SlotLocked
                : HeroRejectReason.None;
        }

        /// <summary>Adds the crew's skills to research modifiers (only heroes on a crew slot count).</summary>
        public static void AddCrew(GameCatalog catalog, PlayerModifiers modifiers, IEnumerable<OwnedHero> heroes)
        {
            foreach (var owned in heroes)
            {
                if (owned.Slot < 0 || !catalog.TryGetHero(owned.HeroId, out var hero))
                    continue;
                ApplyEffect(modifiers, hero.Effect, EffectValue(hero, owned.Level, owned.Stars));
            }
        }

        internal static void ApplyEffect(PlayerModifiers modifiers, string effect, int value)
        {
            var colon = effect.IndexOf(':');
            var kind = colon < 0 ? effect : effect.Substring(0, colon);
            var target = colon < 0 ? string.Empty : effect.Substring(colon + 1);
            switch (kind)
            {
                case "buildTime": modifiers.BuildTimePercent += value; break;
                case "researchTime": modifiers.ResearchTimePercent += value; break;
                case "capacity": modifiers.CapacityPercent += value; break;
                case "production":
                    if (GameCatalog.TryParseResource(target, out var resource))
                        modifiers.ProductionPercent[resource] = (modifiers.ProductionPercent.TryGetValue(resource, out var p) ? p : 0) + value;
                    break;
            }
        }

        public static bool IsKnownEffect(string? effect)
        {
            if (string.IsNullOrEmpty(effect))
                return false;
            var colon = effect!.IndexOf(':');
            var kind = colon < 0 ? effect : effect.Substring(0, colon);
            var target = colon < 0 ? string.Empty : effect.Substring(colon + 1);
            return kind switch
            {
                "buildTime" or "researchTime" => colon < 0,
                "capacity" => target == "All",
                "production" => GameCatalog.TryParseResource(target, out _),
                _ => false
            };
        }

        /// <summary>
        /// One Cryo-Pod pull. <paramref name="sincePity"/> counts pulls since the last pity-rarity-or-better
        /// hero and is updated. <paramref name="roll"/> returns 0 &lt;= n &lt; max.
        /// </summary>
        public static HeroDefinition? Pull(GameCatalog catalog, ref int sincePity, Func<int, int> roll)
        {
            var rules = catalog.HeroRules;
            var rarities = rules.rarities ?? Array.Empty<HeroRarityData>();
            if (catalog.Heroes.Count == 0 || rarities.Length == 0)
                return null;

            var pityIndex = Array.FindIndex(rarities, r => r != null && r.id == rules.pod?.pityRarity);
            HeroRarityData? picked = null;

            if (pityIndex >= 0 && rules.pod!.pity > 0 && sincePity + 1 >= rules.pod.pity)
            {
                // Guaranteed: weighted among the pity rarity and everything above it.
                picked = Weighted(rarities, pityIndex, roll);
            }

            picked ??= Weighted(rarities, 0, roll);
            if (picked == null)
                return null;

            var pickedIndex = Array.IndexOf(rarities, picked);
            sincePity = pityIndex >= 0 && pickedIndex >= pityIndex ? 0 : sincePity + 1;

            var pool = new List<HeroDefinition>();
            foreach (var hero in catalog.Heroes)
            {
                if (hero.Rarity.id == picked.id)
                    pool.Add(hero);
            }

            return pool.Count == 0 ? catalog.Heroes[roll(catalog.Heroes.Count)] : pool[roll(pool.Count)];
        }

        private static HeroRarityData? Weighted(HeroRarityData[] rarities, int from, Func<int, int> roll)
        {
            var total = 0;
            for (var i = from; i < rarities.Length; i++)
                total += Math.Max(0, rarities[i]?.weight ?? 0);
            if (total <= 0)
                return null;

            var n = roll(total);
            for (var i = from; i < rarities.Length; i++)
            {
                n -= Math.Max(0, rarities[i]?.weight ?? 0);
                if (n < 0)
                    return rarities[i];
            }

            return rarities[rarities.Length - 1];
        }
    }
}
