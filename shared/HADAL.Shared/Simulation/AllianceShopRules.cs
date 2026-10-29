using System;
using HADAL.Shared.Enums;

namespace HADAL.Shared.Simulation
{
    /// <summary>
    /// Alliance coins and the alliance shop (docs/31 §2, Last Asylum): every member earns personal coins by helping mates
    /// (<see cref="CoinsPerHelp"/> each, at most <see cref="DailyHelpCoins"/> a UTC day) and from alliance gifts, and
    /// spends them in a shop whose stock is per player and refills every Monday 00:00 UTC.
    /// <code>
    ///   item                    price   stock / week
    ///   Targeted teleport       1,500   1
    ///   Alliance teleport         600   10
    ///   Random teleport           400   3
    ///   Shield 8 h              1,200   1
    ///   Shield 24 h             3,000   1
    ///   Speed-up 1 h (any)        300   10
    ///   Speed-up 8 h (any)      2,000   2
    ///   Build / research 1 h      200   10 each
    ///   Heal 1 h                  150   10
    ///   Food / Titanium /         250   5 each (50,000 capsules)
    ///   Biomass capsule
    ///   Rare hero card            800   3
    ///   Epic hero card          2,500   1
    /// </code>
    /// </summary>
    public static class AllianceShopRules
    {
        public const int CoinsPerHelp = 20;
        public const int DailyHelpCoins = 1_000;

        public sealed class Offer
        {
            public Offer(string itemId, long price, int stock)
            {
                ItemId = itemId;
                Price = price;
                Stock = stock;
            }

            public string ItemId { get; }
            public long Price { get; }

            /// <summary>How many one player may buy per week.</summary>
            public int Stock { get; }
        }

        public static readonly Offer[] Offers =
        {
            new Offer(TeleportRules.ItemFor(TeleportRules.Target), 1_500, 1),
            new Offer(TeleportRules.ItemFor(TeleportRules.Alliance), 600, 10),
            new Offer(TeleportRules.ItemFor(TeleportRules.Random), 400, 3),
            new Offer(PvpRules.Shield8h, 1_200, 1),
            new Offer(PvpRules.Shield24h, 3_000, 1),
            new Offer("speed_any_60", 300, 10),
            new Offer("speed_any_480", 2_000, 2),
            new Offer("speed_build_60", 200, 10),
            new Offer("speed_research_60", 200, 10),
            new Offer("speed_heal_60", 150, 10),
            new Offer(Capsules.Id(ResourceType.Food, 50_000), 250, 5),
            new Offer(Capsules.Id(ResourceType.Titanium, 50_000), 250, 5),
            new Offer(Capsules.Id(ResourceType.Biomass, 50_000), 250, 5),
            new Offer("card_rare", 800, 3),
            new Offer("card_epic", 2_500, 1)
        };

        public static Offer? Find(string? itemId) => Array.Find(Offers, o => o.ItemId == itemId);

        /// <summary>The shop week: weeks since the Monday before 1970-01-01 (a Thursday), so each starts Monday 00:00 UTC.</summary>
        public static long Week(long unixMs) => (unixMs / 86_400_000L + 3) / 7;

        /// <summary>Milliseconds until the next Monday 00:00 UTC.</summary>
        public static long MsToReset(long unixMs) => ((Week(unixMs) + 1) * 7 - 3) * 86_400_000L - unixMs;

        /// <summary>Coins one more help earns, given those already earned from helping today.</summary>
        public static int HelpCoins(int helps, int earnedToday) =>
            Math.Max(0, Math.Min(helps * CoinsPerHelp, DailyHelpCoins - earnedToday));
    }
}
