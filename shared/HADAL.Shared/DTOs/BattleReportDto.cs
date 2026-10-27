using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One battle (entity prefix <c>EntityIds.BattleReport</c>): who fought, what each side lost, what was won.</summary>
    [ProtoContract]
    public sealed class BattleReportDto
    {
        [ProtoMember(1)] public string Id { get; set; } = string.Empty;
        [ProtoMember(2)] public string MonsterType { get; set; } = string.Empty;
        [ProtoMember(3)] public int MonsterLevel { get; set; }
        [ProtoMember(4)] public int X { get; set; }
        [ProtoMember(5)] public int Y { get; set; }
        [ProtoMember(6)] public bool Won { get; set; }
        [ProtoMember(7)] public int Rounds { get; set; }
        [ProtoMember(8)] public long AtMs { get; set; }
        [ProtoMember(9)] public string[] HeroIds { get; set; } = System.Array.Empty<string>();
        [ProtoMember(10)] public BattleStackDto[] Attacker { get; set; } = System.Array.Empty<BattleStackDto>();
        [ProtoMember(11)] public BattleStackDto[] Defender { get; set; } = System.Array.Empty<BattleStackDto>();

        /// <summary>Bag items won (capsules, speed-ups, chests); they arrive when the team is home.</summary>
        [ProtoMember(12)] public ItemCountDto[] Rewards { get; set; } = System.Array.Empty<ItemCountDto>();
        [ProtoMember(13)] public long AttackerPower { get; set; }
        [ProtoMember(14)] public long DefenderPower { get; set; }
    }

    [ProtoContract]
    public sealed class BattleStackDto
    {
        [ProtoMember(1)] public string Type { get; set; } = string.Empty;
        [ProtoMember(2)] public int Tier { get; set; }
        [ProtoMember(3)] public long Start { get; set; }

        /// <summary>Lost in battle; on the player's side they come home wounded (PvE never kills).</summary>
        [ProtoMember(4)] public long Lost { get; set; }
    }
}
