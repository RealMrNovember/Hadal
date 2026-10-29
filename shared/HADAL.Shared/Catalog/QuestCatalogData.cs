using System;

namespace HADAL.Shared.Catalog
{
    /// <summary>
    /// One reward. type: "resource" (id = ResourceType name), "item" (id = an items[] id), "shards"
    /// (id = a hero id, or empty with <see cref="rarity"/> for a random hero of that rarity) or "troops"
    /// (id = "type:tier", straight into the army at home; guide steps only). docs/27.
    /// </summary>
    [Serializable]
    public sealed class RewardData
    {
        public string type = "resource";
        public string id = string.Empty;
        public long amount;
        public string rarity = string.Empty;
    }

    [Serializable]
    public sealed class WeightedRewardData
    {
        public int weight = 1;
        public RewardData reward = new RewardData();
    }

    /// <summary>A bag item: gift chests and hero cards. Opening one pays its guaranteed rewards plus <see cref="rolls"/> draws from <see cref="table"/>.</summary>
    [Serializable]
    public sealed class ItemData
    {
        public string id = string.Empty;

        /// <summary>"chest" or "heroCard" (bag tab and frame colour only).</summary>
        public string kind = "chest";

        public string rarity = "common";
        public RewardData[] guaranteed = Array.Empty<RewardData>();
        public int rolls;
        public WeightedRewardData[] table = Array.Empty<WeightedRewardData>();
    }

    /// <summary>
    /// A task. type: "level" (target building reaches count), "count" (own count of target building),
    /// "counter" (event counter target reaches count: upgrade, research, salvage, boost, pod, heroTrain,
    /// help, chat, gather, attack, monster, open, helpAsk, helped, allianceChat, assist), "heroLevel" (any hero at level count),
    /// "heroes" (own count heroes), "alliance" (be in one), "rings" (count base rings open).
    /// </summary>
    [Serializable]
    public sealed class QuestData
    {
        public string id = string.Empty;
        public string type = string.Empty;
        public string target = string.Empty;
        public int count = 1;
        public RewardData[] rewards = Array.Empty<RewardData>();

        /// <summary>Daily tasks: activity points the task gives when claimed.</summary>
        public int points;
    }

    [Serializable]
    public sealed class DailyChestData
    {
        public int points;
        public RewardData[] rewards = Array.Empty<RewardData>();
    }

    [Serializable]
    public sealed class QuestCatalogData
    {
        /// <summary>Main story tasks, done one after another.</summary>
        public QuestData[] chapter = Array.Empty<QuestData>();

        /// <summary>Reset every day at 00:00 UTC.</summary>
        public QuestData[] daily = Array.Empty<QuestData>();

        public DailyChestData[] dailyChests = Array.Empty<DailyChestData>();

        /// <summary>
        /// The first-session guide (docs/34): short steps done in order before the chapter matters. Their rewards
        /// go straight to the wallet (not as capsules) so each one unblocks the next step.
        /// </summary>
        public QuestData[] onboarding = Array.Empty<QuestData>();
    }
}
