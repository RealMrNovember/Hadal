using System;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using TMPro;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// The alliance shop (docs/31 step 4): the player's alliance coins and the weekly refill countdown on top, then the
    /// offers as a three-column grid of item cards (rarity tile, name, what is left this week, the price button); buying
    /// puts the item in the bag.
    /// </summary>
    public sealed partial class SocialView
    {
        private const int AllianceTabShop = 7;

        private void RenderAllianceShop()
        {
            var coins = View.AllianceCoins ?? new AllianceCoinsDto();
            var head = InkCard(130, UiFrames.Highlight);
            HudIconImage(head.transform, "side_alliance", 0.02f, 0.12f, 0.14f, 0.88f);
            Ink(head.transform, Loc.F("alliance.coins", coins.Coins.ToString("N0")), UiKit.TypeScale.Subtitle, UiFrames.Ink, 0.17f, 0.48f, 0.97f, 0.95f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            var left = Math.Max(0, coins.ResetMs - (long)((Time.realtimeSinceStartup - View.AllianceCoinsAt) * 1000f));
            Ink(head.transform, Loc.F("alliance.shop.refill", Span(left)), UiKit.TypeScale.Caption, UiFrames.InkSoft, 0.17f, 0.05f, 0.97f, 0.48f);

            var offers = coins.Shop ?? Array.Empty<AllianceShopStockDto>();
            if (offers.Length == 0)
            {
                InkNote(Loc.T("alliance.log.loading"));
                return;
            }

            var grid = UiFrames.Grid(_list, 3, 0.7f, 14f);
            grid.GetComponent<UnityEngine.UI.GridLayoutGroup>().padding = new RectOffset(0, 0, 6, 6);
            foreach (var offer in offers)
            {
                var tile = UiFrames.CardBox(grid, null, "Offer");
                var reward = new RewardEntryDto { Type = "item", Id = offer.ItemId, Amount = 1 };
                var slot = Skin.Slot(tile.transform, RewardTone(reward));
                slot.rectTransform.Place(0.17f, 0.5f, 0.83f, 0.95f);
                slot.preserveAspect = true;
                slot.type = UnityEngine.UI.Image.Type.Sliced;
                RewardIcon(slot.transform, reward, 0.12f, 0.12f, 0.88f, 0.88f);
                var name = Ink(tile.transform, RewardText(reward), UiKit.TypeScale.Caption, UiFrames.Ink, 0.05f, 0.33f, 0.95f, 0.5f, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.Fit(12, UiKit.TypeScale.Caption);
                Ink(tile.transform, Loc.F("alliance.shop.stock", offer.Left, offer.Stock), UiKit.TypeScale.Micro, offer.Left > 0 ? UiFrames.InkSoft : UiFrames.InkBad,
                    0.05f, 0.22f, 0.95f, 0.33f, TextAnchor.MiddleCenter);
                var affordable = offer.Left > 0 && coins.Coins >= offer.Price;
                var id = offer.ItemId;
                var buy = CardButton(tile.transform, Loc.F("alliance.shop.price", offer.Price.ToString("N0")), affordable,
                    () => _commands.Dispatch(new BuyAllianceItemCommand { CommandId = NewId(), ClientSequence = NextSequence(), ItemId = id, Count = 1 }),
                    UiKit.Gold, 0.06f, 0.03f, 0.94f, 0.21f);
                buy.GetComponentInChildren<TMP_Text>().fontSizeMax = UiKit.TypeScale.Caption;
            }

            InkNote(Loc.T("alliance.shop.how"), 130);
        }
    }
}
