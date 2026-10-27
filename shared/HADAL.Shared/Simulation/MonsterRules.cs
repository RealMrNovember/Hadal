using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>A kind of map monster (docs/rehber 09 §5.2, 07 §3 enemies).</summary>
    public sealed class MonsterKind
    {
        public string Id = string.Empty;

        /// <summary>The troop type its swarm counts as in battle (rows and counters).</summary>
        public string CountsAs = "armored";

        public double AtkScale = 1, DefScale = 1, HpScale = 1;
        public int MinLevel = 1, MaxLevel = 25;

        /// <summary>Sonar Energy one attack costs.</summary>
        public int Sonar = 40;
    }

    /// <summary>Why an attack was refused.</summary>
    public enum AttackRejectReason
    {
        None = 0,
        UnknownMonster = 1,
        MonsterBusy = 2,
        NotEnoughSonar = 3
    }

    /// <summary>
    /// Map monsters, Sonar Energy and their rewards (docs/rehber 09 §5): a swarm's size and tier grow with its level;
    /// PvE never kills troops, the lost come home wounded; the first win at each level pays a bigger capsule.
    /// </summary>
    public static class MonsterRules
    {
        public static readonly MonsterKind[] Kinds =
        {
            new() { Id = "abyss_crawler", CountsAs = "armored", HpScale = 0.8, MinLevel = 1, MaxLevel = 25, Sonar = 40 },
            new() { Id = "siren_swarm", CountsAs = "arc", AtkScale = 1.2, HpScale = 0.7, MinLevel = 8, MaxLevel = 35, Sonar = 50 }
        };

        public static MonsterKind? Kind(string id) => Kinds.FirstOrDefault(k => k.Id == id);

        /// <summary>Monster levels each belt holds: the outer plain and the Threshold are the beginner grounds.</summary>
        public static (int Min, int Max) LevelsInBelt(string belt) => belt switch
        {
            "abisal_duzluk" => (1, 6),
            "hadal_esigi" => (1, 10),
            "cukur_yamaci" => (8, 20),
            "derin_yamac" => (15, 30),
            "cukur_ekseni" => (25, 35),
            _ => (30, 35)
        };

        /// <summary>Monsters per 100 tiles of a belt.</summary>
        public const double Per100Tiles = 0.6;

        public static int Tier(int level) => Math.Clamp(1 + (level - 1) / 3, 1, 10);

        public static long Count(int level) => (long)Math.Round(20 * Math.Pow(1.25, Math.Max(0, level - 1)));

        public static BattleSide Army(MonsterKind kind, int level)
        {
            var side = new BattleSide { AtkScale = kind.AtkScale, DefScale = kind.DefScale, HpScale = kind.HpScale };
            side.Troops[TroopRules.Key(kind.CountsAs, Tier(level))] = Count(level);
            return side;
        }

        /// <summary>The monster's power as players compare it (troop power × count).</summary>
        public static long Power(TroopCatalogData troops, int level) =>
            (TroopRules.Tier(troops, Tier(level))?.power ?? 1) * Count(level);

        // --- Sonar Energy (09 §5.1) --------------------------------------------------------------

        public const int SonarCapacity = 500;
        public const double SonarPerHour = 30;

        public static double SonarNow(double stored, DateTimeOffset storedAt, DateTimeOffset now) =>
            Math.Min(SonarCapacity, stored + Math.Max(0, (now - storedAt).TotalHours) * SonarPerHour);

        // --- Rewards ------------------------------------------------------------------------------

        /// <summary>
        /// A win pays a small Titanium capsule by level; the first win at a level a bigger one; and a chance of a
        /// speed-up or a bronze chest. Items go to the bag when the team comes home.
        /// </summary>
        public static Dictionary<string, int> Rewards(int level, bool firstWin, Func<int, int> roll)
        {
            var items = new Dictionary<string, int>(StringComparer.Ordinal);
            void Add(string id, int n) => items[id] = (items.TryGetValue(id, out var have) ? have : 0) + n;
            Add(Capsules.Id(ResourceType.Titanium, 200L * level), 1);
            if (firstWin)
                Add(Capsules.Id(ResourceType.Titanium, 1000L * level), 1);
            var r = roll(100);
            if (r < 12)
                Add(Speedups.Id(SpeedupKind.Any, 5), 1);
            else if (r < 18)
                Add("chest_bronze", 1);
            return items;
        }
    }
}
