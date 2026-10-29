using System;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// VIP (VipRules) and the Hadalite shop: the VIP level with its progress, today's sign-in points and VIP chest, every
    /// level's perks; and the Hadalite packs, whose payment opens when the store goes live.
    /// </summary>
    public sealed partial class SocialView
    {
        public void OpenVip() => Open(Page.Vip);

        public void OpenShop() => Open(Page.Shop);

        private string VipKey() => $"{View.VipVersion}";

        private void RenderVip()
        {
            _title.text = Loc.T("vip.title");
            _subtitle.text = string.Empty;
            var vip = View.Vip ?? new VipStateDto();
            var level = vip.Level;
            var next = Math.Min(VipRules.MaxLevel, level + 1);

            // Level and progress.
            var top = UiKit.Box("VipCard", _list, new Color(0.08f, 0.12f, 0.22f, 1f)).Row(270);
            top.sprite = Skin.Get("header") ?? UiKit.Soft;
            top.color = Color.white;
            HudIconImage(top.transform, "vip", 0.03f, 0.18f, 0.25f, 0.92f);
            UiKit.Stroked(UiKit.Label("Level", top.transform, Loc.F("vip.level", level), 64, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold))
                .rectTransform.Place(0.28f, 0.52f, 0.97f, 0.92f);
            if (level < VipRules.MaxLevel)
            {
                var from = VipRules.Thresholds[level];
                var to = VipRules.Thresholds[next];
                var bar = Skin.Bar(top.transform, (float)(vip.Points - from) / Math.Max(1, to - from), "gold");
                bar.Place(0.28f, 0.3f, 0.95f, 0.46f);
                UiKit.Stroked(UiKit.Label("Value", bar, $"{vip.Points:N0} / {to:N0}", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)).rectTransform.Place(0, 0, 1, 1);
                UiKit.Label("Next", top.transform, Loc.F("vip.toNext", (to - vip.Points).ToString("N0"), next), 22, new Color(0.85f, 0.9f, 1f, 1f), TextAnchor.MiddleLeft)
                    .rectTransform.Place(0.28f, 0.08f, 0.97f, 0.28f);
            }
            else
            {
                UiKit.Label("Max", top.transform, Loc.F("vip.max", vip.Points.ToString("N0")), 26, Color.white, TextAnchor.MiddleLeft).rectTransform.Place(0.28f, 0.1f, 0.97f, 0.46f);
            }

            // Today's sign-in points.
            var login = Card(160);
            CardText(login.transform, Loc.T("vip.daily"), 30, UiKit.Text, 0.04f, 0.52f, 0.62f, 0.92f, TextAnchor.MiddleLeft, FontStyle.Bold);
            CardText(login.transform, Loc.F("vip.streak", vip.Streak, vip.LoginPoints.ToString("N0")), 24, UiKit.Muted, 0.04f, 0.08f, 0.64f, 0.52f, TextAnchor.MiddleLeft);
            CardButton(login.transform, vip.LoginReady ? Loc.F("vip.claimPoints", vip.LoginPoints) : Loc.T("event.claimed"), vip.LoginReady,
                () => _commands.Dispatch(new ClaimVipLoginCommand { CommandId = NewId(), ClientSequence = NextSequence() }), UiKit.Gold, 0.66f, 0.2f, 0.97f, 0.8f);

            // Today's VIP chest.
            var chest = Card(190);
            CardText(chest.transform, Loc.F("vip.chest", Math.Max(1, level)), 30, UiKit.Text, 0.04f, 0.68f, 0.62f, 0.95f, TextAnchor.MiddleLeft, FontStyle.Bold);
            var rewards = VipRules.DailyChest(Math.Max(1, level))
                .Select(r => new RewardEntryDto { Type = r.type, Id = r.id, Amount = r.amount }).ToArray();
            RewardStrip(chest.transform, rewards, 0.03f, 0.08f, 0.64f, 0.64f, 190);
            var chestLabel = level <= 0 ? Loc.T("vip.chestLocked") : vip.ChestReady ? Loc.T("vip.openChest") : Loc.T("event.claimed");
            CardButton(chest.transform, chestLabel, level > 0 && vip.ChestReady,
                () => _commands.Dispatch(new ClaimVipChestCommand { CommandId = NewId(), ClientSequence = NextSequence() }), UiKit.Gold, 0.66f, 0.24f, 0.97f, 0.76f);

            var buy = Card(110);
            CardText(buy.transform, Loc.T("vip.howToEarn"), 22, UiKit.Muted, 0.04f, 0.08f, 0.62f, 0.92f, TextAnchor.MiddleLeft);
            CardButton(buy.transform, Loc.T("shop.title"), true, OpenShop, UiKit.Primary, 0.66f, 0.14f, 0.97f, 0.86f);

            // Every level's perks; the player's own is highlighted.
            Header(Loc.T("vip.perks"));
            for (var l = 1; l <= VipRules.MaxLevel; l++)
            {
                var perks = VipRules.PerksOf(l);
                var card = Card(150);
                if (l == level)
                    card.color = new Color(1f, 0.93f, 0.7f, 1f);
                CardText(card.transform, Loc.F("vip.level", l), 34, l <= level ? UiKit.Amber : UiKit.Text, 0.03f, 0.5f, 0.3f, 0.92f, TextAnchor.MiddleLeft, FontStyle.Bold);
                CardText(card.transform, Loc.F("vip.points", VipRules.Thresholds[l].ToString("N0")), 22, UiKit.Muted, 0.03f, 0.1f, 0.3f, 0.5f, TextAnchor.MiddleLeft);
                var lines = Loc.F("vip.perk.speed", perks.BuildSpeed, perks.ResearchSpeed);
                if (perks.Production > 0)
                    lines += "\n" + Loc.F("vip.perk.production", perks.Production);
                if (perks.MarchSpeed > 0)
                    lines += "\n" + Loc.F("vip.perk.march", perks.MarchSpeed);
                if (perks.Capacity > 0)
                    lines += "\n" + Loc.F("vip.perk.capacity", perks.Capacity);
                if (perks.ExtraBuilders > 0)
                    lines += "\n" + Loc.T("vip.perk.builder");
                CardText(card.transform, lines, 22, UiKit.Text, 0.32f, 0.06f, 0.98f, 0.94f, TextAnchor.MiddleLeft);
            }
        }

        private void RenderShop()
        {
            _title.text = Loc.T("shop.title");
            _subtitle.text = string.Empty;

            var intro = Card(140);
            CardText(intro.transform, Loc.T("shop.intro"), 24, UiKit.Text, 0.04f, 0.08f, 0.66f, 0.92f, TextAnchor.MiddleLeft);
            CardButton(intro.transform, Loc.F("vip.level", View.Vip?.Level ?? 0), true, OpenVip, UiKit.Gold, 0.7f, 0.18f, 0.97f, 0.82f);

            Header(Loc.T("shop.packs"));
            var packs = StorePacks.All;
            for (var start = 0; start < packs.Length; start += 2)
            {
                var row = UiKit.Rect("Packs", _list).Row(330);
                for (var c = 0; c < 2 && start + c < packs.Length; c++)
                {
                    var pack = packs[start + c];
                    var tile = Skin.Slot(row, pack.Best ? "legendary" : pack.Hadalite >= 2000 ? "epic" : "rare");
                    tile.rectTransform.Place(c * 0.5f + 0.01f, 0.02f, c * 0.5f + 0.49f, 0.98f);
                    HudIconImage(tile.transform, "res_hadalite", 0.3f, 0.42f, 0.7f, 0.82f);
                    UiKit.Stroked(UiKit.Label("Amount", tile.transform, pack.Hadalite.ToString("N0"), 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold))
                        .rectTransform.Place(0, 0.3f, 1, 0.44f);
                    if (pack.Bonus > 0)
                        UiKit.Stroked(UiKit.Label("Bonus", tile.transform, Loc.F("shop.bonus", pack.Bonus.ToString("N0")), 22, UiKit.Gold, TextAnchor.UpperRight, FontStyle.Bold))
                            .rectTransform.Place(0.3f, 0.84f, 0.95f, 0.97f);
                    if (pack.Best)
                        UiKit.Stroked(UiKit.Label("Best", tile.transform, Loc.T("shop.best"), 22, Color.white, TextAnchor.UpperLeft, FontStyle.Bold))
                            .rectTransform.Place(0.06f, 0.84f, 0.6f, 0.97f);
                    UiKit.Stroked(UiKit.Label("Vip", tile.transform, Loc.F("shop.vip", pack.VipPoints.ToString("N0")), 20, Color.white, TextAnchor.MiddleCenter))
                        .rectTransform.Place(0, 0.22f, 1, 0.31f);
                    var price = $"${pack.PriceCents / 100f:0.00}";
                    var button = UiKit.Button("Buy", tile.transform, price, () => Toast(Loc.T("shop.soon"), UiKit.Amber), UiKit.Ok, 30);
                    ((RectTransform)button.transform).Place(0.12f, 0.03f, 0.88f, 0.2f);
                }
            }

            Note(Loc.T("shop.note"));
        }
    }
}
