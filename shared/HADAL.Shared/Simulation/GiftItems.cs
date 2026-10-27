using System;
using System.Text.RegularExpressions;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Bag items the Overseer sends: a hero at a chosen level and star count, and a pack of troops. Like resource
    /// capsules they carry everything in their id, so they need no catalog entry. Using a hero item recruits the hero
    /// (or raises an owned one to at least that level and stars); using a troop pack puts the troops in the army.
    /// </summary>
    public static class GiftItems
    {
        public const string HeroPrefix = "herogift_";
        public const string TroopPrefix = "troops_";

        private static readonly Regex HeroPattern = new("^herogift_([a-z0-9]+)_(\\d{1,3})_(\\d{1,2})$", RegexOptions.CultureInvariant);
        private static readonly Regex TroopPattern = new("^troops_([a-z0-9]+)_(\\d{1,2})_(\\d{1,9})$", RegexOptions.CultureInvariant);

        /// <summary>"herogift_kael_30_3": Kael at level 30 with 3 stars.</summary>
        public static string Hero(string heroId, int level, int stars) => $"{HeroPrefix}{heroId}_{level}_{stars}";

        /// <summary>"troops_armored_3_500": 500 armored troops of tier 3.</summary>
        public static string Troops(string type, int tier, long count) => $"{TroopPrefix}{type}_{tier}_{count}";

        public static bool TryParseHero(string? id, out string heroId, out int level, out int stars)
        {
            heroId = string.Empty;
            level = stars = 0;
            var m = HeroPattern.Match(id ?? string.Empty);
            if (!m.Success)
                return false;
            heroId = m.Groups[1].Value;
            level = int.Parse(m.Groups[2].Value);
            stars = int.Parse(m.Groups[3].Value);
            return true;
        }

        public static bool TryParseTroops(string? id, out string type, out int tier, out long count)
        {
            type = string.Empty;
            tier = 0;
            count = 0;
            var m = TroopPattern.Match(id ?? string.Empty);
            if (!m.Success)
                return false;
            type = m.Groups[1].Value;
            tier = int.Parse(m.Groups[2].Value);
            count = long.Parse(m.Groups[3].Value);
            return true;
        }

        /// <summary>Highest level a hero may have at this star count.</summary>
        public static int MaxLevel(HeroCatalogData rules, int stars) => Math.Max(1, stars) * Math.Max(1, rules.levelCapPerStar);

        /// <summary>Whether the id names a real hero with a reachable level and star count, or real troops.</summary>
        public static bool IsValid(GameCatalog catalog, string id)
        {
            if (TryParseHero(id, out var heroId, out var level, out var stars))
            {
                var rules = catalog.HeroRules;
                return catalog.TryGetHero(heroId, out _) && stars >= 1 && stars <= rules.maxStars && level >= 1 && level <= MaxLevel(rules, stars);
            }

            if (TryParseTroops(id, out var type, out var tier, out var count))
                return TroopRules.Type(catalog.Troops, type) != null && tier >= 1 && tier <= catalog.Troops.tiers.Length && count is >= 1 and <= 10_000_000;
            return false;
        }

        public static bool Is(string? id) => id != null && (id.StartsWith(HeroPrefix, StringComparison.Ordinal) || id.StartsWith(TroopPrefix, StringComparison.Ordinal));
    }
}
