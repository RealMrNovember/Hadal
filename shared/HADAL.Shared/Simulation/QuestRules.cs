using System;
using System.Collections.Generic;
using HADAL.Shared.Catalog;

namespace HADAL.Shared.Simulation
{
    /// <summary>Why a quest, chest, item or recruit command was refused. Sent in <c>CommandAckDto.Reason</c>.</summary>
    public enum QuestRejectReason
    {
        None = 0,
        UnknownQuest = 1,
        NotCurrent = 2,
        NotDone = 3,
        AlreadyClaimed = 4,
        UnknownChest = 5,
        NotEnoughPoints = 6,
        UnknownItem = 7,
        NotEnoughItems = 8,
        AlreadyOwned = 9,
        NotEnoughCards = 10,
    }

    /// <summary>What a quest's progress is measured against; built from the player's state.</summary>
    public sealed class QuestContext
    {
        public Func<string, int> BuildingLevel { get; set; } = _ => 0;
        public Func<string, int> BuildingCount { get; set; } = _ => 0;
        public IReadOnlyDictionary<string, long> Counters { get; set; } = new Dictionary<string, long>();
        public int MaxHeroLevel { get; set; }
        public int HeroCount { get; set; }
        public bool InAlliance { get; set; }
    }

    /// <summary>A reward resolved to something concrete (a random hero card becomes a hero id).</summary>
    public readonly struct ResolvedReward
    {
        public ResolvedReward(string type, string id, long amount)
        {
            Type = type;
            Id = id;
            Amount = amount;
        }

        public string Type { get; }
        public string Id { get; }
        public long Amount { get; }
    }

    /// <summary>Quests, daily activity, gift chests and hero cards (docs/27_Quests_And_Items.md).</summary>
    public static class QuestRules
    {
        public static readonly string[] Counters = { "upgrade", "research", "salvage", "boost", "pod", "heroTrain", "help", "chat" };

        public static bool IsKnownType(string? type) => type is "level" or "count" or "counter" or "heroLevel" or "heroes" or "alliance";

        /// <summary>Progress towards <see cref="QuestData.count"/> (capped there).</summary>
        public static long Progress(QuestData quest, QuestContext context)
        {
            long value = quest.type switch
            {
                "level" => context.BuildingLevel(quest.target),
                "count" => context.BuildingCount(quest.target),
                "counter" => context.Counters.TryGetValue(quest.target, out var n) ? n : 0,
                "heroLevel" => context.MaxHeroLevel,
                "heroes" => context.HeroCount,
                "alliance" => context.InAlliance ? 1 : 0,
                _ => 0
            };
            return Math.Min(value, Math.Max(1, quest.count));
        }

        public static bool IsDone(QuestData quest, QuestContext context) => Progress(quest, context) >= Math.Max(1, quest.count);

        /// <summary>Day number for daily resets (00:00 UTC).</summary>
        public static int Day(DateTimeOffset now) => (int)(now.ToUnixTimeSeconds() / 86400);

        public static long MillisecondsToReset(DateTimeOffset now) => (Day(now) + 1L) * 86_400_000L - now.ToUnixTimeMilliseconds();

        /// <summary>Turns rewards into concrete ones; random hero cards pick a hero of the rarity with <paramref name="roll"/>.</summary>
        public static List<ResolvedReward> Resolve(GameCatalog catalog, IEnumerable<RewardData> rewards, Func<int, int> roll)
        {
            var result = new List<ResolvedReward>();
            foreach (var r in rewards)
            {
                if (r == null || r.amount <= 0)
                    continue;
                if (r.type == "shards" && string.IsNullOrEmpty(r.id))
                {
                    var pool = new List<HeroDefinition>();
                    foreach (var hero in catalog.Heroes)
                    {
                        if (hero.Rarity.id == r.rarity)
                            pool.Add(hero);
                    }

                    if (pool.Count > 0)
                        result.Add(new ResolvedReward("shards", pool[roll(pool.Count)].Id, r.amount));
                    continue;
                }

                result.Add(new ResolvedReward(r.type, r.id, r.amount));
            }

            return result;
        }

        /// <summary>What opening one <paramref name="item"/> gives (guaranteed part plus weighted draws), resolved.</summary>
        public static List<ResolvedReward> Open(GameCatalog catalog, ItemData item, Func<int, int> roll)
        {
            var picked = new List<RewardData>(item.guaranteed ?? Array.Empty<RewardData>());
            var table = item.table ?? Array.Empty<WeightedRewardData>();
            var total = 0;
            foreach (var w in table)
                total += Math.Max(0, w?.weight ?? 0);

            for (var i = 0; i < item.rolls && total > 0; i++)
            {
                var n = roll(total);
                foreach (var w in table)
                {
                    n -= Math.Max(0, w?.weight ?? 0);
                    if (n < 0)
                    {
                        picked.Add(w!.reward);
                        break;
                    }
                }
            }

            return Resolve(catalog, picked, roll);
        }

        public static int RecruitShards(HeroDefinition hero) => Math.Max(1, hero.Rarity.recruitShards);
    }
}
