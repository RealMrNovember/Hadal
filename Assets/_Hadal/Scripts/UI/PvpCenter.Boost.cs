using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.StateSync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Last Asylum's Boost dialog ("Güçlendirme"), opened wherever the shield is: the map's own-base Shield button, the
    /// alarm's Raise shield. Tabs Shield · War · Economy. The shield tab is the base under its glowing dome, what a shield
    /// does and the red caveat that teams outside are not covered, the shield's time left, then the four shields as cards
    /// in a row (name, rarity tile with its duration, what the bag holds, Use or buy-and-use for Hadalite). War and
    /// Economy list the player's real bonuses as the server sums them (research, crew, VIP, alliance research).
    /// The War Room's shield tab shows the same shield panel.
    /// </summary>
    public sealed partial class PvpCenter
    {
        private UiFrames.Sheet _boost;
        private int _boostTab;
        private string _boostKey = string.Empty;
        private readonly List<(TMP_Text Label, long AtMs, string Format)> _boostClocks = new();

        private bool BoostOpen => _boost is { IsOpen: true };

        /// <summary>Opens the Boost dialog on its shield tab.</summary>
        public void OpenShield() => OpenBoost(0);

        /// <summary>Opens the Boost dialog: 0 shield, 1 war, 2 economy.</summary>
        public void OpenBoost(int tab)
        {
            Close();
            _boostTab = Mathf.Clamp(tab, 0, 2);
            if (!BoostOpen)
                _boost = UiFrames.OpenSheet(_windowCanvas.transform, Loc.T("boost.title"), CloseBoost, height: 0.62f, top: 100f);
            _boostKey = string.Empty;
            RenderBoost(View.ServerNowMs);
        }

        public void CloseBoost()
        {
            _boost?.Close();
            _boost = null;
            _boostClocks.Clear();
        }

        private void TickBoost()
        {
            if (!BoostOpen)
                return;
            var now = View.ServerNowMs;
            if (_boostTab == 0 && ShieldKey(now) != _boostKey)
                RenderBoost(now);
            Tick(_boostClocks, now);
        }

        private void RenderBoost(long now)
        {
            _boostClocks.Clear();
            UiFrames.ClearSheet(_boost);
            var labels = new[] { Loc.T("pvp.tab.shield"), Loc.T("boost.tab.war"), Loc.T("boost.tab.economy") };
            UiFrames.Tabs(_boost.Top, labels, _boostTab, OpenBoost).Place(0, 0, 1, 1);
            if (_boostTab == 0)
            {
                _boostKey = ShieldKey(now);
                ShieldPanel(_boost.Content, _boostClocks);
                return;
            }

            _boostKey = string.Empty;
            foreach (var (label, value) in Bonuses(_boostTab == 1))
                UiFrames.BonusRow(_boost.Content, label + " ↑", value).Row(84);
        }

        // --- Shield panel -------------------------------------------------------------------------

        private long Hadalite() => (View.Data.resources ?? Array.Empty<Hadal.Data.Models.ResourceSaveEntry>())
            .Where(e => ResourceTypeMap.FromStoredValue(e.resourceType) == ResourceType.Hadalite).Select(e => e.amount).DefaultIfEmpty(0).Sum();

        /// <summary>An attack, scout or rally of ours on its way: no shield can go up meanwhile.</summary>
        private bool Outgoing(long now) =>
            (View.Marches?.Marches ?? Array.Empty<MarchDto>()).Any(m => m.Kind is PvpRules.Attack or PvpRules.Scout or PvpRules.Rally && m.ArrivesAtMs > now);

        /// <summary>Everything the shield panel shows except its clocks: it is redrawn only when this changes.</summary>
        private string ShieldKey(long now)
        {
            var pvp = View.Pvp ?? new PvpStatusDto();
            var hadalite = Hadalite();
            var items = string.Join(",", PvpRules.ShieldItems.Select(id =>
                $"{(View.Items.TryGetValue(id, out var n) ? n : 0)}{(hadalite >= PvpRules.ShieldHadalite(id) ? "+" : "-")}"));
            var threats = (pvp.Incoming ?? Array.Empty<IncomingDto>()).Count(x => x.ArrivesAtMs > now && x.Kind != PvpRules.Scout);
            var guards = string.Join(",", (pvp.Garrison ?? Array.Empty<GarrisonDto>()).Select(g => g.MarchId + (g.ArrivesAtMs > now ? "~" : string.Empty)));
            var kept = (pvp.Protected ?? Array.Empty<ResourceAmountEntry>()).Sum(p => p.Value);
            return $"{pvp.ShieldUntilMs > now}|{PvpRules.IsBeginner(ReactorLevel())}|{items}|{Outgoing(now)}|{threats}|{guards}|{kept}";
        }

        /// <summary>The shield in one place (Boost dialog and War Room): the domed base, the rules, the time left, the four shields.</summary>
        private void ShieldPanel(Transform content, List<(TMP_Text Label, long AtMs, string Format)> clocks)
        {
            var pvp = View.Pvp ?? new PvpStatusDto();
            var now = View.ServerNowMs;
            var shielded = pvp.ShieldUntilMs > now;
            var beginner = PvpRules.IsBeginner(ReactorLevel());

            ShieldArt(content, shielded || beginner).Row(330);
            UiFrames.SheetText(content, Loc.T("boost.shield.text"), UiKit.TypeScale.Body, UiFrames.Ink).Row(48);
            UiFrames.SheetText(content, Loc.T("boost.shield.caveat"), UiKit.TypeScale.Body, UiFrames.InkBad).Row(46);

            // Time left on the shield (or why there is none), then what the alarm would say.
            var status = UiFrames.SheetText(content, string.Empty, beginner && !shielded ? UiKit.TypeScale.Caption + 2 : UiKit.TypeScale.Body,
                shielded ? UiFrames.InkGood : UiFrames.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            status.Row(beginner && !shielded ? 84 : 54);
            if (shielded)
                clocks.Add((status, pvp.ShieldUntilMs, "🛡 " + Loc.T("pvp.shield.on")));
            else
                status.text = beginner ? Loc.F("pvp.beginner", PvpRules.BeginnerReactor) : Loc.T("pvp.shield.off");

            var outgoing = Outgoing(now);
            var threatened = (pvp.Incoming ?? Array.Empty<IncomingDto>()).Any(x => x.ArrivesAtMs > now && x.Kind != PvpRules.Scout);
            if (outgoing)
                UiFrames.SheetText(content, Loc.T("pvp.shield.attackOut"), UiKit.TypeScale.Caption + 2, UiFrames.InkBad, TextAnchor.MiddleCenter, FontStyle.Bold).Row(50);
            else if (threatened)
                UiFrames.SheetText(content, shielded ? Loc.T("pvp.alarm.blocked") : Loc.T("pvp.shield.incoming"), UiKit.TypeScale.Caption + 2,
                    shielded ? UiFrames.InkGood : UiFrames.InkBad, TextAnchor.MiddleCenter, FontStyle.Bold).Row(shielded ? 50 : 84);

            // The four shields, as cards in one row.
            var hadalite = Hadalite();
            var cards = UiKit.Rect("Shields", content).Row(400);
            var ids = PvpRules.ShieldItems;
            for (var i = 0; i < ids.Length; i++)
                ShieldCard(UiKit.Rect("Cell", cards).Place((float)i / ids.Length, 0f, (float)(i + 1) / ids.Length, 1f, 5f), ids[i], hadalite, outgoing);

            // What an attack can never take (docs/33 §2), and who guards the base.
            var guard = (pvp.Protected ?? Array.Empty<ResourceAmountEntry>()).Where(p => p.Value > 0).ToList();
            if (guard.Count > 0)
                UiFrames.SheetText(content, Loc.F("pvp.protected", string.Join("  ", guard.Select(p => $"{CostChips.Compact(p.Value)} {Loc.T("resource." + p.Key)}"))),
                    UiKit.TypeScale.Caption, UiFrames.InkSoft).Row(70);
            var guards = pvp.Garrison ?? Array.Empty<GarrisonDto>();
            if (guards.Length > 0)
            {
                var names = string.Join(", ", guards.Select(g => g.ArrivesAtMs > now
                    ? $"{g.OwnerName} ({CostChips.Compact(g.Troops)}, {Loc.Duration((g.ArrivesAtMs - now) / 1000f)})"
                    : $"{g.OwnerName} ({CostChips.Compact(g.Troops)})"));
                var line = UiFrames.SheetText(content, Loc.F("pvp.guards", names), UiKit.TypeScale.Caption, UiFrames.InkGood);
                line.richText = false;
                line.Row(70);
            }
        }

        /// <summary>
        /// One shield: its name in the header, the rarity tile with the duration badge, what the bag holds, and Use (one in
        /// the bag) or buy-and-use with its Hadalite price. Greyed while one of our attacks is out or we cannot pay.
        /// </summary>
        private void ShieldCard(RectTransform cell, string id, long hadalite, bool outgoing)
        {
            var card = UiFrames.SheetBlock(cell, UiFrames.Card, "Shield");
            card.rectTransform.Place(0, 0, 1, 1);
            var head = UiKit.Box("Head", card.transform, UiFrames.Band);
            head.sprite = UiKit.Rounded;
            head.raycastTarget = false;
            head.rectTransform.Place(0f, 0.8f, 1f, 1f);
            var name = UiKit.Label("Name", head.transform, Loc.T("item." + id), UiKit.TypeScale.Caption + 2, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.rectTransform.Place(0.04f, 0f, 0.96f, 1f);
            name.Fit(13, UiKit.TypeScale.Caption + 2);

            var holder = UiKit.Rect("Holder", card.transform).Place(0.1f, 0.38f, 0.9f, 0.78f);
            var tile = UiFrames.ItemTile(holder, "item_" + id, ShieldRarity(id), null, tag: Duration(id));
            var fitter = tile.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            var held = View.Items.TryGetValue(id, out var n) ? n : 0;
            var owned = UiKit.Label("Owned", card.transform, Loc.F("speedup.have", held), UiKit.TypeScale.Caption, UiFrames.InkSoft, TextAnchor.MiddleCenter);
            owned.rectTransform.Place(0.03f, 0.24f, 0.97f, 0.38f);
            owned.Fit(12, UiKit.TypeScale.Caption);

            var price = PvpRules.ShieldHadalite(id);
            var can = !outgoing && (held > 0 || hadalite >= price);
            var text = held > 0 ? Loc.T("pvp.shield.use") : $"💎{price:N0}\n{Loc.T("boost.buyuse")}";
            var shieldId = id;
            var use = UiKit.Button("Use", card.transform, text, () =>
                    _commands.Dispatch(new UseShieldCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, ItemId = shieldId }),
                can ? held > 0 ? UiKit.Primary : UiKit.Gold : UiKit.Panel, UiKit.TypeScale.Body);
            use.interactable = can;
            ((RectTransform)use.transform).Place(0.04f, 0.01f, 0.96f, 0.24f);
        }

        private static string ShieldRarity(string id) => id switch
        {
            PvpRules.Shield24h => "rare",
            PvpRules.Shield48h => "epic",
            PvpRules.Shield7d => "legendary",
            _ => "common"
        };

        /// <summary>The badge on a shield's tile: "8h", "48h", "7d".</summary>
        private static string Duration(string id)
        {
            var span = PvpRules.ShieldDuration(id);
            return span.TotalHours > 48 ? $"{(int)span.TotalDays}d" : $"{(int)span.TotalHours}h";
        }

        /// <summary>
        /// The picture over the shield panel: the base (its icon) under a soft cyan dome with a glassy rim and highlight,
        /// bright while protected and faint without; "Shield" across the foot of the dome.
        /// </summary>
        private static RectTransform ShieldArt(Transform parent, bool up)
        {
            var art = UiKit.Rect("Art", parent);
            var dome = UiKit.Rect("Dome", art);
            var fitter = dome.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            Disc("Halo", UiKit.Circle, new Color(0.42f, 0.84f, 1f, up ? 0.2f : 0.07f), -0.07f, -0.07f, 1.07f, 1.07f);
            Hud.HudIcons.Image(dome, "left_home", 0.16f, 0.12f, 0.84f, 0.72f);
            Disc("Glass", UiKit.Circle, new Color(0.62f, 0.92f, 1f, up ? 0.3f : 0.1f), 0.03f, 0.03f, 0.97f, 0.97f);
            Disc("Shine", UiKit.Circle, new Color(1f, 1f, 1f, up ? 0.28f : 0.1f), 0.2f, 0.6f, 0.48f, 0.86f);
            Disc("Rim", UiKit.CircleRing, new Color(0.78f, 0.96f, 1f, up ? 0.95f : 0.35f), 0.03f, 0.03f, 0.97f, 0.97f);
            if (up)
            {
                Disc("Spark", UiKit.Circle, Color.white, 0.66f, 0.72f, 0.72f, 0.78f);
                Disc("Spark", UiKit.Circle, new Color(1f, 1f, 1f, 0.8f), 0.24f, 0.5f, 0.28f, 0.54f);
            }

            var title = UiKit.Label("Title", art, Loc.T("pvp.tab.shield"), UiKit.TypeScale.Title, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.Place(0f, 0f, 1f, 0.22f);
            UiText.Outline(title, new Color(0.04f, 0.07f, 0.12f, 0.9f));
            return art;

            void Disc(string name, Sprite sprite, Color color, float x0, float y0, float x1, float y1)
            {
                var image = UiKit.Box(name, dome, color);
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.raycastTarget = false;
                image.rectTransform.Place(x0, y0, x1, y1);
            }
        }

        // --- War and Economy ----------------------------------------------------------------------

        private int BuildingLevel(string definitionId) =>
            (View.Data.buildings ?? Array.Empty<Hadal.Data.Models.BuildingSaveEntry>()).Where(b => b.definitionId == definitionId)
            .Select(b => b.level).DefaultIfEmpty(0).Max();

        private static readonly ResourceType[] Produced = { ResourceType.Oxygen, ResourceType.Energy, ResourceType.Food, ResourceType.Titanium, ResourceType.Biomass };

        /// <summary>
        /// The bonuses the server applies, from the same sums it makes (PlayerBonuses): march, return and load and the
        /// plunder-safe share for war; production by resource, gathering, build and research speed, storage and extra
        /// queues for economy. Time bonuses show as the speed they give.
        /// </summary>
        private List<(string Label, string Value)> Bonuses(bool war)
        {
            var m = PlayerBonuses.Of(View, ClientCatalog.Catalog);
            var rows = new List<(string, string)>();
            if (war)
            {
                rows.Add((Loc.T("alliance.research.node.war_march"), Percent(m.MarchSpeedPercent)));
                rows.Add((Loc.T("boost.return"), Percent(m.ReturnSpeedPercent)));
                rows.Add((Loc.T("alliance.research.node.war_load"), Percent(m.MarchLoadPercent)));
                var safe = (PvpRules.ProtectedBaseShare + PvpRules.ProtectedPerVaultLevel * BuildingLevel(PvpRules.VaultBuilding)) * 100.0;
                rows.Add((Loc.T("boost.protected"), Percent(safe)));
                return rows;
            }

            foreach (var type in Produced.Concat(m.ProductionPercent.Keys.Where(k => !Produced.Contains(k))))
                rows.Add((Loc.F("boost.production", Loc.Resource(type.ToString())), Percent(m.ProductionPercent.TryGetValue(type, out var p) ? p : 0)));
            rows.Add((Loc.T("alliance.research.node.dev_gather"), Percent(m.GatherPercent)));
            rows.Add((Loc.T("alliance.research.node.dev_build"), Percent(-m.BuildTimePercent)));
            rows.Add((Loc.T("alliance.research.node.dev_research"), Percent(-m.ResearchTimePercent)));
            rows.Add((Loc.T("alliance.research.node.dev_storage"), Percent(m.CapacityPercent)));
            rows.Add((Loc.T("boost.builders"), "+" + Math.Max(0, m.ExtraBuilders)));
            rows.Add((Loc.T("boost.researchSlots"), "+" + Math.Max(0, m.ExtraResearchSlots)));
            return rows;
        }

        private static string Percent(double value) => $"{Math.Max(0, value):0.00}%";
    }
}
