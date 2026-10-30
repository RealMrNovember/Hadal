using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Services;
using Hadal.Core.StateSync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Heroes in Last Asylum's pages (docs/25, heroes.jpg and survivor_detail.jpg): faction tabs over a four-column grid
    /// of tall painted cards (level, stars, rarity frame, red dot) with "Get heroes" pinned under it and the command
    /// crew below; one hero's page with the living art, the name banner with class and stars, the Attributes / Stars /
    /// Skill tabs as stat rows and the upgrade or promote button at the bottom; the Cryo-Pod and its result.
    /// </summary>
    public sealed partial class SocialView
    {
        private string _heroId = string.Empty;
        private GachaResultDto _podResult;
        private int _heroFilter;
        private int _heroTab;

        /// <summary>Kept for SocialView.Render and Close; the hero list's button now sits in the page's bottom bar.</summary>
        private RectTransform _heroBar;

        /// <summary>Faction order of the list's tabs (factions without heroes are left out).</summary>
        private static readonly string[] FactionOrder = { "AtlanticUnion", "PacificOrder", "Deepborn", "ForgottenOnes", "AbyssCult" };

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
                    change += $" <color=#{Good}>→ {Value(next)}</color>";
            }

            return Loc.F("effect." + kind, change, kind == "production" ? Loc.Resource(subject) : string.Empty);
        }

        /// <summary>The green of a value after an upgrade, as rich text on the light rows.</summary>
        private static string Good => ColorUtility.ToHtmlStringRGB(UiFrames.InkGood);

        private static string BeforeAfter(string before, string after) => $"{before} <color=#{Good}>→ {after}</color>";

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

        private List<string> HeroFactions() =>
            FactionOrder.Where(f => _catalog.Heroes.Any(h => h.Faction == f))
                .Concat(_catalog.Heroes.Select(h => h.Faction).Where(f => !string.IsNullOrEmpty(f) && !FactionOrder.Contains(f)).Distinct())
                .ToList();

        /// <summary>The list's heroes under the current faction tab: found ones by power, then the ones still to find.</summary>
        private List<(HeroDefinition Definition, bool Owned, HeroView Hero)> OrderedHeroes()
        {
            var factions = HeroFactions();
            var filter = _heroFilter > 0 && _heroFilter <= factions.Count ? factions[_heroFilter - 1] : null;
            return _catalog.Heroes
                .Where(d => filter == null || d.Faction == filter)
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

        private long TotalHeroPower() => View.Heroes.Sum(h => _catalog.TryGetHero(h.HeroId, out var d) ? (long)HeroRules.Power(d, h.Level, h.Stars) : 0L);

        private void RenderHeroes()
        {
            BeginPage();
            _title.text = Loc.T("social.heroes");
            BarChips(new[] { ("power", (Func<string>)(() => TotalHeroPower().ToString("N0"))) });

            if (!View.HasHeroes || _catalog.Heroes.Count == 0)
            {
                NoteRow(Loc.T("hero.none"));
                return;
            }

            // Faction tabs, each with a red dot when one of its heroes has something to do.
            var factions = HeroFactions();
            var labels = new[] { Loc.T("hero.tab.all") }.Concat(factions.Select(f => Loc.T("faction." + f))).ToArray();
            var dots = new bool[labels.Length];
            foreach (var definition in _catalog.Heroes)
            {
                var owned = View.TryGetHero(definition.Id, out var hero);
                var index = factions.IndexOf(definition.Faction);
                if (index >= 0 && HeroHasAction(definition, owned, hero))
                    dots[index + 1] = true;
            }

            UiFrames.Tabs(PinTop(100), labels, _heroFilter, i =>
            {
                _heroFilter = i;
                Render(force: true);
            }, dots).Place(0, 0, 1, 1);

            // "Get heroes" stays at the bottom of the page, outside the scrolling list.
            BottomButton(Loc.T("hero.get"), UiKit.Primary, () => Open(Page.Pod));

            var grid = UiFrames.Grid(_list, 4, 0.62f, 12f);
            grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(4, 4, 6, 6);
            foreach (var (definition, owned, hero) in OrderedHeroes())
                HeroCard(grid, definition, owned, hero);

            RenderCrew();
        }

        /// <summary>Command crew: the heroes whose skills are working, one light slot each.</summary>
        private void RenderCrew()
        {
            var rules = _catalog.HeroRules;
            SectionRow(Loc.T("hero.crew"));
            var crew = UiKit.Rect("Crew", _list).Row(190);
            var open = HeroRules.OpenSlots(rules, ReactorLevel());
            var slots = HeroRules.SlotCount(rules);
            for (var slot = 0; slot < slots; slot++)
            {
                var tile = UiFrames.Plate(crew);
                tile.rectTransform.Place((float)slot / slots, 0, (float)(slot + 1) / slots, 1, 5);
                var holder = View.Heroes.FirstOrDefault(h => h.Slot == slot);
                if (slot >= open)
                {
                    UiKit.Label("Slot", tile.transform, Loc.F("hero.slot", slot + 1), UiKit.TypeScale.Body, UiFrames.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold)
                        .rectTransform.Place(0.05f, 0.55f, 0.95f, 0.95f);
                    UiKit.Label("Locked", tile.transform, Loc.F("hero.slot.locked", rules.crewSlotReactorLevels[slot]), UiKit.TypeScale.Micro, UiFrames.InkBad,
                        TextAnchor.MiddleCenter).rectTransform.Place(0.05f, 0.05f, 0.95f, 0.55f);
                    continue;
                }

                if (holder.HeroId != null && _catalog.TryGetHero(holder.HeroId, out var held))
                {
                    Portrait(tile.transform, held, true, 0.02f, 0.04f, 0.42f, 0.96f);
                    UiKit.Label("Name", tile.transform, Loc.T("hero." + held.Id), UiKit.TypeScale.Caption + 2, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold)
                        .rectTransform.Place(0.45f, 0.5f, 0.98f, 0.95f);
                    UiKit.Label("Slot", tile.transform, Loc.F("hero.slot", slot + 1), UiKit.TypeScale.Caption, UiFrames.InkSoft, TextAnchor.MiddleLeft)
                        .rectTransform.Place(0.45f, 0.1f, 0.98f, 0.5f);
                    var id = held.Id;
                    tile.raycastTarget = true;
                    tile.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowHero(id));
                    tile.gameObject.AddComponent<PressFeedback>();
                }
                else
                {
                    UiKit.Label("Slot", tile.transform, Loc.F("hero.slot", slot + 1), UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                        .rectTransform.Place(0.05f, 0.55f, 0.95f, 0.95f);
                    UiKit.Label("Empty", tile.transform, Loc.T("hero.slot.empty"), UiKit.TypeScale.Micro, UiFrames.InkSoft, TextAnchor.MiddleCenter)
                        .rectTransform.Place(0.05f, 0.05f, 0.95f, 0.55f);
                }
            }
        }

        /// <summary>One tall card of the grid: art on the rarity backdrop, class badge, red dot, level and stars.</summary>
        private void HeroCard(Transform grid, HeroDefinition definition, bool owned, HeroView hero)
        {
            var rules = _catalog.HeroRules;
            var tint = HeroCardArt.Tint(definition.Rarity.id);
            var frame = UiKit.Box("HeroCard", grid, tint);
            frame.sprite = UiKit.Rounded;

            var art = UiKit.Rect("Art", frame.transform).Place(0, 0, 1, 1, 5);
            art.gameObject.AddComponent<RectMask2D>();
            var figure = HeroCardArt.Stage(art, definition, owned, new Rect(0.08f, 0.22f, 0.84f, 0.78f));
            if (figure == null)
                Portrait(art, definition, owned, 0.1f, 0.3f, 0.9f, 0.9f);

            // Class badge and grade letter, top left.
            var badge = UiKit.Box("ClassBadge", art, new Color(0.03f, 0.06f, 0.1f, 0.85f));
            badge.sprite = UiKit.Circle;
            badge.type = Image.Type.Simple;
            badge.preserveAspect = true;
            badge.raycastTarget = false;
            badge.rectTransform.Place(0.03f, 0.85f, 0.3f, 0.99f);
            Hud.HudIcons.Image(badge.transform, ClassIcon(definition.HeroClass), 0.1f, 0.1f, 0.9f, 0.9f);
            ArtText(art, HeroCardArt.Letter(definition.Rarity.id), UiKit.TypeScale.Body, Color.Lerp(tint, Color.white, 0.4f), 0.03f, 0.76f, 0.3f, 0.86f);

            // The dark band with level and stars, or the cards still needed.
            var band = UiKit.Box("Band", art, new Color(0.01f, 0.03f, 0.06f, 0.72f));
            band.sprite = null;
            band.raycastTarget = false;
            band.rectTransform.Place(0, 0, 1, 0.24f);
            if (owned)
            {
                ArtText(band.transform, Loc.F("hero.lv", hero.Level), UiKit.TypeScale.Body, Color.white, 0.04f, 0.44f, 0.96f, 0.98f, TextAnchor.MiddleLeft);
                ArtText(band.transform, Stars(hero.Stars, rules.maxStars), UiKit.TypeScale.Caption, Color.white, 0, 0.02f, 1, 0.46f);
            }
            else
            {
                var cards = View.HeroCards.TryGetValue(definition.Id, out var c) ? c : 0;
                var need = QuestRules.RecruitShards(definition);
                ArtText(band.transform, Loc.T("hero." + definition.Id), UiKit.TypeScale.Micro, Color.white, 0.03f, 0.5f, 0.97f, 0.98f).Fit(12, UiKit.TypeScale.Micro);
                Skin.Bar(band.transform, need > 0 ? (float)cards / need : 0f, cards >= need ? "green" : "blue").Place(0.06f, 0.08f, 0.94f, 0.42f);
                ArtText(band.transform, $"{cards}/{need}", UiKit.TypeScale.Micro, Color.white, 0.06f, 0.06f, 0.94f, 0.44f);
            }

            if (HeroHasAction(definition, owned, hero))
                UiFrames.Dot(frame.transform);

            var id = definition.Id;
            frame.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                GameAudio.Play(Sfx.UiTap);
                ShowHero(id);
            });
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
            BeginPage();
            var rules = _catalog.HeroRules;
            _title.text = Loc.T("hero.pod");
            BarChips(_catalog.PodCost.Select(c => c.Key).Distinct().Take(3)
                .Select(type => (Hud.HudIcons.Resource(type), WalletChip(type))).ToArray());

            // The best hero still to find, standing in the open pod.
            var showcase = UiKit.Box("PodShowcase", _list, new Color(0.03f, 0.07f, 0.11f, 1f)).Row(640);
            showcase.sprite = UiKit.Rounded;
            var area = UiKit.Rect("Art", showcase.transform).Place(0, 0, 1, 1, 6);
            area.gameObject.AddComponent<RectMask2D>();
            var featured = _catalog.Heroes.OrderByDescending(h => h.Rarity.basePower).FirstOrDefault(h => !View.TryGetHero(h.Id, out _))
                           ?? _catalog.Heroes.OrderByDescending(h => h.Rarity.basePower).First();
            var figure = HeroCardArt.Stage(area, featured, true, new Rect(0, 0, 1, 1));
            StandFigure(figure, 0f, 0.98f);
            HeroIdleMotion.Attach(area, figure, HeroCardArt.Tint(featured.Rarity.id));
            ArtText(area, $"{HeroCardArt.Letter(featured.Rarity.id)}  {Loc.T("hero." + featured.Id)}", UiKit.TypeScale.Subtitle,
                Color.Lerp(HeroCardArt.Tint(featured.Rarity.id), Color.white, 0.3f), 0.04f, 0.86f, 0.96f, 0.98f, TextAnchor.UpperLeft);

            NoteRow(Loc.T("hero.pod.body"), 170);

            // The odds per rarity and the pity counter.
            var weights = rules.rarities.Sum(r => Math.Max(0, r.weight));
            foreach (var rarity in rules.rarities)
                UiFrames.BonusRow(_list, $"{HeroCardArt.Letter(rarity.id)}  {Loc.T("rarity." + rarity.id)}", $"%{(weights > 0 ? rarity.weight * 100f / weights : 0):0.#}")
                    .Row(84);
            NoteRow(Loc.F("hero.pod.pity", Math.Max(0, rules.pod.pity - View.SincePity), Loc.T("rarity." + rules.pod.pityRarity)), 110);

            var wallet = Wallet();
            var freeLeft = View.FreePullReadyAt < 0 ? -1f : View.FreePullReadyAt - Time.realtimeSinceStartup;
            if (freeLeft > 0)
                NoteRow(Loc.F("hero.pod.next", Loc.Duration(freeLeft)), 100);

            var bar = PinBottom();
            var free = freeLeft <= 0 && View.FreePullReadyAt >= 0;
            var single = free || BuildingProgression.CanAfford(_catalog.PodCost, wallet);
            var one = UiKit.Button("One", bar, free ? Loc.T("hero.pod.free") : $"1×  {Cost(_catalog.PodCost, wallet)}", () => Pull(1),
                single ? free ? UiKit.Gold : UiKit.Primary : UiKit.Panel, UiKit.TypeScale.Body);
            one.interactable = single;
            ((RectTransform)one.transform).Place(0.02f, 0.08f, 0.49f, 0.92f);
            var tenOk = BuildingProgression.CanAfford(_catalog.PodTenCost, wallet);
            var ten = UiKit.Button("Ten", bar, $"10×  {Cost(_catalog.PodTenCost, wallet)}", () => Pull(10), tenOk ? UiKit.Gold : UiKit.Panel, UiKit.TypeScale.Body);
            ten.interactable = tenOk;
            ((RectTransform)ten.transform).Place(0.51f, 0.08f, 0.98f, 0.92f);
        }

        /// <summary>Stands a 3:5 figure on the floor of its area, centred, between the given heights.</summary>
        private static void StandFigure(RawImage figure, float bottom, float top)
        {
            if (figure == null)
                return;
            var r = (RectTransform)figure.transform;
            r.anchorMin = new Vector2(0.5f, bottom);
            r.anchorMax = new Vector2(0.5f, top);
            r.offsetMin = r.offsetMax = Vector2.zero;
            var fit = r.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fit.aspectRatio = 0.6f;
        }

        private void RenderPodResult()
        {
            BeginPage();
            _title.text = Loc.T("hero.pod");
            var pulls = (_podResult?.Pulls ?? Array.Empty<GachaPullEntry>()).Where(p => _catalog.TryGetHero(p.HeroId, out _)).ToList();
            var columns = pulls.Count <= 1 ? 1 : pulls.Count <= 4 ? 2 : 3;
            var grid = UiFrames.Grid(_list, columns, 0.62f, 14f);
            var side = columns == 1 ? 240 : columns == 2 ? 90 : 8;
            grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(side, side, 10, 10);
            foreach (var pull in pulls)
            {
                _catalog.TryGetHero(pull.HeroId, out var hero);
                var tint = HeroCardArt.Tint(hero.Rarity.id);
                var frame = UiKit.Box("Pull", grid, tint);
                frame.sprite = UiKit.Rounded;
                var art = UiKit.Rect("Art", frame.transform).Place(0, 0, 1, 1, 5);
                art.gameObject.AddComponent<RectMask2D>();
                if (HeroCardArt.Stage(art, hero, true, new Rect(0.05f, 0.18f, 0.9f, 0.82f)) == null)
                    Portrait(art, hero, true, 0.1f, 0.3f, 0.9f, 0.9f);
                ArtText(art, HeroCardArt.Letter(hero.Rarity.id), UiKit.TypeScale.Subtitle, Color.Lerp(tint, Color.white, 0.4f), 0.03f, 0.84f, 0.3f, 0.99f);
                var band = UiKit.Box("Band", art, new Color(0.01f, 0.03f, 0.06f, 0.72f));
                band.sprite = null;
                band.raycastTarget = false;
                band.rectTransform.Place(0, 0, 1, 0.24f);
                ArtText(band.transform, Loc.T("hero." + hero.Id), UiKit.TypeScale.Caption, Color.white, 0.03f, 0.5f, 0.97f, 0.98f).Fit(12, UiKit.TypeScale.Caption);
                ArtText(band.transform, pull.IsNew ? Loc.T("hero.new") : Loc.F("hero.shards.got", pull.Shards), UiKit.TypeScale.Caption,
                    pull.IsNew ? UiKit.Gold : UiKit.Accent, 0.03f, 0.04f, 0.97f, 0.5f);
            }

            BottomButton(Loc.T("hero.pod.done"), UiKit.Primary, () => Open(Page.Pod));
        }

        // --- Hero page -----------------------------------------------------------------------

        private void RenderHeroDetail()
        {
            if (!_catalog.TryGetHero(_heroId, out var hero))
            {
                _page = Page.Heroes;
                RenderHeroes();
                return;
            }

            BeginPage();
            var rules = _catalog.HeroRules;
            var owned = View.TryGetHero(hero.Id, out var state);
            var level = owned ? state.Level : 1;
            var stars = owned ? state.Stars : 1;
            var tint = HeroCardArt.Tint(hero.Rarity.id);
            _title.text = Loc.T("hero." + hero.Id);

            // The living art on the rarity backdrop.
            var frame = UiKit.Box("ArtFrame", _list, Color.Lerp(tint, Color.black, 0.35f)).Row(820);
            frame.sprite = UiKit.Rounded;
            var area = UiKit.Rect("Art", frame.transform).Place(0, 0, 1, 1, 5);
            area.gameObject.AddComponent<RectMask2D>();
            var figure = HeroCardArt.Stage(area, hero, owned, new Rect(0, 0, 1, 1));
            StandFigure(figure, 0.01f, 0.97f);
            HeroIdleMotion.Attach(area, figure, tint);

            ArtText(area, HeroCardArt.Letter(hero.Rarity.id), UiKit.TypeScale.Display, Color.Lerp(tint, Color.white, 0.35f), 0.02f, 0.87f, 0.18f, 0.99f);
            var levelPill = UiFrames.Plate(area, new Color(0.02f, 0.04f, 0.08f, 0.6f));
            levelPill.rectTransform.Place(0.66f, 0.9f, 0.98f, 0.985f);
            ArtText(levelPill.transform, owned ? $"{Loc.F("hero.lv", level)} / {HeroRules.LevelCap(rules, stars)}" : Loc.T("hero.locked"), UiKit.TypeScale.Body,
                Color.white, 0.04f, 0, 0.96f, 1);
            var power = HeroRules.Power(hero, level, stars);
            var powerPill = UiFrames.Plate(area, new Color(0.02f, 0.04f, 0.08f, 0.6f));
            powerPill.rectTransform.Place(0.02f, 0.02f, 0.4f, 0.11f);
            Hud.HudIcons.Image(powerPill.transform, "power", 0.03f, 0.08f, 0.25f, 0.92f);
            ArtText(powerPill.transform, power.ToString("N0"), UiKit.TypeScale.Subtitle, UiKit.Gold, 0.27f, 0, 0.98f, 1, TextAnchor.MiddleLeft);

            // Flip through the heroes of the list.
            var ordered = OrderedHeroes();
            var index = ordered.FindIndex(x => x.Definition.Id == hero.Id);
            if (ordered.Count > 1)
            {
                var previous = ordered[(index - 1 + ordered.Count) % ordered.Count].Definition.Id;
                var next = ordered[(index + 1) % ordered.Count].Definition.Id;
                ((RectTransform)UiKit.Button("Prev", area, "‹", () => ShowHero(previous), UiKit.Field, UiKit.TypeScale.Display).transform).Place(0.01f, 0.44f, 0.12f, 0.56f);
                ((RectTransform)UiKit.Button("Next", area, "›", () => ShowHero(next), UiKit.Field, UiKit.TypeScale.Display).transform).Place(0.88f, 0.44f, 0.99f, 0.56f);
            }

            // The name banner: name and class on the left, the stars on the right.
            var banner = UiKit.Box("Banner", _list, Color.Lerp(tint, Color.black, 0.15f)).Row(170);
            banner.sprite = UiKit.Rounded;
            ArtText(banner.transform, Loc.T("hero." + hero.Id), UiKit.TypeScale.Title, Color.white, 0.04f, 0.42f, 0.62f, 0.95f, TextAnchor.MiddleLeft).Fit(24, UiKit.TypeScale.Title);
            Hud.HudIcons.Image(banner.transform, ClassIcon(hero.HeroClass), 0.035f, 0.08f, 0.1f, 0.42f);
            ArtText(banner.transform, $"{Loc.T("class." + hero.HeroClass)} · {Loc.T("faction." + hero.Faction)}", UiKit.TypeScale.Caption + 2, Color.white,
                0.11f, 0.06f, 0.62f, 0.42f, TextAnchor.MiddleLeft, FontStyle.Normal);
            ArtText(banner.transform, Stars(owned ? stars : 0, rules.maxStars), UiKit.TypeScale.Title, Color.white, 0.6f, 0.1f, 0.97f, 0.9f, TextAnchor.MiddleRight);

            var tabs = new[] { Loc.T("hero.tab.attributes"), Loc.T("hero.tab.stars"), Loc.T("hero.tab.skill") };
            UiFrames.Tabs(_list, tabs, _heroTab, i =>
            {
                _heroTab = i;
                Render(force: true);
            }).Row(100);

            if (!owned && _heroTab != 2)
            {
                HeroRecruitTab(hero);
                return;
            }

            switch (_heroTab)
            {
                case 1: HeroStarsTab(hero, state); break;
                case 2: HeroSkillTab(hero, level, stars); break;
                default: HeroAttributesTab(hero, state); break;
            }
        }

        /// <summary>A stat line of the hero page: icon, what, and the value (rich text for a green "→ next").</summary>
        private void StatRow(string icon, string label, string value, float split = 0.5f, float height = 92f, bool lit = true)
        {
            var row = PlateRow(height);
            if (icon != null)
                Hud.HudIcons.Image(row.transform, icon, 0.015f, 0.14f, 0.09f, 0.86f);
            UiKit.Label("Label", row.transform, label, UiKit.TypeScale.Body, lit ? UiFrames.Ink : UiFrames.InkSoft, TextAnchor.MiddleLeft)
                .rectTransform.Place(icon != null ? 0.11f : 0.04f, 0, split, 1);
            UiKit.Label("Value", row.transform, value, UiKit.TypeScale.Body, lit ? UiFrames.Ink : UiFrames.InkSoft, TextAnchor.MiddleRight, FontStyle.Bold)
                .rectTransform.Place(split, 0, 0.97f, 1);
        }

        /// <summary>Two buttons side by side in the page's bottom bar.</summary>
        private void BottomPair(string left, Color leftFill, Action leftClick, bool leftEnabled, string right, Color rightFill, Action rightClick, bool rightEnabled)
        {
            var bar = PinBottom();
            var a = UiKit.Button("Left", bar, left, () => leftClick?.Invoke(), leftEnabled ? leftFill : UiKit.Panel, UiKit.TypeScale.Subtitle);
            a.interactable = leftEnabled;
            ((RectTransform)a.transform).Place(0.02f, 0.08f, 0.49f, 0.92f);
            var b = UiKit.Button("Right", bar, right, () => rightClick?.Invoke(), rightEnabled ? rightFill : UiKit.Panel, UiKit.TypeScale.Subtitle);
            b.interactable = rightEnabled;
            ((RectTransform)b.transform).Place(0.51f, 0.08f, 0.98f, 0.92f);
        }

        private void HeroRecruitTab(HeroDefinition hero)
        {
            var cards = View.HeroCards.TryGetValue(hero.Id, out var c) ? c : 0;
            var need = QuestRules.RecruitShards(hero);
            NoteRow(Loc.T("hero." + hero.Id + ".bio"), 190);
            var row = PlateRow(110);
            Hud.HudIcons.Image(row.transform, "item_card_" + hero.Rarity.id, 0.015f, 0.1f, 0.1f, 0.9f);
            UiFrames.Progress(row.transform, cards, need, cards >= need ? "green" : "blue").Place(0.12f, 0.3f, 0.97f, 0.7f);
            NoteRow(Loc.T("hero.locked.how"), 100);
            var id = hero.Id;
            BottomPair(Loc.T("hero.get"), UiKit.Primary, () => Open(Page.Pod), true,
                Loc.T("hero.recruit"), UiKit.Gold, () => _commands.Dispatch(new RecruitHeroCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), HeroId = id
                }), cards >= need);
        }

        private void HeroAttributesTab(HeroDefinition hero, HeroView state)
        {
            var rules = _catalog.HeroRules;
            var power = HeroRules.Power(hero, state.Level, state.Stars);
            // Command capacity: how many troops this hero takes on a march (docs/rehber 19 §1.2).
            var command = View.Army?.Command?.FirstOrDefault(c => c.HeroId == state.HeroId)?.Capacity
                          ?? TroopRules.CommandCapacity(_catalog.Troops, state.Level, state.Stars, 0);
            var bonus = CombatRules.HeroBonus(new[] { power });
            var cap = HeroRules.LevelCap(rules, state.Stars);
            var nextLevel = state.Level < cap ? state.Level + 1 : (int?)null;

            StatRow(SkillIcon(hero), Loc.T("hero.skill"), Effect(hero, state.Level, state.Stars, nextLevel), 0.3f, 110);
            StatRow("power", Loc.T("hero.stat.power"), nextLevel.HasValue
                ? BeforeAfter(power.ToString("N0"), HeroRules.Power(hero, nextLevel.Value, state.Stars).ToString("N0")) : power.ToString("N0"));
            StatRow("event_war", Loc.T("hero.stat.command"), command.ToString("N0"));
            StatRow(ClassIcon(hero.HeroClass), Loc.T("hero.stat.bonus"), "+%" + bonus.ToString("0.#"));
            StatRow("quest", Loc.T("hero.stat.cap"), $"{state.Level}/{cap}");

            // Crew: the slot this hero holds, or the open slots to put it in.
            var heroId = hero.Id;
            var crew = PlateRow(120);
            UiKit.Label("Crew", crew.transform, state.Slot >= 0 ? Loc.F("hero.on.crew", state.Slot + 1) : Loc.T("hero.crew"), UiKit.TypeScale.Body, UiFrames.Ink,
                TextAnchor.MiddleLeft).rectTransform.Place(0.04f, 0, 0.4f, 1);
            if (state.Slot >= 0)
            {
                var off = UiKit.Button("Unassign", crew.transform, Loc.T("hero.unassign"), () => Assign(heroId, -1), UiKit.Danger, UiKit.TypeScale.Body);
                ((RectTransform)off.transform).Place(0.62f, 0.14f, 0.97f, 0.86f);
            }
            else
            {
                var openSlots = Math.Min(HeroRules.SlotCount(rules), HeroRules.OpenSlots(rules, ReactorLevel()));
                var width = 0.56f / Math.Max(1, openSlots);
                for (var slot = 0; slot < openSlots; slot++)
                {
                    var s = slot;
                    var button = UiKit.Button("Slot", crew.transform, Loc.F("hero.slot", slot + 1), () => Assign(heroId, s), UiKit.Primary, UiKit.TypeScale.Caption + 2);
                    ((RectTransform)button.transform).Place(0.41f + width * slot, 0.14f, 0.41f + width * (slot + 1) - 0.01f, 0.86f);
                }
            }

            // Upgrade: the cost line, the gold button at the bottom; at the cap the button leads to the stars.
            if (state.Level < cap)
            {
                var wallet = Wallet();
                var cost = PlateRow(100);
                UiFrames.CostLine(cost.transform, HeroRules.LevelCost(_catalog, hero, state.Level), wallet).Place(0.02f, 0, 0.98f, 1);
                var verdict = HeroRules.ValidateLevelUp(_catalog, hero, new OwnedHero(state.HeroId, state.Level, state.Stars, state.Shards, state.Slot), wallet);
                BottomButton(Loc.T("hero.upgrade"), UiKit.Gold, () => Send(new LevelUpHeroCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), HeroId = heroId
                }), verdict == HeroRejectReason.None);
            }
            else
            {
                NoteRow(Loc.Reason(nameof(HeroRejectReason.LevelCapReached)), 100);
                BottomButton(Loc.T("hero.promote.button"), UiKit.Gold, () =>
                {
                    _heroTab = 1;
                    Render(force: true);
                });
            }
        }

        private void HeroStarsTab(HeroDefinition hero, HeroView state)
        {
            var rules = _catalog.HeroRules;
            var needed = HeroRules.ShardsForNextStar(rules, state.Stars);
            StatRow("g_star", Loc.T("hero.tab.stars"), Stars(state.Stars, rules.maxStars));
            if (needed <= 0)
            {
                NoteRow(Loc.Reason(nameof(HeroRejectReason.MaxStars)), 110);
                return;
            }

            // Before → after.
            var next = state.Stars + 1;
            StatRow("quest", Loc.T("hero.stat.cap"), BeforeAfter(HeroRules.LevelCap(rules, state.Stars).ToString(), HeroRules.LevelCap(rules, next).ToString()));
            StatRow("power", Loc.T("hero.stat.power"),
                BeforeAfter(HeroRules.Power(hero, state.Level, state.Stars).ToString("N0"), HeroRules.Power(hero, state.Level, next).ToString("N0")));
            StatRow(SkillIcon(hero), Loc.T("hero.skill"), Effect(hero, state.Level, state.Stars, null, next), 0.3f, 110);

            var shards = PlateRow(110);
            Hud.HudIcons.Image(shards.transform, "item_card_" + hero.Rarity.id, 0.015f, 0.1f, 0.1f, 0.9f);
            UiFrames.Progress(shards.transform, state.Shards, needed, state.Shards >= needed ? "green" : "gold").Place(0.12f, 0.3f, 0.97f, 0.7f);

            var heroId = hero.Id;
            BottomPair(Loc.T("hero.find.cards"), UiKit.Primary, () => Open(Page.Pod), true,
                Loc.T("hero.promote.button"), UiKit.Gold, () => Send(new PromoteHeroCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), HeroId = heroId
                }), state.Shards >= needed);
        }

        private void HeroSkillTab(HeroDefinition hero, int level, int stars)
        {
            var rules = _catalog.HeroRules;
            StatRow(SkillIcon(hero), Loc.T("hero.skill"), Effect(hero, level, stars), 0.3f, 110);
            StatRow("event_war", Loc.T("hero.stat.bonus"),
                Loc.F("hero.skill.passive", CombatRules.HeroBonus(new[] { HeroRules.Power(hero, level, stars) }).ToString("0.#")), 0.34f, 110);

            // What each star brings; reached stars are lit (Last Asylum's star list).
            SectionRow(Loc.T("hero.skill.unlocks"));
            for (var s = 1; s <= rules.maxStars; s++)
                StatRow("g_star", new string('★', s), $"%{Math.Abs(HeroRules.EffectValue(hero, level, s))}", 0.5f, 80, s <= stars);
            NoteRow(Loc.T("hero." + hero.Id + ".bio"), 190);
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
