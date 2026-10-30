using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.StateSync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Heroes page in Last Asylum's layout (docs/25): a four-column grid of tall painted hero cards filtered by class,
    /// the command crew and a "Get heroes" button to the Cryo-Pod; one hero's card with the living full-body art,
    /// gear slots, power and the Attributes / Stars / Skill tabs.
    /// </summary>
    public sealed partial class SocialView
    {
        private string _heroId = string.Empty;
        private GachaResultDto _podResult;
        private int _heroFilter;
        private int _heroTab;
        private RectTransform _heroBar;

        /// <summary>Class filter order of the list's tabs (classes without heroes are left out).</summary>
        private static readonly string[] ClassOrder = { "Commander", "Scientist", "Engineer", "Hunter", "Medic", "Pilot" };

        private string HeroesKey()
        {
            var key = new StringBuilder();
            key.Append(_heroFilter).Append('.').Append(_heroTab).Append(';');
            foreach (var h in View.Heroes)
                key.Append(h.HeroId).Append(h.Level).Append('.').Append(h.Stars).Append('.').Append(h.Shards).Append('.').Append(h.Slot).Append(';');
            // Whether each hero can be trained now: the wallet moves every second, so the page (and its living art)
            // redraws only when an upgrade button or red dot would change.
            if (_page != Page.Pod)
            {
                var wallet = Wallet();
                foreach (var h in View.Heroes)
                    key.Append(_catalog.TryGetHero(h.HeroId, out var d)
                                   && HeroRules.ValidateLevelUp(_catalog, d, new OwnedHero(h.HeroId, h.Level, h.Stars, h.Shards, h.Slot), wallet) == HeroRejectReason.None
                        ? '+' : '-');
            }

            return key.ToString();
        }

        private static string RarityColor(string rarity) => rarity switch
        {
            "rare" => "#4FC3F7",
            "epic" => "#CE93D8",
            "legendary" => "#FFB74D",
            _ => "#B0BEC5"
        };

        private static string Stars(int stars, int max) =>
            "<color=#FFB74D>" + new string('★', Math.Max(0, stars)) + "</color><color=#3A5560>" + new string('★', Math.Max(0, max - stars)) + "</color>";

        private static string ClassIcon(string heroClass) => "class_" + heroClass.ToLowerInvariant();

        /// <summary>The icon of a hero's skill: what it speeds up or produces.</summary>
        private static string SkillIcon(HeroDefinition hero)
        {
            var colon = hero.Effect.IndexOf(':');
            var kind = colon < 0 ? hero.Effect : hero.Effect.Substring(0, colon);
            if (kind == "production" && GameCatalog.TryParseResource(hero.Effect.Substring(colon + 1), out var resource))
                return Hud.HudIcons.Resource(resource);
            return kind switch
            {
                "buildTime" => "queue_build",
                "researchTime" => "left_research",
                "capacity" => "side_bag",
                _ => "power"
            };
        }

        /// <summary>"Construction time −%3" for a hero at this level and star count.</summary>
        private static string Effect(HeroDefinition hero, int level, int stars, int? nextLevel = null, int? nextStars = null)
        {
            var colon = hero.Effect.IndexOf(':');
            var kind = colon < 0 ? hero.Effect : hero.Effect.Substring(0, colon);
            var subject = colon < 0 ? string.Empty : hero.Effect.Substring(colon + 1);

            static string Value(int v) => (v > 0 ? "+" : v < 0 ? "-" : string.Empty) + "%" + Math.Abs(v);

            var now = HeroRules.EffectValue(hero, level, stars);
            var change = Value(now);
            if (nextLevel.HasValue || nextStars.HasValue)
            {
                var next = HeroRules.EffectValue(hero, nextLevel ?? level, nextStars ?? stars);
                if (next != now)
                    change += " → " + Value(next);
            }

            return Loc.F("effect." + kind, change, kind == "production" ? Loc.Resource(subject) : string.Empty);
        }

        /// <summary>Text over painted art: keeps its colour, gains a dark outline (and is left alone by InkSurface).</summary>
        private static TMP_Text Outline(TMP_Text label) => UiText.Outline(label, new Color(0.03f, 0.05f, 0.09f, 0.95f));

        private static TMP_Text ArtText(Transform parent, string text, int size, Color color, float x0, float y0, float x1, float y1,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var label = Outline(UiKit.Label("Text", parent, text, size, color, anchor, style));
            label.rectTransform.Place(x0, y0, x1, y1);
            return label;
        }

        /// <summary>
        /// The hero's painted face in a rarity-coloured frame (darkened until found): the head and shoulders of the
        /// full-body art, else the old square bust, else initials.
        /// </summary>
        internal static void Portrait(Transform parent, HeroDefinition hero, bool owned, float x0, float y0, float x1, float y1)
        {
            var full = HeroCardArt.FullBody(hero.Id);
            var painted = full == null ? Hadal.Core.Catalog.CharacterLibrary.Instance?.Portrait(hero.Portrait) : null;
            if (full != null || painted != null)
            {
                var frame = UiKit.Box("PortraitFrame", parent, HeroCardArt.Tint(hero.Rarity.id));
                frame.rectTransform.Place(x0, y0, x1, y1, 6);
                frame.raycastTarget = false;
                var inner = UiKit.Rect("Inner", frame.transform).Place(0, 0, 1, 1, 4);
                inner.gameObject.AddComponent<RectMask2D>();
                if (full != null)
                {
                    // A square crop around the head and chest of the 3:5 figure.
                    HeroCardArt.Stage(inner, hero, owned, new Rect(0.2f, 0.6f, 0.6f, 0.36f));
                }
                else
                {
                    var picture = UiKit.Rect("Portrait", inner).gameObject.AddComponent<RawImage>();
                    picture.texture = painted;
                    picture.color = owned ? Color.white : new Color(0.22f, 0.25f, 0.28f, 1f);
                    picture.raycastTarget = false;
                    ((RectTransform)picture.transform).Place(0, 0, 1, 1);
                }

                var band = UiKit.Box("Band", frame.transform, new Color(0f, 0f, 0f, 0.6f));
                band.rectTransform.Place(0, 0, 1, 0.24f, 4);
                band.raycastTarget = false;
                var caption = UiKit.Label("Class", band.transform, Loc.T("class." + hero.HeroClass), 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                caption.rectTransform.Place(0, 0, 1, 1);
                return;
            }

            ColorUtility.TryParseHtmlString("#" + hero.Color, out var tint);
            var tile = UiKit.Box("Portrait", parent, owned ? new Color(tint.r * 0.35f, tint.g * 0.35f, tint.b * 0.35f, 1f) : new Color(0.05f, 0.08f, 0.1f, 1f));
            tile.rectTransform.Place(x0, y0, x1, y1, 8);
            tile.raycastTarget = false;
            var name = Loc.T("hero." + hero.Id);
            var initial = UiKit.Label("Initial", tile.transform, name.Length > 0 ? name.Substring(0, 1) : "?", 64,
                owned ? tint : new Color(0.25f, 0.33f, 0.37f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            initial.rectTransform.Place(0, 0.25f, 1, 1);
            initial.raycastTarget = false;
            var cls = UiKit.Label("Class", tile.transform, Loc.T("class." + hero.HeroClass), 20, UiKit.Muted, TextAnchor.MiddleCenter);
            cls.rectTransform.Place(0, 0, 1, 0.28f);
            cls.raycastTarget = false;
        }

        // --- List ----------------------------------------------------------------------------

        private List<string> HeroClasses() =>
            ClassOrder.Where(c => _catalog.Heroes.Any(h => h.HeroClass == c))
                .Concat(_catalog.Heroes.Select(h => h.HeroClass).Where(c => !ClassOrder.Contains(c)).Distinct())
                .ToList();

        /// <summary>The list's heroes under the current class tab: found ones by power, then the ones still to find.</summary>
        private List<(HeroDefinition Definition, bool Owned, HeroView Hero)> OrderedHeroes()
        {
            var classes = HeroClasses();
            var filter = _heroFilter > 0 && _heroFilter <= classes.Count ? classes[_heroFilter - 1] : null;
            return _catalog.Heroes
                .Where(d => filter == null || d.HeroClass == filter)
                .Select(d => (Definition: d, Owned: View.TryGetHero(d.Id, out var h), Hero: h))
                .OrderByDescending(x => x.Owned)
                .ThenByDescending(x => x.Owned ? HeroRules.Power(x.Definition, x.Hero.Level, x.Hero.Stars) : 0)
                .ThenByDescending(x => x.Definition.Rarity.basePower)
                .ToList();
        }

        /// <summary>Something to do on this hero: train, promote or recruit (the list's red dot).</summary>
        private bool HeroHasAction(HeroDefinition definition, bool owned, HeroView hero)
        {
            if (!owned)
                return (View.HeroCards.TryGetValue(definition.Id, out var cards) ? cards : 0) >= QuestRules.RecruitShards(definition);
            var needed = HeroRules.ShardsForNextStar(_catalog.HeroRules, hero.Stars);
            return needed > 0 && hero.Shards >= needed
                   || HeroRules.ValidateLevelUp(_catalog, definition, new OwnedHero(hero.HeroId, hero.Level, hero.Stars, hero.Shards, hero.Slot), Wallet()) == HeroRejectReason.None;
        }

        private void RenderHeroes()
        {
            var rules = _catalog.HeroRules;
            _title.text = Loc.T("social.heroes");
            var totalPower = View.Heroes.Sum(h => _catalog.TryGetHero(h.HeroId, out var d) ? HeroRules.Power(d, h.Level, h.Stars) : 0);
            _subtitle.text = Loc.F("hero.power.total", totalPower.ToString("N0"));

            if (!View.HasHeroes || _catalog.Heroes.Count == 0)
            {
                Note(Loc.T("hero.none"));
                return;
            }

            // Class tabs.
            var classes = HeroClasses();
            var labels = new[] { Loc.T("hero.tab.all") }.Concat(classes.Select(c => Loc.T("class." + c))).ToArray();
            var bar = UiKit.Rect("TabsRow", _list).Row(90);
            UiKit.Choice("Tabs", bar, labels, () => _heroFilter, i => { _heroFilter = i; Render(force: true); }).Place(0, 0, 1, 1);

            // The grid of tall cards.
            var ordered = OrderedHeroes();
            const int columns = 4;
            for (var start = 0; start < ordered.Count; start += columns)
            {
                var row = UiKit.Rect("Row", _list).Row(390);
                for (var c = 0; c < columns && start + c < ordered.Count; c++)
                {
                    var (definition, owned, hero) = ordered[start + c];
                    HeroTile(row, definition, owned, hero, c * 0.25f + 0.008f, c * 0.25f + 0.242f);
                }
            }

            // Command crew: the heroes whose skills are working.
            Header(Loc.T("hero.crew"));
            var crew = UiKit.Rect("Crew", _list).Row(170);
            var open = HeroRules.OpenSlots(rules, ReactorLevel());
            var slots = HeroRules.SlotCount(rules);
            for (var slot = 0; slot < slots; slot++)
            {
                var x0 = (float)slot / slots + 0.006f;
                var x1 = (float)(slot + 1) / slots - 0.006f;
                var tile = UiKit.Box("Slot", crew, new Color(0.04f, 0.1f, 0.15f, 0.95f));
                tile.rectTransform.Place(x0, 0, x1, 1);
                var holder = View.Heroes.FirstOrDefault(h => h.Slot == slot);
                if (slot >= open)
                {
                    ArtText(tile.transform, Loc.F("hero.slot", slot + 1), 26, UiKit.Muted, 0.05f, 0.55f, 0.95f, 0.95f);
                    ArtText(tile.transform, Loc.F("hero.slot.locked", rules.crewSlotReactorLevels[slot]), 20, UiKit.Danger, 0.05f, 0.05f, 0.95f, 0.55f,
                        TextAnchor.MiddleCenter, FontStyle.Normal);
                    continue;
                }

                if (holder.HeroId != null && _catalog.TryGetHero(holder.HeroId, out var held))
                {
                    Portrait(tile.transform, held, true, 0.02f, 0.04f, 0.42f, 0.96f);
                    ArtText(tile.transform, Loc.T("hero." + held.Id), 24, UiKit.Text, 0.44f, 0.5f, 0.98f, 0.95f, TextAnchor.MiddleLeft);
                    ArtText(tile.transform, Loc.F("hero.slot", slot + 1), 20, UiKit.Accent, 0.44f, 0.1f, 0.98f, 0.5f, TextAnchor.MiddleLeft, FontStyle.Normal);
                    var id = held.Id;
                    tile.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowHero(id));
                    tile.gameObject.AddComponent<PressFeedback>();
                }
                else
                {
                    ArtText(tile.transform, Loc.F("hero.slot", slot + 1), 26, UiKit.Text, 0.05f, 0.55f, 0.95f, 0.95f);
                    ArtText(tile.transform, Loc.T("hero.slot.empty"), 18, UiKit.Muted, 0.05f, 0.05f, 0.95f, 0.55f, TextAnchor.MiddleCenter, FontStyle.Normal);
                }
            }

            // "Get heroes" stays at the bottom of the sheet, outside the scrolling list.
            var get = UiKit.Button("GetHeroes", HeroBar(), Loc.T("hero.get"), () => Open(Page.Pod), UiKit.Gold, 42);
            ((RectTransform)get.transform).Place(0.2f, 0.06f, 0.8f, 0.94f);
        }

        /// <summary>The fixed bar under the list on the hero list (cleared on every render).</summary>
        private RectTransform HeroBar()
        {
            if (_heroBar == null)
                _heroBar = UiKit.Rect("HeroBar", _body).Place(0.03f, 0.012f, 0.97f, 0.092f);
            for (var i = _heroBar.childCount - 1; i >= 0; i--)
                Destroy(_heroBar.GetChild(i).gameObject);
            _heroBar.gameObject.SetActive(true);
            return _heroBar;
        }

        /// <summary>One tall card of the grid: art on the rarity backdrop, class badge, red dot, level, class and stars.</summary>
        private void HeroTile(RectTransform row, HeroDefinition definition, bool owned, HeroView hero, float x0, float x1)
        {
            var rules = _catalog.HeroRules;
            var tint = HeroCardArt.Tint(definition.Rarity.id);
            var frame = UiKit.Box("HeroTile", row, tint);
            frame.rectTransform.Place(x0, 0, x1, 1);

            var art = UiKit.Rect("Art", frame.transform).Place(0, 0, 1, 1, 5);
            art.gameObject.AddComponent<RectMask2D>();
            var figure = HeroCardArt.Stage(art, definition, owned, new Rect(0.08f, 0.22f, 0.84f, 0.78f));
            if (figure == null)
                Portrait(art, definition, owned, 0.1f, 0.3f, 0.9f, 0.9f);

            // Class badge and grade letter, top left.
            var badge = UiKit.Box("ClassBadge", art, new Color(0.03f, 0.06f, 0.1f, 0.85f));
            badge.sprite = UiKit.Circle;
            badge.type = Image.Type.Simple;
            badge.rectTransform.Place(0.02f, 0.84f, 0.3f, 0.99f);
            badge.preserveAspect = true;
            Hud.HudIcons.Image(badge.transform, ClassIcon(definition.HeroClass), 0.1f, 0.1f, 0.9f, 0.9f);
            ArtText(art, HeroCardArt.Letter(definition.Rarity.id), 24, Color.Lerp(tint, Color.white, 0.4f), 0.02f, 0.76f, 0.32f, 0.85f, TextAnchor.MiddleCenter);

            // Red dot: something to do here.
            if (HeroHasAction(definition, owned, hero))
            {
                var dot = UiKit.Box("Dot", art, new Color(0.95f, 0.2f, 0.18f, 1f));
                dot.sprite = UiKit.Circle;
                dot.type = Image.Type.Simple;
                dot.preserveAspect = true;
                dot.rectTransform.Place(0.8f, 0.9f, 0.97f, 0.99f);
            }

            // The dark band with level, class and stars.
            var band = UiKit.Box("Band", art, new Color(0.01f, 0.03f, 0.06f, 0.78f));
            band.rectTransform.Place(0, 0, 1, 0.24f);
            if (owned)
            {
                Hud.HudIcons.Image(band.transform, ClassIcon(definition.HeroClass), 0.03f, 0.42f, 0.27f, 0.98f);
                ArtText(band.transform, Loc.F("hero.lv", hero.Level), 30, Color.white, 0.27f, 0.42f, 0.98f, 0.98f, TextAnchor.MiddleLeft);
                ArtText(band.transform, Stars(hero.Stars, rules.maxStars), 24, Color.white, 0, 0.02f, 1, 0.44f);
            }
            else
            {
                var cards = View.HeroCards.TryGetValue(definition.Id, out var c) ? c : 0;
                var need = QuestRules.RecruitShards(definition);
                ArtText(band.transform, Loc.T("hero." + definition.Id), 22, Color.white, 0.03f, 0.5f, 0.97f, 0.98f);
                Skin.Bar(band.transform, need > 0 ? (float)cards / need : 0f, cards >= need ? "green" : "blue").Place(0.06f, 0.08f, 0.94f, 0.42f);
                ArtText(band.transform, $"{cards}/{need}", 20, Color.white, 0.06f, 0.06f, 0.94f, 0.44f);
            }

            var id = definition.Id;
            frame.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowHero(id));
            frame.gameObject.AddComponent<PressFeedback>();
        }

        private void ShowHero(string heroId)
        {
            _heroId = heroId;
            _page = Page.HeroDetail;
            Render(force: true);
        }

        // --- Cryo-Pod ------------------------------------------------------------------------

        private void RenderPod()
        {
            var rules = _catalog.HeroRules;
            _title.text = Loc.T("hero.pod");
            _subtitle.text = Loc.F("hero.power.total", View.Heroes.Sum(h => _catalog.TryGetHero(h.HeroId, out var d) ? HeroRules.Power(d, h.Level, h.Stars) : 0).ToString("N0"));

            var back = Card(90);
            CardButton(back.transform, "‹ " + Loc.T("hero.back"), true, () => Open(Page.Heroes), UiKit.Field, 0.01f, 0.08f, 0.35f, 0.92f);

            // The best hero still to find, standing in the open pod.
            var showcase = UiKit.Box("PodShowcase", _list, new Color(0.03f, 0.07f, 0.11f, 1f)).Row(560);
            var area = UiKit.Rect("Art", showcase.transform).Place(0, 0, 1, 1, 6);
            var featured = _catalog.Heroes.OrderByDescending(h => h.Rarity.basePower).FirstOrDefault(h => !View.TryGetHero(h.Id, out _))
                           ?? _catalog.Heroes.OrderByDescending(h => h.Rarity.basePower).First();
            var figure = HeroCardArt.Stage(area, featured, true, new Rect(0, 0, 1, 1));
            if (figure != null)
            {
                var r = (RectTransform)figure.transform;
                r.anchorMin = new Vector2(0.5f, 0f);
                r.anchorMax = new Vector2(0.5f, 0.98f);
                r.offsetMin = r.offsetMax = Vector2.zero;
                r.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                r.GetComponent<AspectRatioFitter>().aspectRatio = 0.6f;
            }

            HeroIdleMotion.Attach(area, figure, HeroCardArt.Tint(featured.Rarity.id));
            ArtText(area, $"{HeroCardArt.Letter(featured.Rarity.id)}  {Loc.T("hero." + featured.Id)}", 34, Color.Lerp(HeroCardArt.Tint(featured.Rarity.id), Color.white, 0.3f),
                0.03f, 0.86f, 0.97f, 0.98f, TextAnchor.UpperLeft);

            var pod = Card(300);
            var weights = rules.rarities.Sum(r => Math.Max(0, r.weight));
            var odds = string.Join("  ", rules.rarities.Select(r => $"<color={RarityColor(r.id)}>{Loc.T("rarity." + r.id)}</color> %{(weights > 0 ? r.weight * 100f / weights : 0):0.#}"));
            CardText(pod.transform, Loc.T("hero.pod.body"), 28, UiKit.Text, 0.04f, 0.62f, 0.96f, 0.96f);
            CardText(pod.transform, odds, 26, UiKit.Muted, 0.04f, 0.44f, 0.96f, 0.62f);
            CardText(pod.transform, Loc.F("hero.pod.pity", Math.Max(0, rules.pod.pity - View.SincePity), Loc.T("rarity." + rules.pod.pityRarity)), 26, UiKit.Amber,
                0.04f, 0.3f, 0.96f, 0.44f);

            var wallet = Wallet();
            var freeLeft = View.FreePullReadyAt < 0 ? -1f : View.FreePullReadyAt - Time.realtimeSinceStartup;
            if (freeLeft <= 0 && View.FreePullReadyAt >= 0)
            {
                CardButton(pod.transform, Loc.T("hero.pod.free"), true, () => Pull(1), UiKit.Amber, 0.03f, 0.04f, 0.48f, 0.28f);
            }
            else
            {
                var single = BuildingProgression.CanAfford(_catalog.PodCost, wallet);
                CardButton(pod.transform, $"1×  {Cost(_catalog.PodCost, wallet)}", single, () => Pull(1), null, 0.03f, 0.04f, 0.48f, 0.28f);
            }

            var ten = BuildingProgression.CanAfford(_catalog.PodTenCost, wallet);
            CardButton(pod.transform, $"10×  {Cost(_catalog.PodTenCost, wallet)}", ten, () => Pull(10), null, 0.52f, 0.04f, 0.97f, 0.28f);
            if (freeLeft > 0)
                CardText(pod.transform, Loc.F("hero.pod.next", Loc.Duration(freeLeft)), 24, UiKit.Muted, 0.04f, 0.28f, 0.96f, 0.32f, TextAnchor.LowerRight);
        }

        // --- Hero card -----------------------------------------------------------------------

        private void RenderHeroDetail()
        {
            if (!_catalog.TryGetHero(_heroId, out var hero))
            {
                _page = Page.Heroes;
                RenderHeroes();
                return;
            }

            var rules = _catalog.HeroRules;
            var owned = View.TryGetHero(hero.Id, out var state);
            var level = owned ? state.Level : 1;
            var stars = owned ? state.Stars : 1;
            var tint = HeroCardArt.Tint(hero.Rarity.id);
            _title.text = Loc.T("hero." + hero.Id);
            _subtitle.text = $"<color={RarityColor(hero.Rarity.id)}>{Loc.T("rarity." + hero.Rarity.id)}</color> · {Loc.T("faction." + hero.Faction)}";

            // The card fills the visible sheet, as in Last Asylum (no scrolling on the hero card).
            var visible = ((RectTransform)_list.parent).rect.height / UiKit.Scale;
            var card = UiKit.Rect("HeroCard", _list).Row(visible > 900f ? visible - 8f : 1440f);

            // Upper two thirds: the living art.
            var frame = UiKit.Box("ArtFrame", card, Color.Lerp(tint, Color.black, 0.35f));
            frame.rectTransform.Place(0, 0.39f, 1, 1);
            var area = UiKit.Rect("Art", frame.transform).Place(0, 0, 1, 1, 5);
            var figure = HeroCardArt.Stage(area, hero, owned, new Rect(0, 0, 1, 1));
            if (figure != null)
            {
                var r = (RectTransform)figure.transform;
                r.anchorMin = new Vector2(0.5f, 0.02f);
                r.anchorMax = new Vector2(0.5f, 0.9f);
                r.offsetMin = r.offsetMax = Vector2.zero;
                var fit = r.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                fit.aspectRatio = 0.6f;
            }

            HeroIdleMotion.Attach(area, figure, tint);

            // Grade letter, name, class.
            ArtText(area, HeroCardArt.Letter(hero.Rarity.id), 64, Color.Lerp(tint, Color.white, 0.35f), 0.02f, 0.88f, 0.22f, 0.99f);
            ArtText(area, Loc.T("hero." + hero.Id), 46, Color.white, 0.2f, 0.92f, 0.8f, 0.995f);
            Hud.HudIcons.Image(area, ClassIcon(hero.HeroClass), 0.3f, 0.855f, 0.37f, 0.915f);
            ArtText(area, $"{Loc.T("class." + hero.HeroClass)} · {Loc.T("faction." + hero.Faction)}", 26, UiKit.Text, 0.37f, 0.85f, 0.9f, 0.92f,
                TextAnchor.MiddleLeft, FontStyle.Normal);

            // Flip through the heroes of the list.
            var ordered = OrderedHeroes();
            var index = ordered.FindIndex(x => x.Definition.Id == hero.Id);
            if (ordered.Count > 1)
            {
                var previous = ordered[(index - 1 + ordered.Count) % ordered.Count].Definition.Id;
                var next = ordered[(index + 1) % ordered.Count].Definition.Id;
                ((RectTransform)UiKit.Button("Prev", area, "‹", () => ShowHero(previous), UiKit.Field, 60).transform).Place(0.01f, 0.48f, 0.11f, 0.6f);
                ((RectTransform)UiKit.Button("Next", area, "›", () => ShowHero(next), UiKit.Field, 60).transform).Place(0.89f, 0.48f, 0.99f, 0.6f);
            }

            // Four gear slots on the sides (gear arrives later).
            string[] gear = { "gear_helmet", "gear_suit", "gear_weapon", "gear_tank" };
            for (var g = 0; g < gear.Length; g++)
            {
                var left = g < 2;
                var y = g % 2 == 0 ? 0.33f : 0.18f;
                var slot = UiKit.Box("GearSlot", area, new Color(0.02f, 0.05f, 0.09f, 0.75f));
                slot.rectTransform.Place(left ? 0.02f : 0.85f, y, left ? 0.15f : 0.98f, y + 0.11f);
                var rim = UiKit.Box("Rim", slot.transform, new Color(tint.r, tint.g, tint.b, 0.6f));
                rim.sprite = UiKit.Ring;
                rim.raycastTarget = false;
                rim.rectTransform.Place(0, 0, 1, 1);
                var icon = Hud.HudIcons.Image(slot.transform, gear[g], 0.14f, 0.14f, 0.86f, 0.86f);
                icon.color = new Color(1f, 1f, 1f, 0.45f);
                slot.raycastTarget = true;
                slot.gameObject.AddComponent<Button>().onClick.AddListener(() => Toast(Loc.T("hero.gear.soon"), UiKit.Muted));
            }

            // Power, level and stars at the feet of the figure.
            var power = HeroRules.Power(hero, level, stars);
            var powerBand = UiKit.Box("PowerBand", area, new Color(0.01f, 0.03f, 0.06f, 0.7f));
            powerBand.rectTransform.Place(0.22f, 0.01f, 0.78f, 0.12f);
            Hud.HudIcons.Image(powerBand.transform, "power", 0.02f, 0.1f, 0.2f, 0.9f);
            ArtText(powerBand.transform, power.ToString("N0"), 48, UiKit.Gold, 0.2f, 0, 0.98f, 1, TextAnchor.MiddleLeft);
            ArtText(area, owned ? $"{Loc.F("hero.lv", level)} / {HeroRules.LevelCap(rules, stars)}" : Loc.T("hero.locked"), 30, Color.white, 0.2f, 0.12f, 0.8f, 0.17f);
            ArtText(area, Stars(owned ? stars : 0, rules.maxStars), 34, Color.white, 0.2f, 0.165f, 0.8f, 0.22f);

            // Lower third: the open tab, the tabs under it.
            var body = UiKit.Box("TabBody", card, new Color(0.03f, 0.08f, 0.12f, 0.96f));
            body.rectTransform.Place(0, 0.075f, 1, 0.385f);
            var tabs = new[] { Loc.T("hero.tab.attributes"), Loc.T("hero.tab.stars"), Loc.T("hero.tab.skill") };
            UiKit.Choice("HeroTabs", card, tabs, () => _heroTab, i => { _heroTab = i; Render(force: true); }).Place(0, 0, 1, 0.07f);

            var content = UiKit.Rect("Content", body.transform).Place(0, 0, 1, 1, 14);
            if (!owned && _heroTab != 2)
            {
                HeroRecruitTab(content, hero);
                return;
            }

            switch (_heroTab)
            {
                case 1: HeroStarsTab(content, hero, state); break;
                case 2: HeroSkillTab(content, hero, level, stars); break;
                default: HeroAttributesTab(content, hero, state); break;
            }
        }

        private void HeroRecruitTab(RectTransform content, HeroDefinition hero)
        {
            var cards = View.HeroCards.TryGetValue(hero.Id, out var c) ? c : 0;
            var need = QuestRules.RecruitShards(hero);
            ArtText(content, Loc.T("hero." + hero.Id + ".bio"), 26, UiKit.Text, 0.02f, 0.58f, 0.98f, 0.98f, TextAnchor.UpperLeft, FontStyle.Normal);
            Skin.Bar(content, need > 0 ? (float)cards / need : 0f, cards >= need ? "green" : "blue").Place(0.02f, 0.4f, 0.6f, 0.5f);
            ArtText(content, Loc.F("hero.cards", cards, need), 26, cards >= need ? UiKit.Ok : UiKit.Muted, 0.62f, 0.38f, 0.98f, 0.52f, TextAnchor.MiddleLeft);
            ArtText(content, Loc.T("hero.locked.how"), 22, UiKit.Muted, 0.02f, 0.26f, 0.98f, 0.38f, TextAnchor.MiddleLeft, FontStyle.Normal);
            var id = hero.Id;
            CardButton(content, Loc.T("hero.recruit"), cards >= need, () => _commands.Dispatch(new RecruitHeroCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), HeroId = id
            }), UiKit.Gold, 0.52f, 0.0f, 0.98f, 0.24f);
            CardButton(content, Loc.T("hero.get"), true, () => Open(Page.Pod), UiKit.Primary, 0.02f, 0.0f, 0.48f, 0.24f);
        }

        /// <summary>One stat chip: small label over a bold value.</summary>
        private static void StatChip(RectTransform content, string icon, string label, string value, float x0, float x1, float y0, float y1)
        {
            var chip = UiKit.Box("Stat", content, new Color(0.06f, 0.14f, 0.2f, 1f));
            chip.rectTransform.Place(x0, y0, x1, y1, 3);
            Hud.HudIcons.Image(chip.transform, icon, 0.03f, 0.15f, 0.3f, 0.85f);
            ArtText(chip.transform, label, 20, UiKit.Muted, 0.3f, 0.5f, 0.98f, 0.96f, TextAnchor.MiddleLeft, FontStyle.Normal);
            ArtText(chip.transform, value, 30, Color.white, 0.3f, 0.04f, 0.98f, 0.54f, TextAnchor.MiddleLeft);
        }

        private void HeroAttributesTab(RectTransform content, HeroDefinition hero, HeroView state)
        {
            var rules = _catalog.HeroRules;
            var power = HeroRules.Power(hero, state.Level, state.Stars);
            // Command capacity: how many troops this hero takes on a march (docs/rehber 19 §1.2).
            var command = View.Army?.Command?.FirstOrDefault(c => c.HeroId == state.HeroId)?.Capacity
                          ?? TroopRules.CommandCapacity(_catalog.Troops, state.Level, state.Stars, 0);
            var bonus = CombatRules.HeroBonus(new[] { power });
            var cap = HeroRules.LevelCap(rules, state.Stars);
            StatChip(content, "power", Loc.T("hero.stat.power"), power.ToString("N0"), 0f, 0.25f, 0.74f, 1f);
            StatChip(content, "event_war", Loc.T("hero.stat.command"), command.ToString("N0"), 0.25f, 0.5f, 0.74f, 1f);
            StatChip(content, ClassIcon(hero.HeroClass), Loc.T("hero.stat.bonus"), "+%" + bonus.ToString("0.#"), 0.5f, 0.75f, 0.74f, 1f);
            StatChip(content, "quest", Loc.T("hero.stat.cap"), $"{state.Level}/{cap}", 0.75f, 1f, 0.74f, 1f);

            Hud.HudIcons.Image(content, SkillIcon(hero), 0.01f, 0.56f, 0.08f, 0.71f);
            ArtText(content, $"{Loc.T("hero.skill")}: {Effect(hero, state.Level, state.Stars, state.Level < cap ? state.Level + 1 : null)}", 26, UiKit.Accent,
                0.09f, 0.56f, 0.99f, 0.71f, TextAnchor.MiddleLeft);

            // Crew.
            var heroId = hero.Id;
            var openSlots = Math.Min(HeroRules.SlotCount(rules), HeroRules.OpenSlots(rules, ReactorLevel()));
            ArtText(content, state.Slot >= 0 ? Loc.F("hero.on.crew", state.Slot + 1) : Loc.T("hero.crew"), 24, UiKit.Text, 0.01f, 0.31f, 0.4f, 0.53f,
                TextAnchor.MiddleLeft, FontStyle.Normal);
            if (state.Slot >= 0)
            {
                CardButton(content, Loc.T("hero.unassign"), true, () => Assign(heroId, -1), UiKit.Danger, 0.6f, 0.31f, 0.99f, 0.53f);
            }
            else
            {
                var width = 0.58f / Math.Max(1, openSlots);
                for (var slot = 0; slot < openSlots; slot++)
                {
                    var s = slot;
                    CardButton(content, Loc.F("hero.slot", slot + 1), true, () => Assign(heroId, s), UiKit.Field,
                        0.41f + width * slot, 0.31f, 0.41f + width * (slot + 1) - 0.01f, 0.53f);
                }
            }

            // Upgrade: the cost and the gold button.
            var wallet = Wallet();
            var verdict = HeroRules.ValidateLevelUp(_catalog, hero, new OwnedHero(state.HeroId, state.Level, state.Stars, state.Shards, state.Slot), wallet);
            if (state.Level < cap)
            {
                ArtText(content, Cost(HeroRules.LevelCost(_catalog, hero, state.Level), wallet), 26, UiKit.Text, 0.01f, 0.02f, 0.5f, 0.26f,
                    TextAnchor.MiddleLeft, FontStyle.Normal);
                CardButton(content, Loc.T("hero.upgrade"), verdict == HeroRejectReason.None, () => Send(new LevelUpHeroCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), HeroId = heroId
                }), UiKit.Gold, 0.52f, 0.0f, 0.99f, 0.27f);
            }
            else
            {
                ArtText(content, Loc.Reason(nameof(HeroRejectReason.LevelCapReached)), 26, UiKit.Amber, 0.01f, 0.02f, 0.99f, 0.26f,
                    TextAnchor.MiddleLeft, FontStyle.Normal);
            }
        }

        private void HeroStarsTab(RectTransform content, HeroDefinition hero, HeroView state)
        {
            var rules = _catalog.HeroRules;
            var needed = HeroRules.ShardsForNextStar(rules, state.Stars);
            ArtText(content, Stars(state.Stars, rules.maxStars), 56, Color.white, 0, 0.8f, 1, 1);
            if (needed <= 0)
            {
                ArtText(content, Loc.Reason(nameof(HeroRejectReason.MaxStars)), 30, UiKit.Gold, 0, 0.4f, 1, 0.75f);
                return;
            }

            // Before → after.
            var next = state.Stars + 1;
            string Line(string label, string before, string after) => $"{label}   {before}  <color=#7CFC9A>→ {after}</color>";
            var lines = string.Join("\n",
                Line(Loc.T("hero.stat.cap"), HeroRules.LevelCap(rules, state.Stars).ToString(), HeroRules.LevelCap(rules, next).ToString()),
                Line(Loc.T("hero.stat.power"), HeroRules.Power(hero, state.Level, state.Stars).ToString("N0"), HeroRules.Power(hero, state.Level, next).ToString("N0")),
                $"{Loc.T("hero.skill")}   {Effect(hero, state.Level, state.Stars, null, next)}");
            Hud.HudIcons.Image(content, SkillIcon(hero), 0.01f, 0.42f, 0.1f, 0.62f);
            ArtText(content, lines, 26, UiKit.Text, 0.12f, 0.36f, 0.99f, 0.8f, TextAnchor.MiddleLeft, FontStyle.Normal);

            Skin.Bar(content, (float)state.Shards / needed, state.Shards >= needed ? "green" : "gold").Place(0.02f, 0.28f, 0.7f, 0.36f);
            ArtText(content, Loc.F("hero.shards", state.Shards, needed), 24, state.Shards >= needed ? UiKit.Ok : UiKit.Muted, 0.72f, 0.26f, 0.99f, 0.38f,
                TextAnchor.MiddleLeft);

            var heroId = hero.Id;
            CardButton(content, Loc.T("hero.promote.button"), state.Shards >= needed, () => Send(new PromoteHeroCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), HeroId = heroId
            }), UiKit.Gold, 0.52f, 0.0f, 0.99f, 0.24f);
            CardButton(content, Loc.T("hero.find.cards"), true, () => Open(Page.Pod), UiKit.Primary, 0.01f, 0.0f, 0.48f, 0.24f);
        }

        private void HeroSkillTab(RectTransform content, HeroDefinition hero, int level, int stars)
        {
            var rules = _catalog.HeroRules;
            // The skill on the left, the leader's troop bonus on the right.
            var skill = UiKit.Box("SkillIcon", content, new Color(0.06f, 0.14f, 0.2f, 1f));
            skill.rectTransform.Place(0.01f, 0.64f, 0.15f, 0.99f);
            Hud.HudIcons.Image(skill.transform, SkillIcon(hero), 0.1f, 0.1f, 0.9f, 0.9f);
            ArtText(content, Loc.T("hero.skill"), 28, UiKit.Gold, 0.17f, 0.84f, 0.99f, 0.99f, TextAnchor.MiddleLeft);
            ArtText(content, Effect(hero, level, stars), 26, UiKit.Accent, 0.17f, 0.64f, 0.99f, 0.84f, TextAnchor.MiddleLeft, FontStyle.Normal);

            var passive = UiKit.Box("PassiveIcon", content, new Color(0.06f, 0.14f, 0.2f, 1f));
            passive.rectTransform.Place(0.85f, 0.27f, 0.99f, 0.62f);
            Hud.HudIcons.Image(passive.transform, "event_war", 0.1f, 0.1f, 0.9f, 0.9f);
            ArtText(content, Loc.T("hero.stat.bonus"), 28, UiKit.Gold, 0.01f, 0.47f, 0.83f, 0.62f, TextAnchor.MiddleRight);
            ArtText(content, Loc.F("hero.skill.passive", CombatRules.HeroBonus(new[] { HeroRules.Power(hero, level, stars) }).ToString("0.#")), 24, UiKit.Text,
                0.01f, 0.27f, 0.83f, 0.47f, TextAnchor.MiddleRight, FontStyle.Normal);

            // What each star unlocks; reached stars are lit.
            var unlocks = string.Join("   ", Enumerable.Range(1, rules.maxStars).Select(s =>
            {
                var value = Math.Abs(HeroRules.EffectValue(hero, level, s));
                return s <= stars ? $"<color=#FFB74D>{s}★ %{value}</color>" : $"<color=#5A7480>{s}★ %{value}</color>";
            }));
            ArtText(content, Loc.T("hero.skill.unlocks") + "   " + unlocks, 24, UiKit.Text, 0.01f, 0.13f, 0.99f, 0.26f, TextAnchor.MiddleLeft, FontStyle.Normal);
            ArtText(content, Loc.T("hero." + hero.Id + ".bio"), 20, UiKit.Muted, 0.01f, 0f, 0.99f, 0.13f, TextAnchor.MiddleLeft, FontStyle.Normal);
        }

        private void RenderPodResult()
        {
            _title.text = Loc.T("hero.pod");
            _subtitle.text = string.Empty;
            foreach (var pull in _podResult?.Pulls ?? Array.Empty<GachaPullEntry>())
            {
                if (!_catalog.TryGetHero(pull.HeroId, out var hero))
                    continue;
                var card = Card(150);
                Portrait(card.transform, hero, true, 0.01f, 0.02f, 0.17f, 0.98f);
                CardText(card.transform, $"<color={RarityColor(hero.Rarity.id)}>{Loc.T("hero." + hero.Id)}</color>", 36, UiKit.Text,
                    0.2f, 0.5f, 0.75f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                CardText(card.transform, Loc.T("rarity." + hero.Rarity.id), 26, UiKit.Muted, 0.2f, 0.08f, 0.75f, 0.5f);
                CardText(card.transform, pull.IsNew ? Loc.T("hero.new") : Loc.F("hero.shards.got", pull.Shards), 32,
                    pull.IsNew ? UiKit.Amber : UiKit.Accent, 0.7f, 0.1f, 0.97f, 0.9f, TextAnchor.MiddleRight, FontStyle.Bold);
            }

            var done = Card(110);
            CardButton(done.transform, Loc.T("hero.pod.done"), true, () => Open(Page.Pod), null, 0.25f, 0.1f, 0.75f, 0.9f);
        }

        private void Pull(int count) => Send(new ExecuteGachaCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), BannerId = HeroRules.PodBanner,
            GachaType = HADAL.Shared.Enums.GachaType.CryoPodSalvage, PullCount = count
        });

        private void Assign(string heroId, int slot) => Send(new AssignHeroCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), HeroId = heroId, Slot = slot
        });

        private void Send(ExecuteGachaCommand command) => _commands.Dispatch(command);
        private void Send(LevelUpHeroCommand command) => _commands.Dispatch(command);
        private void Send(PromoteHeroCommand command) => _commands.Dispatch(command);
        private void Send(AssignHeroCommand command) => _commands.Dispatch(command);
    }
}
