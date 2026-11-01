using System;
using System.Collections.Generic;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// The Lure Trap (Yem Kapanı, docs/29 §4.1), shared by the server and the client. Members charge their alliance's trap
    /// with bait (every map monster they beat while the event is live); the bait levels the trap, and each level adds
    /// <see cref="AttackBonusPerLevel"/> to every hit on the Gulper. The leader or a war-duty officer schedules each of the
    /// <see cref="Traps"/> traps: a start time and a difficulty. At that time a Gulper rises at the alliance's rally point
    /// for <see cref="DefaultFightMinutes"/> minutes; only the alliance's members can hit it, and it never fights back
    /// (no troops are lost). Damage is the members' points; the alliance's damage over both traps decides its tier.
    /// </summary>
    public static class LureTrapRules
    {
        public const int Traps = 2;
        public const int MaxDifficulty = 10;
        public const int DefaultFightMinutes = 30;
        public const int DefaultMinMembers = 20;

        /// <summary>The Gulper's monster kind (MonsterRules, event only).</summary>
        public const string GulperKind = "gulper";

        /// <summary>Bait a trap needs for each level (index = level; level 0 needs none).</summary>
        public static readonly long[] BaitForLevel = { 0, 50, 150, 300, 500, 800 };

        public static int MaxLevel => BaitForLevel.Length - 1;

        public const double AttackBonusPerLevel = 0.05;

        /// <summary>A trap must be scheduled at least this far ahead, so members can plan for it.</summary>
        public const int MinLeadMinutes = 10;

        /// <summary>
        /// One hit fights only the front of the Gulper's vast body: this many ordinary swarms of its level
        /// (MonsterRules.Count), or what is left when less. The team chips at it; the body is the alliance's half hour.
        /// </summary>
        public const int FrontScale = 20;

        public static long Front(int level, long left) => Math.Min(Math.Max(0, left), MonsterRules.Count(level) * FrontScale);

        /// <summary>The trap's level for the bait it holds.</summary>
        public static int Level(long bait)
        {
            var level = 0;
            for (var i = 1; i < BaitForLevel.Length; i++)
            {
                if (bait >= BaitForLevel[i])
                    level = i;
            }

            return level;
        }

        /// <summary>Bait still missing for the next level (0 at the top).</summary>
        public static long BaitToNext(long bait)
        {
            var level = Level(bait);
            return level >= MaxLevel ? 0 : BaitForLevel[level + 1] - bait;
        }

        /// <summary>Extra damage of every hit at a trap level (0.05 a level).</summary>
        public static double AttackBonus(int level) => Math.Clamp(level, 0, MaxLevel) * AttackBonusPerLevel;

        /// <summary>The Gulper's monster level at a difficulty: 5 at difficulty 1, three more each step (32 at 10).</summary>
        public static int GulperLevel(int difficulty) => 5 + 3 * (Math.Clamp(difficulty, 1, MaxDifficulty) - 1);

        public static bool IsDifficulty(int difficulty) => difficulty >= 1 && difficulty <= MaxDifficulty;

        /// <summary>
        /// The alliance tier reached with <paramref name="damage"/> over both traps: the number of thresholds met (0 = none).
        /// Thresholds are ascending.
        /// </summary>
        public static int Tier(long damage, IReadOnlyList<long> thresholds)
        {
            var tier = 0;
            for (var i = 0; i < thresholds.Count; i++)
            {
                if (damage >= thresholds[i])
                    tier = i + 1;
            }

            return tier;
        }

        /// <summary>
        /// Whether a trap may be scheduled to start at <paramref name="startMs"/>: far enough ahead, and its whole fight
        /// inside the event.
        /// </summary>
        public static bool CanStart(long startMs, long nowMs, long eventEndsMs, int fightMinutes) =>
            startMs >= nowMs + MinLeadMinutes * 60_000L && startMs + fightMinutes * 60_000L <= eventEndsMs;
    }
}
