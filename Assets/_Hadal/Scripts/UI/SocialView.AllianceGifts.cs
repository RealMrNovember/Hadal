using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Alliance gifts (docs/31 step 5): the gift level card with its bar and Collect all, then a row per gift — the chest,
    /// who earned it and how, the points it adds and when it goes — with Collect.
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
            var waiting = GiftsWaiting();
            var head = InkCard(250, UiFrames.Highlight);
            var chest = new RewardEntryDto { Type = "item", Id = string.IsNullOrEmpty(gifts.Chest) ? AllianceGiftRules.Chest(1) : gifts.Chest, Amount = 1 };
            RewardIcon(head.transform, chest, 0.02f, 0.3f, 0.2f, 0.95f);
            Ink(head.transform, Loc.F("alliance.gifts.level", Math.Max(1, gifts.Level)), UiKit.TypeScale.Subtitle, UiFrames.Ink, 0.23f, 0.72f, 0.97f, 0.96f,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            if (gifts.NextPoints > 0)
                ProgressBar(head.transform, gifts.Points, gifts.NextPoints, 0.23f, 0.56f, 0.97f, 0.7f);
            else
                Ink(head.transform, Loc.F("alliance.gifts.points", gifts.Points.ToString("N0")), UiKit.TypeScale.Caption, UiFrames.Ink, 0.23f, 0.55f, 0.97f, 0.72f);
            Ink(head.transform, Loc.T("alliance.gifts.how"), UiKit.TypeScale.Micro, UiFrames.InkSoft, 0.23f, 0.3f, 0.97f, 0.55f);
            CardButton(head.transform, waiting > 0 ? $"{Loc.T("alliance.gifts.all")} ({waiting})" : Loc.T("alliance.gifts.all"), waiting > 0,
                () => CollectGift(string.Empty), UiKit.Ok, 0.3f, 0.03f, 0.7f, 0.27f);

            var list = (gifts.Gifts ?? Array.Empty<AllianceGiftDto>()).Where(g => g.ExpiresAtMs > NowMs).ToList();
            if (list.Count == 0)
            {
                InkNote(Loc.T("alliance.gifts.none"));
                return;
            }

            foreach (var g in list)
            {
                var card = InkCard(140, g.Collected ? UiFrames.CardAlt : (Color?)null);
                RewardIcon(card.transform, chest, 0.02f, 0.12f, 0.14f, 0.88f);
                var text = Ink(card.transform, GiftText(g), UiKit.TypeScale.Body, g.Collected ? UiFrames.InkSoft : UiFrames.Ink, 0.17f, 0.42f, 0.7f, 0.95f,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
                text.richText = false;
                text.Fit(16, UiKit.TypeScale.Body);
                Ink(card.transform, $"+{g.Points:N0} · {Loc.F("alliance.gifts.expires", Span(g.ExpiresAtMs - NowMs))}", UiKit.TypeScale.Caption, UiFrames.InkSoft,
                    0.17f, 0.05f, 0.7f, 0.42f);
                var id = g.Id;
                CardButton(card.transform, g.Collected ? Loc.T("event.claimed") : Loc.T("alliance.gifts.collect"), !g.Collected, () => CollectGift(id),
                    UiKit.Ok, 0.72f, 0.22f, 0.97f, 0.78f);
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
