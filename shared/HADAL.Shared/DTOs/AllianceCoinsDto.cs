using ProtoBuf;

namespace HADAL.Shared.DTOs
{
    /// <summary>One alliance shop offer with what this player may still buy this week.</summary>
    [ProtoContract]
    public sealed class AllianceShopStockDto
    {
        [ProtoMember(1)] public string ItemId { get; set; } = string.Empty;
        [ProtoMember(2)] public long Price { get; set; }
        [ProtoMember(3)] public int Left { get; set; }
        [ProtoMember(4)] public int Stock { get; set; }
    }

    /// <summary>The player's alliance coins, today's help coins and this week's shop (docs/31 §2).</summary>
    [ProtoContract]
    public sealed class AllianceCoinsDto
    {
        [ProtoMember(1)] public long Coins { get; set; }
        [ProtoMember(2)] public int HelpCoinsToday { get; set; }
        [ProtoMember(3)] public int DailyHelpCoins { get; set; }

        /// <summary>Until the shop refills (Monday 00:00 UTC), from when this was sent.</summary>
        [ProtoMember(4)] public long ResetMs { get; set; }
        [ProtoMember(5)] public AllianceShopStockDto[] Shop { get; set; } = System.Array.Empty<AllianceShopStockDto>();
    }
}
