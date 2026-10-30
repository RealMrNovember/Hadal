using System;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// What the bag, heroes, quests, rewards, expedition and VIP pages share (Last Asylum's full-screen pages): tabs
    /// pinned under the top bar, an action bar pinned at the bottom, resource chips in the bar, the back arrow that
    /// goes one page back, and the reward tiles every list of rewards is drawn with.
    /// </summary>
    public sealed partial class SocialView
    {
        /// <summary>The page body's side and top margin, in canvas units.</summary>
        private const float PageGutter = 18f;

        private float _pinnedTop;
        private float _pinnedBottom;

        /// <summary>Starts a page: the list fills the body until tabs or an action bar are pinned.</summary>
        private void BeginPage()
        {
            _pinnedTop = _pinnedBottom = 0f;
            _subtitle.text = string.Empty;
            FitList();
            MarkPage();
        }

        private void FitList()
        {
            var list = (RectTransform)_list.parent;
            list.anchorMin = Vector2.zero;
            list.anchorMax = Vector2.one;
            list.offsetMin = new Vector2(PageGutter, _pinnedBottom + PageGutter);
            list.offsetMax = new Vector2(-PageGutter, -(_pinnedTop + PageGutter));
        }

        /// <summary>A strip pinned under the top bar (tabs), <paramref name="height"/> canvas units tall.</summary>
        private RectTransform PinTop(float height)
        {
            var strip = PinnedPart.Create(_body, _list, "PinTop");
            strip.anchorMin = new Vector2(0, 1);
            strip.anchorMax = Vector2.one;
            strip.pivot = new Vector2(0.5f, 1f);
            strip.offsetMin = new Vector2(PageGutter, -PageGutter - height);
            strip.offsetMax = new Vector2(-PageGutter, -PageGutter);
            _pinnedTop = PageGutter + height;
            FitList();
            return strip;
        }

        /// <summary>The action bar pinned at the bottom of the page (the genre's one big button under the list).</summary>
        private RectTransform PinBottom(float height = 150f)
        {
            var bar = PinnedPart.Create(_body, _list, "PinBottom");
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(1, 0);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = new Vector2(PageGutter, 10f);
            bar.offsetMax = new Vector2(-PageGutter, 10f + height);
            _pinnedBottom = 10f + height;
            FitList();
            return bar;
        }

        /// <summary>One big button centred in the bottom bar.</summary>
        private Button BottomButton(string label, Color fill, Action click, bool enabled = true, float x0 = 0.24f, float x1 = 0.76f)
        {
            var bar = PinBottom();
            var button = UiKit.Button("Action", bar, label, () => click?.Invoke(), enabled ? fill : UiKit.Panel, UiKit.TypeScale.Subtitle);
            button.interactable = enabled;
            ((RectTransform)button.transform).Place(x0, 0.08f, x1, 0.92f);
            return button;
        }

        /// <summary>The right side of the top bar: chips (icon and live value) and an optional square button.</summary>
        private RectTransform BarChips((string Icon, Func<string> Value)[] chips, string buttonIcon = null, Action button = null)
        {
            var right = _title.transform.parent.Find("Right");
            var row = PinnedPart.Create(right, _list, "BarChips");
            row.Place(0, 0, 1, 1);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.padding = new RectOffset(0, 20, 0, 0);
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var width = chips.Length >= 3 ? button != null ? 108f : 124f : 150f;
            foreach (var (icon, value) in chips)
                UiFrames.Chip(row, icon, value, width);
            if (button != null)
                UiFrames.BarButton(row, buttonIcon, button);
            return row;
        }

        private Func<string> WalletChip(ResourceType type) => () =>
            CostChips.Compact(Wallet().TryGetValue(type, out var amount) ? amount : 0);

        /// <summary>The bar's back arrow: one page back (hero card to the list, pod result to the pod…), else closed.</summary>
        private void Back()
        {
            switch (_page)
            {
                case Page.HeroDetail:
                case Page.Pod:
                    Open(Page.Heroes);
                    break;
                case Page.PodResult:
                    Open(Page.Pod);
                    break;
                case Page.DiveResult:
                    _title.color = Color.white;
                    Open(Page.Expedition);
                    break;
                case Page.Reward:
                    LeaveReward();
                    break;
                default:
                    Close();
                    break;
            }
        }

        // --- Reward tiles ------------------------------------------------------------------------

        /// <summary>The tile a reward is drawn as: its icon, its rarity frame and the amount for the corner.</summary>
        private (string Icon, string Rarity, string Count) RewardTile(string type, string id, long amount, string rarity = "")
        {
            var count = amount > 1 || type == "resource" ? CostChips.Compact(amount) : string.Empty;
            switch (type)
            {
                case "resource":
                    return (GameCatalog.TryParseResource(id, out var resource) ? Hud.HudIcons.Resource(resource) : "event_gift",
                        resource == ResourceType.Hadalite ? "epic" : "common", count);
                case "item":
                    _catalog.TryGetItem(id, out var item);
                    return (QuestText.ItemIcon(id, item?.kind), BagTone(id), count);
                case "shards":
                    var tone = string.IsNullOrEmpty(id) ? rarity : _catalog.TryGetHero(id, out var hero) ? hero.Rarity.id : rarity;
                    tone = string.IsNullOrEmpty(tone) ? "common" : tone;
                    return ("item_card_" + tone, tone, count);
                case "hero":
                    return ("nav_heroes", _catalog.TryGetHero(id, out var found) ? found.Rarity.id : "epic", string.Empty);
                case "vip":
                    return ("vip", "epic", count);
                case "troops":
                    return (TroopRulesIcon(id), "rare", count);
                default:
                    return ("event_gift", "common", count);
            }
        }

        private static string TroopRulesIcon(string key) =>
            HADAL.Shared.Simulation.TroopRules.TryParseKey(key, out var troop, out _) ? "item_troops_" + troop : "event_war";

        private (string Icon, string Rarity, string Count)[] RewardTiles(RewardData[] rewards) =>
            (rewards ?? Array.Empty<RewardData>()).Select(r => RewardTile(r.type, r.id, r.amount, r.rarity)).ToArray();

        /// <summary>A grid of reward cells (tile and name), four across.</summary>
        private void RewardGrid(Transform parent, (string Type, string Id, long Amount)[] rewards)
        {
            var grid = UiFrames.Grid(parent, 4, 0.92f, 16);
            foreach (var (type, id, amount) in rewards)
            {
                var (icon, rarity, count) = RewardTile(type, id, amount);
                UiFrames.RewardCell(grid, icon, rarity, count, type == "item" ? QuestText.Item(id) : type == "resource" ? Loc.Resource(id)
                    : QuestText.Reward(type, id, amount));
            }
        }

        // --- Quest "Go" ----------------------------------------------------------------------------

        /// <summary>
        /// Where a task's Go button leads: pages of this sheet directly, buildings and the map through the guide's
        /// routes; null when nothing on screen does the task (then the row shows "Not yet").
        /// </summary>
        private Action QuestGo(QuestData quest)
        {
            if (quest == null)
                return null;
            switch (quest.type)
            {
                case "heroLevel":
                case "heroes":
                    return () => Open(Page.Heroes);
                case "counter" when quest.target is "heroTrain":
                    return () => Open(Page.Heroes);
                case "counter" when quest.target is "pod":
                    return () => Open(Page.Pod);
                case "counter" when quest.target is "open":
                    return () => Open(Page.Bag);
                case "counter" when quest.target is "chat":
                    return OpenChat;
                case "counter" when quest.target is "allianceChat":
                    return OpenAllianceChat;
                case "alliance":
                case "counter" when quest.target is "help" or "helpAsk" or "helped":
                    return () => OpenAlliance(help: View.MyAlliance != null);
            }

            var guide = OnboardingGuide.Instance;
            if (guide == null || !guide.CanGoTo(quest.type, quest.target))
                return null;
            return () =>
            {
                Close();
                guide.GoTo(quest.type, quest.target);
            };
        }

        /// <summary>A light plate row in the list, <paramref name="height"/> reference pixels tall.</summary>
        private Image PlateRow(float height) => UiFrames.Plate(_list).Row(height);

        /// <summary>A pale section band in the list.</summary>
        private void SectionRow(string title) => UiFrames.Section(_list, title).Row(66);

        /// <summary>A note card in the list (empty states, explanations).</summary>
        private void NoteRow(string text, float height = 150f) => UiFrames.Note(_list, text).Row(height);
    }
}
