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

            /// <summary>The first-session guide's "Go" for its current step (docs/34).</summary>
            public Action Guide;
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

            /// <summary>The survival loop (docs/35): repair the running emergency, ask the alliance to help the repair.</summary>
            public Action<string> RepairEmergency;
            public Action<string> AskRepairHelp;
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
        private GameObject _queueDiscs;
        private Text _buildersCount;
        private Text _researchCount;
        private Text _marchCount;
        private GameObject _buildersIdle;
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
            BuildSurvivalAlarm();
            BuildBottomBar();
            RegisterGuideTargets();
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
            TrackChip(type, chip);
        }

        private void BuildRightColumn()
        {
            // Event grid under the shop, two columns like the genre's: Cryo-Pod (free pull timer), events, expedition.
            var pod = Disc(_lower.transform, "Pod", "event_pod", Loc.T("hud.pod"), 0.86f, 0.79f, 0.98f, 0.865f, _actions.Pod);
            _podLabel = pod.Label;
            _podDot = pod.Dot;
            _eventsDot = Disc(_lower.transform, "Events", "event_gift", Loc.T("hud.events"), 0.725f, 0.79f, 0.845f, 0.865f, () => _actions.Events?.Invoke()).Dot;
            Disc(_lower.transform, "War", "event_war", Loc.T("hud.expedition"), 0.86f, 0.705f, 0.98f, 0.78f, () => _actions.Expedition?.Invoke());
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

            Keep(Disc(_lower.transform, "Expand", "left_expand", Loc.T("hud.expand"), 0.015f, 0.455f, 0.135f, 0.535f, _actions.Expand));
            _helpDot = Keep(Disc(_lower.transform, "Help", "left_help", Loc.T("hud.help"), 0.015f, 0.365f, 0.135f, 0.445f, _actions.AllianceHelp)).Dot;
            _questDot = Keep(Disc(_lower.transform, "Quests", "quest", Loc.T("hud.quests"), 0.015f, 0.275f, 0.135f, 0.355f, _actions.Quests)).Dot;
        }

        private void BuildLowerRight()
        {
            // Navy tiles with the caption inside (Last Asylum's Alliance / Mail / Bag), stacked above the task strip.
            _warDot = Tile(_lower.transform, "War", "event_war", Loc.T("pvp.hud"), 0.835f, 0.455f, 0.985f, 0.522f, () => _actions.War?.Invoke());
            _allianceDot = Tile(_lower.transform, "Alliance", "side_alliance", Loc.T("social.alliance"), 0.835f, 0.38f, 0.985f, 0.447f, _actions.Alliance);
            _mailDot = Tile(_lower.transform, "Mail", "side_mail", Loc.T("hud.mail"), 0.835f, 0.305f, 0.985f, 0.372f, () => _actions.Mail?.Invoke());
            _bagDot = Tile(_lower.transform, "Bag", "side_bag", Loc.T("hud.bag"), 0.835f, 0.23f, 0.985f, 0.297f, _actions.Bag);
        }

        private void BuildQueue()
        {
            // Under the avatar, Last Asylum's counters: builders busy / total, research, marches out. The builder disc
            // folds the running list beside the column open and shut.
            var discs = UiKit.Rect("QueueDiscs", _lower.transform).Place(0, 0, 1, 1);
            _queueDiscs = discs.gameObject;
            var builders = Disc(discs, "Builders", "queue_build", string.Empty, 0.015f, 0.79f, 0.135f, 0.87f, () => { _queueOpen = !_queueOpen; _nextRefresh = 0; });
            _buildersCount = builders.Label;
            _buildersIdle = builders.Dot;
            _researchCount = Disc(discs, "Research", "left_research", string.Empty, 0.015f, 0.7f, 0.135f, 0.78f, _actions.Research).Label;
            _marchCount = Disc(discs, "Marches", "queue_drone", string.Empty, 0.015f, 0.61f, 0.135f, 0.69f, () => _actions.Territory?.Invoke()).Label;

            _queue = UiKit.Rect("Queue", discs).Place(0.15f, 0.65f, 0.55f, 0.868f);
        }

        private void BuildTask()
        {
            var strip = Tap(_lower.transform, "Task", 0.0f, 0.118f, 0.8f, 0.182f, () =>
            {
                if (_taskDone && _taskQuest.Length > 0)
                    _actions.ClaimQuest?.Invoke(_taskQuest);
                else if (_taskGuide)
                    _actions.Guide?.Invoke();
                else if (_taskQuest.Length > 0)
                    _actions.Quests?.Invoke();
                else if (_taskBuilding.Length > 0)
                    _actions.Building?.Invoke(_taskBuilding);
            }, new Color(0, 0, 0, 0.6f));
            // No box: the scroll and two outlined lines over a fade, so the base shows through (Last Asylum).
            Skinned(strip.GetComponent<Image>(), "hud_fade").type = Image.Type.Simple;
            HudIcons.Image(strip, "quest", -0.01f, -0.08f, 0.15f, 1.08f);
            _task = Outlined(UiKit.Label("Text", strip, string.Empty, 27, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold));
            _task.rectTransform.Place(0.165f, 0, 0.99f, 1);
            _task.lineSpacing = 0.9f;
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
            _chatPreview = UiKit.EmojiLabel("Lines", chat, string.Empty, 24, UiKit.Text, TextAnchor.MiddleLeft);
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

        /// <summary>
        /// A porthole disc with the icon in it and an outlined caption under it (the left and event columns). With an
        /// empty caption the label sits on the disc's lower rim instead, for a counter like "1/3".
        /// </summary>
        private static Side Disc(Transform parent, string name, string icon, string caption, float x0, float y0, float x1, float y1, Action onClick)
        {
            var root = Tap(parent, name, x0, y0, x1, y1, onClick, new Color(0, 0, 0, 0.001f));
            var counter = caption.Length == 0;
            var disc = Skinned(UiKit.Box("Disc", root, new Color(0.05f, 0.09f, 0.15f, 0.9f)), "hud_disc");
            disc.type = Image.Type.Simple;
            disc.preserveAspect = true;
            disc.raycastTarget = false;
            disc.rectTransform.Place(0.04f, counter ? 0.04f : 0.26f, 0.96f, 1f);
            HudIcons.Image(disc.transform, icon, 0.17f, 0.17f, 0.83f, 0.83f);
            var label = Outlined(UiKit.Label("Caption", root, caption, counter ? 26 : 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold));
            label.rectTransform.Place(-0.2f, counter ? 0.02f : 0f, 1.2f, counter ? 0.3f : 0.27f);
            return new Side(label, Dot(root));
        }

        /// <summary>A navy tile with the icon on top and the caption inside its lower edge; returns its badge.</summary>
        private static GameObject Tile(Transform parent, string name, string icon, string caption, float x0, float y0, float x1, float y1, Action onClick)
        {
            var root = Tap(parent, name, x0, y0, x1, y1, onClick, new Color(0.1f, 0.16f, 0.26f, 0.95f));
            Skinned(root.GetComponent<Image>(), "hud_tile");
            HudIcons.Image(root, icon, 0.16f, 0.3f, 0.84f, 0.96f);
            Outlined(UiKit.Label("Caption", root, caption, 23, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)).rectTransform.Place(0.02f, 0.05f, 0.98f, 0.32f);
            return Dot(root);
        }

        /// <summary>A red badge on the top-right corner; <see cref="Count"/> puts a number in it.</summary>
        private static GameObject Dot(Transform parent)
        {
            var dot = Skinned(UiKit.Box("Dot", parent, new Color(0.95f, 0.18f, 0.15f, 1f)), "hud_badge");
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.9f, 0.93f);
            dot.rectTransform.sizeDelta = new Vector2(30, 30);
            dot.raycastTarget = false;
            var count = UiKit.Label("Count", dot.transform, string.Empty, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            count.rectTransform.Place(0, 0.04f, 1, 1);
            count.raycastTarget = false;
            dot.gameObject.SetActive(false);
            return dot.gameObject;
        }

        /// <summary>Shows a badge with a number (99+ caps it), or hides it at zero.</summary>
        private static void Count(GameObject dot, int count)
        {
            dot.SetActive(count > 0);
            if (count <= 0)
                return;
            var text = count > 99 ? "99+" : count.ToString();
            dot.GetComponentInChildren<Text>().text = text;
            ((RectTransform)dot.transform).sizeDelta = new Vector2(text.Length == 1 ? 42 : 30 + 16 * text.Length, 42);
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
                _queueDiscs.SetActive(!inWorld);
                _task.transform.parent.gameObject.SetActive(!inWorld);
                foreach (var go in _baseOnly)
                    go.SetActive(!inWorld);
            }

            PulseSurvival();

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
            RefreshLocks();
            RefreshSurvival();
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
                // Attack marches carry a monster kind, gathering marches a node type.
                var target = m.Kind == "attack" ? WorldMapView.MonsterName(m.NodeType) : WorldMapView.NodeName(m.NodeType);
                rows.Add(("queue_drone", $"{who} · {target} {m.NodeLevel}", (until - serverNow) / 1000f, "march"));
            }

            var builders = Math.Max(1, _catalog.Progression.builderSlots) + Modifiers().ExtraBuilders;
            var building = Buildings().Count(b => b.IsUnderConstruction);
            var marchCount = marchList?.Marches?.Length ?? 0;
            _buildersCount.text = $"{building}/{builders}";
            _buildersCount.color = building < builders ? UiKit.Amber : Color.white;
            // An idle builder is the genre's most nagged-about slot: a dot until one is put to work.
            _buildersIdle.SetActive(building < builders);
            _researchCount.text = $"{View.ActiveResearch.Count}/1";
            _marchCount.text = marchList != null && marchList.Queues > 0 ? $"{marchCount}/{marchList.Queues}" : $"{marchCount}";

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
            if (RefreshGuideTask())
                return;

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
                || (quests.Guide != null && quests.Guide.Progress >= quests.Guide.Target)
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
            Count(_warDot, (View.Pvp?.Incoming ?? Array.Empty<IncomingDto>()).Count(i => i.ArrivesAtMs > nowMs));
            _eventsDot.SetActive(View.Events.Events.Any(e => EventRules.HasClaim(e, nowMs)));
            _chatDot.SetActive(View.UnreadChat > 0);
            Count(_mailDot, Math.Max(View.Mail.Unread, View.Mail.Unclaimed));

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
            // A shared location shows as its words and coordinates, not the raw "@loc|…" it travels as.
            var text = (SocialView.PinPreview(line.Text) ?? line.Text ?? string.Empty).Replace("<", "<​");
            return $"<color=#FFB74D>{tag}{(line.SenderName ?? string.Empty).Replace("<", "<​")}</color>: {text}";
        }

        // --- Popups --------------------------------------------------------------------------

        private void ShowResource(ResourceType type)
        {
            if (ShowSurvivalFor(type))
                return;

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
