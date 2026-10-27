using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI.Hud
{
    /// <summary>
    /// The main screen frame (docs/26_HUD.md), laid out like the genre's portrait SLGs: a header (avatar,
    /// resource chips, power, shop), side columns (events on the right, queue and shortcuts on the left,
    /// alliance/mail/bag on the lower right), a suggested-task strip and a bottom bar (Heroes, chat
    /// preview, Territory). Panels open over it; the header stays visible above full-screen pages.
    /// </summary>
    public sealed class MainHudView : MonoBehaviour
    {
        /// <summary>What the HUD can open; wired by <see cref="BaseManagementView"/>.</summary>
        public sealed class Actions
        {
            public Action Heroes;
            public Action Pod;
            public Action Alliance;
            public Action AllianceHelp;
            public Action Chat;
            public Action Research;
            public Action Expand;
            public Action Settings;
            public Action Quests;
            public Action Bag;
            public Action<string> ClaimQuest;
            public Action<string> Building;
            public Action<string, Color> Toast;
            public Func<bool> SheetOpen;
            public Action Territory;

            /// <summary>The Depth Map campaign (docs/rehber 08 §5).</summary>
            public Action Expedition;
            public Func<bool> InWorld;
        }

        private const float TicksPerMinute = 60f / SimulationTick.IntervalSeconds;

        private static readonly ResourceType[] RowOne = { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Food };
        private static readonly ResourceType[] RowTwo = { ResourceType.Oxygen, ResourceType.Energy, ResourceType.Alloy };

        private IStateSyncService _stateSync;
        private Actions _actions;
        private static GameCatalog _catalog => ClientCatalog.Catalog;

        private Canvas _canvas;
        private RectTransform _safe;
        private GameObject _lower; // everything below the header; hidden while a panel covers it
        private Text _level;
        private Text _power;
        private Text _hadalite;
        private readonly Dictionary<ResourceType, Text> _chips = new();
        private Text _podLabel;
        private GameObject _podDot;
        private GameObject _heroDot;
        private GameObject _allianceDot;
        private GameObject _helpDot;
        private GameObject _questDot;
        private Image _territoryIcon;
        private Text _territoryLabel;
        private bool _shownInWorld;
        private GameObject _bagDot;
        private string _taskQuest = string.Empty;
        private bool _taskDone;
        private Text _chatPreview;
        private GameObject _chatDot;
        private RectTransform _queue;
        private Text _queueTitle;
        private bool _queueOpen = true;
        private Text _task;
        private string _taskBuilding = string.Empty;
        private GameObject _popup;
        private float _nextRefresh;
        private float _lastPointerTime = -1f;
        private int _chatVersion = -1;

        private ClientStateView View => _stateSync.View;

        public void Initialize(IStateSyncService stateSync, Actions actions)
        {
            _stateSync = stateSync;
            _actions = actions;
            Build();
        }

        // --- Layout --------------------------------------------------------------------------

        private void Build()
        {
            _canvas = UiKit.Canvas("HudUi", 35);
            _canvas.transform.SetParent(transform, false);
            _safe = UiKit.Rect("Safe", _canvas.transform);
            _safe.Place(0, 0, 1, 1);
            _safe.gameObject.AddComponent<SafeArea>();

            BuildHeader();
            _lower = UiKit.Rect("Lower", _safe).Place(0, 0, 1, 1).gameObject;
            BuildRightColumn();
            BuildQueue();
            BuildLeftColumn();
            BuildLowerRight();
            BuildTask();
            BuildBottomBar();
        }

        private void BuildHeader()
        {
            var header = UiKit.Rect("Header", _safe).Place(0, 0.895f, 1, 1);
            var shade = UiKit.Box("Shade", header, new Color(0f, 0.02f, 0.04f, 0.55f));
            shade.rectTransform.Place(0, 0, 1, 1);
            shade.raycastTarget = false;

            // Avatar with the base level under it; opens settings and the profile.
            var avatar = Tap(header, "Avatar", 0.012f, 0.22f, 0.16f, 0.98f, _actions.Settings, new Color(0.05f, 0.12f, 0.16f, 0.95f));
            Frame(avatar, UiKit.Accent);
            HudIcons.Image(avatar, "avatar", 0.06f, 0.06f, 0.94f, 0.94f);
            var levelBox = UiKit.Box("Level", header, new Color(0.02f, 0.05f, 0.08f, 0.95f));
            levelBox.rectTransform.Place(0.012f, 0.02f, 0.16f, 0.22f);
            levelBox.raycastTarget = false;
            _level = Outlined(UiKit.Label("Text", levelBox.transform, string.Empty, 26, UiKit.Amber, TextAnchor.MiddleCenter, FontStyle.Bold));
            _level.rectTransform.Place(0, 0, 1, 1);

            // Resource chips: three on the first row, two plus power on the second.
            for (var i = 0; i < RowOne.Length; i++)
                Chip(header, RowOne[i], 0.175f + i * 0.19f, 0.54f, 0.175f + (i + 1) * 0.19f - 0.01f, 0.95f);
            for (var i = 0; i < RowTwo.Length; i++)
                Chip(header, RowTwo[i], 0.175f + i * 0.19f, 0.1f, 0.175f + (i + 1) * 0.19f - 0.01f, 0.5f);

            // Power sits under the shop; the second resource row carries Superalloy (docs/rehber K-01).
            var power = Tap(header, "Power", 0.76f, 0.1f, 0.988f, 0.5f, () => ShowPowerInfo(), new Color(0.02f, 0.05f, 0.08f, 0.85f));
            HudIcons.Image(power, "power", 0.01f, 0.05f, 0.27f, 0.95f);
            _power = Outlined(UiKit.Label("Value", power, string.Empty, 28, UiKit.Amber, TextAnchor.MiddleLeft, FontStyle.Bold));
            _power.rectTransform.Place(0.29f, 0, 1, 1);

            // Shop: Hadalite balance; opens the Cryo-Pod until the shop exists.
            var shop = Tap(header, "Shop", 0.76f, 0.54f, 0.988f, 0.95f, _actions.Pod, new Color(0.85f, 0.42f, 0.08f, 0.95f));
            HudIcons.Image(shop, "shop", 0.02f, 0.05f, 0.42f, 0.95f);
            _hadalite = Outlined(UiKit.Label("Value", shop, string.Empty, 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            _hadalite.rectTransform.Place(0.4f, 0, 1, 1);
        }

        private void Chip(RectTransform parent, ResourceType type, float x0, float y0, float x1, float y1)
        {
            var chip = Tap(parent, "Chip_" + type, x0, y0, x1, y1, () => ShowResource(type), new Color(0.02f, 0.05f, 0.08f, 0.85f));
            HudIcons.Image(chip, HudIcons.Resource(type), 0.01f, 0.05f, 0.33f, 0.95f);
            var value = Outlined(UiKit.Label("Value", chip, string.Empty, 28, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold));
            value.rectTransform.Place(0.35f, 0, 1, 1);
            _chips[type] = value;
        }

        private void BuildRightColumn()
        {
            // Event column under the shop: Cryo-Pod (free pull timer), events and sieges (coming).
            var pod = SideButton(_lower.transform, "Pod", "event_pod", Loc.T("hud.pod"), 0.835f, 0.80f, 0.985f, 0.875f, _actions.Pod);
            _podLabel = pod.Label;
            _podDot = pod.Dot;
            SideButton(_lower.transform, "Events", "event_gift", Loc.T("hud.events"), 0.835f, 0.71f, 0.985f, 0.785f, () => Soon("hud.events"));
            SideButton(_lower.transform, "War", "event_war", Loc.T("hud.expedition"), 0.835f, 0.62f, 0.985f, 0.695f, () => _actions.Expedition?.Invoke());
        }

        /// <summary>Base-only buttons; the world map hides them (its own controls sit there).</summary>
        private readonly List<GameObject> _baseOnly = new();

        private void BuildLeftColumn()
        {
            Side Keep(Side side)
            {
                _baseOnly.Add(side.Label.transform.parent.gameObject);
                return side;
            }

            Keep(SideButton(_lower.transform, "Expand", "left_expand", Loc.T("hud.expand"), 0.012f, 0.45f, 0.142f, 0.515f, _actions.Expand));
            Keep(SideButton(_lower.transform, "Research", "left_research", Loc.T("hud.research"), 0.012f, 0.37f, 0.142f, 0.435f, _actions.Research));
            _helpDot = Keep(SideButton(_lower.transform, "Help", "left_help", Loc.T("hud.help"), 0.012f, 0.29f, 0.142f, 0.355f, _actions.AllianceHelp)).Dot;
            _questDot = Keep(SideButton(_lower.transform, "Quests", "quest", Loc.T("hud.quests"), 0.012f, 0.21f, 0.142f, 0.275f, _actions.Quests)).Dot;
        }

        private void BuildLowerRight()
        {
            _allianceDot = SideButton(_lower.transform, "Alliance", "side_alliance", Loc.T("social.alliance"), 0.845f, 0.37f, 0.985f, 0.44f, _actions.Alliance).Dot;
            SideButton(_lower.transform, "Mail", "side_mail", Loc.T("hud.mail"), 0.845f, 0.285f, 0.985f, 0.355f, () => Soon("hud.mail"));
            _bagDot = SideButton(_lower.transform, "Bag", "side_bag", Loc.T("hud.bag"), 0.845f, 0.2f, 0.985f, 0.27f, _actions.Bag).Dot;
        }

        private void BuildQueue()
        {
            var title = Tap(_lower.transform, "QueueTitle", 0.012f, 0.845f, 0.45f, 0.88f, () => { _queueOpen = !_queueOpen; _nextRefresh = 0; },
                new Color(0.02f, 0.05f, 0.08f, 0.8f));
            _queueTitle = Outlined(UiKit.Label("Text", title, string.Empty, 28, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold));
            _queueTitle.rectTransform.Place(0.04f, 0, 0.98f, 1);

            _queue = UiKit.Rect("Queue", _lower.transform).Place(0.012f, 0.6f, 0.45f, 0.84f);
        }

        private void BuildTask()
        {
            var strip = Tap(_lower.transform, "Task", 0.012f, 0.125f, 0.66f, 0.18f, () =>
            {
                if (_taskDone && _taskQuest.Length > 0)
                    _actions.ClaimQuest?.Invoke(_taskQuest);
                else if (_taskQuest.Length > 0)
                    _actions.Quests?.Invoke();
                else if (_taskBuilding.Length > 0)
                    _actions.Building?.Invoke(_taskBuilding);
            }, new Color(0.02f, 0.05f, 0.08f, 0.75f));
            HudIcons.Image(strip, "quest", 0.0f, 0.0f, 0.16f, 1f);
            _task = Outlined(UiKit.Label("Text", strip, string.Empty, 26, UiKit.Text, TextAnchor.MiddleLeft));
            _task.rectTransform.Place(0.17f, 0, 0.99f, 1);
        }

        private void BuildBottomBar()
        {
            var bar = UiKit.Box("BottomBar", _lower.transform, new Color(0.02f, 0.06f, 0.09f, 0.94f));
            bar.rectTransform.Place(0, 0, 1, 0.115f);
            var line = UiKit.Box("TopLine", bar.transform, new Color(UiKit.Accent.r, UiKit.Accent.g, UiKit.Accent.b, 0.5f));
            line.rectTransform.Place(0, 0.97f, 1, 1);
            line.raycastTarget = false;

            var heroes = Tap(bar.transform, "Heroes", 0, 0, 0.24f, 1.18f, _actions.Heroes, new Color(0.06f, 0.20f, 0.30f, 1f));
            Frame(heroes, UiKit.Accent);
            HudIcons.Image(heroes, "nav_heroes", 0.1f, 0.3f, 0.9f, 1.0f);
            Outlined(UiKit.Label("Label", heroes, Loc.T("hud.heroes"), 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)).rectTransform.Place(0, 0.02f, 1, 0.3f);
            _heroDot = Dot(heroes);

            var chat = Tap(bar.transform, "Chat", 0.245f, 0.04f, 0.755f, 0.94f, _actions.Chat, new Color(0, 0, 0, 0.35f));
            _chatPreview = UiKit.Label("Lines", chat, string.Empty, 24, UiKit.Text, TextAnchor.MiddleLeft);
            _chatPreview.rectTransform.Place(0.03f, 0.03f, 0.97f, 0.97f);
            _chatPreview.horizontalOverflow = HorizontalWrapMode.Overflow;
            _chatPreview.verticalOverflow = VerticalWrapMode.Truncate;
            _chatDot = Dot(chat);

            // Territory goes out to the world map; on the map the same button brings you home.
            var territory = Tap(bar.transform, "Territory", 0.76f, 0, 1, 1.18f, () => _actions.Territory?.Invoke(), new Color(0.06f, 0.20f, 0.30f, 1f));
            Frame(territory, UiKit.Accent);
            _territoryIcon = HudIcons.Image(territory, "nav_world", 0.1f, 0.3f, 0.9f, 1.0f);
            _territoryLabel = Outlined(UiKit.Label("Label", territory, Loc.T("hud.territory"), 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            _territoryLabel.rectTransform.Place(0, 0.02f, 1, 0.3f);
        }

        // --- Widgets -------------------------------------------------------------------------

        private static RectTransform Tap(Transform parent, string name, float x0, float y0, float x1, float y1, Action onClick, Color fill)
        {
            var box = UiKit.Box(name, parent, fill);
            box.rectTransform.Place(x0, y0, x1, y1);
            var button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = box;
            button.onClick.AddListener(() => onClick?.Invoke());
            box.gameObject.AddComponent<PressFeedback>();
            return box.rectTransform;
        }

        private readonly struct Side
        {
            public Side(Text label, GameObject dot)
            {
                Label = label;
                Dot = dot;
            }

            public Text Label { get; }
            public GameObject Dot { get; }
        }

        /// <summary>A round-ish icon button with an outlined caption under it, like the genre's side icons.</summary>
        private static Side SideButton(Transform parent, string name, string icon, string caption, float x0, float y0, float x1, float y1, Action onClick)
        {
            var root = Tap(parent, name, x0, y0, x1, y1, onClick, new Color(0, 0, 0, 0.001f));
            HudIcons.Image(root, icon, 0.08f, 0.25f, 0.92f, 1f);
            var label = Outlined(UiKit.Label("Caption", root, caption, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            label.rectTransform.Place(-0.2f, 0f, 1.2f, 0.28f);
            return new Side(label, Dot(root));
        }

        private static GameObject Dot(Transform parent)
        {
            var dot = UiKit.Box("Dot", parent, new Color(0.95f, 0.18f, 0.15f, 1f));
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.92f, 0.95f);
            dot.rectTransform.sizeDelta = new Vector2(26, 26);
            dot.raycastTarget = false;
            dot.gameObject.SetActive(false);
            return dot.gameObject;
        }

        private static void Frame(Transform parent, Color color)
        {
            var outline = parent.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.8f);
            outline.effectDistance = new Vector2(3, -3);
        }

        private static Text Outlined(Text text)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.9f);
            outline.effectDistance = new Vector2(2, -2);
            return text;
        }

        // --- Refresh -------------------------------------------------------------------------

        private void Update()
        {
            if (_stateSync == null || _canvas == null)
                return;

            _lower.SetActive(!(_actions.SheetOpen?.Invoke() ?? false));

            var inWorld = _actions.InWorld?.Invoke() ?? false;
            if (inWorld != _shownInWorld)
            {
                _shownInWorld = inWorld;
                _territoryIcon.sprite = HudIcons.Get(inWorld ? "left_home" : "nav_world");
                _territoryLabel.text = Loc.T(inWorld ? "hud.base" : "hud.territory");
                _queue.gameObject.SetActive(!inWorld);
                _queueTitle.transform.parent.gameObject.SetActive(!inWorld);
                _task.transform.parent.gameObject.SetActive(!inWorld);
                foreach (var go in _baseOnly)
                    go.SetActive(!inWorld);
            }

            // Rows are rebuilt on refresh; never under a finger, or the tap is lost.
            if (Input.touchCount > 0 || Input.GetMouseButton(0))
                _lastPointerTime = Time.unscaledTime;
            if (Time.unscaledTime - _lastPointerTime < 0.3f)
                return;
            if (Time.unscaledTime < _nextRefresh && _chatVersion == View.ChatVersion)
                return;
            _nextRefresh = Time.unscaledTime + 0.5f;

            var wallet = Wallet();
            var caps = Capacities();
            foreach (var pair in _chips)
            {
                wallet.TryGetValue(pair.Key, out var amount);
                caps.TryGetValue(pair.Key, out var cap);
                pair.Value.text = Compact(amount);
                pair.Value.color = amount <= 0 ? UiKit.Danger : cap > 0 && amount >= cap ? UiKit.Amber : UiKit.Text;
            }

            _hadalite.text = Compact(wallet.TryGetValue(ResourceType.Hadalite, out var h) ? h : 0);
            _level.text = Loc.F("hud.level", ReactorLevel());
            _power.text = Power().ToString("N0");

            RefreshPod();
            RefreshQueue();
            RefreshTask();
            RefreshSocial();
        }

        private void RefreshPod()
        {
            var ready = View.FreePullReadyAt >= 0 && View.FreePullReadyAt <= Time.realtimeSinceStartup;
            _podDot.SetActive(ready);
            _podLabel.text = ready ? Loc.T("hud.pod.free")
                : View.FreePullReadyAt > 0 ? Loc.Duration(View.FreePullReadyAt - Time.realtimeSinceStartup)
                : Loc.T("hud.pod");

            // Heroes tab: a free pull or a promotion waiting.
            var promotable = View.Heroes.Any(x => HeroRules.ShardsForNextStar(_catalog.HeroRules, x.Stars) is var need && need > 0 && x.Shards >= need);
            _heroDot.SetActive(ready || promotable);
        }

        private void RefreshQueue()
        {
            var rows = new List<(string Icon, string Text, float Left, string Key)>();
            var now = Time.realtimeSinceStartup;
            foreach (var b in Buildings())
            {
                if (b.IsUnderConstruction)
                    rows.Add(("queue_build", $"{Loc.Building(b.definitionId)} → {Loc.F("base.level", b.level + 1)}", b.constructionEndsAt - now, b.instanceKey));
                if (b.missionEndsAt > now)
                    rows.Add(("queue_drone", Loc.T("mission." + b.missionId), b.missionEndsAt - now, b.instanceKey));
            }

            foreach (var r in View.ActiveResearch)
                rows.Add(("left_research", Loc.T("research.node." + r.NodeId), r.EndsAt - now, string.Empty));

            var builders = Math.Max(1, _catalog.Progression.builderSlots) + Modifiers().ExtraBuilders;
            var building = Buildings().Count(b => b.IsUnderConstruction);
            var idle = rows.Count == 0;
            _queueTitle.text = $"{(_queueOpen ? "▼" : "▶")} {Loc.T("hud.queue")}  {building}/{builders}" +
                               (idle ? $"   <color=#FFB347>{Loc.T("hud.queue.free")}</color>" : string.Empty);

            for (var i = _queue.childCount - 1; i >= 0; i--)
                Destroy(_queue.GetChild(i).gameObject);
            if (!_queueOpen)
                return;

            const float rowHeight = 0.24f;
            for (var i = 0; i < Math.Min(rows.Count, 4); i++)
            {
                var row = rows[i];
                var key = row.Key;
                var y1 = 1f - i * (rowHeight + 0.02f);
                var rect = Tap(_queue, "Row", 0, y1 - rowHeight, 1, y1, () =>
                {
                    if (key.Length > 0)
                        _actions.Building?.Invoke(key);
                    else
                        _actions.Research?.Invoke();
                }, new Color(0.03f, 0.10f, 0.14f, 0.85f));
                HudIcons.Image(rect, row.Icon, 0.01f, 0.05f, 0.17f, 0.95f);
                Outlined(UiKit.Label("Name", rect, row.Text, 22, UiKit.Text, TextAnchor.MiddleLeft)).rectTransform.Place(0.19f, 0.45f, 0.99f, 1f);
                Outlined(UiKit.Label("Time", rect, Loc.Duration(row.Left), 24, row.Left <= _catalog.Progression.freeFinishSeconds ? UiKit.Accent : UiKit.Amber,
                    TextAnchor.MiddleLeft, FontStyle.Bold)).rectTransform.Place(0.19f, 0f, 0.99f, 0.5f);
            }


        }

        /// <summary>The current chapter task (claim it when done); without quests, the next upgrade to make.</summary>
        private void RefreshTask()
        {
            var chapter = View.Quests?.Chapter;
            if (chapter != null && _catalog.TryGetQuest(chapter.Id, out var quest, out _))
            {
                _taskQuest = quest.id;
                _taskDone = chapter.Progress >= chapter.Target;
                _task.text = _taskDone
                    ? $"<color=#FFB74D>{Loc.T("quest.claim")}!</color>  {QuestText.Task(quest)}"
                    : $"{QuestText.Task(quest)}  <color=#8AA9B5>({chapter.Progress}/{chapter.Target})</color>";
                return;
            }

            _taskQuest = string.Empty;
            _taskDone = false;
            var candidates = Buildings()
                .Where(b => b.level > 0 && !b.IsUnderConstruction && _catalog.TryGetBuilding(b.definitionId, out var d) && b.level < d.MaxLevel)
                .ToList();
            var reactor = ReactorLevel();
            var behind = candidates.Where(c => c.definitionId != BuildingProgression.ReactorId && c.level < reactor).OrderBy(c => c.level).ToList();
            var b = behind.Count > 0 ? behind[0] : candidates.FirstOrDefault(c => c.definitionId == BuildingProgression.ReactorId);

            if (string.IsNullOrEmpty(b.instanceKey))
            {
                _taskBuilding = string.Empty;
                _task.text = Loc.T("hud.task.none");
                return;
            }

            _taskBuilding = b.instanceKey;
            _task.text = Loc.F("hud.task.upgrade", Loc.Building(b.definitionId), b.level + 1);
        }

        private void RefreshSocial()
        {
            var quests = View.Quests;
            var questReady = quests != null && ((quests.Chapter != null && quests.Chapter.Progress >= quests.Chapter.Target)
                || quests.Daily.Any(d => !d.Claimed && d.Progress >= d.Target)
                || (_catalog.Quests.dailyChests ?? Array.Empty<DailyChestData>()).Any(c => quests.DailyPoints >= c.points && !quests.ClaimedChests.Contains(c.points)));
            _questDot.SetActive(questReady);
            _bagDot.SetActive(View.Items.Any(p => p.Value > 0)
                || View.HeroCards.Any(p => _catalog.TryGetHero(p.Key, out var h) && p.Value >= QuestRules.RecruitShards(h)));
            _allianceDot.SetActive(View.MyAlliance != null && View.HelpRequests.Any(r => !r.HelpedByMe));
            _helpDot.SetActive(_allianceDot.activeSelf);
            _chatDot.SetActive(View.UnreadChat > 0);

            if (_chatVersion == View.ChatVersion)
                return;
            _chatVersion = View.ChatVersion;
            var lines = View.Chat.Skip(Math.Max(0, View.Chat.Count - 3)).Select(Line);
            _chatPreview.text = View.Chat.Count == 0 ? $"<color=#8AA9B5>{Loc.T("chat.empty")}</color>" : string.Join("\n", lines);
        }

        private string Line(HADAL.Shared.DTOs.ChatMessageDto line)
        {
            if (line.IsSystem)
            {
                var bar = line.Text.IndexOf('|');
                return "<color=#FFB74D>· " + (bar < 0 ? Loc.T("chat." + line.Text) : Loc.F("chat." + line.Text.Substring(0, bar), line.Text.Substring(bar + 1))) + "</color>";
            }

            var tag = string.IsNullOrEmpty(line.AllianceTag) ? string.Empty : $"[{line.AllianceTag}]";
            var text = (line.Text ?? string.Empty).Replace("<", "<​");
            return $"<color=#FFB74D>{tag}{(line.SenderName ?? string.Empty).Replace("<", "<​")}</color>: {text}";
        }

        // --- Popups --------------------------------------------------------------------------

        private void ShowResource(ResourceType type)
        {
            var wallet = Wallet();
            var caps = Capacities();
            wallet.TryGetValue(type, out var amount);
            caps.TryGetValue(type, out var cap);
            var rate = View.ProductionMilliPerTick.TryGetValue(type, out var milli) ? milli * TicksPerMinute / ProductionFormulas.Milli
                : View.ProductionPerTick.TryGetValue(type, out var whole) ? whole * TicksPerMinute : 0f;
            var sign = rate >= 0 ? "+" : string.Empty;
            Popup(Loc.Resource(type.ToString()), HudIcons.Resource(type),
                $"{amount:N0} / {cap:N0}\n{sign}{rate:0} {Loc.Resource(type.ToString())}{Loc.T("base.permin")}\n\n{Loc.T("hud.resource.hint")}");
        }

        private void ShowPowerInfo() =>
            Popup(Loc.T("hud.power"), "power", $"{Power():N0}\n\n{Loc.T("hud.power.hint")}");

        private void Popup(string title, string icon, string body)
        {
            if (_popup != null)
                Destroy(_popup);

            var dim = UiKit.Box("Popup", _canvas.transform, new Color(0, 0, 0, 0.55f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => Destroy(dim.gameObject));
            _popup = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.1f, 0.38f, 0.9f, 0.62f);
            Frame(card.transform, UiKit.Accent);
            HudIcons.Image(card.transform, icon, 0.04f, 0.62f, 0.22f, 0.95f);
            UiKit.Label("Title", card.transform, title, 44, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.25f, 0.68f, 0.96f, 0.95f);
            UiKit.Label("Body", card.transform, body, 32, UiKit.Text, TextAnchor.UpperLeft).rectTransform.Place(0.06f, 0.06f, 0.94f, 0.62f);
        }

        private void Soon(string key) => _actions.Toast?.Invoke(Loc.F("hud.soon", Loc.T(key)), UiKit.Amber);

        // --- State helpers -------------------------------------------------------------------

        private BuildingSaveEntry[] Buildings() => View.Data.buildings ?? Array.Empty<BuildingSaveEntry>();

        private int ReactorLevel() =>
            Buildings().Where(b => b.definitionId == BuildingProgression.ReactorId).Select(b => b.level).DefaultIfEmpty(0).Max();

        private PlayerModifiers Modifiers()
        {
            var modifiers = ResearchRules.Modifiers(_catalog, View.ResearchLevels);
            HeroRules.AddCrew(_catalog, modifiers, View.Heroes.Select(h => new OwnedHero(h.HeroId, h.Level, h.Stars, h.Shards, h.Slot)));
            return modifiers;
        }

        /// <summary>Same formula as the server: each building level's table power (docs/rehber K-14) plus every hero's power.</summary>
        private long Power() =>
            Buildings().Sum(b => _catalog.TryGetBuilding(b.definitionId, out var d) ? BuildingProgression.Power(d, b.level) : b.level * 10L)
            + View.Heroes.Sum(h => _catalog.TryGetHero(h.HeroId, out var d) ? HeroRules.Power(d, h.Level, h.Stars) : 0);

        private Dictionary<ResourceType, long> Wallet()
        {
            var wallet = new Dictionary<ResourceType, long>();
            foreach (var entry in View.Data.resources ?? Array.Empty<ResourceSaveEntry>())
                wallet[ResourceTypeMap.FromStoredValue(entry.resourceType)] = entry.amount;
            return wallet;
        }

        private Dictionary<ResourceType, long> Capacities()
        {
            if (View.Capacities.Count > 0)
                return View.Capacities.ToDictionary(p => p.Key, p => p.Value);
            return _catalog.Capacities();
        }

        private static string Compact(long value) =>
            value >= 1_000_000 ? $"{value / 1_000_000d:0.#}M"
            : value >= 10_000 ? $"{value / 1_000d:0.#}K"
            : value.ToString("N0");
    }
}
