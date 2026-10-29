using System;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Alliance gifts (docs/31 §2, Last Asylum): when a member defeats a monster, buys Hadalite or wins an event rank, a
    /// gift drops for every member of the alliance at that moment. Each member collects it once within
    /// <see cref="Lifetime"/>: an alliance chest for themselves, <see cref="CoinsPerGift"/> alliance coins, and gift
    /// points for the alliance. Points raise the alliance's gift level, and the level picks the chest.
    /// <code>
    ///   gift level   points needed   chest
    ///   1            0               bronze
    ///   2            2,000           bronze
    ///   3            6,000           bronze
    ///   4            15,000          silver
    ///   5            35,000          silver
    ///   6            75,000          silver
    ///   7            150,000         gold
    ///   8            300,000         gold
    ///   9            600,000         gold
    ///   10           1,200,000       gold
    ///   points per collect: monster 5 + its level, purchase 20 per 1,000 Hadalite (at least 20), event 50
    /// </code>
    /// </summary>
    public static class AllianceGiftRules
    {
        public const string Monster = "monster";
        public const string Purchase = "purchase";
        public const string Event = "event";

        public static readonly TimeSpan Lifetime = TimeSpan.FromDays(3);

        /// <summary>Gifts kept per alliance (the oldest go first).</summary>
        public const int Keep = 100;

        public const int CoinsPerGift = 10;

        public static readonly long[] LevelPoints = { 0, 0, 2_000, 6_000, 15_000, 35_000, 75_000, 150_000, 300_000, 600_000, 1_200_000 };

        public static int MaxLevel => LevelPoints.Length - 1;

        public static bool IsSource(string? source) => source == Monster || source == Purchase || source == Event;

        /// <summary>The gift level for the alliance's gift points (1 to MaxLevel).</summary>
        public static int Level(long points)
        {
            var level = 1;
            while (level < MaxLevel && points >= LevelPoints[level + 1])
                level++;
            return level;
        }

        /// <summary>Points one collect adds: <paramref name="arg"/> is the monster's level or the Hadalite bought.</summary>
        public static long Points(string source, long arg) => source switch
        {
            Monster => 5 + Math.Max(0, arg),
            Purchase => Math.Max(20, arg / 1_000 * 20),
            Event => 50,
            _ => 0
        };

        /// <summary>The chest a gift holds at a gift level.</summary>
        public static string Chest(int level) => level >= 7 ? "chest_gold" : level >= 4 ? "chest_silver" : "chest_bronze";
    }
}
