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

        /// <summary>The swarm's size against <see cref="MonsterRules.Count(int)"/> (a boss is one huge body: many "troops").</summary>
        public double CountScale = 1;

        /// <summary>Share of a belt's swarms of this kind, against the other kinds the belt's levels allow.</summary>
        public int Weight = 1;

        /// <summary>Titanium capsule size against an ordinary swarm of the same level.</summary>
        public double RewardScale = 1;

        /// <summary>
        /// An event boss: the map never lays it out by itself; an event (its Bosses list) puts it on a tile for the
        /// event's duration. Every team may hit it, no one takes it, and it stays until the event ends.
        /// </summary>
        public bool EventOnly;
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
            new() { Id = "abyss_crawler", CountsAs = "armored", HpScale = 0.8, MinLevel = 1, MaxLevel = 25, Sonar = 40, Weight = 3 },
            new() { Id = "siren_swarm", CountsAs = "arc", AtkScale = 1.2, HpScale = 0.7, MinLevel = 8, MaxLevel = 35, Sonar = 50, Weight = 2 },

            // The common mid-depth hunter: bites hard, breaks easily.
            new() { Id = "abyss_angler", CountsAs = "harpoon", AtkScale = 1.25, HpScale = 0.85, MinLevel = 10, MaxLevel = 28, Sonar = 50, Weight = 3, RewardScale = 1.2 },

            // Deeper and heavier: a bigger school that bites harder still.
            new() { Id = "lantern_maw", CountsAs = "harpoon", AtkScale = 1.4, HpScale = 1.1, MinLevel = 20, MaxLevel = 35, Sonar = 65, Weight = 1, CountScale = 1.3, RewardScale = 1.8 },

            // The floor tank: weak claws, a shell that takes long to crack.
            new() { Id = "armored_crab", CountsAs = "armored", AtkScale = 0.8, DefScale = 1.5, HpScale = 1.6, MinLevel = 14, MaxLevel = 35, Sonar = 60, Weight = 2, RewardScale = 1.5 },

            // Event bosses: one colossal body that a whole alliance takes turns on.
            new() { Id = "chasm_maw", CountsAs = "armored", AtkScale = 1.3, DefScale = 1.4, HpScale = 2.5, MinLevel = 10, MaxLevel = 35, Sonar = 80, CountScale = 6, RewardScale = 4, EventOnly = true },
            new() { Id = "depth_kraken", CountsAs = "arc", AtkScale = 1.6, DefScale = 1.2, HpScale = 2.2, MinLevel = 15, MaxLevel = 35, Sonar = 100, CountScale = 8, RewardScale = 5, EventOnly = true }
        };

        public static MonsterKind? Kind(string id) => Kinds.FirstOrDefault(k => k.Id == id);

        /// <summary>The kinds the map lays out by itself (event bosses only come with an event).</summary>
        public static MonsterKind[] Roaming => Kinds.Where(k => !k.EventOnly).ToArray();

        /// <summary>The roaming kinds whose levels overlap the belt's.</summary>
        public static MonsterKind[] KindsInBelt(string belt)
        {
            var (min, max) = LevelsInBelt(belt);
            return Kinds.Where(k => !k.EventOnly && k.MinLevel <= max && k.MaxLevel >= min).ToArray();
        }

        /// <summary>Picks one of <paramref name="kinds"/> by weight; <paramref name="roll"/>(n) returns 0 … n-1.</summary>
        public static MonsterKind Pick(IReadOnlyList<MonsterKind> kinds, Func<int, int> roll)
        {
            var r = roll(Math.Max(1, kinds.Sum(k => Math.Max(1, k.Weight))));
            foreach (var kind in kinds)
            {
                r -= Math.Max(1, kind.Weight);
                if (r < 0)
                    return kind;
            }

            return kinds[kinds.Count - 1];
        }

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

        /// <summary>The kind's swarm size at a level (<see cref="MonsterKind.CountScale"/>).</summary>
        public static long Count(MonsterKind kind, int level) => (long)Math.Round(Count(level) * kind.CountScale);

        public static BattleSide Army(MonsterKind kind, int level)
        {
            var side = new BattleSide { AtkScale = kind.AtkScale, DefScale = kind.DefScale, HpScale = kind.HpScale };
            side.Troops[TroopRules.Key(kind.CountsAs, Tier(level))] = Count(kind, level);
            return side;
        }

        /// <summary>The monster's power as players compare it (troop power × count).</summary>
        public static long Power(TroopCatalogData troops, int level) =>
            (TroopRules.Tier(troops, Tier(level))?.power ?? 1) * Count(level);

        /// <summary>
        /// A kind's power: troop power × its swarm size, weighed by the mean of its attack, defence and health scales,
        /// so a crab's shell or a boss reads stronger than a crawler swarm of the same level.
        /// </summary>
        public static long Power(TroopCatalogData troops, MonsterKind kind, int level) =>
            (long)Math.Round((TroopRules.Tier(troops, Tier(level))?.power ?? 1) * Count(kind, level) * (kind.AtkScale + kind.DefScale + kind.HpScale) / 3);

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
        public static Dictionary<string, int> Rewards(int level, bool firstWin, Func<int, int> roll) =>
            Rewards(Kinds[0], level, firstWin, roll);

        /// <summary>A hit on an event boss that did not bring it down still pays a small capsule for the damage done.</summary>
        public static Dictionary<string, int> BossHitRewards(int level) =>
            new(StringComparer.Ordinal) { [Capsules.Id(ResourceType.Titanium, 100L * level)] = 1 };

        /// <summary>
        /// The kind's rewards: the capsule grows with <see cref="MonsterKind.RewardScale"/> (whole hundreds); an event
        /// boss has no first-win capsule; its final blow adds only a bronze chest, since every contributor is paid when it
        /// dies or its event ends (EventDefinition.BossRanks, mailed by the server).
        /// </summary>
        public static Dictionary<string, int> Rewards(MonsterKind kind, int level, bool firstWin, Func<int, int> roll)
        {
            var items = new Dictionary<string, int>(StringComparer.Ordinal);
            void Add(string id, int n) => items[id] = (items.TryGetValue(id, out var have) ? have : 0) + n;
            Add(Capsules.Id(ResourceType.Titanium, (long)Math.Round(2 * level * kind.RewardScale) * 100), 1);
            if (kind.EventOnly)
            {
                Add("chest_bronze", 1);
                return items;
            }

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
