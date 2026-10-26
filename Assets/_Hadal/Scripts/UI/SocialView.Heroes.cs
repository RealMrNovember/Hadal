using System;
using System.Linq;
using System.Text;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.StateSync;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>Heroes page: command crew, the Cryo-Pod, the roster and one hero's training screen (docs/25).</summary>
    public sealed partial class SocialView
    {
        private string _heroId = string.Empty;
        private GachaResultDto _podResult;

        private string HeroesKey()
        {
            var key = new StringBuilder();
            foreach (var h in View.Heroes)
                key.Append(h.HeroId).Append(h.Level).Append('.').Append(h.Stars).Append('.').Append(h.Shards).Append('.').Append(h.Slot).Append(';');
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

        /// <summary>The hero's painted portrait in a rarity-coloured frame (darkened until found); initials if it is missing.</summary>
        private static void Portrait(Transform parent, HeroDefinition hero, bool owned, float x0, float y0, float x1, float y1)
        {
            var painted = Hadal.Core.Catalog.CharacterLibrary.Instance?.Portrait(hero.Portrait);
            if (painted != null)
            {
                ColorUtility.TryParseHtmlString(RarityColor(hero.Rarity.id), out var rim);
                var frame = UiKit.Box("PortraitFrame", parent, rim);
                frame.rectTransform.Place(x0, y0, x1, y1, 6);
                frame.raycastTarget = false;
                var picture = UiKit.Rect("Portrait", frame.transform).gameObject.AddComponent<RawImage>();
                picture.texture = painted;
                picture.color = owned ? Color.white : new Color(0.22f, 0.25f, 0.28f, 1f);
                picture.raycastTarget = false;
                ((RectTransform)picture.transform).Place(0, 0, 1, 1, 4);
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

        private void RenderHeroes()
        {
            var rules = _catalog.HeroRules;
            _title.text = Loc.T("social.heroes");
            var totalPower = View.Heroes.Sum(h => _catalog.TryGetHero(h.HeroId, out var d) ? HeroRules.Power(d, h.Level, h.Stars) : 0);
            _subtitle.text = Loc.F("hero.power.total", totalPower);

            if (!View.HasHeroes || _catalog.Heroes.Count == 0)
            {
                Note(Loc.T("hero.none"));
                return;
            }

            // Command crew.
            Header(Loc.T("hero.crew"));
            var open = HeroRules.OpenSlots(rules, ReactorLevel());
            for (var slot = 0; slot < HeroRules.SlotCount(rules); slot++)
            {
                var card = Card(150);
                var holder = View.Heroes.FirstOrDefault(h => h.Slot == slot);
                if (slot >= open)
                {
                    CardText(card.transform, Loc.F("hero.slot", slot + 1), 34, UiKit.Muted, 0.04f, 0.5f, 0.7f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                    CardText(card.transform, Loc.F("hero.slot.locked", rules.crewSlotReactorLevels[slot]), 28, UiKit.Danger, 0.04f, 0.05f, 0.95f, 0.5f);
                    continue;
                }

                if (holder.HeroId != null && _catalog.TryGetHero(holder.HeroId, out var hero))
                {
                    Portrait(card.transform, hero, true, 0.01f, 0.02f, 0.17f, 0.98f);
                    CardText(card.transform, $"{Loc.F("hero.slot", slot + 1)} · <color={RarityColor(hero.Rarity.id)}>{Loc.T("hero." + hero.Id)}</color>", 32, UiKit.Text,
                        0.2f, 0.5f, 0.7f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                    CardText(card.transform, Effect(hero, holder.Level, holder.Stars), 28, UiKit.Accent, 0.2f, 0.05f, 0.7f, 0.5f);
                    var id = hero.Id;
                    CardButton(card.transform, Loc.T("hero.open"), true, () => ShowHero(id));
                }
                else
                {
                    CardText(card.transform, Loc.F("hero.slot", slot + 1), 34, UiKit.Text, 0.04f, 0.5f, 0.7f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                    CardText(card.transform, Loc.T("hero.slot.empty"), 28, UiKit.Muted, 0.04f, 0.05f, 0.7f, 0.5f);
                }
            }

            // Cryo-Pod.
            Header(Loc.T("hero.pod"));
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

            // Roster: owned by power, then the ones still to find.
            Header(Loc.T("hero.roster"));
            var ordered = _catalog.Heroes
                .Select(d => (Definition: d, Owned: View.TryGetHero(d.Id, out var h), Hero: h))
                .OrderByDescending(x => x.Owned)
                .ThenByDescending(x => x.Owned ? HeroRules.Power(x.Definition, x.Hero.Level, x.Hero.Stars) : 0)
                .ToList();
            foreach (var (definition, owned, hero) in ordered)
            {
                var card = Card(190);
                Portrait(card.transform, definition, owned, 0.01f, 0.02f, 0.2f, 0.98f);
                CardText(card.transform, $"<color={RarityColor(definition.Rarity.id)}>{Loc.T("hero." + definition.Id)}</color>", 36, UiKit.Text,
                    0.23f, 0.66f, 0.75f, 0.96f, TextAnchor.UpperLeft, FontStyle.Bold);
                CardText(card.transform, $"{Loc.T("rarity." + definition.Rarity.id)} · {Loc.T("faction." + definition.Faction)}", 24, UiKit.Muted, 0.23f, 0.46f, 0.98f, 0.66f);
                if (owned)
                {
                    CardText(card.transform, $"{Loc.F("base.level", hero.Level)}  {Stars(hero.Stars, rules.maxStars)}   {Loc.F("hero.power", HeroRules.Power(definition, hero.Level, hero.Stars))}",
                        26, UiKit.Text, 0.23f, 0.25f, 0.98f, 0.46f);
                    CardText(card.transform, Effect(definition, hero.Level, hero.Stars), 26, UiKit.Accent, 0.23f, 0.03f, 0.98f, 0.25f);
                }
                else
                {
                    // Hero cards collected towards recruiting this hero.
                    var cards = View.HeroCards.TryGetValue(definition.Id, out var c) ? c : 0;
                    var need = QuestRules.RecruitShards(definition);
                    CardText(card.transform, Loc.F("hero.cards", cards, need), 24, cards >= need ? UiKit.Accent : UiKit.Muted, 0.23f, 0.25f, 0.7f, 0.46f);
                    CardText(card.transform, Loc.T("hero.locked.how"), 20, UiKit.Muted, 0.23f, 0.03f, 0.7f, 0.25f);
                    if (cards >= need)
                    {
                        var recruitId = definition.Id;
                        CardButton(card.transform, Loc.T("hero.recruit"), true, () => _commands.Dispatch(new RecruitHeroCommand
                        {
                            CommandId = NewId(), ClientSequence = NextSequence(), HeroId = recruitId
                        }), UiKit.Amber, 0.72f, 0.1f, 0.97f, 0.5f);
                    }
                }

                var id = definition.Id;
                var tap = card.gameObject.AddComponent<Button>();
                tap.onClick.AddListener(() => ShowHero(id));
            }
        }

        private void ShowHero(string heroId)
        {
            _heroId = heroId;
            _page = Page.HeroDetail;
            Render(force: true);
        }

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
            _title.text = Loc.T("hero." + hero.Id);
            _subtitle.text = $"<color={RarityColor(hero.Rarity.id)}>{Loc.T("rarity." + hero.Rarity.id)}</color> · {Loc.T("class." + hero.HeroClass)} · {Loc.T("faction." + hero.Faction)}";

            var back = Card(90);
            CardButton(back.transform, "‹ " + Loc.T("hero.back"), true, () => Open(Page.Heroes), UiKit.Field, 0.01f, 0.08f, 0.35f, 0.92f);

            // Animated 3D model (drag to turn, tap to wave) beside the painted portrait and the story.
            var showcase = Card(640);
            var stageTexture = _stage.Show(hero.Model);
            if (stageTexture != null)
            {
                var stage = UiKit.Rect("Stage", showcase.transform).gameObject.AddComponent<RawImage>();
                stage.texture = stageTexture;
                stage.color = owned ? Color.white : new Color(0.25f, 0.28f, 0.3f, 1f);
                ((RectTransform)stage.transform).Place(0.01f, 0.02f, 0.5f, 0.98f, 4);
                stage.gameObject.AddComponent<HeroStageInput>().Stage = _stage;
                CardText(showcase.transform, Loc.T("hero.stage.hint"), 20, UiKit.Muted, 0.02f, 0.02f, 0.5f, 0.09f, TextAnchor.LowerCenter);
                Portrait(showcase.transform, hero, owned, 0.53f, 0.5f, 0.98f, 0.98f);
                CardText(showcase.transform, Loc.T("hero." + hero.Id + ".bio"), 24, UiKit.Text, 0.53f, 0.03f, 0.98f, 0.48f);
            }
            else
            {
                Portrait(showcase.transform, hero, owned, 0.01f, 0.3f, 0.45f, 0.98f);
                CardText(showcase.transform, Loc.T("hero." + hero.Id + ".bio"), 27, UiKit.Text, 0.48f, 0.05f, 0.97f, 0.95f);
            }

            if (!owned)
            {
                RecruitCard(hero, View.HeroCards.TryGetValue(hero.Id, out var cards) ? cards : 0);
                Note(Loc.T("hero.locked.how"));
                return;
            }

            var cap = HeroRules.LevelCap(rules, state.Stars);
            var stats = Card(200);
            CardText(stats.transform, $"{Loc.F("base.level", state.Level)} / {cap}    {Stars(state.Stars, rules.maxStars)}", 34, UiKit.Text,
                0.04f, 0.6f, 0.96f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
            CardText(stats.transform, Loc.F("hero.power", HeroRules.Power(hero, state.Level, state.Stars)), 28, UiKit.Amber, 0.04f, 0.38f, 0.96f, 0.6f);
            CardText(stats.transform, Loc.T("hero.skill") + ": " + Effect(hero, state.Level, state.Stars), 28, UiKit.Accent, 0.04f, 0.05f, 0.96f, 0.38f);

            // Training.
            var wallet = Wallet();
            var levelVerdict = HeroRules.ValidateLevelUp(_catalog, hero, new OwnedHero(state.HeroId, state.Level, state.Stars, state.Shards, state.Slot), wallet);
            var train = Card(240);
            CardText(train.transform, Loc.T("hero.train"), 38, UiKit.Text, 0.04f, 0.62f, 0.68f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
            if (state.Level < cap)
            {
                CardText(train.transform, Effect(hero, state.Level, state.Stars, state.Level + 1), 26, UiKit.Muted, 0.04f, 0.36f, 0.68f, 0.62f);
                CardText(train.transform, $"{Loc.T("base.cost")}: {Cost(HeroRules.LevelCost(_catalog, hero, state.Level), wallet)}", 26, UiKit.Amber, 0.04f, 0.04f, 0.7f, 0.36f, TextAnchor.LowerLeft);
            }
            else
            {
                CardText(train.transform, Loc.Reason(nameof(HeroRejectReason.LevelCapReached)), 26, UiKit.Danger, 0.04f, 0.1f, 0.68f, 0.6f);
            }

            var heroId = hero.Id;
            CardButton(train.transform, Loc.T("hero.train"), levelVerdict == HeroRejectReason.None, () => Send(new LevelUpHeroCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), HeroId = heroId
            }));

            // Promotion.
            var needed = HeroRules.ShardsForNextStar(rules, state.Stars);
            var promote = Card(200);
            CardText(promote.transform, Loc.T("hero.promote"), 38, UiKit.Text, 0.04f, 0.55f, 0.68f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
            CardText(promote.transform, needed > 0
                    ? Loc.F("hero.shards", state.Shards, needed) + "\n" + Loc.F("hero.promote.body", HeroRules.LevelCap(rules, state.Stars + 1))
                    : Loc.Reason(nameof(HeroRejectReason.MaxStars)),
                26, needed > 0 && state.Shards >= needed ? UiKit.Accent : UiKit.Muted, 0.04f, 0.05f, 0.68f, 0.55f);
            CardButton(promote.transform, Loc.T("hero.promote"), needed > 0 && state.Shards >= needed, () => Send(new PromoteHeroCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), HeroId = heroId
            }), UiKit.Amber);

            // Crew.
            var crew = Card(200);
            var openSlots = Math.Min(HeroRules.SlotCount(rules), HeroRules.OpenSlots(rules, ReactorLevel()));
            CardText(crew.transform, state.Slot >= 0 ? Loc.F("hero.on.crew", state.Slot + 1) : Loc.T("hero.off.crew"), 32, UiKit.Text,
                0.04f, 0.62f, 0.96f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
            if (state.Slot >= 0)
            {
                CardButton(crew.transform, Loc.T("hero.unassign"), true, () => Assign(heroId, -1), UiKit.Danger, 0.03f, 0.08f, 0.5f, 0.55f);
            }
            else
            {
                for (var slot = 0; slot < openSlots; slot++)
                {
                    var s = slot;
                    var width = 0.94f / Math.Max(1, openSlots);
                    CardButton(crew.transform, Loc.F("hero.slot", slot + 1), true, () => Assign(heroId, s), null,
                        0.03f + width * slot, 0.08f, 0.03f + width * (slot + 1) - 0.02f, 0.55f);
                }
            }
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
            CardButton(done.transform, Loc.T("hero.pod.done"), true, () => Open(Page.Heroes), null, 0.25f, 0.1f, 0.75f, 0.9f);
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
