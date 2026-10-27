using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
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
        private int _renderedVersion = -1;
        private Vector3 _lastPointer;
        private bool _dragging;
        private bool _gestureOnUi;
        private float _lastPinch;
        private Vector2 _downAt;

        public bool IsOpen { get; private set; }

        /// <summary>Opens a private conversation with a base's owner (id, name); set by BaseManagementView.</summary>
        public Action<string, string> MessagePlayer;

        private ClientStateView View => _stateSync.View;

        public void Initialize(IStateSyncService stateSync, ICommandDispatcher commands, Action<string, Color> toast, Hadal.Core.Events.ILocalEventBus localBus = null)
        {
            _stateSync = stateSync;
            _commands = commands;
            _toast = toast;
            InitializeNodes(localBus);
        }

        // --- Enter and leave -----------------------------------------------------------------

        public void Open()
        {
            if (IsOpen)
                return;
            Build();
            IsOpen = true;
            GameMode.InWorld = true;

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
            coordsBox.rectTransform.Place(0.2f, 0.19f, 0.52f, 0.23f);
            coordsBox.raycastTarget = false;
            _coords = UiKit.Label("Text", coordsBox.transform, string.Empty, 26, UiKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            _coords.rectTransform.Place(0, 0, 1, 1);

            var home = UiKit.Button("Home", safe, Loc.T("world.myBase"), GoHome, UiKit.Field, 26);
            home.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)home.transform).Place(0.54f, 0.19f, 0.8f, 0.23f);

            BuildMarches(safe);

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

        private void ShowBase(WorldBaseDto b, bool me)
        {
            ClosePopup();
            var dim = UiKit.Box("Popup", _popupCanvas.transform, new Color(0, 0, 0, 0.5f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(ClosePopup);
            _popup = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.08f, 0.36f, 0.92f, 0.64f);
            Hud.HudIcons.Image(card.transform, "left_home", 0.04f, 0.6f, 0.22f, 0.95f);
            var tag = string.IsNullOrEmpty(b.AllianceTag) ? string.Empty : $"[{b.AllianceTag}] ";
            UiKit.Label("Name", card.transform, tag + Escape(b.Name), 44, me ? UiKit.Accent : UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.25f, 0.72f, 0.96f, 0.95f);
            var lines = $"{Loc.F("world.level", b.Level)}    {Loc.F("hero.power", b.Power.ToString("N0"))}\n" +
                        $"{Loc.F("world.coords", b.X, b.Y)}    {(b.Online ? Loc.T("world.online") : Loc.T("world.offline"))}" +
                        (me ? "\n" + Loc.T("world.you") : string.Empty);
            UiKit.Label("Info", card.transform, lines, 30, UiKit.Muted, TextAnchor.UpperLeft).rectTransform.Place(0.06f, 0.3f, 0.96f, 0.68f);
            if (me)
            {
                var done = UiKit.Button("Close", card.transform, Loc.T("hero.pod.done"), ClosePopup, UiKit.Accent, 30);
                ((RectTransform)done.transform).Place(0.3f, 0.06f, 0.7f, 0.26f);
                return;
            }

            // Write to them, or bring them into the alliance (R3+ may invite players without one).
            var info = b;
            var message = UiKit.Button("Message", card.transform, Loc.T("alliance.message"), () =>
            {
                ClosePopup();
                MessagePlayer?.Invoke(info.PlayerId, info.Name);
            }, UiKit.Accent, 28);
            ((RectTransform)message.transform).Place(0.04f, 0.06f, 0.34f, 0.26f);

            var canInvite = HADAL.Shared.Simulation.AlliancePermissions.CanInvite(View.MyRank()) && string.IsNullOrEmpty(b.AllianceTag);
            var invite = UiKit.Button("Invite", card.transform, Loc.T("alliance.invite"), () => _commands.Dispatch(new InviteToAllianceCommand
            {
                CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, PlayerId = info.PlayerId
            }), canInvite ? UiKit.Accent : UiKit.Field, 28);
            invite.interactable = canInvite;
            ((RectTransform)invite.transform).Place(0.36f, 0.06f, 0.66f, 0.26f);

            var close = UiKit.Button("Close", card.transform, Loc.T("hero.pod.done"), ClosePopup, UiKit.Field, 28);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.68f, 0.06f, 0.96f, 0.26f);
        }

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
            HandleInput();

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
            UpdateMarches();

            var tile = WorldToTile(_focus);
            _coords.text = Loc.F("world.coords", tile.x, tile.y);

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
                _distance = Mathf.Clamp(_distance * _lastPinch / Mathf.Max(1f, pinch), 40f, 180f);
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
                _distance = Mathf.Clamp(_distance * (1f - wheel * 0.1f), 40f, 180f);
        }

        private static bool PointerOverUi()
        {
            var events = EventSystem.current;
            if (events == null)
                return false;
            return Input.touchCount > 0 ? events.IsPointerOverGameObject(Input.GetTouch(0).fingerId) : events.IsPointerOverGameObject();
        }

        private static Vector3 TileToWorld(int x, int y) => Origin + new Vector3(x * Tile, 0f, y * Tile);

        private static Vector2Int WorldToTile(Vector3 world) =>
            new(Mathf.RoundToInt((world.x - Origin.x) / Tile), Mathf.RoundToInt((world.z - Origin.z) / Tile));
    }
}
