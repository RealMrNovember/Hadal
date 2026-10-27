using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.Events;
using Hadal.Data.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Resource nodes and gathering marches on the world map (docs/rehber 09 §4): a marker and plate per node,
    /// a details card with a Gather button, and a strip of the player's marches with timers and Recall.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private static readonly Dictionary<string, (string Model, Color Color)> NodeLook = new()
        {
            ["enkaz_alani"] = ("env_rock_medium", new Color(0.62f, 0.68f, 0.74f)),
            ["kobalt_kabugu"] = ("env_rock_large", new Color(0.36f, 0.52f, 0.95f)),
            ["nodul_sahasi"] = ("env_rock_medium", new Color(0.66f, 0.45f, 0.95f)),
            ["masif_sulfur_bacasi"] = ("env_coral_branch", new Color(0.98f, 0.55f, 0.2f)),
            ["metan_hidrat"] = ("env_crystal_cluster", new Color(0.55f, 0.92f, 0.85f)),
            ["balina_lesi"] = ("env_coral_branch", new Color(0.95f, 0.9f, 0.75f)),
            ["hadalit_damari"] = ("env_crystal_cluster", new Color(0.98f, 0.35f, 0.75f))
        };

        private ILocalEventBus _localBus;
        private readonly Dictionary<string, (GameObject Model, RectTransform Plate)> _nodes = new();
        private RectTransform _marchPanel;
        private float _nextMarchRefresh;
        private int _marchVersion = -1;

        private void InitializeNodes(ILocalEventBus localBus)
        {
            _localBus = localBus;
            _localBus?.Subscribe<CommandResolvedEvent>(OnCommandResolved);
        }

        private void OnDestroy() => _localBus?.Unsubscribe<CommandResolvedEvent>(OnCommandResolved);

        private void OnCommandResolved(CommandResolvedEvent result)
        {
            if (result.Type is not (CommandType.MarchGather or CommandType.RecallMarch or CommandType.InviteToAlliance))
                return;
            if (result.Result == CommandResultCode.Rejected)
            {
                _toast?.Invoke(Loc.Reason(result.Reason), UiKit.Danger);
                return;
            }

            _toast?.Invoke(Loc.T(result.Type switch
            {
                CommandType.MarchGather => "world.march.sent",
                CommandType.RecallMarch => "world.march.recalled",
                _ => "alliance.invited"
            }), UiKit.Accent);
            ClosePopup();
            if (IsOpen && _requested.x >= 0)
                Request(_requested.x, _requested.y);
        }

        public static string NodeName(string type) => Loc.T("node." + type);

        // --- Nodes -----------------------------------------------------------------------------

        private void ApplyNodes(WorldViewDto view)
        {
            var seen = new HashSet<string>();
            foreach (var n in view.Nodes ?? Array.Empty<WorldNodeDto>())
            {
                seen.Add(n.Id);
                if (!_nodes.TryGetValue(n.Id, out var entry))
                {
                    entry = (BuildNode(n), BuildNodePlate());
                    _nodes[n.Id] = entry;
                }

                entry.Model.transform.position = TileToWorld(n.X, n.Y);
                StyleNodePlate(entry.Plate, n);
            }

            // Nodes the server no longer lists ran dry, faded or moved.
            foreach (var id in _nodes.Keys.Where(k => !seen.Contains(k)).ToList())
            {
                Destroy(_nodes[id].Model);
                Destroy(_nodes[id].Plate.gameObject);
                _nodes.Remove(id);
            }
        }

        private GameObject BuildNode(WorldNodeDto n)
        {
            var root = new GameObject("Node_" + n.Id);
            root.transform.SetParent(_root, false);
            var look = NodeLook.TryGetValue(n.Type, out var l) ? l : ("env_rock_medium", Color.white);
            var prefab = CharacterLibrary.Instance?.Model(look.Item1);
            if (prefab != null)
            {
                var model = Instantiate(prefab, root.transform);
                model.transform.localScale = Vector3.one * (1.6f + n.Level * 0.25f);
                model.transform.localRotation = Quaternion.Euler(0f, (n.X * 37 + n.Y * 11) % 360, 0f);
            }

            return root;
        }

        private RectTransform BuildNodePlate()
        {
            var plate = UiKit.Box("NodePlate", _labels, new Color(0.02f, 0.05f, 0.08f, 0.75f));
            plate.rectTransform.sizeDelta = new Vector2(250, 52);
            var tap = plate.gameObject.AddComponent<Button>();
            tap.targetGraphic = plate;
            var level = UiKit.Box("Level", plate.transform, Color.white);
            level.rectTransform.Place(0f, 0f, 0.22f, 1f);
            level.raycastTarget = false;
            UiKit.Label("Text", level.transform, string.Empty, 24, UiKit.Abyss, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);
            var name = UiKit.Label("Name", plate.transform, string.Empty, 22, Color.white, TextAnchor.MiddleLeft);
            name.rectTransform.Place(0.26f, 0f, 0.98f, 1f);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            return plate.rectTransform;
        }

        private void StyleNodePlate(RectTransform plate, WorldNodeDto n)
        {
            var color = NodeLook.TryGetValue(n.Type, out var look) ? look.Color : Color.white;
            plate.Find("Level").GetComponent<Image>().color = color;
            plate.Find("Level/Text").GetComponent<Text>().text = n.Level.ToString();
            var state = n.Mine ? " <color=#38D6F2>◆</color>" : n.Busy ? " <color=#FFB347>●</color>" : string.Empty;
            plate.Find("Name").GetComponent<Text>().text = NodeName(n.Type) + state;
            var button = plate.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            var info = n;
            button.onClick.AddListener(() => ShowNode(info));
        }

        private void UpdateNodePlates()
        {
            // Node plates show only when zoomed in enough to read them.
            var near = _distance < 120f;
            foreach (var (model, plate) in _nodes.Values)
            {
                var screen = _camera.WorldToScreenPoint(model.transform.position + Vector3.up * 4f);
                var visible = near && screen.z > 0f && _popup == null;
                plate.gameObject.SetActive(visible);
                if (!visible)
                    continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_labels, screen, null, out var local);
                plate.anchorMin = plate.anchorMax = new Vector2(0.5f, 0.5f);
                plate.anchoredPosition = local;
            }
        }

        private int ReactorLevel() =>
            (View.Data.buildings ?? Array.Empty<BuildingSaveEntry>()).Where(b => b.definitionId == BuildingProgression.ReactorId)
                .Select(b => b.level).DefaultIfEmpty(0).Max();

        /// <summary>A resource node: the round menu with level, what is left, the trip and a Gather button.</summary>
        private void ShowNode(WorldNodeDto n)
        {
            var catalog = ClientCatalog.Catalog;
            var world = catalog.World;
            var reactor = ReactorLevel();
            var view = View.WorldView;

            var rates = GatherRules.Rates(world, catalog.Gather, n.Type, n.Level, reactor);
            var lines = new List<string>
            {
                string.Join("   ", rates.Select(r => $"+{CostChips.Compact((long)r.Value)} {Loc.Resource(r.Key.ToString())}{Loc.T("base.perhour")}"))
            };
            if (view != null)
            {
                var trip = GatherRules.TravelSeconds(world, view.MyX, view.MyY, n.X, n.Y);
                var load = Math.Min(GatherRules.Load(catalog.Gather, reactor), n.Remaining);
                lines.Add(Loc.F("world.node.trip", Loc.Duration(trip), Loc.Duration(GatherRules.GatherSeconds(world, rates, load)), CostChips.Compact(load)));
            }

            var marches = View.Marches;
            var queues = marches?.Queues ?? GatherRules.Queues(catalog.Gather, reactor);
            var busy = marches?.Marches?.Length ?? 0;
            string blocked = n.Mine ? Loc.T("world.node.mine")
                : n.Busy ? Loc.T("world.node.busy")
                : queues == 0 ? Loc.F("world.noqueue", catalog.Gather.queueReactors.DefaultIfEmpty(0).Min())
                : busy >= queues ? Loc.T("reason.NoFreeQueue")
                : null;
            if (blocked != null)
                lines.Add($"<color=#FFB347>{blocked}</color>");
            else if (n.FadesAtMs > 0)
                lines.Add(Loc.F("world.node.fades", Loc.Duration(Math.Max(0, (n.FadesAtMs - View.ServerNowMs) / 1000f))));

            var id = n.Id;
            var items = new List<TargetMenu.Item> { new("queue_drone", Loc.T("world.gather"), () => OpenMarchSetup(n), blocked == null) };
            GlideTo(n.X, n.Y);
            var x = n.X;
            var y = n.Y;
            TargetMenu.Show(() => IsOpen ? _camera : null, () => TileToWorld(x, y) + Vector3.up * 2f,
                $"{Loc.F("base.level", n.Level)}  {NodeName(n.Type)}", string.Join("\n", lines), items,
                n.Capacity > 0 ? n.Remaining / (float)n.Capacity : -1f, $"{CostChips.Compact(n.Remaining)} / {CostChips.Compact(n.Capacity)}");
        }

        private void Gather(string nodeId, string heroId) =>
            _commands.Dispatch(new MarchGatherCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, NodeId = nodeId, HeroId = heroId ?? string.Empty });

        private void Recall(string marchId) =>
            _commands.Dispatch(new RecallMarchCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, MarchId = marchId });

        // --- Marches: setup, the queue at the top left, submarines on the map -------------------

        private string _setupHero = string.Empty;

        private void BuildMarches(Transform safe)
        {
            // Same place and logic as the base's build queue (top left, under the header): "Sefer 1/3" and a row per march.
            _marchPanel = UiKit.Rect("Marches", safe).Place(0.012f, 0.56f, 0.5f, 0.885f);
        }

        private HashSet<string> MarchingHeroes() =>
            new((View.Marches?.Marches ?? Array.Empty<MarchDto>()).Select(m => m.HeroId).Where(h => !string.IsNullOrEmpty(h)));

        /// <summary>The genre's march setup: pick who leads, see the crew, the load and the trip, then send.</summary>
        private void OpenMarchSetup(WorldNodeDto n, string heroId = null)
        {
            ClosePopup();
            var catalog = ClientCatalog.Catalog;
            var world = catalog.World;
            var reactor = ReactorLevel();
            var view = View.WorldView;
            var marching = MarchingHeroes();
            var heroes = View.Heroes
                .Select(h => (View: h, Ok: catalog.TryGetHero(h.HeroId, out var d), Def: d))
                .Where(h => h.Ok)
                .Select(h => (h.View, h.Def, Power: HeroRules.Power(h.Def, h.View.Level, h.View.Stars), Busy: marching.Contains(h.View.HeroId)))
                .OrderBy(h => h.Busy).ThenByDescending(h => h.Power).ToList();
            _setupHero = heroId ?? heroes.Where(h => !h.Busy).Select(h => h.View.HeroId).FirstOrDefault() ?? string.Empty;

            var dim = UiKit.Box("Popup", _popupCanvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(ClosePopup);
            _popup = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.04f, 0.2f, 0.96f, 0.8f);
            card.gameObject.AddComponent<Button>();
            if (heroId == null)
            {
                card.gameObject.AddComponent<CanvasGroup>();
                card.gameObject.AddComponent<PopIn>();
            }

            var header = UiKit.Box("Header", card.transform, new Color(0.05f, 0.16f, 0.22f, 1f));
            header.rectTransform.Place(0, 0.89f, 1, 1);
            UiKit.Label("Title", header.transform, Loc.T("march.setup"), 42, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0.45f, 0.8f, 1f);
            UiKit.Label("Target", header.transform, $"{Loc.F("base.level", n.Level)}  {NodeName(n.Type)}", 28, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.05f, 0.02f, 0.8f, 0.5f);
            var close = UiKit.Button("Close", header.transform, "X", ClosePopup, UiKit.Field, 40);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.84f, 0.18f, 0.96f, 0.82f);

            UiKit.Label("Pick", card.transform, Loc.T("march.commander"), 30, UiKit.Muted, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0.82f, 0.95f, 0.88f);

            // Commander grid: four portraits a row; heroes already out are dimmed.
            const int columns = 4;
            const float top = 0.81f, rowHeight = 0.2f;
            for (var i = 0; i < Math.Min(heroes.Count, 8); i++)
            {
                var h = heroes[i];
                var col = i % columns;
                var row = i / columns;
                var x0 = 0.04f + col * 0.23f;
                var y1 = top - row * (rowHeight + 0.01f);
                var slot = UiKit.Box("Hero", card.transform, h.View.HeroId == _setupHero ? UiKit.Accent : new Color(0, 0, 0, 0.35f));
                slot.rectTransform.Place(x0, y1 - rowHeight, x0 + 0.22f, y1);
                var pick = h.View.HeroId;
                if (!h.Busy)
                    slot.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                    {
                        Destroy(_popup);
                        _popup = null;
                        OpenMarchSetup(n, pick);
                    });
                SocialView.Portrait(slot.transform, h.Def, !h.Busy, 0.04f, 0.26f, 0.96f, 0.97f);
                UiKit.Label("Name", slot.transform, h.Busy ? Loc.T("march.busy") : Loc.T("hero." + h.View.HeroId), 22,
                    h.Busy ? UiKit.Amber : UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0.12f, 1, 0.27f);
                UiKit.Label("Power", slot.transform, CostChips.Compact(h.Power), 20, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0, 0, 1, 0.13f);
            }

            if (heroes.Count == 0)
                UiKit.Label("None", card.transform, Loc.T("march.noheroes"), 28, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0.05f, 0.5f, 0.95f, 0.8f);

            // Crew, load, trip and gathering time for the chosen commander.
            var rates = GatherRules.Rates(world, catalog.Gather, n.Type, n.Level, reactor);
            var load = Math.Min(GatherRules.Load(catalog.Gather, reactor), n.Remaining);
            var trip = view == null ? 0 : GatherRules.TravelSeconds(world, view.MyX, view.MyY, n.X, n.Y);
            var lines = $"{Loc.F("march.crew", GatherRules.Crew(reactor))}\n{Loc.F("march.load", CostChips.Compact(load))}\n" +
                        $"{Loc.F("march.trip", Loc.Duration(trip), Loc.Duration(GatherRules.GatherSeconds(world, rates, load)))}";
            var info = UiKit.Box("Info", card.transform, new Color(0.04f, 0.11f, 0.15f, 1f));
            info.rectTransform.Place(0.04f, 0.17f, 0.96f, 0.38f);
            UiKit.Label("Text", info.transform, lines, 30, UiKit.Text, TextAnchor.MiddleLeft).rectTransform.Place(0.05f, 0.05f, 0.95f, 0.95f);

            var can = !string.IsNullOrEmpty(_setupHero);
            var go = UiKit.Button("Go", card.transform, Loc.T("march.go"), () =>
            {
                Gather(n.Id, _setupHero);
                ClosePopup();
            }, can ? UiKit.Ok : UiKit.Panel, 40);
            go.interactable = can;
            ((RectTransform)go.transform).Place(0.2f, 0.03f, 0.8f, 0.14f);
        }

        /// <summary>Where a march is now, in tiles (outbound, at the node, or on the way back).</summary>
        private Vector3 MarchPosition(MarchDto m, long now)
        {
            var from = TileToWorld(m.FromX, m.FromY);
            var to = TileToWorld(m.X, m.Y);
            if (now < m.ArrivesAtMs)
                return Vector3.Lerp(from, to, Mathf.InverseLerp(m.StartedAtMs, m.ArrivesAtMs, now));
            if (now < m.GatheredAtMs)
                return to;
            var back = m.HomeAtMs - m.GatheredAtMs;
            return Vector3.Lerp(to, from, back <= 0 ? 1f : Mathf.Clamp01((now - m.GatheredAtMs) / (float)back));
        }

        private readonly Dictionary<string, (RectTransform Sub, Text Label, List<RectTransform> Dots)> _subs = new();

        /// <summary>Each march as a submarine marker moving on a dotted route, with the commander and crew on it.</summary>
        private void UpdateSubmarines()
        {
            var marches = View.Marches?.Marches ?? Array.Empty<MarchDto>();
            var now = View.ServerNowMs;
            var live = new HashSet<string>();
            var catalog = ClientCatalog.Catalog;
            foreach (var m in marches)
            {
                live.Add(m.Id);
                if (!_subs.TryGetValue(m.Id, out var sub))
                {
                    var rect = UiKit.Rect("Sub", _labels);
                    rect.sizeDelta = new Vector2(96f, 96f);
                    var disc = rect.gameObject.AddComponent<Image>();
                    disc.sprite = UiKit.Circle;
                    disc.color = new Color(0.13f, 0.52f, 0.86f, 0.95f);
                    disc.raycastTarget = false;
                    Hud.HudIcons.Image(rect, "queue_drone", 0.12f, 0.12f, 0.88f, 0.88f).raycastTarget = false;
                    var plate = UiKit.Box("Plate", rect, new Color(0.02f, 0.05f, 0.08f, 0.85f));
                    plate.raycastTarget = false;
                    plate.rectTransform.Place(-1.1f, -0.55f, 2.1f, -0.05f);
                    var label = UiKit.Label("Text", plate.transform, string.Empty, 22, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    label.rectTransform.Place(0, 0, 1, 1);
                    sub = (rect, label, new List<RectTransform>());
                    for (var d = 0; d < 14; d++)
                    {
                        var dot = UiKit.Rect("Dot", _labels);
                        dot.sizeDelta = new Vector2(12f, 12f);
                        var img = dot.gameObject.AddComponent<Image>();
                        img.sprite = UiKit.Circle;
                        img.color = new Color(0.55f, 0.85f, 1f, 0.75f);
                        img.raycastTarget = false;
                        sub.Dots.Add(dot);
                    }

                    rect.SetAsLastSibling();
                    _subs[m.Id] = sub;
                }

                var hero = catalog.TryGetHero(m.HeroId, out var def) ? Loc.T("hero." + def.Id) : Loc.T("march.divers");
                var phase = now < m.ArrivesAtMs ? "world.march.outbound" : now < m.GatheredAtMs ? "world.march.gathering" : "world.march.returning";
                var until = now < m.ArrivesAtMs ? m.ArrivesAtMs : now < m.GatheredAtMs ? m.GatheredAtMs : m.HomeAtMs;
                sub.Label.text = $"{hero} · {Loc.F("march.crew.short", m.Crew)}\n<size=18>{Loc.F(phase, Loc.Duration(Math.Max(0, (until - now) / 1000f)))}</size>";

                var visible = _popup == null;
                var position = Project(MarchPosition(m, now) + Vector3.up * 2f, out var onScreen);
                sub.Sub.gameObject.SetActive(visible && onScreen);
                sub.Sub.anchoredPosition = position;

                // The route: dots between the base and the node.
                var a = Project(TileToWorld(m.FromX, m.FromY) + Vector3.up * 0.5f, out var aOk);
                var b = Project(TileToWorld(m.X, m.Y) + Vector3.up * 0.5f, out var bOk);
                for (var d = 0; d < sub.Dots.Count; d++)
                {
                    var dot = sub.Dots[d];
                    dot.gameObject.SetActive(visible && aOk && bOk);
                    dot.anchoredPosition = Vector2.Lerp(a, b, (d + 0.5f) / sub.Dots.Count);
                }
            }

            foreach (var id in _subs.Keys.Where(k => !live.Contains(k)).ToList())
            {
                Destroy(_subs[id].Sub.gameObject);
                foreach (var dot in _subs[id].Dots)
                    Destroy(dot.gameObject);
                _subs.Remove(id);
            }
        }

        private Vector2 Project(Vector3 world, out bool onScreen)
        {
            var screen = _camera.WorldToScreenPoint(world);
            onScreen = screen.z > 0f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_labels, screen, null, out var local);
            return local;
        }


        /// <summary>The queue at the top left: "Sefer 1/3", then a row per march with commander, crew, phase, time and Recall.</summary>
        private void UpdateMarches()
        {
            UpdateSubmarines();
            if (_marchPanel == null || (Time.unscaledTime < _nextMarchRefresh && _marchVersion == View.IncomeVersion))
                return;
            if (Input.touchCount > 0 || Input.GetMouseButton(0))
                return;
            _nextMarchRefresh = Time.unscaledTime + 1f;
            _marchVersion = View.IncomeVersion;

            for (var i = _marchPanel.childCount - 1; i >= 0; i--)
                Destroy(_marchPanel.GetChild(i).gameObject);

            var list = View.Marches;
            if (list == null)
                return;
            var marches = list.Marches ?? Array.Empty<MarchDto>();
            const float titleHeight = 0.11f, rowHeight = 0.2f;
            var title = UiKit.Box("Title", _marchPanel, new Color(0.02f, 0.05f, 0.08f, 0.8f));
            title.rectTransform.Place(0, 1f - titleHeight, 1f, 1f);
            title.raycastTarget = false;
            var heading = list.Queues == 0 && list.NextQueueReactor > 0
                ? Loc.F("world.noqueue", list.NextQueueReactor)
                : Loc.F("march.queue", marches.Length, list.Queues) + (marches.Length < list.Queues ? $"   <color=#FFB347>{Loc.T("hud.queue.free")}</color>" : string.Empty);
            UiKit.Label("Text", title.transform, "▼ " + heading, 26, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.04f, 0, 0.98f, 1);

            var now = View.ServerNowMs;
            var catalog = ClientCatalog.Catalog;
            for (var i = 0; i < marches.Length; i++)
            {
                var m = marches[i];
                var y1 = 1f - titleHeight - 0.015f - i * (rowHeight + 0.015f);
                var row = UiKit.Box("March", _marchPanel, new Color(0.03f, 0.10f, 0.14f, 0.88f));
                row.rectTransform.Place(0, y1 - rowHeight, 1f, y1);
                var march = m;
                row.gameObject.AddComponent<Button>().onClick.AddListener(() => GlideTo(march.X, march.Y));
                if (catalog.TryGetHero(m.HeroId, out var def))
                    SocialView.Portrait(row.transform, def, true, 0.01f, 0.06f, 0.2f, 0.94f);
                else
                    Hud.HudIcons.Image(row.transform, "queue_drone", 0.02f, 0.1f, 0.18f, 0.9f);

                var phase = now < m.ArrivesAtMs ? "world.march.outbound" : now < m.GatheredAtMs ? "world.march.gathering" : "world.march.returning";
                var until = now < m.ArrivesAtMs ? m.ArrivesAtMs : now < m.GatheredAtMs ? m.GatheredAtMs : m.HomeAtMs;
                var hero = def != null ? Loc.T("hero." + def.Id) : Loc.T("march.divers");
                UiKit.Label("Who", row.transform, $"{hero} · {Loc.F("march.crew.short", m.Crew)}", 22, UiKit.Text, TextAnchor.UpperLeft, FontStyle.Bold)
                    .rectTransform.Place(0.23f, 0.52f, 0.98f, 0.95f);
                UiKit.Label("Where", row.transform, $"{NodeName(m.NodeType)} {m.NodeLevel} · <color=#FFB347>{Loc.F(phase, Loc.Duration(Math.Max(0, (until - now) / 1000f)))}</color>",
                    20, UiKit.Muted, TextAnchor.LowerLeft).rectTransform.Place(0.23f, 0.06f, 0.72f, 0.5f);
                if (now < m.GatheredAtMs)
                {
                    var id = m.Id;
                    var recall = UiKit.Button("Recall", row.transform, Loc.T("world.recall"), () => Recall(id), UiKit.Field, 20);
                    recall.GetComponentInChildren<Text>().color = UiKit.Text;
                    ((RectTransform)recall.transform).Place(0.73f, 0.08f, 0.98f, 0.5f);
                }
            }
        }
    }
}
