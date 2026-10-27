using System;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
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
                : Speedups.TryParse(id, out var kind, out var value, out var percent)
                    ? Loc.F("item.speedup", Loc.T("speedup.kind." + kind.ToString().ToLowerInvariant()), SpeedupAmount(value, percent))
                    : Loc.T("item." + id);

        /// <summary>"%50" or "1 sa" — what one speed-up takes off.</summary>
        public static string SpeedupAmount(int value, bool percent) => percent ? $"%{value}" : Loc.Duration(value * 60f);

        public static string ItemDescription(string id) =>
            Capsules.TryParse(id, out _, out _) ? Loc.T("item.capsule.desc")
            : Speedups.TryParse(id, out var kind, out _, out _) ? Loc.T(kind == SpeedupKind.March ? "item.speedup.march.desc" : "item.speedup.desc")
            : Loc.T("item." + id + ".desc");

        /// <summary>Icon of a bag item: the resource for capsules, a card stack or a gift otherwise.</summary>
        public static string ItemIcon(string id, string kind) =>
            Capsules.TryParse(id, out var resource, out _) ? Hud.HudIcons.Resource(resource)
            : Speedups.TryParse(id, out var speed, out _, out _) ? speed == SpeedupKind.March ? "nav_world" : speed == SpeedupKind.Build ? "queue_build"
                : speed == SpeedupKind.Research ? "left_research" : speed == SpeedupKind.Heal ? "left_help" : "power"
            : kind == "heroCard" ? "nav_heroes" : "event_gift";

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

        private string _bagSelected = string.Empty;
        private int _bagCount = 1;
        private GameObject _bagStats;

        /// <summary>
        /// The bag in Last Asylum's layout: tabs (Special · Resources · Speed-ups · Heroes), a grid of square tiles
        /// framed by rarity or package size with the amount on top and the count in the corner; tapping one opens a
        /// panel under its row with what it gives, a count slider and Use. The chart button shows bag + owned totals.
        /// </summary>
        private void RenderBag()
        {
            _title.text = Loc.T("bag.title");
            _subtitle.text = string.Empty;

            var tabs = new[] { Loc.T("bag.tab.special"), Loc.T("bag.tab.resources"), Loc.T("bag.tab.speedups"), Loc.T("bag.tab.heroes") };
            var bar = UiKit.Rect("TabsRow", _list).Row(96);
            UiKit.Choice("Tabs", bar, tabs, () => _bagTab, i => { _bagTab = i; _bagSelected = string.Empty; Render(force: true); }).Place(0, 0, 0.86f, 1);
            var stats = UiKit.Button("Stats", bar, "Σ", OpenBagStats, UiKit.Field, 40);
            stats.GetComponentInChildren<Text>().color = UiKit.Accent;
            ((RectTransform)stats.transform).Place(0.88f, 0.08f, 1f, 0.92f);

            var entries = View.Items.Where(p => p.Value > 0)
                .Select(p => (Id: p.Key, Count: p.Value, Tab: BagTab(p.Key)))
                .Where(e => e.Tab == _bagTab)
                .OrderBy(e => Capsules.TryParse(e.Id, out var r, out _) ? (int)r : 99)
                .ThenByDescending(e => Capsules.TryParse(e.Id, out _, out var a) ? a : 0)
                .ThenBy(e => e.Id, StringComparer.Ordinal)
                .ToList();

            if (entries.Count == 0 && !(_bagTab == 3 && View.HeroCards.Any(p => p.Value > 0)))
            {
                Note(Loc.T(_bagTab == 2 ? "bag.speedups.empty" : "bag.empty"));
                return;
            }

            const int columns = 4;
            for (var start = 0; start < entries.Count; start += columns)
            {
                var row = UiKit.Rect("Row", _list).Row(210);
                for (var c = 0; c < columns && start + c < entries.Count; c++)
                    BagTile(row, entries[start + c].Id, entries[start + c].Count, c * 0.25f + 0.01f, c * 0.25f + 0.24f);

                // The use panel opens under the row of the chosen tile.
                var chosen = entries.Skip(start).Take(columns).FirstOrDefault(e => e.Id == _bagSelected);
                if (chosen.Id != null)
                    BagUsePanel(chosen.Id, chosen.Count);
            }

            // Hero cards of heroes not recruited yet stay on the Heroes tab.
            if (_bagTab == 3)
            {
                foreach (var (heroId, count) in View.HeroCards.Where(p => p.Value > 0).Select(p => (p.Key, p.Value)))
                {
                    if (_catalog.TryGetHero(heroId, out var hero))
                        RecruitCard(hero, count);
                }
            }
        }

        /// <summary>0 special (chests), 1 resources (capsules), 2 speed-ups, 3 heroes (cards).</summary>
        private int BagTab(string id)
        {
            if (Capsules.TryParse(id, out _, out _))
                return 1;
            if (id.StartsWith("speed_", StringComparison.Ordinal))
                return 2;
            return _catalog.TryGetItem(id, out var item) && item.kind == "heroCard" ? 3 : 0;
        }

        /// <summary>Frame tone: rarity for items, package size for resources (common, rare, epic, legendary).</summary>
        private string BagTone(string id)
        {
            if (Capsules.TryParse(id, out _, out var amount))
                return amount >= 1_000_000 ? "legendary" : amount >= 100_000 ? "epic" : amount >= 10_000 ? "rare" : "common";
            return _catalog.TryGetItem(id, out var item) && !string.IsNullOrEmpty(item.rarity) ? item.rarity : "common";
        }

        /// <summary>Frame colour for the tone (green, blue, purple, orange), when the skin is missing.</summary>
        private Color BagFrame(string id)
        {
            return BagTone(id) switch
            {
                "legendary" => new Color(0.91f, 0.55f, 0.16f, 1f),
                "epic" => new Color(0.6f, 0.33f, 0.86f, 1f),
                "rare" => new Color(0.2f, 0.5f, 0.86f, 1f),
                _ => new Color(0.27f, 0.66f, 0.33f, 1f)
            };
        }

        private void BagTile(RectTransform row, string id, int count, float x0, float x1)
        {
            var chosen = id == _bagSelected;
            var frame = UiKit.Box("Tile", row, BagFrame(id));
            frame.rectTransform.Place(x0, 0.04f, x1, 0.96f);
            var slot = Skin.Get("slot_" + BagTone(id));
            if (slot != null)
            {
                frame.sprite = slot;
                frame.color = Color.white;
            }
            if (chosen)
            {
                var outline = frame.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.82f, 0.3f, 1f);
                outline.effectDistance = new Vector2(5, -5);
            }

            // A soft inner shade and the icon in the middle, Last Asylum's card look.
            if (slot == null)
            {
                var inner = UiKit.Box("Inner", frame.transform, new Color(1f, 1f, 1f, 0.12f));
                inner.rectTransform.Place(0.06f, 0.06f, 0.94f, 0.94f);
                inner.raycastTarget = false;
            }

            _catalog.TryGetItem(id, out var item);
            HudIconImage(frame.transform, QuestText.ItemIcon(id, item?.kind), 0.18f, 0.16f, 0.82f, 0.8f);
            var top = Capsules.TryParse(id, out _, out var amount) ? CostChips.Compact(amount)
                : Speedups.TryParse(id, out _, out var minutes, out var percent) ? QuestText.SpeedupAmount(minutes, percent) : null;
            if (top != null)
                Outlined(UiKit.Label("Amount", frame.transform, top, 26, Color.white, TextAnchor.UpperCenter, FontStyle.Bold))
                    .rectTransform.Place(0, 0.76f, 1, 0.98f);
            Outlined(UiKit.Label("Count", frame.transform, count.ToString("N0"), 26, Color.white, TextAnchor.LowerRight, FontStyle.Bold))
                .rectTransform.Place(0.05f, 0.03f, 0.94f, 0.3f);

            var pick = id;
            frame.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                _bagSelected = _bagSelected == pick ? string.Empty : pick;
                _bagCount = 1;
                Render(force: true);
            });
            frame.gameObject.AddComponent<PressFeedback>();
        }

        private void BagUsePanel(string id, int count)
        {
            var panel = UiKit.Box("Use", _list, new Color(0.9f, 0.93f, 0.96f, 1f)).Row(300);
            var dark = new Color(0.1f, 0.16f, 0.22f, 1f);
            UiKit.Label("Name", panel.transform, QuestText.Item(id), 32, dark, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.04f, 0.78f, 0.96f, 0.96f);
            var desc = Capsules.TryParse(id, out var resource, out var amount)
                ? Loc.F("bag.capsule.gives", CostChips.Compact(amount), Loc.Resource(resource.ToString()))
                : QuestText.ItemDescription(id);
            UiKit.Label("Desc", panel.transform, desc, 24, new Color(0.3f, 0.4f, 0.47f, 1f), TextAnchor.MiddleCenter).rectTransform.Place(0.04f, 0.58f, 0.96f, 0.78f);

            var max = Math.Max(1, Math.Min(count, 50));
            _bagCount = Mathf.Clamp(_bagCount, 1, max);
            var minus = UiKit.Button("Minus", panel.transform, "-", () => { _bagCount = Math.Max(1, _bagCount - 1); Render(force: true); }, new Color(0.25f, 0.5f, 0.85f, 1f), 40);
            ((RectTransform)minus.transform).Place(0.04f, 0.33f, 0.14f, 0.53f);
            var countText = UiKit.Label("CountText", panel.transform, _bagCount.ToString(), 30, dark, TextAnchor.MiddleCenter, FontStyle.Bold);
            countText.rectTransform.Place(0.8f, 0.33f, 0.96f, 0.53f);
            var slider = UiKit.Slider("Count", panel.transform, max <= 1 ? 1f : (_bagCount - 1f) / (max - 1f), v =>
            {
                _bagCount = Mathf.Clamp(1 + Mathf.RoundToInt(v * (max - 1)), 1, max);
                countText.text = _bagCount.ToString();
            });
            ((RectTransform)slider.transform).Place(0.16f, 0.33f, 0.66f, 0.53f);
            var plus = UiKit.Button("Plus", panel.transform, "+", () => { _bagCount = Math.Min(max, _bagCount + 1); Render(force: true); }, new Color(0.25f, 0.5f, 0.85f, 1f), 40);
            ((RectTransform)plus.transform).Place(0.67f, 0.33f, 0.77f, 0.53f);

            if (Speedups.TryParse(id, out _, out _, out _))
            {
                UiKit.Label("How", panel.transform, Loc.T("bag.speedup.how"), 24, dark, TextAnchor.MiddleCenter).rectTransform.Place(0.04f, 0.05f, 0.96f, 0.3f);
                return;
            }

            var use = UiKit.Button("UseButton", panel.transform, Loc.T("bag.use"), () =>
            {
                OpenItems(id, _bagCount);
                _bagSelected = string.Empty;
            }, new Color(0.25f, 0.5f, 0.85f, 1f), 34);
            use.GetComponentInChildren<Text>().color = Color.white;
            ((RectTransform)use.transform).Place(0.28f, 0.05f, 0.72f, 0.27f);
        }

        /// <summary>Last Asylum's resource overview: what the bag holds, what is owned, and both together.</summary>
        private void OpenBagStats()
        {
            if (_bagStats != null)
                Destroy(_bagStats);
            var dim = UiKit.Box("BagStats", _canvas.transform, new Color(0, 0, 0, 0.65f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => Destroy(_bagStats));
            _bagStats = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.04f, 0.28f, 0.96f, 0.72f);
            card.gameObject.AddComponent<Button>();
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>();
            UiKit.Label("Title", card.transform, Loc.T("bag.stats.title"), 34, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0.86f, 0.85f, 0.97f);

            string[] head = { Loc.T("bag.stats.type"), Loc.T("bag.stats.items"), Loc.T("bag.stats.owned"), Loc.T("bag.stats.total") };
            float[] cols = { 0.04f, 0.4f, 0.6f, 0.8f, 0.97f };
            for (var c = 0; c < head.Length; c++)
                UiKit.Label("Head", card.transform, head[c], 22, UiKit.Muted, c == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, FontStyle.Bold)
                    .rectTransform.Place(cols[c], 0.76f, cols[c + 1], 0.84f);

            var wallet = Wallet();
            var types = new[] { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Food, ResourceType.Energy, ResourceType.Oxygen };
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                long inBag = View.Items.Where(p => p.Value > 0).Sum(p => Capsules.TryParse(p.Key, out var r, out var a) && r == type ? a * p.Value : 0);
                wallet.TryGetValue(type, out var owned);
                var y1 = 0.74f - i * 0.14f;
                var row = UiKit.Box("Row", card.transform, i % 2 == 0 ? new Color(1, 1, 1, 0.05f) : new Color(0, 0, 0, 0));
                row.rectTransform.Place(0.02f, y1 - 0.13f, 0.98f, y1);
                row.raycastTarget = false;
                HudIconImage(row.transform, Hud.HudIcons.Resource(type), 0.01f, 0.1f, 0.09f, 0.9f);
                UiKit.Label("Name", row.transform, Loc.Resource(type.ToString()), 24, UiKit.Text, TextAnchor.MiddleLeft).rectTransform.Place(0.11f, 0, 0.4f, 1);
                var values = new[] { inBag, owned, inBag + owned };
                for (var c = 0; c < values.Length; c++)
                    UiKit.Label("V", row.transform, CostChips.Compact(values[c]), 24, c == 2 ? UiKit.Amber : UiKit.Text, TextAnchor.MiddleRight, FontStyle.Bold)
                        .rectTransform.Place(cols[c + 1] - 0.02f, 0, cols[c + 2] - 0.02f, 1);
            }
        }

        private static Text Outlined(Text text)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.85f);
            outline.effectDistance = new Vector2(2, -2);
            text.raycastTarget = false;
            return text;
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
            CardButton(done.transform, Loc.T("hero.pod.done"), true, () =>
            {
                var back = _afterReward;
                _afterReward = Page.None;
                if (back != Page.None)
                    Open(back);
                else
                    Close();
            }, null, 0.25f, 0.1f, 0.75f, 0.9f);
        }

        private static Image HudIconImage(Transform parent, string id, float x0, float y0, float x1, float y1) =>
            Hud.HudIcons.Image(parent, id, x0, y0, x1, y1);

        private static void ProgressBar(Transform parent, long value, long max, float x0, float y0, float x1, float y1)
        {
            var bar = Skin.Bar(parent, max > 0 ? (float)value / max : 0f, value >= max && max > 0 ? "gold" : "green");
            bar.Place(x0, y0, x1, y1);
            UiKit.Stroked(UiKit.Label("Value", bar, $"{value:N0}/{max:N0}", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)).rectTransform.Place(0, 0, 1, 1);
        }
    }
}
