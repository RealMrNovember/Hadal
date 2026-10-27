using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>Depth Map progress (entity <c>EntityIds.Expedition</c>, docs/rehber 08 §5).</summary>
    [ProtoContract]
    public sealed class ExpeditionStateDto
    {
        /// <summary>Stages cleared in order from chapter 1 stage 1.</summary>
        [ProtoMember(1)] public int Cleared { get; set; }

        /// <summary>Rewarded dives used today and the daily allowance.</summary>
        [ProtoMember(2)] public int RewardedToday { get; set; }
        [ProtoMember(3)] public int DailyRewarded { get; set; }

        /// <summary>Power the dives are judged with.</summary>
        [ProtoMember(4)] public long Power { get; set; }
    }

    /// <summary>One dive result (entity prefix <c>EntityIds.DiveReport</c>).</summary>
    [ProtoContract]
    public sealed class DiveReportDto
    {
        [ProtoMember(1)] public int Chapter { get; set; }
        [ProtoMember(2)] public int Stage { get; set; }
        [ProtoMember(3)] public bool Won { get; set; }
        [ProtoMember(4)] public long Power { get; set; }
        [ProtoMember(5)] public long Recommended { get; set; }
        [ProtoMember(6)] public long OxygenSpent { get; set; }

        /// <summary>First clear of the stage.</summary>
        [ProtoMember(7)] public bool FirstClear { get; set; }

        /// <summary>The dive used one of today's rewarded dives.</summary>
        [ProtoMember(8)] public bool Rewarded { get; set; }

        [ProtoMember(9)] public RewardEntryDto[] Rewards { get; set; } = System.Array.Empty<RewardEntryDto>();
    }
}
