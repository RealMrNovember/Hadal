using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// VIP (VipRules) and the Hadalite shop in Last Asylum's pages (vip.jpg): the crown banner with the level, the
    /// sign-in streak and the points bar; a perk card that flips through the levels; the daily free pack as reward tiles;
    /// the shop as a grid of offer cards, whose payment opens when the store goes live.
    /// </summary>
    public sealed partial class SocialView
    {
        /// <summary>The VIP level whose perks the card shows (the player's own when the page opens).</summary>
        private int _vipShown;

        public void OpenVip()
        {
            _vipShown = 0;
            Open(Page.Vip);
        }

        public void OpenShop() => Open(Page.Shop);

        private string VipKey() => $"{View.VipVersion}";

        private void RenderVip()
        {
            BeginPage();
            _title.text = Loc.T("vip.title");
            BarChips(new[] { (Hud.HudIcons.Resource(ResourceType.Hadalite), WalletChip(ResourceType.Hadalite)) }, "shop", OpenShop);
            var vip = View.Vip ?? new VipStateDto();
            var level = vip.Level;
            var next = Math.Min(VipRules.MaxLevel, level + 1);

            // The banner: crown and level, today's sign-in line, the points bar and the claim for today's points.
            var top = UiFrames.Plate(_list, UiFrames.Band).Row(470);
            Hud.HudIcons.Image(top.transform, "vip", 0.34f, 0.44f, 0.66f, 0.98f);
            var title = UiKit.Label("Level", top.transform, Loc.F("vip.level", level), UiKit.TypeScale.Display, UiKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(0.2f, 0.34f, 0.8f, 0.48f);
            UiText.Outline(title, new Color(0.24f, 0.12f, 0.02f, 0.95f));
            UiKit.Label("Streak", top.transform, Loc.F("vip.streak", vip.Streak, vip.LoginPoints.ToString("N0")), UiKit.TypeScale.Caption + 2, UiFrames.Ink,
                TextAnchor.MiddleLeft).rectTransform.Place(0.04f, 0.21f, 0.96f, 0.33f);
            if (level < VipRules.MaxLevel)
            {
                var from = VipRules.Thresholds[level];
                var to = VipRules.Thresholds[next];
                var bar = Skin.Bar(top.transform, (float)(vip.Points - from) / Math.Max(1, to - from), "gold");
                bar.Place(0.04f, 0.06f, 0.7f, 0.18f);
                var value = UiKit.Label("Value", bar, $"{vip.Points:N0} / {to:N0}", UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                value.rectTransform.Place(0, 0, 1, 1);
                UiText.Outline(value, new Color(0.02f, 0.04f, 0.08f, 0.9f));
            }
            else
            {
                UiKit.Label("Max", top.transform, Loc.F("vip.max", vip.Points.ToString("N0")), UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold)
                    .rectTransform.Place(0.04f, 0.04f, 0.7f, 0.2f);
            }

            var claim = UiKit.Button("Claim", top.transform, vip.LoginReady ? Loc.F("vip.claimPoints", vip.LoginPoints) : Loc.T("event.claimed"),
                () => _commands.Dispatch(new ClaimVipLoginCommand { CommandId = NewId(), ClientSequence = NextSequence() }),
                vip.LoginReady ? UiKit.Gold : UiKit.Panel, UiKit.TypeScale.Caption + 2);
            claim.interactable = vip.LoginReady;
            ((RectTransform)claim.transform).Place(0.72f, 0.03f, 0.97f, 0.21f);
            if (vip.LoginReady)
                UiFrames.Dot(claim.transform);

            RenderPerkCard(level);
            RenderVipPack(vip, level);
            NoteRow(Loc.T("vip.howToEarn"), 120);
            BottomButton(Loc.T("shop.title"), UiKit.Primary, OpenShop);
        }

        /// <summary>One level's perks with ‹ › to the others; the player's level reads as active, the others say what they need.</summary>
        private void RenderPerkCard(int level)
        {
            if (_vipShown < 1 || _vipShown > VipRules.MaxLevel)
                _vipShown = Mathf.Clamp(level, 1, VipRules.MaxLevel);
            var shown = _vipShown;

            var head = UiKit.Rect("PerkHead", _list).Row(96);
            var band = UiFrames.Section(head, shown <= level
                ? $"{Loc.F("vip.level", shown)} · {Loc.T("vip.perks")}"
                : $"{Loc.F("vip.level", shown)} · {Loc.F("vip.points", VipRules.Thresholds[shown].ToString("N0"))}");
            band.Place(0.12f, 0.05f, 0.88f, 0.95f);
            if (shown > 1)
                ((RectTransform)UiKit.Button("Prev", head, "‹", () => { _vipShown = shown - 1; Render(force: true); }, UiKit.Primary, UiKit.TypeScale.Title).transform)
                    .Place(0f, 0.05f, 0.1f, 0.95f);
            if (shown < VipRules.MaxLevel)
                ((RectTransform)UiKit.Button("Next", head, "›", () => { _vipShown = shown + 1; Render(force: true); }, UiKit.Primary, UiKit.TypeScale.Title).transform)
                    .Place(0.9f, 0.05f, 1f, 0.95f);

            // Only the bonuses the level actually gives.
            var perks = VipRules.PerksOf(shown);
            var lines = new List<string>();
            if (perks.BuildSpeed > 0)
                lines.Add(Loc.F("vip.perk.build", perks.BuildSpeed));
            if (perks.ResearchSpeed > 0)
                lines.Add(Loc.F("vip.perk.research", perks.ResearchSpeed));
            if (perks.Production > 0)
                lines.Add(Loc.F("vip.perk.production", perks.Production));
            if (perks.MarchSpeed > 0)
                lines.Add(Loc.F("vip.perk.march", perks.MarchSpeed));
            if (perks.Capacity > 0)
                lines.Add(Loc.F("vip.perk.capacity", perks.Capacity));
            if (perks.ExtraBuilders > 0)
                lines.Add(Loc.T("vip.perk.builder"));
            var active = shown <= level;
            for (var i = 0; i < lines.Count; i++)
            {
                var row = UiFrames.Plate(_list, i % 2 == 0 ? UiFrames.Card : UiFrames.CardAlt).Row(80);
                UiKit.Label("Bullet", row.transform, "•", UiKit.TypeScale.Subtitle, active ? UiFrames.InkGood : UiFrames.InkSoft, TextAnchor.MiddleCenter)
                    .rectTransform.Place(0.01f, 0, 0.07f, 1);
                UiKit.Label("Perk", row.transform, lines[i], UiKit.TypeScale.Body, active ? UiFrames.Ink : UiFrames.InkSoft, TextAnchor.MiddleLeft, active ? FontStyle.Bold : FontStyle.Normal)
                    .rectTransform.Place(0.08f, 0, 0.97f, 1);
            }
        }

        /// <summary>The level's daily free pack: its rewards as tiles and the button that opens it.</summary>
        private void RenderVipPack(VipStateDto vip, int level)
        {
            SectionRow(Loc.F("vip.chest", Math.Max(1, level)));
            var row = PlateRow(240);
            var tiles = RewardTiles(VipRules.DailyChest(Math.Max(1, level)).ToArray());
            for (var i = 0; i < tiles.Length && i < 4; i++)
            {
                var (icon, rarity, count) = tiles[i];
                var x = 0.02f + i * 0.165f;
                UiFrames.ItemTile(row.transform, icon, rarity, count).Place(x, 0.14f, x + 0.15f, 0.86f);
            }

            var label = level <= 0 ? Loc.T("vip.chestLocked") : vip.ChestReady ? Loc.T("vip.openChest") : Loc.T("event.claimed");
            var ready = level > 0 && vip.ChestReady;
            var button = UiKit.Button("Open", row.transform, label, () => _commands.Dispatch(new ClaimVipChestCommand { CommandId = NewId(), ClientSequence = NextSequence() }),
                ready ? UiKit.Ok : UiKit.Panel, UiKit.TypeScale.Body);
            button.interactable = ready;
            ((RectTransform)button.transform).Place(0.71f, 0.26f, 0.97f, 0.74f);
            if (ready)
                UiFrames.Dot(button.transform);
        }

        /// <summary>The Hadalite shop: the packs as a two-column grid of offer cards with the price button under each.</summary>
        private void RenderShop()
        {
            BeginPage();
            _title.text = Loc.T("shop.title");
            BarChips(new[]
            {
                (Hud.HudIcons.Resource(ResourceType.Hadalite), WalletChip(ResourceType.Hadalite)),
                ("vip", (Func<string>)(() => Loc.F("vip.level", View.Vip?.Level ?? 0)))
            }, "vip", OpenVip);

            NoteRow(Loc.T("shop.intro"), 130);
            var grid = UiFrames.Grid(_list, 2, 0.84f, 18f);
            grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(4, 4, 6, 6);
            foreach (var pack in StorePacks.All)
                OfferCard(grid, pack);
            NoteRow(Loc.T("shop.note"), 120);
        }

        private void OfferCard(Transform grid, StorePacks.Pack pack)
        {
            var card = UiKit.Rect("Pack", grid);
            Skin.Slot(card, pack.Best ? "legendary" : pack.Hadalite >= 2000 ? "epic" : "rare").rectTransform.Place(0, 0, 1, 1);
            Hud.HudIcons.Image(card, "res_hadalite", 0.28f, 0.44f, 0.72f, 0.84f);
            var amount = UiKit.Label("Amount", card, pack.Hadalite.ToString("N0"), UiKit.TypeScale.Title, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            amount.rectTransform.Place(0, 0.31f, 1, 0.45f);
            UiText.Outline(amount, new Color(0.02f, 0.04f, 0.08f, 0.9f));
            var vipLine = UiKit.Label("Vip", card, Loc.F("shop.vip", pack.VipPoints.ToString("N0")), UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter);
            vipLine.rectTransform.Place(0, 0.22f, 1, 0.31f);
            UiText.Outline(vipLine, new Color(0.02f, 0.04f, 0.08f, 0.9f));
            if (pack.Bonus > 0)
            {
                var bonus = UiKit.Label("Bonus", card, Loc.F("shop.bonus", pack.Bonus.ToString("N0")), UiKit.TypeScale.Caption, UiKit.Gold, TextAnchor.UpperRight, FontStyle.Bold);
                bonus.rectTransform.Place(0.3f, 0.84f, 0.95f, 0.97f);
                UiText.Outline(bonus, new Color(0.02f, 0.04f, 0.08f, 0.9f));
            }

            if (pack.Best)
            {
                var best = UiKit.Box("Best", card, UiFrames.InkBad);
                best.sprite = UiKit.Rounded;
                best.raycastTarget = false;
                best.rectTransform.Place(0.04f, 0.86f, 0.36f, 0.97f);
                UiKit.Stroked(UiKit.Label("Text", best.transform, Loc.T("shop.best"), UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold))
                    .rectTransform.Place(0, 0, 1, 1);
            }

            var button = UiKit.Button("Buy", card, $"${pack.PriceCents / 100f:0.00}", () => Toast(Loc.T("shop.soon"), UiKit.Amber), UiKit.Ok, UiKit.TypeScale.Body);
            ((RectTransform)button.transform).Place(0.1f, 0.03f, 0.9f, 0.2f);
        }
    }
}
