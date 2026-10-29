using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.Network;
using Hadal.Core.State;
using Hadal.Core.StateSync;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The outer world (docs/28_World_Map.md): a seabed far away from the base with every player's base on
    /// its map tile, a name plate ("[TAG] Name" and level) over each, its own camera (drag to pan, pinch to
    /// zoom) and a tap for details. Areas load from the server as the camera moves.
    /// </summary>
    public sealed partial class WorldMapView : MonoBehaviour
    {
        private static readonly Vector3 Origin = new(10000f, 0f, 0f);
        private const float Tile = 8f;
        private const float FloorTile = 20f;
        private const int FloorRadius = 6;

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private Action<string, Color> _toast;
        private ulong _sequence;

        private Transform _root;
        private Camera _camera;
        private Camera _baseCamera;
        private Canvas _canvas;
        private RectTransform _labels;
        private Text _coords;
        private GameObject _overlay;
        private GameObject _popup;
        private Canvas _popupCanvas;
        private float _baseFogDensity;
        private readonly List<Transform> _floor = new();
        private readonly Dictionary<string, (GameObject Model, RectTransform Plate)> _bases = new();
        private readonly Dictionary<(int, int), GameObject> _decor = new();

        private Vector3 _focus;
        private float _distance = 90f;
        private Vector2Int _requested = new(int.MinValue, int.MinValue);
        private float _nextViewRefresh;
        private const float ViewRefreshSeconds = 8f;
        private int _renderedVersion = -1;
        private Vector3 _lastPointer;
        private bool _dragging;
        private bool _gestureOnUi;
        private float _lastPinch;
        private Vector2 _downAt;

        public bool IsOpen { get; private set; }

        /// <summary>Opens a private conversation with a base's owner (id, name); set by BaseManagementView.</summary>
        public Action<string, string> MessagePlayer;

        /// <summary>Shares a tile in chat: x, y, kind ("base", "enemy", "node", "me") and a label.</summary>
        public Action<int, int, string, string> ShareLocation;

        private ClientStateView View => _stateSync.View;

        public void Initialize(IStateSyncService stateSync, ICommandDispatcher commands, Action<string, Color> toast, Hadal.Core.Events.ILocalEventBus localBus = null)
        {
            _stateSync = stateSync;
            _commands = commands;
            _toast = toast;
            InitializeNodes(localBus);
        }

        // --- Enter and leave -----------------------------------------------------------------

        /// <summary>Out to the world behind the curtain (dark, logo, the world appears); <paramref name="then"/> runs once it is open.</summary>
        public void Open(Action then = null)
        {
            if (IsOpen)
            {
                then?.Invoke();
                return;
            }

            SceneCurtain.ToWorld(() =>
            {
                OpenNow();
                then?.Invoke();
            });
        }

        /// <summary>Back to the base with the curtain (Territory button, "Enter base").</summary>
        public void CloseToBase()
        {
            if (IsOpen)
                SceneCurtain.ToBase(() =>
                {
                    Close();
                    Hadal.Core.State.CameraFocus.Settle();
                });
        }

        private void OpenNow()
        {
            if (IsOpen)
                return;
            Build();
            IsOpen = true;
            GameMode.InWorld = true;
            Hadal.Core.Services.GameAudio.Play(Hadal.Core.Services.Sfx.SonarPing, 0.7f);

            _baseCamera = Camera.main;
            if (_baseCamera != null && _baseCamera != _camera)
                _baseCamera.enabled = false;
            _camera.enabled = true;
            _root.gameObject.SetActive(true);

            // The base's thick fog hides everything at map distance; thin it while the map is open.
            _baseFogDensity = RenderSettings.fogDensity;
            RenderSettings.fogDensity = 0.0035f;
            _canvas.gameObject.SetActive(true);

            // Start over our own base; the first view tells us where it is.
            Request(-1, -1);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            GameMode.InWorld = false;
            _camera.enabled = false;
            if (_baseCamera != null)
                _baseCamera.enabled = true;
            RenderSettings.fogDensity = _baseFogDensity;
            _root.gameObject.SetActive(false);
            _canvas.gameObject.SetActive(false);
            ClosePopup();
            TargetMenu.Hide();
        }

        /// <summary>Opens the map over a shared location (chat pins, profile cards).</summary>
        public void OpenAt(int x, int y)
        {
            Open(() =>
            {
                _focus = TileToWorld(x, y);
                Request(x, y);
            });
        }

        public void GoHome()
        {
            var view = View.WorldView;
            if (view == null)
                return;
            _focus = TileToWorld(view.MyX, view.MyY);
            Request(view.MyX, view.MyY);
        }

        // --- Building the scene --------------------------------------------------------------

        private void Build()
        {
            if (_root != null)
                return;

            _root = new GameObject("WorldMap").transform;
            _root.SetParent(transform, false);
            _root.position = Origin;

            var cameraGo = new GameObject("WorldCamera");
            cameraGo.transform.SetParent(_root, false);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.010f, 0.045f, 0.070f);
            _camera.fieldOfView = 45f;
            _camera.nearClipPlane = 1f;
            _camera.farClipPlane = 400f;
            _camera.enabled = false;

            var floorRoot = new GameObject("Floor").transform;
            floorRoot.SetParent(_root, false);
            var tile = CharacterLibrary.Instance?.Model("env_seabed_tile");
            for (var x = -FloorRadius; x <= FloorRadius; x++)
            {
                for (var z = -FloorRadius; z <= FloorRadius; z++)
                {
                    var t = tile != null ? Instantiate(tile, floorRoot).transform : GameObject.CreatePrimitive(PrimitiveType.Plane).transform;
                    t.SetParent(floorRoot, false);
                    _floor.Add(t);
                }
            }

            // Name plates and the map's own controls.
            _canvas = UiKit.Canvas("WorldUi", 34);
            _canvas.transform.SetParent(transform, false);
            _labels = UiKit.Rect("Labels", _canvas.transform).Place(0, 0, 1, 1);
            var safe = UiKit.Rect("Safe", _canvas.transform).Place(0, 0, 1, 1);
            safe.gameObject.AddComponent<Hud.SafeArea>();
            _overlay = safe.gameObject;

            var coordsBox = UiKit.Box("Coords", safe, new Color(0.02f, 0.05f, 0.08f, 0.85f));
            coordsBox.rectTransform.Place(0.2f, 0.105f, 0.52f, 0.145f);
            // The kingdom badge (docs/39): "Krallık #n · X Y"; a tap opens the kingdom list.
            coordsBox.gameObject.AddComponent<Button>().onClick.AddListener(() => KingdomList.Show(_stateSync, _commands));
            _coords = UiKit.Label("Text", coordsBox.transform, string.Empty, 26, UiKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            _coords.rectTransform.Place(0.03f, 0, 0.97f, 1);
            _coords.raycastTarget = false;
            _coords.resizeTextForBestFit = true;
            _coords.resizeTextMinSize = 16;
            _coords.resizeTextMaxSize = 26;

            var home = UiKit.Button("Home", safe, Loc.T("world.myBase"), GoHome, UiKit.Field, 26);
            home.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)home.transform).Place(0.54f, 0.105f, 0.8f, 0.145f);

            BuildMarches(safe);
            BuildSearch(safe);
            BuildHomeArrow();
            BuildGuideArrow();

            // Base details open above the HUD.
            _popupCanvas = UiKit.Canvas("WorldPopup", 46);
            _popupCanvas.transform.SetParent(transform, false);

            _root.gameObject.SetActive(false);
            _canvas.gameObject.SetActive(false);
        }

        // --- Server areas --------------------------------------------------------------------

        private void Request(int x, int y)
        {
            _requested = new Vector2Int(x, y);
            _commands.Dispatch(new ViewWorldCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = ++_sequence,
                X = x,
                Y = y
            });
        }

        private void ApplyView()
        {
            var view = View.WorldView;
            if (view == null || _renderedVersion == View.WorldVersion)
                return;
            RecentreIfTeleported(view);

            // The first answer to "around my base" moves the camera there.
            if (_requested.x < 0)
            {
                _focus = TileToWorld(view.MyX, view.MyY);
                _requested = new Vector2Int(view.CenterX, view.CenterY);
            }

            _renderedVersion = View.WorldVersion;
            var seen = new HashSet<string>();
            foreach (var b in view.Bases)
            {
                seen.Add(b.PlayerId);
                if (!_bases.TryGetValue(b.PlayerId, out var entry))
                {
                    entry = (BuildBase(b), BuildPlate());
                    _bases[b.PlayerId] = entry;
                }

                entry.Model.transform.position = TileToWorld(b.X, b.Y);
                StylePlate(entry.Plate, b, view);
            }

            // Bases outside the area are dropped only when they are far from the camera.
            foreach (var id in _bases.Keys.Where(k => !seen.Contains(k)).ToList())
            {
                var (model, plate) = _bases[id];
                if (Vector3.Distance(model.transform.position, _focus) < (view.Radius - 2) * Tile)
                    continue;
                Destroy(model);
                Destroy(plate.gameObject);
                _bases.Remove(id);
            }

            ApplyNodes(view);
            ApplyMonsters(view);
        }

        private GameObject BuildBase(WorldBaseDto b)
        {
            var root = new GameObject("Base_" + b.PlayerId);
            root.transform.SetParent(_root, false);
            var library = CharacterLibrary.Instance;
            void Part(string id, Vector3 offset, float scale)
            {
                var prefab = library?.Model(id);
                if (prefab == null)
                    return;
                var part = Instantiate(prefab, root.transform);
                part.transform.localPosition = offset;
                part.transform.localScale = Vector3.one * scale;
            }

            // Bigger bases look bigger: a reactor, then domes and towers as the reactor grows.
            Part("bld_core_reactor", Vector3.zero, 1.5f);
            if (b.Level >= 3)
                Part("bld_oxygen_plant", new Vector3(3.4f, 0, -1.2f), 1f);
            if (b.Level >= 5)
                Part("bld_mega_dome", new Vector3(-3.6f, 0, 0.8f), 0.9f);
            if (b.Level >= 8)
                Part("bld_shield_generator", new Vector3(0.8f, 0, 3.6f), 1f);
            if (b.Level >= 12)
                Part("bld_radar_center", new Vector3(-1.8f, 0, -3.6f), 0.9f);
            return root;
        }

        private RectTransform BuildPlate()
        {
            var plate = UiKit.Box("Plate", _labels, new Color(0.02f, 0.05f, 0.08f, 0.8f));
            plate.rectTransform.sizeDelta = new Vector2(340, 70);
            var tap = plate.gameObject.AddComponent<Button>();
            tap.targetGraphic = plate;
            var level = UiKit.Box("Level", plate.transform, UiKit.Amber);
            level.rectTransform.Place(0f, 0f, 0.2f, 1f);
            level.raycastTarget = false;
            var levelText = UiKit.Label("Text", level.transform, string.Empty, 28, UiKit.Abyss, TextAnchor.MiddleCenter, FontStyle.Bold);
            levelText.rectTransform.Place(0, 0, 1, 1);
            var name = UiKit.Label("Name", plate.transform, string.Empty, 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.rectTransform.Place(0.23f, 0f, 0.98f, 1f);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            return plate.rectTransform;
        }

        private void StylePlate(RectTransform plate, WorldBaseDto b, WorldViewDto view)
        {
            var me = b.PlayerId == View.PlayerId;
            var myTag = View.MyAlliance?.Tag ?? string.Empty;
            var ally = !me && myTag.Length > 0 && b.AllianceTag == myTag;
            var tag = string.IsNullOrEmpty(b.AllianceTag) ? string.Empty : $"[{b.AllianceTag}] ";
            var color = me ? "#38D6F2" : ally ? "#7FB2FF" : "#FFFFFF";
            plate.Find("Name").GetComponent<Text>().text = $"<color={color}>{tag}{Escape(b.Name)}</color>{(b.Online ? " <color=#5BE37A>●</color>" : string.Empty)}";
            plate.Find("Level/Text").GetComponent<Text>().text = b.Level.ToString();
            plate.GetComponent<Image>().color = me ? new Color(0.02f, 0.2f, 0.26f, 0.9f) : new Color(0.02f, 0.05f, 0.08f, 0.8f);

            var button = plate.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            var info = b;
            button.onClick.AddListener(() => ShowBase(info, me));
        }

        /// <summary>A base on the map: the round menu with name, level and power; message or invite others (Last Asylum pattern).</summary>
        private void ShowBase(WorldBaseDto b, bool me)
        {
            var tag = string.IsNullOrEmpty(b.AllianceTag) ? string.Empty : $"[{b.AllianceTag}] ";
            var info = $"{Loc.F("world.level", b.Level)}   {Loc.F("hero.power", b.Power.ToString("N0"))}\n" +
                       $"{Loc.F("world.coords", b.X, b.Y)}   {(b.Online ? Loc.T("world.online") : Loc.T("world.offline"))}";
            var items = new List<TargetMenu.Item>();
            var target = b;
            if (me)
            {
                items.Add(new TargetMenu.Item("left_home", Loc.T("world.enter"), CloseToBase));
                items.Add(new TargetMenu.Item("avatar", Loc.T("profile.title"), () => ProfileCard.Instance?.ShowMine()));
                items.Add(new TargetMenu.Item("chat", Loc.T("share.title"), () => ShareLocation?.Invoke(target.X, target.Y, "me", target.Name)));
                items.Add(new TargetMenu.Item("nav_world", Loc.T("world.teleport.random"), () => Teleport(TeleportRules.Random, 0, 0, string.Empty)));
            }
            else
            {
                var ally = !string.IsNullOrEmpty(b.AllianceTag) && b.AllianceTag == View.MyAlliance?.Tag;
                items.Add(new TargetMenu.Item("avatar", Loc.T("profile.title"), () => ProfileCard.Instance?.Show(target.PlayerId, target.Name, target.AvatarId)));
                items.Add(new TargetMenu.Item("chat", Loc.T("alliance.message"), () => MessagePlayer?.Invoke(target.PlayerId, target.Name)));
                items.Add(new TargetMenu.Item("nav_world", Loc.T("share.title"), () => ShareLocation?.Invoke(target.X, target.Y, ally ? "base" : "enemy",
                    (string.IsNullOrEmpty(target.AllianceTag) ? string.Empty : $"[{target.AllianceTag}] ") + target.Name)));
                var canInvite = HADAL.Shared.Simulation.AlliancePermissions.CanInvite(View.MyRank()) && string.IsNullOrEmpty(b.AllianceTag);
                items.Add(new TargetMenu.Item("side_alliance", Loc.T("alliance.invite"), () => _commands.Dispatch(new InviteToAllianceCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, PlayerId = target.PlayerId
                }), canInvite));
                if (ally)
                    items.Add(new TargetMenu.Item("nav_world", Loc.T("world.teleport.ally"), () => Teleport(TeleportRules.Alliance, 0, 0, target.PlayerId)));
            }

            if (!me)
            {
                var ally = !string.IsNullOrEmpty(b.AllianceTag) && b.AllianceTag == View.MyAlliance?.Tag;
                AddPvpItems(items, b, ally);
                // Greyed-out attack buttons say why (live feedback: "it wouldn't let me attack you").
                if (!ally && PvpBlock(b) is { } block)
                    info += $"\n<color=#FFB347>{block}</color>";
            }
            else if (b.ShieldUntilMs > View.ServerNowMs)
                info += $"\n<color=#7FE9FF>🛡 {Loc.F("pvp.shield.plate", Clock(b.ShieldUntilMs - View.ServerNowMs))}</color>";

            GlideTo(b.X, b.Y);
            var x = b.X;
            var y = b.Y;
            TargetMenu.Show(() => IsOpen ? _camera : null, () => TileToWorld(x, y) + Vector3.up * 3f, tag + Escape(b.Name), info, items);
        }

        private Vector3? _glide;

        /// <summary>The camera slides to a tapped tile.</summary>
        private void GlideTo(int x, int y) => _glide = TileToWorld(x, y);

        private void ClosePopup()
        {
            if (_popup != null)
                Destroy(_popup);
            _popup = null;
        }

        private static string Escape(string text) => (text ?? string.Empty).Replace("<", "<​");

        // --- Frame -----------------------------------------------------------------------------

        private void Update()
        {
            if (!IsOpen || _camera == null)
                return;

            ApplyView();
            UpdateShields();
            HandleSeaTap();
            TickTeamDetails();
            // Other players' teams start and turn back all the time: ask for the area again every few seconds.
            if (_requested.x >= 0 && Time.unscaledTime >= _nextViewRefresh)
            {
                _nextViewRefresh = Time.unscaledTime + ViewRefreshSeconds;
                Request(_requested.x, _requested.y);
            }

            HandleInput();
            if (_glide is { } glide)
            {
                _focus = Vector3.Lerp(_focus, glide, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
                if ((_focus - glide).sqrMagnitude < 0.01f || _dragging)
                    _glide = null;
            }

            UpdateHomeArrow();
            UpdateGuideArrow();

            var pitch = Quaternion.Euler(55f, 20f, 0f);
            _camera.transform.SetPositionAndRotation(_focus - pitch * Vector3.forward * _distance, pitch);

            // The floor follows the camera in whole tiles so the seabed never ends.
            var cx = Mathf.Round((_focus.x - Origin.x) / FloorTile);
            var cz = Mathf.Round((_focus.z - Origin.z) / FloorTile);
            var i = 0;
            for (var x = -FloorRadius; x <= FloorRadius; x++)
            {
                for (var z = -FloorRadius; z <= FloorRadius; z++)
                {
                    var t = _floor[i++];
                    t.position = Origin + new Vector3((cx + x) * FloorTile, -0.05f, (cz + z) * FloorTile);
                    t.rotation = Quaternion.Euler(0f, (((int)(cx + x) * 7 + (int)(cz + z) * 13) & 3) * 90f, 0f);
                }
            }

            ScatterDecor();
            UpdatePlates();
            UpdateNodePlates();
            UpdateMonsterPlates();
            UpdateSonar();
            UpdateMarches();
            UpdateTerritory();

            var tile = WorldToTile(_focus);
            _coords.text = $"{Loc.F("kingdom.number", View.MyKingdom)} · {Loc.F("world.coords", tile.x, tile.y)}";

            // Ask for a new area once the camera has travelled far from the last one.
            var view = View.WorldView;
            if (view != null && _requested.x >= 0 && (Mathf.Abs(tile.x - _requested.x) > view.Radius / 2 || Mathf.Abs(tile.y - _requested.y) > view.Radius / 2))
                Request(Mathf.Clamp(tile.x, 0, view.MapSize - 1), Mathf.Clamp(tile.y, 0, view.MapSize - 1));
        }

        private void UpdatePlates()
        {
            foreach (var (model, plate) in _bases.Values)
            {
                var screen = _camera.WorldToScreenPoint(model.transform.position + Vector3.up * 7f);
                var visible = screen.z > 0f && _popup == null;
                plate.gameObject.SetActive(visible);
                if (!visible)
                    continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_labels, screen, null, out var local);
                plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
                plate.anchoredPosition = local;
            }
        }

        /// <summary>Rocks, kelp and crystals on a fixed pattern per map cell, so the same place always looks the same.</summary>
        private void ScatterDecor()
        {
            var library = CharacterLibrary.Instance;
            if (library == null)
                return;
            var center = WorldToTile(_focus);
            const int cell = 4;
            var cx = Mathf.FloorToInt(center.x / (float)cell);
            var cy = Mathf.FloorToInt(center.y / (float)cell);
            var keep = new HashSet<(int, int)>();
            for (var x = cx - 5; x <= cx + 5; x++)
            {
                for (var y = cy - 5; y <= cy + 5; y++)
                {
                    keep.Add((x, y));
                    if (_decor.ContainsKey((x, y)))
                        continue;
                    var hash = (x * 73856093) ^ (y * 19349663);
                    var kinds = new[] { "env_rock_large", "env_rock_medium", "env_kelp_cluster", "env_crystal_cluster", "env_coral_branch", null, null };
                    var kind = kinds[Mathf.Abs(hash) % kinds.Length];
                    var prefab = kind == null ? null : library.Model(kind);
                    if (prefab == null)
                    {
                        _decor[(x, y)] = null;
                        continue;
                    }

                    var go = Instantiate(prefab, _root);
                    // Off the base tiles (multiples of the spacing): a decoration sits between bases.
                    var ox = 1.5f + Mathf.Abs(hash >> 3) % 10 / 10f;
                    var oy = 1.5f + Mathf.Abs(hash >> 7) % 10 / 10f;
                    go.transform.position = TileToWorld(x * cell, y * cell) + new Vector3(ox * Tile, 0f, oy * Tile);
                    go.transform.rotation = Quaternion.Euler(0f, Mathf.Abs(hash) % 360, 0f);
                    go.transform.localScale = Vector3.one * (1.2f + Mathf.Abs(hash >> 11) % 10 / 10f);
                    _decor[(x, y)] = go;
                }
            }

            foreach (var key in _decor.Keys.Where(k => !keep.Contains(k)).ToList())
            {
                if (_decor[key] != null)
                    Destroy(_decor[key]);
                _decor.Remove(key);
            }
        }

        private void HandleInput()
        {
            var down = Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
            if (down)
            {
                _gestureOnUi = PointerOverUi();
                _downAt = Input.mousePosition;
            }

            if (Input.touchCount == 0 && !Input.GetMouseButton(0))
            {
                _gestureOnUi = false;
                _dragging = false;
            }

            if (_gestureOnUi || _popup != null)
                return;

            if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0);
                var b = Input.GetTouch(1);
                var pinch = Vector2.Distance(a.position, b.position);
                if (b.phase == TouchPhase.Began)
                    _lastPinch = pinch;
                _distance = Mathf.Clamp(_distance * _lastPinch / Mathf.Max(1f, pinch), MinDistance, MaxDistance);
                _lastPinch = pinch;
                _dragging = false;
            }
            else if (Input.GetMouseButton(0))
            {
                var pointer = Input.mousePosition;
                if (_dragging)
                {
                    var scale = _distance / Mathf.Max(1f, Screen.height) * 1.8f;
                    var delta = pointer - _lastPointer;
                    _focus += Quaternion.Euler(0f, 20f, 0f) * new Vector3(-delta.x, 0f, -delta.y) * scale;
                    var size = (View.WorldView?.MapSize ?? 200) * Tile;
                    _focus = new Vector3(Mathf.Clamp(_focus.x, Origin.x, Origin.x + size), 0f, Mathf.Clamp(_focus.z, Origin.z, Origin.z + size));
                }

                _lastPointer = pointer;
                _dragging = true;
            }

            var wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)
                _distance = Mathf.Clamp(_distance * (1f - wheel * 0.1f), MinDistance, MaxDistance);
        }

        private static bool PointerOverUi()
        {
            var events = EventSystem.current;
            if (events == null)
                return false;
            return Input.touchCount > 0 ? events.IsPointerOverGameObject(Input.GetTouch(0).fingerId) : events.IsPointerOverGameObject();
        }

        // --- Home arrow (Last Asylum): always points at the player's base while it is off screen -----

        private RectTransform _homeArrow;
        private RectTransform _homePointer;
        private Text _homeDistance;

        private void BuildHomeArrow()
        {
            _homeArrow = UiKit.Rect("HomeArrow", _labels);
            _homeArrow.sizeDelta = new Vector2(130f, 130f);
            var disc = _homeArrow.gameObject.AddComponent<Image>();
            disc.sprite = UiKit.Circle;
            disc.color = new Color(0.13f, 0.52f, 0.86f, 0.95f);
            _homeArrow.gameObject.AddComponent<Button>().onClick.AddListener(GoHome);
            _homeArrow.gameObject.AddComponent<PressFeedback>();
            Hud.HudIcons.Image(_homeArrow, "left_home", 0.2f, 0.32f, 0.8f, 0.92f).raycastTarget = false;
            _homeDistance = UiKit.Label("Distance", _homeArrow, string.Empty, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _homeDistance.rectTransform.Place(0f, 0.02f, 1f, 0.34f);

            // A small triangle on the rim of the disc turns towards the base.
            _homePointer = UiKit.Rect("Pointer", _homeArrow);
            _homePointer.sizeDelta = new Vector2(44f, 44f);
            var tip = UiKit.Label("Tip", _homePointer, "▲", 40, new Color(0.13f, 0.52f, 0.86f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            tip.rectTransform.Place(0, 0, 1, 1);
            _homeArrow.gameObject.SetActive(false);
        }

        private void UpdateHomeArrow()
        {
            var view = View.WorldView;
            if (_homeArrow == null || view == null)
                return;

            var home = TileToWorld(view.MyX, view.MyY);
            var screen = _camera.WorldToScreenPoint(home);
            var onScreen = screen.z > 0f && screen.x > 0f && screen.y > 0f && screen.x < Screen.width && screen.y < Screen.height;
            _homeArrow.gameObject.SetActive(!onScreen && _popup == null && !TargetMenu.IsOpen);
            if (onScreen)
                return;

            // Direction from the screen centre to the base, clamped to an inner frame clear of the HUD.
            var centre = new Vector2(Screen.width / 2f, Screen.height / 2f);
            var dir = (Vector2)screen - centre;
            if (screen.z < 0f)
                dir = -dir;
            if (dir.sqrMagnitude < 1f)
                dir = Vector2.up;
            dir.Normalize();
            var half = _labels.rect.size / 2f;
            var frame = new Vector2(half.x - 110f, half.y - 330f);
            var scale = Mathf.Min(frame.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)), frame.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            _homeArrow.anchorMin = _homeArrow.anchorMax = new Vector2(0.5f, 0.5f);
            _homeArrow.anchoredPosition = dir * scale;
            _homePointer.anchorMin = _homePointer.anchorMax = new Vector2(0.5f, 0.5f);
            _homePointer.anchoredPosition = dir * 82f;
            _homePointer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);

            var tile = WorldToTile(_focus);
            _homeDistance.text = Loc.F("world.tiles", GatherRules.Km(tile.x, tile.y, view.MyX, view.MyY).ToString("0.#"));
        }

        private static Vector3 TileToWorld(int x, int y) => Origin + new Vector3(x * Tile, 0f, y * Tile);

        private static Vector2Int WorldToTile(Vector3 world) =>
            new(Mathf.RoundToInt((world.x - Origin.x) / Tile), Mathf.RoundToInt((world.z - Origin.z) / Tile));
    }
}
