using System;
using System.Collections.Generic;
using HADAL.Shared.DTOs;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Mobilization (Seferberlik, docs/29 §4.5), shared by the server (pool, scoring) and the client (cards, texts).
    /// <para>
    /// Every alliance of the event gets a pool of task cards. A member takes one card at a time; the slot it came from
    /// draws a new card after a short refill. The task is finished by playing: the scoring hooks the events already use
    /// (gathering, training, research, …) move it on, and the moment it is done its points go to the player and to the
    /// alliance. A held task can be dropped (it is gone) or refreshed (rerolled, a few times a day with a cooldown).
    /// Cards come in four colours; the rarer the colour, the bigger the target and the more points:
    /// </para>
    /// <code>
    ///   tier     colour   share   target × base   points
    ///   1        green     45 %       1           100
    ///   2        blue      30 %       2           200
    ///   3        purple    17 %       3           340
    ///   4        gold       8 %       5           560
    /// </code>
    /// Only alliances with <see cref="DefaultMinMembers"/> members (event setting) can take tasks. Cards are drawn from a
    /// stable hash of event, alliance, slot and draw number, so a restart or a second server deals the same cards.
    /// </summary>
    public static class MobilizationRules
    {
        public const int DefaultMinMembers = 15;
        public const int DefaultSlots = 8;
        public const int MaxSlots = 16;
        public const int DefaultRefillMinutes = 5;
        public const int DefaultTasksPerDay = 8;
        public const int DefaultRefreshesPerDay = 3;
        public const int DefaultRefreshCooldownMinutes = 10;

        public const int Green = 1;
        public const int Blue = 2;
        public const int Purple = 3;
        public const int Gold = 4;

        /// <summary>Colour names by tier (index 0 unused): client art and localisation keys.</summary>
        public static readonly string[] TierNames = { string.Empty, "green", "blue", "purple", "gold" };

        private static readonly int[] TierWeights = { 0, 45, 30, 17, 8 };
        private static readonly long[] TierPoints = { 0, 100, 200, 340, 560 };
        private static readonly long[] TierScale = { 0, 1, 2, 3, 5 };

        public static string TierName(int tier) => tier >= 1 && tier <= Gold ? TierNames[tier] : TierNames[Green];

        public static long Points(int tier) => tier >= 1 && tier <= Gold ? TierPoints[tier] : TierPoints[Green];

        /// <summary>A task kind: its id, the green target, and the scoring hooks that move it.</summary>
        public sealed class Kind
        {
            public Kind(string id, long baseTarget, params string[] actions)
            {
                Id = id;
                BaseTarget = baseTarget;
                Actions = actions;
            }

            public string Id { get; }
            public long BaseTarget { get; }
            public string[] Actions { get; }
        }

        /// <summary>Every task kind, from the existing score kinds (docs/29 §2.2), in the Overseer panel's order.</summary>
        public static readonly Kind[] Kinds =
        {
            new Kind("gather", 150_000, EventActions.Gather),
            new Kind("train", 60, EventActions.Train(1), EventActions.Train(2), EventActions.Train(3), EventActions.Train(4), EventActions.Train(5)),
            new Kind("research", 1, EventActions.Research),
            new Kind("build_power", 1_500, EventActions.BuildPower),
            new Kind("monster_kill", 3, EventActions.MonsterKill),
            new Kind("heal", 150, EventActions.Heal),
            new Kind("speedup_minutes", 90, EventActions.SpeedupMinutes),
            new Kind("hero_level", 4, EventActions.HeroLevel),
            new Kind("help", 15, EventActions.Help),
            new Kind("dive", 2, EventActions.Dive)
        };

        public static Kind? Find(string id)
        {
            foreach (var kind in Kinds)
            {
                if (kind.Id == id)
                    return kind;
            }

            return null;
        }

        public static bool IsKind(string id) => Find(id) != null;

        /// <summary>Whether a scoring hook moves a task of this kind.</summary>
        public static bool Moves(string kindId, string action)
        {
            var kind = Find(kindId);
            return kind != null && Array.IndexOf(kind.Actions, action) >= 0;
        }

        /// <summary>The target of a task: base × tier scale × the event's percentage (at least 1).</summary>
        public static long Target(string kindId, int tier, int percent)
        {
            var kind = Find(kindId);
            if (kind == null)
                return 1;
            var t = tier >= 1 && tier <= Gold ? tier : Green;
            var scaled = kind.BaseTarget * TierScale[t] * Math.Max(1, percent) / 100;
            return Math.Max(1, scaled);
        }

        /// <summary>
        /// The card a slot deals on its <paramref name="draw"/>-th draw: kind and colour from a stable hash, so the same
        /// event, alliance, slot and draw always deal the same card. <paramref name="kinds"/> limits the kinds (empty = all).
        /// </summary>
        public static MobilizationTaskDto Deal(string eventId, string allianceId, int slot, int draw, IList<string>? kinds, int percent)
        {
            var allowed = new List<Kind>();
            foreach (var kind in Kinds)
            {
                if (kinds == null || kinds.Count == 0 || kinds.Contains(kind.Id))
                    allowed.Add(kind);
            }

            if (allowed.Count == 0)
                allowed.AddRange(Kinds);
            var hash = Hash(eventId + "|" + allianceId + "|" + slot + "|" + draw);
            var chosen = allowed[(int)(hash % (uint)allowed.Count)];
            var roll = (int)((hash >> 8) % 100u);
            var tier = Green;
            var sum = 0;
            for (var t = Green; t <= Gold; t++)
            {
                sum += TierWeights[t];
                if (roll < sum)
                {
                    tier = t;
                    break;
                }
            }

            return new MobilizationTaskDto
            {
                Kind = chosen.Id, Tier = tier, Target = Target(chosen.Id, tier, percent), Points = Points(tier), Slot = slot
            };
        }

        /// <summary>The reroll of a held task: a fresh card for the player's <paramref name="refresh"/>-th refresh.</summary>
        public static MobilizationTaskDto Reroll(string eventId, string playerId, int refresh, IList<string>? kinds, int percent)
        {
            var card = Deal(eventId, "refresh:" + playerId, -1, refresh, kinds, percent);
            card.Slot = -1;
            return card;
        }

        /// <summary>FNV-1a over UTF-16 code units: the same value on every machine and run (string.GetHashCode is not).</summary>
        public static uint Hash(string text)
        {
            var hash = 2166136261u;
            foreach (var c in text)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            // A final mix so neighbouring draws do not share their low bits.
            hash ^= hash >> 15;
            hash *= 0x2c1b3c6du;
            hash ^= hash >> 12;
            return hash;
        }

        /// <summary>Progress as a share 0…1.</summary>
        public static float Share(long progress, long target) => target <= 0 ? 1f : Math.Min(1f, Math.Max(0f, (float)progress / target));
    }
}
