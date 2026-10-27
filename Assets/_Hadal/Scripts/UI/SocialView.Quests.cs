using System;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>Player-facing text for tasks, rewards and bag items (docs/27).</summary>
    public static class QuestText
    {
        public static string Task(QuestData quest)
        {
            if (quest == null)
                return string.Empty;
            return quest.type switch
            {
                "level" => Loc.F("quest.level", Loc.Building(quest.target), quest.count),
                "count" => Loc.F("quest.count", Loc.Building(quest.target), quest.count),
                "counter" => Loc.F("quest.counter." + quest.target, quest.count),
                "heroLevel" => Loc.F("quest.heroLevel", quest.count),
                "heroes" => Loc.F("quest.heroes", quest.count),
                "alliance" => Loc.T("quest.alliance"),
                _ => quest.id
            };
        }

        /// <summary>Bag item name; resource capsules read "Titanium Capsule · 1,500".</summary>
        public static string Item(string id) =>
            Capsules.TryParse(id, out var resource, out var amount)
                ? Loc.F("item.capsule", Loc.Resource(resource.ToString()), amount.ToString("N0"))
                : Loc.T("item." + id);

        public static string ItemDescription(string id) =>
            Capsules.TryParse(id, out _, out _) ? Loc.T("item.capsule.desc") : Loc.T("item." + id + ".desc");

        /// <summary>Icon of a bag item: the resource for capsules, a card stack or a gift otherwise.</summary>
        public static string ItemIcon(string id, string kind) =>
            Capsules.TryParse(id, out var resource, out _) ? Hud.HudIcons.Resource(resource) : kind == "heroCard" ? "nav_heroes" : "event_gift";

        public static string Hero(string id) => Loc.T("hero." + id);

        /// <summary>"1500 Titanyum", "Bronz Sandık ×2", "Lyra Moss kartı ×10"; random cards name the rarity.</summary>
        public static string Reward(string type, string id, long amount, string rarity = "") => type switch
        {
            "resource" => $"{amount:N0} {Loc.Resource(id)}",
            "item" => amount > 1 ? $"{Item(id)} ×{amount}" : Item(id),
            "shards" when string.IsNullOrEmpty(id) => Loc.F("reward.cards.random", Loc.T("rarity." + rarity), amount),
            "shards" => Loc.F("reward.cards", Hero(id), amount),
            "hero" => Loc.F("reward.hero", Hero(id)),
            _ => id
        };

        public static string Rewards(RewardData[] rewards) =>
            string.Join("  ·  ", (rewards ?? Array.Empty<RewardData>()).Select(r => Reward(r.type, r.id, r.amount, r.rarity)));
    }

    /// <summary>Quests (chapter and daily), the bag and reward results (docs/27).</summary>
    public sealed partial class SocialView
    {
        private int _questTab;
        private int _bagTab;
        private RewardReportDto _reward;

        public void OpenQuests() => Open(Page.Quests);

        public void OpenBag() => Open(Page.Bag);

        /// <summary>The HUD's task strip claims a finished chapter task directly.</summary>
        public void ClaimQuest(string questId) => _commands.Dispatch(new ClaimQuestCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), QuestId = questId
        });

        private string QuestKey() => $"{View.QuestVersion}|{_questTab}|{_bagTab}";

        private void RenderQuests()
        {
            var state = View.Quests;
            _title.text = Loc.T("quest.title");
            _subtitle.text = state == null ? string.Empty : Loc.F("quest.chapter", state.ChapterNumber, state.ChapterTotal);

            UiKit.Choice("Tabs", _list, new[] { Loc.T("quest.tab.chapter"), Loc.T("quest.tab.daily") },
                () => _questTab, i => { _questTab = i; Render(force: true); }).Row(90);

            if (state == null)
            {
                Note(Loc.T("quest.none"));
                return;
            }

            if (_questTab == 0)
                RenderChapter(state);
            else
                RenderDaily(state);
        }

        private void RenderChapter(QuestStateDto state)
        {
            var current = state.Chapter;
            if (current == null || !_catalog.TryGetQuest(current.Id, out var quest, out _))
            {
                Note(Loc.T("quest.chapter.done"));
                return;
            }

            var done = current.Progress >= current.Target;
            var card = Card(330);
            HudIconImage(card.transform, "quest", 0.02f, 0.55f, 0.18f, 0.95f);
            CardText(card.transform, QuestText.Task(quest), 36, UiKit.Text, 0.21f, 0.6f, 0.97f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
            ProgressBar(card.transform, current.Progress, current.Target, 0.21f, 0.47f, 0.97f, 0.57f);
            CardText(card.transform, $"{Loc.T("quest.reward")}: {QuestText.Rewards(quest.rewards)}", 26, UiKit.Amber, 0.04f, 0.05f, 0.68f, 0.42f, TextAnchor.MiddleLeft);
            var id = quest.id;
            CardButton(card.transform, done ? Loc.T("quest.claim") : Loc.T("quest.notyet"), done, () => _commands.Dispatch(new ClaimQuestCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), QuestId = id
            }), UiKit.Amber, 0.71f, 0.08f, 0.97f, 0.42f);

            // The next tasks, so the player sees where the story goes.
            var chapter = _catalog.Quests.chapter ?? Array.Empty<QuestData>();
            Header(Loc.T("quest.next"));
            for (var i = state.ChapterNumber; i < Math.Min(chapter.Length, state.ChapterNumber + 3); i++)
            {
                var next = Card(120);
                CardText(next.transform, $"{i + 1}. {QuestText.Task(chapter[i])}", 28, UiKit.Muted, 0.04f, 0.5f, 0.97f, 0.95f, TextAnchor.UpperLeft);
                CardText(next.transform, QuestText.Rewards(chapter[i].rewards), 24, UiKit.Amber, 0.04f, 0.05f, 0.97f, 0.5f);
            }
        }

        private void RenderDaily(QuestStateDto state)
        {
            // Activity chests along a points bar.
            var chests = _catalog.Quests.dailyChests ?? Array.Empty<DailyChestData>();
            var max = chests.Length > 0 ? chests.Max(c => c.points) : 100;
            var bar = Card(260);
            CardText(bar.transform, Loc.F("quest.points", state.DailyPoints, max), 32, UiKit.Text, 0.04f, 0.72f, 0.7f, 0.96f, TextAnchor.UpperLeft, FontStyle.Bold);
            CardText(bar.transform, Loc.F("quest.reset", Loc.Duration(View.DailyResetAt - Time.realtimeSinceStartup)), 22, UiKit.Muted, 0.6f, 0.72f, 0.97f, 0.96f, TextAnchor.UpperRight);
            ProgressBar(bar.transform, state.DailyPoints, max, 0.04f, 0.58f, 0.96f, 0.66f);
            for (var i = 0; i < chests.Length; i++)
            {
                var chest = chests[i];
                var claimed = state.ClaimedChests.Contains(chest.points);
                var ready = !claimed && state.DailyPoints >= chest.points;
                var x0 = 0.04f + i * 0.92f / chests.Length;
                var x1 = x0 + 0.92f / chests.Length - 0.02f;
                var points = chest.points;
                var button = CardButton(bar.transform, claimed ? "✓" : chest.points.ToString(), ready, () => _commands.Dispatch(new ClaimDailyChestCommand
                {
                    CommandId = NewId(), ClientSequence = NextSequence(), Points = points
                }), ready ? UiKit.Amber : UiKit.Field, x0, 0.06f, x1, 0.5f);
                HudIconImage(button.transform, "event_gift", 0.05f, 0.35f, 0.95f, 1.0f).color = claimed ? new Color(1, 1, 1, 0.35f) : Color.white;
            }

            foreach (var progress in state.Daily)
            {
                if (!_catalog.TryGetQuest(progress.Id, out var quest, out _))
                    continue;
                var done = progress.Progress >= progress.Target;
                var card = Card(170);
                CardText(card.transform, QuestText.Task(quest), 30, progress.Claimed ? UiKit.Muted : UiKit.Text, 0.04f, 0.58f, 0.68f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                ProgressBar(card.transform, progress.Progress, progress.Target, 0.04f, 0.4f, 0.68f, 0.52f);
                var reward = Loc.F("quest.points.give", quest.points) + (quest.rewards?.Length > 0 ? "  ·  " + QuestText.Rewards(quest.rewards) : string.Empty);
                CardText(card.transform, reward, 22, UiKit.Amber, 0.04f, 0.04f, 0.68f, 0.36f, TextAnchor.MiddleLeft);
                var id = quest.id;
                CardButton(card.transform, progress.Claimed ? "✓" : done ? Loc.T("quest.claim") : Loc.T("quest.notyet"), done && !progress.Claimed,
                    () => _commands.Dispatch(new ClaimQuestCommand { CommandId = NewId(), ClientSequence = NextSequence(), QuestId = id }), UiKit.Amber);
            }
        }

        private void RenderBag()
        {
            _title.text = Loc.T("bag.title");
            _subtitle.text = string.Empty;
            UiKit.Choice("Tabs", _list, new[] { Loc.T("bag.tab.items"), Loc.T("bag.tab.cards") },
                () => _bagTab, i => { _bagTab = i; Render(force: true); }).Row(90);

            if (_bagTab == 0)
            {
                var items = View.Items.Where(p => p.Value > 0).OrderBy(p => p.Key).ToList();
                if (items.Count == 0)
                {
                    Note(Loc.T("bag.empty"));
                    return;
                }

                foreach (var (id, count) in items.Select(p => (p.Key, p.Value)))
                {
                    _catalog.TryGetItem(id, out var item);
                    var card = Card(190);
                    HudIconImage(card.transform, QuestText.ItemIcon(id, item?.kind), 0.02f, 0.1f, 0.2f, 0.9f);
                    CardText(card.transform, $"<color={RarityColor(item?.rarity ?? "common")}>{QuestText.Item(id)}</color>  ×{count}", 32, UiKit.Text,
                        0.23f, 0.55f, 0.7f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
                    CardText(card.transform, QuestText.ItemDescription(id), 22, UiKit.Muted, 0.23f, 0.05f, 0.7f, 0.55f);
                    var itemId = id;
                    CardButton(card.transform, Loc.T("bag.open"), true, () => OpenItems(itemId, 1), null, 0.72f, 0.55f, 0.97f, 0.92f);
                    if (count > 1)
                        CardButton(card.transform, Loc.F("bag.open.all", Math.Min(count, 50)), true, () => OpenItems(itemId, Math.Min(count, 50)), UiKit.Amber, 0.72f, 0.08f, 0.97f, 0.45f);
                }

                return;
            }

            // Hero cards of heroes not recruited yet.
            var cards = View.HeroCards.Where(p => p.Value > 0).ToList();
            if (cards.Count == 0)
            {
                Note(Loc.T("bag.cards.empty"));
                return;
            }

            foreach (var (heroId, count) in cards.Select(p => (p.Key, p.Value)))
            {
                if (!_catalog.TryGetHero(heroId, out var hero))
                    continue;
                RecruitCard(hero, count);
            }
        }

        /// <summary>A not-yet-owned hero with its card count and, when enough, the recruit button.</summary>
        private void RecruitCard(HeroDefinition hero, int count)
        {
            var need = QuestRules.RecruitShards(hero);
            var card = Card(190);
            Portrait(card.transform, hero, count >= need, 0.01f, 0.03f, 0.2f, 0.97f);
            CardText(card.transform, $"<color={RarityColor(hero.Rarity.id)}>{Loc.T("hero." + hero.Id)}</color>", 32, UiKit.Text, 0.23f, 0.6f, 0.7f, 0.95f, TextAnchor.UpperLeft, FontStyle.Bold);
            ProgressBar(card.transform, count, need, 0.23f, 0.38f, 0.7f, 0.5f);
            CardText(card.transform, Loc.F("hero.cards", count, need), 24, count >= need ? UiKit.Accent : UiKit.Muted, 0.23f, 0.05f, 0.7f, 0.36f);
            var id = hero.Id;
            CardButton(card.transform, Loc.T("hero.recruit"), count >= need, () => _commands.Dispatch(new RecruitHeroCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), HeroId = id
            }), UiKit.Amber);
        }

        private void OpenItems(string id, int count) => _commands.Dispatch(new OpenItemCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), ItemId = id, Count = count
        });

        private void RenderReward()
        {
            _title.text = Loc.T("reward.title");
            _subtitle.text = string.Empty;
            var rewards = _reward?.Rewards ?? Array.Empty<RewardEntryDto>();

            // Same kind of reward collapses into one line (ten resource draws read as one total).
            foreach (var group in rewards.GroupBy(r => (r.Type, r.Id)))
            {
                var total = group.Sum(r => r.Amount);
                var card = Card(140);
                var (type, id) = group.Key;
                if ((type == "shards" || type == "hero") && _catalog.TryGetHero(id, out var hero))
                    Portrait(card.transform, hero, true, 0.01f, 0.04f, 0.17f, 0.96f);
                else
                    HudIconImage(card.transform, type == "resource" && GameCatalog.TryParseResource(id, out var resource) ? Hud.HudIcons.Resource(resource)
                        : type == "item" ? QuestText.ItemIcon(id, string.Empty) : "nav_heroes", 0.02f, 0.1f, 0.16f, 0.9f);
                CardText(card.transform, QuestText.Reward(type, id, total), 32, type == "hero" ? UiKit.Amber : UiKit.Text, 0.2f, 0.1f, 0.97f, 0.9f,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
            }

            var done = Card(110);
            CardButton(done.transform, Loc.T("hero.pod.done"), true, Close, null, 0.25f, 0.1f, 0.75f, 0.9f);
        }

        private static Image HudIconImage(Transform parent, string id, float x0, float y0, float x1, float y1) =>
            Hud.HudIcons.Image(parent, id, x0, y0, x1, y1);

        private static void ProgressBar(Transform parent, long value, long max, float x0, float y0, float x1, float y1)
        {
            var back = UiKit.Box("Bar", parent, new Color(0, 0, 0, 0.45f));
            back.rectTransform.Place(x0, y0, x1, y1);
            back.raycastTarget = false;
            var fill = UiKit.Box("Fill", back.transform, UiKit.Accent);
            fill.rectTransform.Place(0, 0, Mathf.Clamp01(max > 0 ? (float)value / max : 0f), 1);
            fill.raycastTarget = false;
            var label = UiKit.Label("Value", back.transform, $"{value}/{max}", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Place(0, 0, 1, 1);
        }
    }
}
