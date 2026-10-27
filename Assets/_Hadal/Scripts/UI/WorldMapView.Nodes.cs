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

            // A soft ring on the seabed in the node's colour marks it from afar.
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(root.transform, false);
            ring.transform.localScale = new Vector3(5.5f, 0.02f, 5.5f);
            var renderer = ring.GetComponent<Renderer>();
            renderer.material.color = new Color(look.Item2.r, look.Item2.g, look.Item2.b, 1f) * 0.6f;
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

        private void ShowNode(WorldNodeDto n)
        {
            ClosePopup();
            var catalog = ClientCatalog.Catalog;
            var world = catalog.World;
            var reactor = ReactorLevel();
            var view = View.WorldView;

            var dim = UiKit.Box("Popup", _popupCanvas.transform, new Color(0, 0, 0, 0.5f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(ClosePopup);
            _popup = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.06f, 0.3f, 0.94f, 0.7f);
            card.gameObject.AddComponent<Button>(); // taps on the card do not close it
            var color = NodeLook.TryGetValue(n.Type, out var look) ? look.Color : Color.white;
            var badge = UiKit.Box("Level", card.transform, color);
            badge.rectTransform.Place(0.05f, 0.8f, 0.17f, 0.95f);
            UiKit.Label("Text", badge.transform, n.Level.ToString(), 40, UiKit.Abyss, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);
            UiKit.Label("Name", card.transform, NodeName(n.Type), 42, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.21f, 0.8f, 0.96f, 0.95f);

            var rates = GatherRules.Rates(world, catalog.Gather, n.Type, n.Level, reactor);
            var lines = new List<string>
            {
                string.Join("   ", rates.Select(r => $"+{r.Value:N0} {Loc.Resource(r.Key.ToString())}{Loc.T("base.perhour")}")),
                Loc.F("world.node.left", n.Remaining.ToString("N0"), n.Capacity.ToString("N0"))
            };
            if (view != null)
            {
                var trip = GatherRules.TravelSeconds(world, view.MyX, view.MyY, n.X, n.Y);
                var load = Math.Min(GatherRules.Load(catalog.Gather, reactor), n.Remaining);
                var gather = GatherRules.GatherSeconds(world, rates, load);
                lines.Add(Loc.F("world.node.trip", Loc.Duration(trip), Loc.Duration(gather), load.ToString("N0")));
            }

            if (n.FadesAtMs > 0)
                lines.Add(Loc.F("world.node.fades", Loc.Duration(Math.Max(0, (n.FadesAtMs - View.ServerNowMs) / 1000f))));
            lines.Add(Loc.F("world.coords", n.X, n.Y));
            UiKit.Label("Info", card.transform, string.Join("\n", lines), 28, UiKit.Muted, TextAnchor.UpperLeft)
                .rectTransform.Place(0.06f, 0.26f, 0.96f, 0.76f);

            var marches = View.Marches;
            var queues = marches?.Queues ?? GatherRules.Queues(catalog.Gather, reactor);
            var busy = marches?.Marches?.Length ?? 0;
            string blocked = n.Mine ? Loc.T("world.node.mine")
                : n.Busy ? Loc.T("world.node.busy")
                : queues == 0 ? Loc.F("world.noqueue", catalog.Gather.queueReactors.DefaultIfEmpty(0).Min())
                : busy >= queues ? Loc.T("reason.NoFreeQueue")
                : null;
            if (blocked != null)
                UiKit.Label("Blocked", card.transform, blocked, 26, UiKit.Amber, TextAnchor.MiddleCenter).rectTransform.Place(0.05f, 0.17f, 0.95f, 0.26f);

            var gatherButton = UiKit.Button("Gather", card.transform, Loc.T("world.gather"), () => Gather(n.Id), blocked == null ? UiKit.Accent : UiKit.Field, 32);
            gatherButton.interactable = blocked == null;
            ((RectTransform)gatherButton.transform).Place(0.06f, 0.04f, 0.5f, 0.16f);
            var close = UiKit.Button("Close", card.transform, Loc.T("hero.pod.done"), ClosePopup, UiKit.Field, 32);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.54f, 0.04f, 0.94f, 0.16f);
        }

        private void Gather(string nodeId) =>
            _commands.Dispatch(new MarchGatherCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, NodeId = nodeId });

        private void Recall(string marchId) =>
            _commands.Dispatch(new RecallMarchCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, MarchId = marchId });

        // --- Marches strip ---------------------------------------------------------------------

        private void BuildMarches(Transform safe)
        {
            _marchPanel = UiKit.Rect("Marches", safe).Place(0.02f, 0.245f, 0.7f, 0.5f);
        }

        private void UpdateMarches()
        {
            if (_marchPanel == null || (Time.unscaledTime < _nextMarchRefresh && _marchVersion == View.IncomeVersion))
                return;
            _nextMarchRefresh = Time.unscaledTime + 1f;
            _marchVersion = View.IncomeVersion;

            for (var i = _marchPanel.childCount - 1; i >= 0; i--)
                Destroy(_marchPanel.GetChild(i).gameObject);

            var list = View.Marches;
            if (list == null)
                return;
            var marches = list.Marches ?? Array.Empty<MarchDto>();
            const float row = 0.16f;
            var title = UiKit.Box("Title", _marchPanel, new Color(0.02f, 0.05f, 0.08f, 0.85f));
            title.rectTransform.Place(0, 1f - row, 0.62f, 1f);
            title.raycastTarget = false;
            var heading = list.Queues == 0 && list.NextQueueReactor > 0
                ? Loc.F("world.noqueue", list.NextQueueReactor)
                : Loc.F("world.marches", marches.Length, list.Queues);
            UiKit.Label("Text", title.transform, heading, 22, UiKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);

            var now = View.ServerNowMs;
            for (var i = 0; i < marches.Length; i++)
            {
                var m = marches[i];
                var y1 = 1f - row * (i + 1) - 0.02f;
                var box = UiKit.Box("March", _marchPanel, new Color(0.02f, 0.05f, 0.08f, 0.85f));
                box.rectTransform.Place(0, y1 - row, 1f, y1);
                var (key, until) = now < m.ArrivesAtMs ? ("world.march.outbound", m.ArrivesAtMs)
                    : now < m.GatheredAtMs ? ("world.march.gathering", m.GatheredAtMs)
                    : ("world.march.returning", m.HomeAtMs);
                var left = Loc.Duration(Math.Max(0, (until - now) / 1000f));
                var cargo = string.Join(" ", (m.Cargo ?? Array.Empty<ResourceAmountEntry>()).Select(c => $"{c.Value:N0} {Loc.Resource(c.Key.ToString())}"));
                UiKit.Label("Text", box.transform, $"{NodeName(m.NodeType)} {m.NodeLevel} · {Loc.F(key, left)}\n<size=18>{cargo}</size>", 20, UiKit.Text)
                    .rectTransform.Place(0.03f, 0f, 0.7f, 1f);
                if (now < m.GatheredAtMs)
                {
                    var id = m.Id;
                    var recall = UiKit.Button("Recall", box.transform, Loc.T("world.recall"), () => Recall(id), UiKit.Field, 20);
                    recall.GetComponentInChildren<Text>().color = UiKit.Text;
                    ((RectTransform)recall.transform).Place(0.72f, 0.12f, 0.98f, 0.88f);
                }
            }
        }
    }
}
