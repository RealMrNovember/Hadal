using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// VIP (genre staple, docs/15): points from the daily sign-in streak, Hadalite purchases and VIP point items raise
    /// the VIP level; each level speeds building, research and marches, raises production and storage, opens an extra
    /// builder at VIP 6, and pays a daily VIP chest. Levels and bonuses follow Last Asylum's VIP 1-15 table (VIP 9 at
    /// 100,000 points, one point per premium gem, day two of a sign-in run worth 55 points).
    /// </summary>
    public static class VipRules
    {
        /// <summary>Points needed for VIP 0 … 15.</summary>
        public static readonly long[] Thresholds =
            { 0, 100, 500, 1_500, 4_000, 10_000, 20_000, 35_000, 60_000, 100_000, 150_000, 250_000, 400_000, 600_000, 900_000, 1_500_000 };

        public static int MaxLevel => Thresholds.Length - 1;

        // Bonuses by level (index = VIP level).
        private static readonly int[] BuildSpeed = { 0, 0, 0, 10, 10, 15, 20, 25, 30, 35, 40, 45, 50, 50, 50, 50 };
        private static readonly int[] ResearchSpeed = { 0, 0, 0, 0, 0, 0, 0, 5, 8, 10, 13, 15, 18, 20, 23, 25 };
        private static readonly int[] Production = { 0, 3, 5, 8, 10, 13, 15, 18, 20, 23, 25, 25, 25, 25, 25, 25 };
        private static readonly int[] MarchSpeed = { 0, 0, 0, 0, 5, 5, 5, 5, 8, 8, 10, 10, 13, 13, 15, 15 };
        private static readonly int[] Capacity = { 0, 0, 0, 5, 5, 10, 10, 15, 15, 20, 20, 25, 25, 30, 30, 35 };
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
            public Perks(int build, int research, int production, int march, int capacity, int builders)
            {
                BuildSpeed = build;
                ResearchSpeed = research;
                Production = production;
                MarchSpeed = march;
                Capacity = capacity;
                ExtraBuilders = builders;
            }

            public int BuildSpeed { get; }
            public int ResearchSpeed { get; }
            public int Production { get; }
            public int MarchSpeed { get; }
            public int Capacity { get; }
            public int ExtraBuilders { get; }
        }

        public static Perks PerksOf(int level)
        {
            level = Math.Max(0, Math.Min(MaxLevel, level));
            return new Perks(BuildSpeed[level], ResearchSpeed[level], Production[level], MarchSpeed[level], Capacity[level], level >= ExtraBuilderLevel ? 1 : 0);
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
            modifiers.MarchSpeedPercent += perks.MarchSpeed;
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

        /// <summary>VIP points for signing in on day <paramref name="streak"/> of a run (1 = first day): 30, 55, 80, … up to 300.</summary>
        public static long LoginPoints(int streak) => Math.Min(300, 30 + 25L * Math.Max(0, streak - 1));

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
