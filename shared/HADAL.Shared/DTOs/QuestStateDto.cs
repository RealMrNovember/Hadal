using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>Quest progress (entity <c>EntityIds.Quests</c>, docs/27).</summary>
    [ProtoContract]
    public sealed class QuestStateDto
    {
        /// <summary>The current chapter task; null once every chapter task is done.</summary>
        [ProtoMember(1)] public QuestProgressDto? Chapter { get; set; }

        /// <summary>1-based number of the current chapter task.</summary>
        [ProtoMember(2)] public int ChapterNumber { get; set; }

        [ProtoMember(3)] public int ChapterTotal { get; set; }
        [ProtoMember(4)] public QuestProgressDto[] Daily { get; set; } = System.Array.Empty<QuestProgressDto>();
        [ProtoMember(5)] public int DailyPoints { get; set; }
        [ProtoMember(6)] public int[] ClaimedChests { get; set; } = System.Array.Empty<int>();

        /// <summary>Time until the daily reset (00:00 UTC).</summary>
        [ProtoMember(7)] public long DailyResetMs { get; set; }

        /// <summary>The current first-session guide step (docs/34); null when the guide is done or was skipped.</summary>
        [ProtoMember(8)] public QuestProgressDto? Guide { get; set; }

        /// <summary>1-based number of <see cref="Guide"/> and the number of guide steps.</summary>
        [ProtoMember(9)] public int GuideNumber { get; set; }

        [ProtoMember(10)] public int GuideTotal { get; set; }
    }

    [ProtoContract]
    public sealed class QuestProgressDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public long Progress { get; set; }
        [ProtoMember(3)] public long Target { get; set; }
        [ProtoMember(4)] public bool Claimed { get; set; }
    }

    /// <summary>Bag contents (entity <c>EntityIds.Inventory</c>).</summary>
    [ProtoContract]
    public sealed class InventoryDto
    {
        [ProtoMember(1)] public ItemCountDto[] Items { get; set; } = System.Array.Empty<ItemCountDto>();

        /// <summary>Hero cards of heroes the player does not have yet (owned heroes keep theirs on the hero).</summary>
        [ProtoMember(2)] public ItemCountDto[] HeroCards { get; set; } = System.Array.Empty<ItemCountDto>();
    }

    [ProtoContract]
    public sealed class ItemCountDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public int Count { get; set; }
    }

    /// <summary>What a claim, chest or item gave (entity <c>EntityIds.RewardReport</c> + command id).</summary>
    [ProtoContract]
    public sealed class RewardReportDto
    {
        [ProtoMember(1)] public RewardEntryDto[] Rewards { get; set; } = System.Array.Empty<RewardEntryDto>();
    }

    [ProtoContract]
    public sealed class RewardEntryDto
    {
        /// <summary>"resource", "item", "shards" or "hero" (a recruited hero).</summary>
        [ProtoMember(1)] public string Type { get; set; } = string.Empty;
        [ProtoMember(2)] public string Id { get; set; } = string.Empty;
        [ProtoMember(3)] public long Amount { get; set; }
    }
}
