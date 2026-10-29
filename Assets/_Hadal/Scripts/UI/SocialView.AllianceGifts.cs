using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Alliance gifts (docs/31 step 5): the gift level bar, then a row per gift — the chest, who earned it and how,
    /// the points it adds — with Collect, and Collect all on top.
    /// </summary>
    public sealed partial class SocialView
    {
        private const int AllianceTabGifts = 8;

        private int GiftsWaiting() =>
            (View.AllianceGifts?.Gifts ?? Array.Empty<AllianceGiftDto>()).Count(g => !g.Collected && g.ExpiresAtMs > NowMs);

        private void CollectGift(string giftId) =>
            _commands.Dispatch(new CollectAllianceGiftCommand { CommandId = NewId(), ClientSequence = NextSequence(), GiftId = giftId ?? string.Empty });

        private void RenderAllianceGifts()
        {
            var gifts = View.AllianceGifts ?? new AllianceGiftsDto();
            var head = Card(190);
            head.color = new Color(0.06f, 0.16f, 0.24f, 1f);
            var chest = new RewardEntryDto { Type = "item", Id = string.IsNullOrEmpty(gifts.Chest) ? AllianceGiftRules.Chest(1) : gifts.Chest, Amount = 1 };
            RewardIcon(head.transform, chest, 0.02f, 0.15f, 0.17f, 0.85f);
            CardText(head.transform, Loc.F("alliance.gifts.level", Math.Max(1, gifts.Level)), 34, UiKit.Gold, 0.2f, 0.6f, 0.97f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
            if (gifts.NextPoints > 0)
                ProgressBar(head.transform, gifts.Points, gifts.NextPoints, 0.2f, 0.32f, 0.97f, 0.52f);
            else
                CardText(head.transform, Loc.F("alliance.gifts.points", gifts.Points.ToString("N0")), 24, UiKit.Text, 0.2f, 0.3f, 0.97f, 0.55f, TextAnchor.MiddleLeft);
            CardText(head.transform, Loc.T("alliance.gifts.how"), 20, UiKit.Muted, 0.2f, 0.02f, 0.97f, 0.3f, TextAnchor.MiddleLeft);

            var waiting = GiftsWaiting();
            var all = UiKit.Rect("CollectAll", _list).Row(100);
            CardButton(all, waiting > 0 ? $"{Loc.T("alliance.gifts.all")} ({waiting})" : Loc.T("alliance.gifts.all"), waiting > 0, () => CollectGift(string.Empty),
                UiKit.Gold, 0.25f, 0.08f, 0.75f, 0.92f);

            var list = (gifts.Gifts ?? Array.Empty<AllianceGiftDto>()).Where(g => g.ExpiresAtMs > NowMs).ToList();
            if (list.Count == 0)
            {
                Note(Loc.T("alliance.gifts.none"));
                return;
            }

            foreach (var g in list)
            {
                var card = Card(140);
                if (g.Collected)
                    card.color = new Color(0.04f, 0.09f, 0.12f, 0.9f);
                RewardIcon(card.transform, chest, 0.02f, 0.12f, 0.14f, 0.88f);
                var text = CardText(card.transform, GiftText(g), 24, g.Collected ? UiKit.Muted : UiKit.Text, 0.16f, 0.4f, 0.7f, 0.95f, TextAnchor.MiddleLeft);
                text.supportRichText = false;
                CardText(card.transform, $"+{g.Points:N0} · {Loc.F("alliance.gifts.expires", Span(g.ExpiresAtMs - NowMs))}", 20, UiKit.Muted,
                    0.16f, 0.05f, 0.7f, 0.4f, TextAnchor.MiddleLeft);
                var id = g.Id;
                CardButton(card.transform, g.Collected ? Loc.T("event.claimed") : Loc.T("alliance.gifts.collect"), !g.Collected, () => CollectGift(id),
                    UiKit.Gold, 0.72f, 0.22f, 0.97f, 0.78f);
            }
        }

        private static string GiftText(AllianceGiftDto g) => g.Source switch
        {
            AllianceGiftRules.Monster => Loc.F("alliance.gifts.monster", g.ByName, g.Arg),
            AllianceGiftRules.Purchase => Loc.F("alliance.gifts.purchase", g.ByName),
            _ => Loc.F("alliance.gifts.event", g.ByName)
        };
    }
}
