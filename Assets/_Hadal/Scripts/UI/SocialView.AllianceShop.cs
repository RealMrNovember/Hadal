using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The alliance shop (docs/31 step 4): the player's alliance coins, the weekly refill countdown and the offers in
    /// tiles, each with its price and what is left this week; buying puts the item in the bag.
    /// </summary>
    public sealed partial class SocialView
    {
        private const int AllianceTabShop = 7;

        private void RenderAllianceShop()
        {
            var coins = View.AllianceCoins ?? new AllianceCoinsDto();
            var head = Card(140);
            head.color = new Color(0.06f, 0.16f, 0.24f, 1f);
            HudIconImage(head.transform, "side_alliance", 0.02f, 0.12f, 0.14f, 0.88f);
            CardText(head.transform, Loc.F("alliance.coins", coins.Coins.ToString("N0")), 34, UiKit.Gold, 0.16f, 0.5f, 0.97f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
            var left = Math.Max(0, coins.ResetMs - (long)((Time.realtimeSinceStartup - View.AllianceCoinsAt) * 1000f));
            CardText(head.transform, Loc.F("alliance.shop.refill", Span(left)), 22, UiKit.Muted, 0.16f, 0.05f, 0.97f, 0.5f, TextAnchor.MiddleLeft);

            var offers = coins.Shop ?? Array.Empty<AllianceShopStockDto>();
            if (offers.Length == 0)
            {
                Note(Loc.T("alliance.log.loading"));
                return;
            }

            for (var start = 0; start < offers.Length; start += 2)
            {
                var row = UiKit.Rect("Offers", _list).Row(300);
                for (var c = 0; c < 2 && start + c < offers.Length; c++)
                {
                    var offer = offers[start + c];
                    var tile = UiKit.Box("Offer", row, UiKit.Field);
                    tile.rectTransform.Place(c * 0.5f + 0.005f, 0.02f, c * 0.5f + 0.495f, 0.98f);
                    var reward = new RewardEntryDto { Type = "item", Id = offer.ItemId, Amount = 1 };
                    RewardIcon(tile.transform, reward, 0.05f, 0.42f, 0.38f, 0.96f);
                    CardText(tile.transform, RewardText(reward), 24, UiKit.Text, 0.4f, 0.55f, 0.97f, 0.96f, TextAnchor.UpperLeft, FontStyle.Bold);
                    CardText(tile.transform, Loc.F("alliance.shop.stock", offer.Left, offer.Stock), 22, offer.Left > 0 ? UiKit.Muted : UiKit.Danger,
                        0.4f, 0.4f, 0.97f, 0.55f, TextAnchor.MiddleLeft);
                    var affordable = offer.Left > 0 && coins.Coins >= offer.Price;
                    var id = offer.ItemId;
                    var buy = CardButton(tile.transform, Loc.F("alliance.shop.price", offer.Price.ToString("N0")), affordable,
                        () => _commands.Dispatch(new BuyAllianceItemCommand { CommandId = NewId(), ClientSequence = NextSequence(), ItemId = id, Count = 1 }),
                        UiKit.Gold, 0.08f, 0.05f, 0.92f, 0.36f);
                    buy.GetComponentInChildren<Text>().fontSize = 28;
                }
            }

            Note(Loc.T("alliance.shop.how"));
        }
    }
}
