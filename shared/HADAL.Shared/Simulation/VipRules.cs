using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// VIP (genre staple, docs/15): points from the daily sign-in streak, Hadalite purchases and VIP point items raise
    /// the VIP level; each level speeds building and research, raises production and storage, opens an extra builder
    /// at VIP 6, and pays a daily VIP chest.
    /// </summary>
    public static class VipRules
    {
        /// <summary>Points needed for VIP 0 … 10.</summary>
        public static readonly long[] Thresholds = { 0, 100, 500, 1_500, 4_000, 10_000, 25_000, 50_000, 100_000, 200_000, 400_000 };

        public static int MaxLevel => Thresholds.Length - 1;

        // Bonuses by level (index = VIP level).
        private static readonly int[] BuildSpeed = { 0, 2, 4, 6, 8, 10, 12, 15, 18, 20, 25 };
        private static readonly int[] ResearchSpeed = { 0, 2, 4, 6, 8, 10, 12, 15, 18, 20, 25 };
        private static readonly int[] Production = { 0, 0, 2, 4, 5, 6, 8, 10, 12, 15, 20 };
        private static readonly int[] Capacity = { 0, 0, 0, 5, 5, 10, 10, 15, 15, 20, 25 };
        public const int ExtraBuilderLevel = 6;

        public const string ItemPrefix = "vip_";
        private static readonly Regex ItemPattern = new("^vip_(\\d{1,7})$", RegexOptions.CultureInvariant);

        public static int Level(long points)
        {
            var level = 0;
            for (var i = 1; i < Thresholds.Length; i++)
            {
                if (points >= Thresholds[i])
                    level = i;
            }

            return level;
        }

        /// <summary>One VIP level's bonuses, for the VIP page.</summary>
        public readonly struct Perks
        {
            public Perks(int build, int research, int production, int capacity, int builders)
            {
                BuildSpeed = build;
                ResearchSpeed = research;
                Production = production;
                Capacity = capacity;
                ExtraBuilders = builders;
            }

            public int BuildSpeed { get; }
            public int ResearchSpeed { get; }
            public int Production { get; }
            public int Capacity { get; }
            public int ExtraBuilders { get; }
        }

        public static Perks PerksOf(int level)
        {
            level = Math.Max(0, Math.Min(MaxLevel, level));
            return new Perks(BuildSpeed[level], ResearchSpeed[level], Production[level], Capacity[level], level >= ExtraBuilderLevel ? 1 : 0);
        }

        /// <summary>Adds the VIP bonuses to research and crew modifiers (server and client alike).</summary>
        public static void Apply(PlayerModifiers modifiers, int level)
        {
            var perks = PerksOf(level);
            if (level <= 0)
                return;
            modifiers.BuildTimePercent -= perks.BuildSpeed;
            modifiers.ResearchTimePercent -= perks.ResearchSpeed;
            modifiers.CapacityPercent += perks.Capacity;
            modifiers.ExtraBuilders += perks.ExtraBuilders;
            if (perks.Production > 0)
            {
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    if (type is ResourceType.Unknown or ResourceType.Research or ResourceType.Alloy or ResourceType.Hadalite)
                        continue;
                    modifiers.ProductionPercent[type] = (modifiers.ProductionPercent.TryGetValue(type, out var p) ? p : 0) + perks.Production;
                }
            }
        }

        /// <summary>VIP points for signing in on day <paramref name="streak"/> of a run (1 = first day): 40, 60, … up to 200.</summary>
        public static long LoginPoints(int streak) => Math.Min(200, 40 + 20L * Math.Max(0, streak - 1));

        /// <summary>The daily VIP chest of a level: speed-ups and resource packages growing with the level.</summary>
        public static List<RewardData> DailyChest(int level)
        {
            var rewards = new List<RewardData>();
            if (level <= 0)
                return rewards;
            rewards.Add(new RewardData { type = "item", id = Speedups.Id(SpeedupKind.Any, 60), amount = level });
            rewards.Add(new RewardData { type = "resource", id = nameof(ResourceType.Titanium), amount = 5_000L * level });
            rewards.Add(new RewardData { type = "resource", id = nameof(ResourceType.Food), amount = 5_000L * level });
            if (level >= 3)
                rewards.Add(new RewardData { type = "resource", id = nameof(ResourceType.Hadalite), amount = 10L * level });
            if (level >= 5)
                rewards.Add(new RewardData { type = "item", id = "chest_silver", amount = 1 });
            if (level >= 8)
                rewards.Add(new RewardData { type = "item", id = "chest_gold", amount = 1 });
            return rewards;
        }

        /// <summary>"vip_500": a bag item worth 500 VIP points.</summary>
        public static string Item(long points) => ItemPrefix + points;

        public static bool TryParseItem(string? id, out long points)
        {
            points = 0;
            var m = ItemPattern.Match(id ?? string.Empty);
            if (!m.Success)
                return false;
            points = long.Parse(m.Groups[1].Value);
            return points > 0;
        }
    }

    /// <summary>
    /// Hadalite packs of the shop. Prices are list prices in US cents; the store shows the platform's local price once
    /// payments exist. Every Hadalite bought also gives as many VIP points.
    /// </summary>
    public static class StorePacks
    {
        public sealed class Pack
        {
            public Pack(string id, long hadalite, long bonus, int priceCents, bool best = false)
            {
                Id = id;
                Hadalite = hadalite;
                Bonus = bonus;
                PriceCents = priceCents;
                Best = best;
            }

            public string Id { get; }
            public long Hadalite { get; }

            /// <summary>Extra Hadalite on top (first-purchase style bonus shown on the card).</summary>
            public long Bonus { get; }
            public int PriceCents { get; }
            public bool Best { get; }
            public long VipPoints => Hadalite;
        }

        public static readonly Pack[] All =
        {
            new Pack("hadalite_100", 100, 0, 99),
            new Pack("hadalite_550", 500, 50, 499),
            new Pack("hadalite_1200", 1_000, 200, 999),
            new Pack("hadalite_2600", 2_000, 600, 1_999),
            new Pack("hadalite_6800", 5_000, 1_800, 4_999, best: true),
            new Pack("hadalite_14000", 10_000, 4_000, 9_999),
        };

        public static Pack? Find(string id) => Array.Find(All, p => p.Id == id);
    }
}
