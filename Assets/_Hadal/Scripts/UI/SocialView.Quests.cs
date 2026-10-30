using System;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.Services;
using TMPro;
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
                "rings" => Loc.F("quest.rings", quest.count),
                _ => quest.id
            };
        }

        /// <summary>Bag item name; resource capsules read "Titanium Capsule · 1,500".</summary>
        public static string Item(string id) =>
            VipRules.TryParseItem(id, out var vipPoints) ? Loc.F("item.vip", vipPoints.ToString("N0"))
            : GiftItems.TryParseHero(id, out var heroId, out var level, out var stars) ? Loc.F("item.herogift", Hero(heroId), level, stars)
            : GiftItems.TryParseTroops(id, out var type, out var tier, out var count) ? Loc.F("item.troops", count.ToString("N0"), tier, Loc.T("troop." + type))
            : Capsules.TryParse(id, out var resource, out var amount)
                ? Loc.F("item.capsule", Loc.Resource(resource.ToString()), amount.ToString("N0"))
                : Speedups.TryParse(id, out var kind, out var value, out var percent)
                    ? Loc.F("item.speedup", Loc.T("speedup.kind." + kind.ToString().ToLowerInvariant()), SpeedupAmount(value, percent))
                    : Loc.T("item." + id);

        /// <summary>"%50" or "1 sa" — what one speed-up takes off.</summary>
        public static string SpeedupAmount(int value, bool percent) => percent ? $"%{value}" : Loc.Duration(value * 60f);

        public static string ItemDescription(string id) =>
            VipRules.TryParseItem(id, out _) ? Loc.T("item.vip.desc")
            : GiftItems.TryParseHero(id, out var heroId, out var level, out var stars)
                ? Loc.F("item.herogift.desc", ClientCatalog.Catalog.TryGetHero(heroId, out var hero) ? HeroRules.Power(hero, level, stars).ToString("N0") : "?")
            : GiftItems.TryParseTroops(id, out _, out _, out _) ? Loc.T("item.troops.desc")
            : Capsules.TryParse(id, out _, out _) ? Loc.T("item.capsule.desc")
            : Speedups.TryParse(id, out var kind, out _, out _) ? Loc.T(kind == SpeedupKind.March ? "item.speedup.march.desc" : "item.speedup.desc")
            : Loc.T("item." + id + ".desc");

        /// <summary>
        /// Icon of a bag item: its own painted icon (art/gpu/items.json → Icons/item_*), the gift box for anything
        /// without one.
        /// </summary>
        public static string ItemIcon(string id, string kind)
        {
            var icon = OwnIcon(id, kind);
            return Hud.HudIcons.Get(icon) != null ? icon : "event_gift";
        }

        private static string OwnIcon(string id, string kind) =>
            TeleportRules.IsItem(id) || RenameRules.IsItem(id) ? "item_" + id
            : VipRules.TryParseItem(id, out var vip) ? vip <= 100 ? "item_vip_small" : vip <= 1000 ? "item_vip_mid" : "item_vip_large"
            : GiftItems.TryParseHero(id, out _, out _, out _) ? "item_herogift"
            : GiftItems.TryParseTroops(id, out var troop, out _, out _) ? "item_troops_" + troop
            : Capsules.TryParse(id, out var resource, out _) ? "item_cap_" + resource.ToString().ToLowerInvariant()
            : Speedups.TryParse(id, out var speed, out _, out _) ? "item_speed_" + speed.ToString().ToLowerInvariant()
            : kind is "chest" or "heroCard" || id.StartsWith("chest_", StringComparison.Ordinal) || id.StartsWith("card_", StringComparison.Ordinal)
              || id.StartsWith("shield_", StringComparison.Ordinal) ? "item_" + id // shields (planned): shield_8h, _24h, _48h, _7d
            : "event_gift";

        public static string Hero(string id) => Loc.T("hero." + id);

        /// <summary>"1500 Titanyum", "Bronz Sandık ×2", "Lyra Moss kartı ×10"; random cards name the rarity.</summary>
        public static string Reward(string type, string id, long amount, string rarity = "") => type switch
        {
            "resource" => $"{amount:N0} {Loc.Resource(id)}",
            "item" => amount > 1 ? $"{Item(id)} ×{amount}" : Item(id),
            "shards" when string.IsNullOrEmpty(id) => Loc.F("reward.cards.random", Loc.T("rarity." + rarity), amount),
            "shards" => Loc.F("reward.cards", Hero(id), amount),
            "hero" => Loc.F("reward.hero", Hero(id)),
            "vip" => Loc.F("reward.vip", amount.ToString("N0")),
            "troops" when TroopRules.TryParseKey(id, out var troop, out var troopTier) => Loc.F("reward.troops", amount.ToString("N0"), troopTier, Loc.T("troop." + troop)),
            _ => id
        };

        public static string Rewards(RewardData[] rewards) =>
            string.Join("  ·  ", (rewards ?? Array.Empty<RewardData>()).Select(r => Reward(r.type, r.id, r.amount, r.rarity)));
    }

    /// <summary>
    /// Quests (story and daily), the bag and reward results (docs/27) in Last Asylum's pages: daily_quests.jpg's tabs,
    /// timer, chest bar and task rows with reward tiles and a green Claim or blue Go; bag.jpg's resource chips, tabs and
    /// four-column rarity grid, with the item dialog (icon, description, owned count, a slider, Use / Use all).
    /// </summary>
    public sealed partial class SocialView
    {
        private int _questTab;
        private int _bagTab;
        private RewardReportDto _reward;
        private int _bagCount;
        private UiWindow _itemWindow;
        private UiWindow _bagStats;

        /// <summary>Task rows are tall enough for square reward tiles (Last Asylum's quest rows).</summary>
        private const float QuestRowHeight = 280f;

        public void OpenQuests() => Open(Page.Quests);

        public void OpenBag() => Open(Page.Bag);

        /// <summary>The HUD's task strip claims a finished chapter task directly.</summary>
        public void ClaimQuest(string questId) => _commands.Dispatch(new ClaimQuestCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), QuestId = questId
        });

        private string QuestKey() => $"{View.QuestVersion}|{_questTab}|{_bagTab}";

        // --- Quests --------------------------------------------------------------------------------

        private void RenderQuests()
        {
            BeginPage();
            var state = View.Quests;
            _title.text = Loc.T("quest.title");
            var dots = state == null ? null : new[] { StoryReady(state), DailyReady(state) };
            UiFrames.Tabs(PinTop(100), new[] { Loc.T("quest.tab.chapter"), Loc.T("quest.tab.daily") }, _questTab, i =>
            {
                _questTab = i;
                Render(force: true);
            }, dots).Place(0, 0, 1, 1);

            if (state == null)
            {
                NoteRow(Loc.T("quest.none"));
                return;
            }

            if (_questTab == 0)
                RenderChapter(state);
            else
                RenderDaily(state);
        }

        private static bool StoryReady(QuestStateDto state) =>
            state.Guide != null && state.Guide.Progress >= state.Guide.Target
            || state.Chapter != null && state.Chapter.Progress >= state.Chapter.Target;

        private bool DailyReady(QuestStateDto state) =>
            state.Daily.Any(p => !p.Claimed && p.Progress >= p.Target)
            || (_catalog.Quests.dailyChests ?? Array.Empty<DailyChestData>()).Any(c => !state.ClaimedChests.Contains(c.points) && state.DailyPoints >= c.points);

        private void RenderChapter(QuestStateDto state)
        {
            RenderGuide(state);

            SectionRow(Loc.F("quest.chapter", state.ChapterNumber, state.ChapterTotal));
            var current = state.Chapter;
            if (current == null || !_catalog.TryGetQuest(current.Id, out var quest, out _))
                NoteRow(Loc.T("quest.chapter.done"));
            else
                TaskRow(quest, current.Progress, current.Target, false);

            // The next tasks, so the player sees where the story goes.
            var chapter = _catalog.Quests.chapter ?? Array.Empty<QuestData>();
            if (state.ChapterNumber < chapter.Length)
            {
                SectionRow(Loc.T("quest.next"));
                for (var i = state.ChapterNumber; i < Math.Min(chapter.Length, state.ChapterNumber + 3); i++)
                    UiFrames.QuestRow(_list, $"{i + 1}. {QuestText.Task(chapter[i])}", RewardTiles(chapter[i].rewards).Take(5).ToArray(), null, false, null)
                        .Row(QuestRowHeight);
            }

            RenderUpcoming();
        }

        /// <summary>"Upgrade the reactor to Lv. 3 (1/1)": the task with its progress, green once done.</summary>
        private static string TaskTitle(QuestData quest, long progress, long target)
        {
            var ink = ColorUtility.ToHtmlStringRGB(progress >= target ? UiFrames.InkGood : UiFrames.InkSoft);
            return $"{QuestText.Task(quest)} <color=#{ink}>({Math.Min(progress, target):N0}/{target:N0})</color>";
        }

        /// <summary>
        /// One task row: reward tiles (activity points first for daily tasks), and Claim when done, Go when something on
        /// screen does it, a grey Not yet otherwise, a grey Claimed afterwards.
        /// </summary>
        private void TaskRow(QuestData quest, long progress, long target, bool claimed, int points = 0)
        {
            var tiles = RewardTiles(quest.rewards);
            if (points > 0)
                tiles = new[] { ("g_star", "rare", points.ToString()) }.Concat(tiles).ToArray();
            var done = progress >= target;
            var id = quest.id;
            var go = done || claimed ? null : QuestGo(quest);
            if (claimed)
                UiFrames.QuestRow(_list, TaskTitle(quest, progress, target), tiles.Take(5).ToArray(), Loc.T("event.claimed"), true, null, false).Row(QuestRowHeight);
            else if (done)
                UiFrames.QuestRow(_list, TaskTitle(quest, progress, target), tiles.Take(5).ToArray(), Loc.T("quest.claim"), true, () => ClaimQuest(id)).Row(QuestRowHeight);
            else
                UiFrames.QuestRow(_list, TaskTitle(quest, progress, target), tiles.Take(5).ToArray(), go != null ? Loc.T("guide.go") : Loc.T("quest.notyet"),
                    false, go, go != null).Row(QuestRowHeight);
        }

        private void RenderDaily(QuestStateDto state)
        {
            ChestTrack(state);

            // Ready to claim first, then open tasks, the claimed ones last (as Last Asylum sorts them).
            var rows = state.Daily
                .Select(p => (Progress: p, Known: _catalog.TryGetQuest(p.Id, out var q, out _), Quest: q))
                .Where(r => r.Known)
                .OrderBy(r => r.Progress.Claimed ? 2 : r.Progress.Progress >= r.Progress.Target ? 0 : 1)
                .ToList();
            foreach (var row in rows)
                TaskRow(row.Quest, row.Progress.Progress, row.Progress.Target, row.Progress.Claimed, row.Quest.points);
        }

        /// <summary>
        /// The daily activity card: the reset timer, the points medal and a bar with the chests standing at their points
        /// (alternately above and below it). A ready chest bounces with a red dot and opens on tap; the others show what
        /// they hold.
        /// </summary>
        private void ChestTrack(QuestStateDto state)
        {
            var chests = _catalog.Quests.dailyChests ?? Array.Empty<DailyChestData>();
            var max = chests.Length > 0 ? chests.Max(c => c.points) : 100;
            var card = UiFrames.Plate(_list, UiFrames.Band).Row(360);

            var timer = UiFrames.Plate(card.transform, new Color(0.08f, 0.12f, 0.2f, 0.72f));
            timer.rectTransform.Place(0.02f, 0.86f, 0.46f, 0.98f);
            var time = UiKit.Label("Timer", timer.transform, string.Empty, UiKit.TypeScale.Caption, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            time.rectTransform.Place(0.04f, 0, 0.96f, 1);
            time.Fit(14, UiKit.TypeScale.Caption);
            UiText.Outline(time, new Color(0.02f, 0.04f, 0.08f, 0.9f));
            LiveText.Bind(time, () => Loc.F("quest.reset", Loc.Duration(Mathf.Max(0f, View.DailyResetAt - Time.realtimeSinceStartup))), 1f);

            // The medal with today's points.
            Hud.HudIcons.Image(card.transform, "g_star", 0.01f, 0.38f, 0.13f, 0.62f);
            var points = UiKit.Label("Points", card.transform, state.DailyPoints.ToString("N0"), UiKit.TypeScale.Body, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            points.rectTransform.Place(0.01f, 0.25f, 0.13f, 0.38f);
            UiText.Outline(points, new Color(0.02f, 0.04f, 0.08f, 0.9f));

            const float x0 = 0.15f, x1 = 0.95f;
            Skin.Bar(card.transform, max > 0 ? (float)state.DailyPoints / max : 0f, "gold").Place(x0, 0.45f, x1, 0.53f);
            for (var i = 0; i < chests.Length; i++)
            {
                var chest = chests[i];
                var claimed = state.ClaimedChests.Contains(chest.points);
                var ready = !claimed && state.DailyPoints >= chest.points;
                var x = x0 + (x1 - x0) * chest.points / Mathf.Max(1, max);
                var above = i % 2 == 0;

                var tick = UiKit.Box("Tick", card.transform, Color.white);
                tick.sprite = null;
                tick.raycastTarget = false;
                tick.rectTransform.Place(x - 0.003f, 0.43f, x + 0.003f, 0.55f);

                var holder = UiKit.Rect("Chest", card.transform).Place(x - 0.06f, above ? 0.62f : 0.06f, x + 0.06f, above ? 0.84f : 0.31f);
                var icon = Hud.HudIcons.Image(holder, i == chests.Length - 1 ? "item_chest_gold" : i >= chests.Length / 2 ? "item_chest_silver" : "item_chest_bronze",
                    0, 0, 1, 1);
                icon.color = claimed ? new Color(1, 1, 1, 0.4f) : Color.white;
                icon.raycastTarget = true;
                var label = UiKit.Label("Points", card.transform, claimed ? "✓" : chest.points.ToString(), UiKit.TypeScale.Caption, claimed ? UiFrames.InkGood : UiFrames.Ink,
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                label.rectTransform.Place(x - 0.06f, above ? 0.545f : 0.33f, x + 0.06f, above ? 0.635f : 0.42f);

                var pointsNeeded = chest.points;
                var rewards = chest.rewards;
                var button = holder.gameObject.AddComponent<Button>();
                button.targetGraphic = icon;
                button.onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    if (ready)
                        _commands.Dispatch(new ClaimDailyChestCommand { CommandId = NewId(), ClientSequence = NextSequence(), Points = pointsNeeded });
                    else
                        Toast(QuestText.Rewards(rewards), UiKit.Accent);
                });
                holder.gameObject.AddComponent<PressFeedback>();
                if (ready)
                {
                    UiFrames.Dot(holder);
                    holder.gameObject.AddComponent<Bob>();
                }
            }
        }

        // --- Bag -------------------------------------------------------------------------------------

        /// <summary>
        /// The bag in Last Asylum's page: resource chips and the overview button in the bar, tabs (Special · Resources ·
        /// Speed-ups · Heroes), a four-column grid of rarity tiles with the amount on top and the count in the corner;
        /// a tap opens the item dialog. The Heroes tab lists the cards of heroes still to recruit under the grid.
        /// </summary>
        private void RenderBag()
        {
            BeginPage();
            _title.text = Loc.T("bag.title");
            BarChips(new[]
            {
                (Hud.HudIcons.Resource(ResourceType.Titanium), WalletChip(ResourceType.Titanium)),
                (Hud.HudIcons.Resource(ResourceType.Biomass), WalletChip(ResourceType.Biomass)),
                (Hud.HudIcons.Resource(ResourceType.Food), WalletChip(ResourceType.Food))
            }, "g_details", OpenBagStats);

            var cards = View.HeroCards.Where(p => p.Value > 0 && _catalog.TryGetHero(p.Key, out _)).ToList();
            var recruitable = cards.Any(p => _catalog.TryGetHero(p.Key, out var h) && p.Value >= QuestRules.RecruitShards(h));
            var tabs = new[] { Loc.T("bag.tab.special"), Loc.T("bag.tab.resources"), Loc.T("bag.tab.speedups"), Loc.T("bag.tab.heroes") };
            UiFrames.Tabs(PinTop(100), tabs, _bagTab, i =>
            {
                _bagTab = i;
                Render(force: true);
            }, new[] { false, false, false, recruitable }).Place(0, 0, 1, 1);

            var entries = View.Items.Where(p => p.Value > 0)
                .Select(p => (Id: p.Key, Count: p.Value, Tab: BagTab(p.Key)))
                .Where(e => e.Tab == _bagTab)
                .OrderBy(e => Capsules.TryParse(e.Id, out var r, out _) ? (int)r : 99)
                .ThenByDescending(e => Capsules.TryParse(e.Id, out _, out var a) ? a : 0)
                .ThenBy(e => e.Id, StringComparer.Ordinal)
                .ToList();

            if (entries.Count == 0 && !(_bagTab == 3 && cards.Count > 0))
            {
                NoteRow(Loc.T(_bagTab == 2 ? "bag.speedups.empty" : "bag.empty"));
                return;
            }

            // The resources tab empties itself into the wallet in one tap (each package kind is one command).
            if (_bagTab == 1 && entries.Count > 0)
            {
                var packages = entries.Select(e => (e.Id, e.Count)).ToList();
                BottomButton(Loc.F("bag.openAll", packages.Sum(p => p.Count).ToString("N0")), UiKit.Gold, () =>
                {
                    foreach (var (packageId, packageCount) in packages)
                        OpenItems(packageId, packageCount);
                });
            }

            if (entries.Count > 0)
            {
                var grid = UiFrames.Grid(_list, 4, 1f, 18f);
                grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(4, 4, 6, 6);
                foreach (var entry in entries)
                    BagTile(grid, entry.Id, entry.Count);
            }

            // Hero cards of heroes not recruited yet stay on the Heroes tab.
            if (_bagTab == 3 && cards.Count > 0)
            {
                SectionRow(Loc.T("bag.tab.cards"));
                foreach (var (heroId, count) in cards.Select(p => (p.Key, p.Value)))
                {
                    if (_catalog.TryGetHero(heroId, out var hero))
                        RecruitCard(hero, count);
                }
            }
        }

        /// <summary>0 special (chests), 1 resources (capsules), 2 speed-ups, 3 heroes (cards).</summary>
        private int BagTab(string id)
        {
            if (GiftItems.TryParseHero(id, out _, out _, out _))
                return 3;
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
            if (GiftItems.TryParseHero(id, out var heroId, out _, out _))
                return _catalog.TryGetHero(heroId, out var hero) ? hero.Rarity.id : "rare";
            if (VipRules.TryParseItem(id, out var vip))
                return vip >= 5000 ? "legendary" : vip >= 1000 ? "epic" : vip >= 500 ? "rare" : "common";
            if (GiftItems.TryParseTroops(id, out _, out var tier, out _))
                return tier >= 9 ? "legendary" : tier >= 7 ? "epic" : tier >= 4 ? "rare" : "common";
            // Speed-ups by the time they take off: up to an hour green, 8 hours blue, a day purple, more gold.
            if (Speedups.TryParse(id, out _, out var minutes, out var percent))
                return percent ? minutes >= 50 ? "epic" : "rare"
                    : minutes > 1440 ? "legendary" : minutes > 480 ? "epic" : minutes > 60 ? "rare" : "common";
            if (TeleportRules.IsItem(id))
                return id == TeleportRules.TargetItem ? "epic" : id == TeleportRules.AllianceItem ? "rare" : "common";
            if (RenameRules.IsItem(id))
                return "rare";
            switch (id)
            {
                case "shield_8h": return "common";
                case "shield_24h": return "rare";
                case "shield_48h": return "epic";
                case "shield_7d": return "legendary";
            }
            return _catalog.TryGetItem(id, out var item) && !string.IsNullOrEmpty(item.rarity) ? item.rarity : "common";
        }

        /// <summary>What a tile shows over its icon: a package's amount, a speed-up's time, a gift's level and stars.</summary>
        private static string BagTag(string id) =>
            GiftItems.TryParseHero(id, out _, out var giftLevel, out var giftStars) ? Loc.F("item.herogift.short", giftLevel, giftStars)
            : GiftItems.TryParseTroops(id, out _, out var giftTier, out var giftCount) ? $"T{giftTier} ×{CostChips.Compact(giftCount)}"
            : VipRules.TryParseItem(id, out var vip) ? "VIP " + CostChips.Compact(vip)
            : Capsules.TryParse(id, out _, out var amount) ? CostChips.Compact(amount)
            : Speedups.TryParse(id, out _, out var minutes, out var percent) ? QuestText.SpeedupAmount(minutes, percent) : null;

        private void BagTile(Transform grid, string id, int count)
        {
            _catalog.TryGetItem(id, out var item);
            // Chests wear the red dot: something waits inside.
            var chest = item?.kind == "chest" || id.StartsWith("chest_", StringComparison.Ordinal);
            UiFrames.ItemTile(grid, QuestText.ItemIcon(id, item?.kind), BagTone(id), count.ToString("N0"), () => OpenItemDialog(id), BagTag(id), chest);
        }

        /// <summary>How many of one item a single Use takes at most (server: PlayerSession.MaxOpenAtOnce).</summary>
        private const int MaxOpenAtOnce = 10_000;

        /// <summary>
        /// Last Asylum's item dialog: the tile and what the item does, the owned count under the name; for items used in
        /// numbers a − slider + row with the count (starting at all of them), then Use and Use all. Teleports go one at
        /// a time; the targeted one is used on the map and speed-ups on a timer, so those only explain.
        /// </summary>
        private void OpenItemDialog(string id)
        {
            var count = View.Items.TryGetValue(id, out var owned) ? owned : 0;
            if (count <= 0)
                return;
            if (_itemWindow != null)
                _itemWindow.Close();

            var hero = GiftItems.TryParseHero(id, out _, out _, out _);
            var capsule = Capsules.TryParse(id, out var resource, out var amount);
            var teleport = TeleportRules.IsItem(id);
            var speedup = Speedups.TryParse(id, out _, out _, out _);
            var max = hero || teleport ? 1 : Math.Max(1, capsule ? count : Math.Min(count, MaxOpenAtOnce));
            var countable = max > 1 && !speedup;
            var usable = !speedup && id != TeleportRules.TargetItem;

            UiWindow window = null;
            window = UiWindow.Open(_canvas.transform, QuestText.Item(id), Loc.F("speedup.have", count.ToString("N0")), () =>
            {
                if (_itemWindow == window)
                    _itemWindow = null;
            });
            _itemWindow = window;

            // The tile on the left, what it does on the right.
            _catalog.TryGetItem(id, out var item);
            var head = UiKit.Rect("Item", window.Body).Row(250);
            var tile = UiFrames.ItemTile(head, QuestText.ItemIcon(id, item?.kind), BagTone(id), null, null, BagTag(id));
            tile.anchorMin = tile.anchorMax = new Vector2(0, 0.5f);
            tile.pivot = new Vector2(0, 0.5f);
            tile.sizeDelta = new Vector2(170, 170);
            tile.anchoredPosition = new Vector2(12, 0);
            var desc = capsule ? Loc.F("bag.capsule.gives", CostChips.Compact(amount), Loc.Resource(resource.ToString())) : QuestText.ItemDescription(id);
            if (id == TeleportRules.TargetItem)
                desc += "\n" + Loc.T("bag.teleport.target");
            else if (speedup)
                desc += "\n" + Loc.T("bag.speedup.how");
            var text = UiKit.Label("Desc", head, desc, UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleLeft);
            text.rectTransform.Place(0, 0.04f, 1, 0.96f);
            text.rectTransform.offsetMin = new Vector2(206, text.rectTransform.offsetMin.y);
            text.Fit(18, UiKit.TypeScale.Body);

            _bagCount = max;
            if (countable)
                CountSlider(window.Body, max);

            if (!usable)
            {
                window.Fit();
                return;
            }

            void Use(int n)
            {
                window.Close();
                if (id == TeleportRules.RandomItem)
                    _commands.Dispatch(new TeleportCommand { CommandId = NewId(), ClientSequence = NextSequence(), Mode = TeleportRules.Random });
                else if (id == TeleportRules.AllianceItem)
                    _commands.Dispatch(new TeleportCommand { CommandId = NewId(), ClientSequence = NextSequence(), Mode = TeleportRules.Alliance });
                else
                    OpenItems(id, n);
            }

            window.AddButton(Loc.T("bag.use"), null, UiKit.Primary, () => Use(_bagCount));
            if (countable)
                window.AddButton(Loc.F("bag.useAll", max.ToString("N0")), null, UiKit.Gold, () => Use(max));
            window.Fit();
        }

        /// <summary>The genre's count row: − and + around a gold slider, the count in a white box on the right.</summary>
        private void CountSlider(Transform parent, int max)
        {
            var row = UiKit.Rect("Count", parent).Row(130);
            var blue = UiKit.Primary;
            var box = UiFrames.Plate(row, Color.white);
            box.rectTransform.Place(0.8f, 0.16f, 1f, 0.84f);
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.55f, 0.62f, 0.72f, 1f);
            var countText = UiKit.Label("CountText", box.transform, _bagCount.ToString("N0"), UiKit.TypeScale.Subtitle, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            countText.rectTransform.Place(0, 0, 1, 1);
            countText.Fit(16, UiKit.TypeScale.Subtitle);

            UnityEngine.UI.Slider slider = null;
            void Set(int n)
            {
                _bagCount = Mathf.Clamp(n, 1, max);
                countText.text = _bagCount.ToString("N0");
                slider?.SetValueWithoutNotify((_bagCount - 1f) / (max - 1f));
            }

            var minus = UiKit.Button("Minus", row, "−", () => Set(_bagCount - 1), blue, UiKit.TypeScale.Title);
            ((RectTransform)minus.transform).Place(0, 0.1f, 0.11f, 0.9f);
            slider = UiKit.Slider("Slider", row, 1f, v => Set(1 + Mathf.RoundToInt(v * (max - 1))));
            ((RectTransform)slider.transform).Place(0.13f, 0, 0.65f, 1);
            // Gold fill on a dark track with a blue grip, as in Last Asylum.
            var track = slider.transform.Find("Track").GetComponent<Image>();
            track.sprite = UiKit.Rounded;
            track.color = new Color(0.36f, 0.42f, 0.52f, 1f);
            var fill = slider.fillRect.GetComponent<Image>();
            fill.sprite = UiKit.Rounded;
            fill.color = new Color(0.95f, 0.69f, 0.24f, 1f);
            var grip = slider.handleRect.GetComponent<Image>();
            grip.sprite = UiKit.Rounded;
            grip.color = new Color(0.3f, 0.56f, 0.92f, 1f);
            var plus = UiKit.Button("Plus", row, "+", () => Set(_bagCount + 1), blue, UiKit.TypeScale.Title);
            ((RectTransform)plus.transform).Place(0.67f, 0.1f, 0.78f, 0.9f);
            Set(max);
        }

        /// <summary>Last Asylum's resource overview: what the bag holds, what is owned, and both together.</summary>
        private void OpenBagStats()
        {
            if (_bagStats != null)
                _bagStats.Close();
            UiWindow window = null;
            window = UiWindow.Open(_canvas.transform, Loc.T("bag.stats.title"), string.Empty, () =>
            {
                if (_bagStats == window)
                    _bagStats = null;
            });
            _bagStats = window;

            UiFrames.TableRow(window.Body, new[] { Loc.T("bag.stats.type"), Loc.T("bag.stats.items"), Loc.T("bag.stats.owned"), Loc.T("bag.stats.total") }, true).Row(70);
            var wallet = Wallet();
            var types = new[] { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Food, ResourceType.Energy, ResourceType.Oxygen };
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                long inBag = View.Items.Where(p => p.Value > 0).Sum(p => Capsules.TryParse(p.Key, out var r, out var a) && r == type ? a * p.Value : 0);
                wallet.TryGetValue(type, out var have);
                var row = UiFrames.TableRow(window.Body, new[]
                {
                    Loc.Resource(type.ToString()), CostChips.Compact(inBag), CostChips.Compact(have), CostChips.Compact(inBag + have)
                }, false, false, i).Row(84);
                Hud.HudIcons.Image(row, Hud.HudIcons.Resource(type), 0.005f, 0.12f, 0.055f, 0.88f);
            }

            window.Fit();
        }

        private static TMP_Text Outlined(TMP_Text text)
        {
            UiText.Outline(text, new Color(0, 0, 0, 0.85f));
            text.raycastTarget = false;
            return text;
        }

        /// <summary>A not-yet-recruited hero: the face, the card count towards recruiting and the Recruit button.</summary>
        private void RecruitCard(HeroDefinition hero, int count)
        {
            var need = QuestRules.RecruitShards(hero);
            var card = PlateRow(200);
            Portrait(card.transform, hero, count >= need, 0.01f, 0.05f, 0.18f, 0.95f);
            UiKit.Label("Name", card.transform, Loc.T("hero." + hero.Id), UiKit.TypeScale.Body + 2, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.21f, 0.55f, 0.7f, 0.92f);
            UiFrames.Progress(card.transform, count, need, count >= need ? "green" : "blue").Place(0.21f, 0.2f, 0.68f, 0.42f);
            var id = hero.Id;
            var button = UiKit.Button("Recruit", card.transform, Loc.T("hero.recruit"), () => _commands.Dispatch(new RecruitHeroCommand
            {
                CommandId = NewId(), ClientSequence = NextSequence(), HeroId = id
            }), count >= need ? UiKit.Gold : UiKit.Panel, UiKit.TypeScale.Body);
            button.interactable = count >= need;
            ((RectTransform)button.transform).Place(0.72f, 0.22f, 0.97f, 0.78f);
        }

        private void OpenItems(string id, int count) => _commands.Dispatch(new OpenItemCommand
        {
            CommandId = NewId(), ClientSequence = NextSequence(), ItemId = id, Count = count
        });

        // --- Rewards ----------------------------------------------------------------------------------

        /// <summary>The page the player was on (for the reward dialog to return to), and its mark in the list.</summary>
        private Page _lastPage = Page.None;
        private GameObject _lastPageMark;

        /// <summary>Called by every page here: remembers it until the list is cleared or the sheet closes.</summary>
        private void MarkPage()
        {
            if (_page == Page.Reward)
                return;
            _lastPage = _page;
            _lastPageMark = UiKit.Rect("PageMark", _list).gameObject;
            _lastPageMark.AddComponent<LayoutElement>().ignoreLayout = true;
            if (_sheet.GetComponent<DisableWatch>() == null)
                _sheet.AddComponent<DisableWatch>().Disabled = () => _lastPageMark = null;
        }

        /// <summary>
        /// Rewards received, as Last Asylum shows them: a dialog over the page the player was on (or over the base when
        /// the sheet was closed), the rewards as tiles with their names, and Done.
        /// </summary>
        private void RenderReward()
        {
            var rewards = (_reward?.Rewards ?? Array.Empty<RewardEntryDto>())
                .GroupBy(r => (r.Type, r.Id))
                .Select(g => (g.Key.Type, g.Key.Id, Amount: g.Sum(r => r.Amount)))
                .ToArray();
            UiWindow window = null;
            window = UiWindow.Open(_canvas.transform, Loc.T("reward.title"), string.Empty, null);
            RewardRows(window.Body, rewards);
            window.AddButton(Loc.T("hero.pod.done"), null, UiKit.Primary, () => window.Close());
            window.Fit();
            LeaveReward();
        }

        /// <summary>Back to where the reward came from: the event, mail or alliance page, the page here, or the base.</summary>
        private void LeaveReward()
        {
            var back = _afterReward != Page.None ? _afterReward : _lastPageMark != null ? _lastPage : Page.None;
            _afterReward = Page.None;
            if (back != Page.None)
            {
                _page = back;
                Render(force: true);
            }
            else
            {
                Close();
            }
        }

        /// <summary>Reward cells four to a row in a dialog's body (rows, so the dialog can size itself at once).</summary>
        private void RewardRows(Transform parent, (string Type, string Id, long Amount)[] rewards)
        {
            for (var start = 0; start < rewards.Length; start += 4)
            {
                var row = UiKit.Rect("Rewards", parent).Row(250);
                for (var c = 0; c < 4 && start + c < rewards.Length; c++)
                {
                    var (type, id, amount) = rewards[start + c];
                    var (icon, rarity, count) = RewardTile(type, id, amount);
                    var name = type == "item" ? QuestText.Item(id) : type == "resource" ? Loc.Resource(id) : QuestText.Reward(type, id, amount);
                    UiFrames.RewardCell(row, icon, rarity, count, name).Place(c * 0.25f + 0.01f, 0.02f, c * 0.25f + 0.24f, 0.98f);
                }
            }
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
