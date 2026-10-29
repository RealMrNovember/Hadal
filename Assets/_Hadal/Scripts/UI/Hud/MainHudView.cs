using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.DTOs;
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
    public sealed partial class MainHudView : MonoBehaviour
    {
        /// <summary>What the HUD can open; wired by <see cref="BaseManagementView"/>.</summary>
        public sealed class Actions
        {
            public Action Heroes;
            public Action Pod;
            public Action Alliance;
            public Action AllianceHelp;

            /// <summary>Helps every open request of a kind (HelpKinds.Heal or HelpKinds.General): the floating help bubbles.</summary>
            public Action<string> HelpAll;
            public Action Chat;
            public Action Research;
            public Action Expand;
            public Action Settings;

            /// <summary>The header avatar: the player's own profile (avatar, settings).</summary>
            public Action Profile;
            public Action Quests;
            public Action Bag;
            public Action<string> ClaimQuest;
            public Action<string> Building;
            public Action<string, Color> Toast;
            public Func<bool> SheetOpen;
            public Action Territory;

            /// <summary>The event centre (docs/29).</summary>
            public Action Events;

            /// <summary>VIP page and the Hadalite shop.</summary>
            public Action Vip;
            public Action Shop;

            /// <summary>The mailbox (docs/29 §2.1).</summary>
            public Action Mail;

            /// <summary>The war window: shield, war log, the alliance's attacks and defences (docs/30).</summary>
            public Action War;

            /// <summary>The Depth Map campaign (docs/rehber 08 §5).</summary>
            public Action Expedition;
            public Func<bool> InWorld;
        }

        private const float TicksPerMinute = 60f / SimulationTick.IntervalSeconds;

        // Last Asylum-style header: four floating resource pills on top, power and the rest under them.
        private static readonly ResourceType[] RowOne = { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Food, ResourceType.Energy };
        private static readonly ResourceType[] RowTwo = { ResourceType.Oxygen, ResourceType.Alloy };

        private IStateSyncService _stateSync;
        private Actions _actions;
        private static GameCatalog _catalog => ClientCatalog.Catalog;

        private Canvas _canvas;
        private RectTransform _safe;
        private GameObject _lower; // everything below the header; hidden while a panel covers it
        private Text _level;
        private RectTransform _avatarSlot;
        private int _avatarVersion = -1;
        private Text _power;
        private Text _hadalite;
        private readonly Dictionary<ResourceType, Text> _chips = new();
        private Text _podLabel;
        private GameObject _podDot;
        private GameObject _heroDot;
        private GameObject _allianceDot;
        private GameObject _warDot;
        private GameObject _helpDot;
        private GameObject _questDot;
        private Image _territoryIcon;
        private Text _territoryLabel;
        private bool _shownInWorld;
        private GameObject _bagDot;
        private GameObject _mailDot;
        private GameObject _eventsDot;
        private Text _vipLabel;
        private GameObject _vipDot;
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
            BuildHelpBubbles();
            BuildTask();
            BuildBottomBar();
        }

        private void BuildHeader()
        {
            // Pinned to the very top of the screen (not the safe area), like the genre: no dead band above it.
            var header = UiKit.Rect("Header", _canvas.transform).Place(0, 0.91f, 1, 1);
            header.offsetMax = new Vector2(0, -6);

            // Avatar with the base level under it; opens settings and the profile.
            var avatar = Tap(header, "Avatar", 0.012f, 0.26f, 0.135f, 0.98f, _actions.Profile ?? _actions.Settings, new Color(0.05f, 0.12f, 0.16f, 0.95f));
            Frame(avatar, UiKit.Accent);
            _avatarSlot = UiKit.Rect("Face", avatar).Place(0.06f, 0.06f, 0.94f, 0.94f);
            var levelBox = Skinned(UiKit.Box("Level", header, new Color(0.05f, 0.3f, 0.12f, 0.95f)), "btn_green");
            levelBox.rectTransform.Place(0.022f, 0.04f, 0.125f, 0.26f);
            levelBox.raycastTarget = false;
            _level = Outlined(UiKit.Label("Text", levelBox.transform, string.Empty, 26, UiKit.Amber, TextAnchor.MiddleCenter, FontStyle.Bold));
            _level.rectTransform.Place(0, 0, 1, 1);

            // Resource chips: three on the first row, two plus power on the second.
            const float left = 0.15f, step = 0.155f;
            for (var i = 0; i < RowOne.Length; i++)
                Chip(header, RowOne[i], left + i * step, 0.56f, left + (i + 1) * step - 0.008f, 0.94f);

            // Power under the resources (the number players compare), then oxygen and Superalloy (docs/rehber K-01).
            var power = Tap(header, "Power", left, 0.1f, left + 0.25f, 0.48f, () => ShowPowerInfo(), new Color(0.02f, 0.05f, 0.08f, 0.6f));
            Skinned(power.GetComponent<Image>(), "pill");
            HudIcons.Image(power, "power", -0.04f, -0.08f, 0.24f, 1.08f);
            _power = Outlined(UiKit.Label("Value", power, string.Empty, 30, UiKit.Amber, TextAnchor.MiddleLeft, FontStyle.Bold));
            _power.rectTransform.Place(0.27f, 0, 1, 1);
            for (var i = 0; i < RowTwo.Length; i++)
                Chip(header, RowTwo[i], left + 0.258f + i * step, 0.1f, left + 0.258f + (i + 1) * step - 0.008f, 0.48f);

            // Shop: Hadalite balance; opens the Cryo-Pod until the shop exists.
            var shop = Tap(header, "Shop", 0.78f, 0.5f, 0.988f, 0.95f, () => (_actions.Shop ?? _actions.Pod)?.Invoke(), new Color(0.85f, 0.42f, 0.08f, 0.95f));
            Skinned(shop.GetComponent<Image>(), "btn_gold");
            HudIcons.Image(shop, "shop", 0.02f, 0.05f, 0.42f, 0.95f);
            _hadalite = Outlined(UiKit.Label("Value", shop, string.Empty, 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            _hadalite.rectTransform.Place(0.4f, 0, 1, 1);

            // VIP under the shop: the level, a red dot when today's points or chest wait.
            var vip = Tap(header, "Vip", 0.80f, 0.1f, 0.988f, 0.46f, () => _actions.Vip?.Invoke(), new Color(0.5f, 0.12f, 0.1f, 0.95f));
            Skinned(vip.GetComponent<Image>(), "btn_red");
            HudIcons.Image(vip, "vip", 0.02f, 0.02f, 0.38f, 0.98f);
            _vipLabel = Outlined(UiKit.Label("Value", vip, "VIP 0", 28, UiKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold));
            _vipLabel.rectTransform.Place(0.36f, 0.1f, 1, 1);
            _vipDot = Dot(vip);
        }

        private void Chip(RectTransform parent, ResourceType type, float x0, float y0, float x1, float y1)
        {
            // A dark pill with the icon spilling over its left end, the value in bold white (Last Asylum).
            var chip = Tap(parent, "Chip_" + type, x0, y0, x1, y1, () => ShowResource(type), new Color(0.02f, 0.05f, 0.08f, 0.6f));
            Skinned(chip.GetComponent<Image>(), "pill");
            HudIcons.Image(chip, HudIcons.Resource(type), -0.06f, -0.12f, 0.34f, 1.12f);
            var value = Outlined(UiKit.Label("Value", chip, string.Empty, 30, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold));
            value.rectTransform.Place(0.3f, 0, 1, 1);
            _chips[type] = value;
        }

        private void BuildRightColumn()
        {
            // Event column under the shop: Cryo-Pod (free pull timer), events and sieges (coming).
            var pod = SideButton(_lower.transform, "Pod", "event_pod", Loc.T("hud.pod"), 0.835f, 0.83f, 0.985f, 0.905f, _actions.Pod);
            _podLabel = pod.Label;
            _podDot = pod.Dot;
            _eventsDot = SideButton(_lower.transform, "Events", "event_gift", Loc.T("hud.events"), 0.835f, 0.74f, 0.985f, 0.815f, () => _actions.Events?.Invoke()).Dot;
            SideButton(_lower.transform, "War", "event_war", Loc.T("hud.expedition"), 0.835f, 0.65f, 0.985f, 0.725f, () => _actions.Expedition?.Invoke());
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
            _warDot = SideButton(_lower.transform, "War", "event_war", Loc.T("pvp.hud"), 0.845f, 0.455f, 0.985f, 0.525f, () => _actions.War?.Invoke()).Dot;
            _allianceDot = SideButton(_lower.transform, "Alliance", "side_alliance", Loc.T("social.alliance"), 0.845f, 0.37f, 0.985f, 0.44f, _actions.Alliance).Dot;
            _mailDot = SideButton(_lower.transform, "Mail", "side_mail", Loc.T("hud.mail"), 0.845f, 0.285f, 0.985f, 0.355f, () => _actions.Mail?.Invoke()).Dot;
            _bagDot = SideButton(_lower.transform, "Bag", "side_bag", Loc.T("hud.bag"), 0.845f, 0.2f, 0.985f, 0.27f, _actions.Bag).Dot;
        }

        private void BuildQueue()
        {
            var title = Tap(_lower.transform, "QueueTitle", 0.012f, 0.875f, 0.45f, 0.91f, () => { _queueOpen = !_queueOpen; _nextRefresh = 0; },
                new Color(0.02f, 0.05f, 0.08f, 0.8f));
            Skinned(title.GetComponent<Image>(), "pill");
            _queueTitle = Outlined(UiKit.Label("Text", title, string.Empty, 28, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold));
            _queueTitle.rectTransform.Place(0.04f, 0, 0.98f, 1);

            _queue = UiKit.Rect("Queue", _lower.transform).Place(0.012f, 0.63f, 0.45f, 0.87f);
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
            Skinned(strip.GetComponent<Image>(), "pill");
            HudIcons.Image(strip, "quest", 0.0f, 0.0f, 0.16f, 1f);
            _task = Outlined(UiKit.Label("Text", strip, string.Empty, 26, UiKit.Text, TextAnchor.MiddleLeft));
            _task.rectTransform.Place(0.17f, 0, 0.99f, 1);
        }

        private void BuildBottomBar()
        {
            // A navy bar; Heroes and Territory are buttons the chat panel's height either side of it.
            var bar = Skinned(UiKit.Box("BottomBar", _lower.transform, new Color(0.02f, 0.06f, 0.09f, 0.94f)), "tab_off");
            bar.rectTransform.Place(0, 0, 1, 0.1f);
            bar.rectTransform.offsetMin = new Vector2(-30, -30);
            bar.rectTransform.offsetMax = new Vector2(30, 0);
            var content = UiKit.Rect("Content", bar.transform).Place(0, 0, 1, 1);
            content.offsetMin = new Vector2(30, 30);
            content.offsetMax = new Vector2(-30, 0);
            var rule = UiKit.Box("Brass", bar.transform, new Color(0.84f, 0.65f, 0.29f, 1f));
            rule.rectTransform.Place(0, 1, 1, 1);
            rule.rectTransform.offsetMin = new Vector2(0, -5);
            rule.sprite = null;
            rule.raycastTarget = false;

            var heroes = Tap(content, "Heroes", 0.012f, 0.1f, 0.2f, 0.88f, _actions.Heroes, new Color(0.06f, 0.20f, 0.30f, 1f));
            Skinned(heroes.GetComponent<Image>(), "btn_navy");
            HudIcons.Image(heroes, "nav_heroes", 0.15f, 0.36f, 0.85f, 1.02f);
            Outlined(UiKit.Label("Label", heroes, Loc.T("hud.heroes"), 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)).rectTransform.Place(0, 0.1f, 1, 0.38f);
            _heroDot = Dot(heroes);

            var chat = Tap(content, "Chat", 0.21f, 0.1f, 0.79f, 0.88f, _actions.Chat, new Color(0, 0, 0, 0.35f));
            Skinned(chat.GetComponent<Image>(), "pill");
            _chatPreview = UiKit.Label("Lines", chat, string.Empty, 24, UiKit.Text, TextAnchor.MiddleLeft);
            _chatPreview.rectTransform.Place(0.03f, 0.03f, 0.97f, 0.97f);
            _chatPreview.horizontalOverflow = HorizontalWrapMode.Overflow;
            _chatPreview.verticalOverflow = VerticalWrapMode.Truncate;
            _chatDot = Dot(chat);

            // Territory goes out to the world map; on the map the same button brings you home.
            var territory = Tap(content, "Territory", 0.8f, 0.1f, 0.988f, 0.88f, () => _actions.Territory?.Invoke(), new Color(0.06f, 0.20f, 0.30f, 1f));
            Skinned(territory.GetComponent<Image>(), "btn_navy");
            _territoryIcon = HudIcons.Image(territory, "nav_world", 0.15f, 0.36f, 0.85f, 1.02f);
            _territoryLabel = Outlined(UiKit.Label("Label", territory, Loc.T("hud.territory"), 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            _territoryLabel.rectTransform.Place(0, 0.1f, 1, 0.38f);
        }

        // --- Widgets -------------------------------------------------------------------------

        /// <summary>The chosen portrait in the header (the diver icon until one is picked).</summary>
        private void RefreshAvatar()
        {
            if (_avatarSlot == null || _avatarVersion == View.ProfileVersion)
                return;
            _avatarVersion = View.ProfileVersion;
            foreach (Transform child in _avatarSlot)
                Destroy(child.gameObject);
            var avatar = View.MyProfile?.AvatarId;
            if (string.IsNullOrEmpty(avatar) || Hadal.Core.Catalog.CharacterLibrary.Instance?.Portrait(avatar) == null)
                HudIcons.Image(_avatarSlot, "avatar", 0, 0, 1, 1).raycastTarget = false;
            else
                AvatarDisc.Draw(_avatarSlot, avatar, View.MyProfile.Name, View.PlayerId);
        }

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

        /// <summary>Puts a skin sprite (art/ui/make_skin.py) on a HUD box; without the skin it keeps its flat colour.</summary>
        private static Image Skinned(Image image, string sprite)
        {
            var skin = Skin.Get(sprite);
            if (skin == null)
                return image;
            image.sprite = skin;
            image.type = Image.Type.Sliced;
            image.color = sprite == "pill" ? new Color(1f, 1f, 1f, 0.92f) : Color.white;
            return image;
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
            _vipLabel.text = "VIP " + View.Vip.Level;
            _vipDot.SetActive(View.Vip.LoginReady || View.Vip.ChestReady);
            _level.text = Loc.F("hud.level", ReactorLevel());
            RefreshAvatar();
            _power.text = Power().ToString("N0");

            RefreshPod();
            RefreshQueue();
            RefreshTask();
            RefreshSocial();
            RefreshHelpBubbles();
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

            // Marches sit in the same queue as builders (the genre's "who is out" list); a tap opens the map.
            var marchList = View.Marches;
            var serverNow = View.ServerNowMs;
            foreach (var m in marchList?.Marches ?? Array.Empty<HADAL.Shared.DTOs.MarchDto>())
            {
                var until = serverNow < m.ArrivesAtMs ? m.ArrivesAtMs : serverNow < m.GatheredAtMs ? m.GatheredAtMs : m.HomeAtMs;
                var who = _catalog.TryGetHero(m.HeroId, out var leader) ? Loc.T("hero." + leader.Id) : Loc.T("march.divers");
                rows.Add(("queue_drone", $"{who} · {Loc.T("node." + m.NodeType)} {m.NodeLevel}", (until - serverNow) / 1000f, "march"));
            }

            var builders = Math.Max(1, _catalog.Progression.builderSlots) + Modifiers().ExtraBuilders;
            var building = Buildings().Count(b => b.IsUnderConstruction);
            var idle = rows.Count == 0;
            var marchCount = marchList?.Marches?.Length ?? 0;
            var marchText = marchList != null && marchList.Queues > 0 ? $"   {Loc.F("march.queue", marchCount, marchList.Queues)}" : string.Empty;
            _queueTitle.text = $"{(_queueOpen ? "▼" : "▶")} {Loc.T("hud.queue")}  {building}/{builders}{marchText}" +
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
                    if (key == "march")
                        _actions.Territory?.Invoke();
                    else if (key.Length > 0)
                        _actions.Building?.Invoke(key);
                    else
                        _actions.Research?.Invoke();
                }, new Color(0.03f, 0.10f, 0.14f, 0.85f));
                Skinned(rect.GetComponent<Image>(), "pill");
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
            // The alliance button also lights for alliance gifts waiting to be collected (docs/31 step 5).
            var helpWaiting = View.MyAlliance != null && View.HelpRequests.Any(r => !r.HelpedByMe);
            var giftsWaiting = View.MyAlliance != null && View.AllianceGifts.Gifts.Any(g => !g.Collected && g.ExpiresAtMs > View.ServerNowMs);
            _allianceDot.SetActive(helpWaiting || giftsWaiting);
            _helpDot.SetActive(helpWaiting);
            var nowMs = View.ServerNowMs;
            _warDot.SetActive((View.Pvp?.Incoming ?? Array.Empty<IncomingDto>()).Any(i => i.ArrivesAtMs > nowMs));
            _eventsDot.SetActive(View.Events.Events.Any(e => EventRules.HasClaim(e, nowMs)));
            _chatDot.SetActive(View.UnreadChat > 0);
            _mailDot.SetActive(View.Mail.Unread > 0 || View.Mail.Unclaimed > 0);

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
            VipRules.Apply(modifiers, View.Vip.Level);
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
