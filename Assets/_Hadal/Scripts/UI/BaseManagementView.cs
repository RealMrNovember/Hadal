using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.Contracts;
using Hadal.Core.Events;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Hadal.UI
{
    /// <summary>
    /// Base management (docs/23_Base_Management.md): the build menu for a free slot, the building
    /// panel (level, output, upgrade, free finish, remove / cancel), the "Expand Base" page that
    /// opens outer rings, construction timers above buildings and rejection toasts. Every number comes from the shared catalog and BuildingProgression, the
    /// same rules the server enforces; the server's answer is the final word.
    /// </summary>
    public sealed partial class BaseManagementView : MonoBehaviour
    {
        private const float TicksPerMinute = 60f / SimulationTick.IntervalSeconds;

        private IStateSyncService _stateSync;
        private ICircularGridService _grid;
        private ICommandDispatcher _commands;
        private ILocalEventBus _localBus;
        // Read on use: a balance published from the Overseer replaces the catalog at runtime.
        private static GameCatalog _catalog => ClientCatalog.Catalog;
        private bool _subscribed;
        private ulong _sequence;

        private Canvas _canvas;
        private RectTransform _overlay;
        private CanvasGroup _sheet;
        private Text _title;
        private Text _subtitle;
        private RectTransform _list;
        private Text _toast;
        private float _toastUntil;
        private Button _expand;
        private bool _expandOpen;
        private string _confirmRemove = string.Empty;

        private int _ring;
        private int _sector;
        private string _buildingKey = string.Empty;
        private float _nextRefresh;
        private readonly Dictionary<string, (RectTransform Root, Image Fill, Text Label)> _timers = new();

        public bool IsOpen => _sheet != null && _sheet.gameObject.activeSelf;
        public string OpenBuildingKey => IsOpen ? _buildingKey : string.Empty;

        [Inject]
        public void Construct(IStateSyncService stateSync, ICircularGridService grid, ICommandDispatcher commands, ILocalEventBus localBus)
        {
            _stateSync = stateSync;
            _grid = grid;
            _commands = commands;
            _localBus = localBus;
            if (isActiveAndEnabled)
                Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            if (!_subscribed || _localBus == null)
                return;

            _localBus.Unsubscribe<BaseSlotTappedEvent>(OnSlotTapped);
            _localBus.Unsubscribe<CommandResolvedEvent>(OnCommandResolved);
            _localBus.Unsubscribe<BuildingStateChangedEvent>(OnStateChanged);
            _subscribed = false;
        }

        private void Subscribe()
        {
            if (_localBus == null || _subscribed)
                return;

            if (_canvas == null)
                Build();

            _localBus.Subscribe<BaseSlotTappedEvent>(OnSlotTapped);
            _localBus.Subscribe<CommandResolvedEvent>(OnCommandResolved);
            _localBus.Subscribe<BuildingStateChangedEvent>(OnStateChanged);
            _subscribed = true;
        }

        // --- Layout --------------------------------------------------------------------------

        private void Build()
        {
            _canvas = UiKit.Canvas("BaseUi", 40);
            _canvas.transform.SetParent(transform, false);

            _overlay = UiKit.Rect("Timers", _canvas.transform).Place(0, 0, 1, 1);

            var sheetImage = UiKit.Box("Sheet", _canvas.transform, UiKit.Panel);
            sheetImage.rectTransform.Place(0.02f, 0.01f, 0.98f, 0.52f);
            _sheet = sheetImage.gameObject.AddComponent<CanvasGroup>();

            _title = UiKit.Label("Title", sheetImage.transform, string.Empty, 52, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            _title.rectTransform.Place(0.05f, 0.88f, 0.8f, 0.98f);
            _subtitle = UiKit.Label("Subtitle", sheetImage.transform, string.Empty, 32, UiKit.Muted);
            _subtitle.rectTransform.Place(0.05f, 0.81f, 0.95f, 0.88f);

            var close = UiKit.Button("Close", sheetImage.transform, "X", Close, UiKit.Field, 40);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.84f, 0.88f, 0.96f, 0.98f);

            _list = UiKit.ScrollList("List", sheetImage.transform);
            ((RectTransform)_list.parent).Place(0.04f, 0.03f, 0.96f, 0.8f);

            _expand = UiKit.Button("Expand", _canvas.transform, Loc.T("base.expand"), OpenExpand, new Color(0.03f, 0.10f, 0.14f, 0.85f), 36);
            _expand.GetComponentInChildren<Text>().color = UiKit.Accent;
            ((RectTransform)_expand.transform).Place(0.03f, 0.02f, 0.4f, 0.07f);

            _toast = UiKit.Label("Toast", _canvas.transform, string.Empty, 38, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _toast.rectTransform.Place(0.05f, 0.55f, 0.95f, 0.6f);
            var outline = _toast.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(2, -2);

            _sheet.gameObject.SetActive(false);
        }

        // --- Events --------------------------------------------------------------------------

        private void OnSlotTapped(BaseSlotTappedEvent tap)
        {
            _ring = tap.Ring;
            _sector = tap.Sector;
            _buildingKey = tap.BuildingInstanceKey ?? string.Empty;
            _expandOpen = false;
            _researchOpen = false;
            _confirmRemove = string.Empty;
            _sheet.gameObject.SetActive(true);
            Render();
        }

        /// <summary>Shows the next ring's requirements and the button that opens it.</summary>
        public void OpenExpand()
        {
            _expandOpen = true;
            _researchOpen = false;
            _buildingKey = string.Empty;
            _confirmRemove = string.Empty;
            _sheet.gameObject.SetActive(true);
            Render();
        }

        private void OnStateChanged(BuildingStateChangedEvent state)
        {
            if (!IsOpen)
                return;

            // The slot may have just been built on (build menu -> building panel).
            if (string.IsNullOrEmpty(_buildingKey) && !_expandOpen && !_researchOpen)
                _buildingKey = Buildings().FirstOrDefault(b => b.cellRing == _ring && b.cellSector == _sector).instanceKey ?? string.Empty;
            Render();
        }

        private void OnCommandResolved(CommandResolvedEvent result)
        {
            if (result.Type is not (CommandType.PlaceBuilding or CommandType.UpgradeBuilding or CommandType.FinishConstruction
                or CommandType.RemoveBuilding or CommandType.UnlockRing or CommandType.StartResearch or CommandType.FinishResearch
                or CommandType.BoostBuilding or CommandType.Overdrive or CommandType.StartSalvage))
                return;

            if (result.Result == CommandResultCode.Rejected)
            {
                Toast(Loc.Reason(result.Reason), UiKit.Danger);
                return;
            }

            Toast(result.Type switch
            {
                CommandType.FinishConstruction => Loc.T("base.done"),
                CommandType.RemoveBuilding => Loc.T("base.removed"),
                CommandType.UnlockRing => Loc.T("base.expand.done"),
                CommandType.StartResearch or CommandType.FinishResearch or CommandType.BoostBuilding
                    or CommandType.Overdrive or CommandType.StartSalvage => FunctionToast(result.Type),
                _ => Loc.T("base.started")
            }, UiKit.Accent);

            // The removed building's panel has nothing left to show.
            if (result.Type == CommandType.RemoveBuilding && IsOpen && !_expandOpen && !_researchOpen)
                Close();
            else if (IsOpen)
                Render();
        }

        private void Update()
        {
            if (_canvas == null)
                return;

            UpdateTimers();
            ShowSalvageReports();
            _expand.gameObject.SetActive(!IsOpen);

            if (_toast.text.Length > 0 && Time.unscaledTime > _toastUntil)
                _toast.text = string.Empty;

            // Countdowns and affordability change every second.
            if (IsOpen && Time.unscaledTime >= _nextRefresh)
                Render();
        }

        public void Close() => _sheet.gameObject.SetActive(false);

        private void Toast(string text, Color color)
        {
            _toast.text = text;
            _toast.color = color;
            _toastUntil = Time.unscaledTime + 3f;
        }

        // --- Rendering -----------------------------------------------------------------------

        private string _renderedPage = string.Empty;

        /// <summary>Rebuilds the sheet. The once-a-second refresh keeps the scroll position of the same page.</summary>
        private void Render()
        {
            _nextRefresh = Time.unscaledTime + 1f;
            var page = $"{_expandOpen}|{_researchOpen}|{_researchTree}|{_buildingKey}|{_ring}:{_sector}";
            var scroll = page == _renderedPage ? _list.anchoredPosition : Vector2.zero;
            _renderedPage = page;

            for (var i = _list.childCount - 1; i >= 0; i--)
                Destroy(_list.GetChild(i).gameObject);

            RenderPage();
            _list.anchoredPosition = scroll;
        }

        private void RenderPage()
        {
            if (_expandOpen)
            {
                RenderExpand();
                return;
            }

            if (_researchOpen)
            {
                RenderResearch();
                return;
            }

            var building = Buildings().FirstOrDefault(b => b.instanceKey == _buildingKey && !string.IsNullOrEmpty(_buildingKey));
            if (!string.IsNullOrEmpty(building.instanceKey))
                RenderBuilding(building);
            else
                RenderBuildMenu();
        }

        private void RenderExpand()
        {
            _title.text = Loc.T("base.expand");
            var maxRings = _catalog.Grid.maxRings;
            var ring = _grid.UnlockedRingCount + 1;
            _subtitle.text = Loc.F("base.expand.open.rings", _grid.UnlockedRingCount, maxRings);

            if (ring > maxRings || !_catalog.TryGetRingUnlock(ring, out var unlock))
            {
                UiKit.Label("All", _list, Loc.T("base.expand.all"), 34, UiKit.Muted, TextAnchor.MiddleCenter).Row(160);
                return;
            }

            var wallet = Wallet();
            var finished = new HashSet<string>(Buildings().Where(b => b.level > 0).Select(b => b.definitionId));
            var verdict = BaseRules.ValidateUnlock(_catalog, ring, _grid.UnlockedRingCount, ReactorLevel(), finished, wallet);

            var needs = new StringBuilder();
            needs.Append(Check(ReactorLevel() >= unlock.ReactorLevel)).Append(Loc.F("base.needs.reactor", unlock.ReactorLevel));
            if (!string.IsNullOrEmpty(unlock.RequiresBuilding))
                needs.Append("\n").Append(Check(finished.Contains(unlock.RequiresBuilding))).Append(Loc.Building(unlock.RequiresBuilding));

            AddCard(
                Loc.F("base.expand.ring", ring),
                $"{Loc.F("base.expand.body", _catalog.GetSectorCount(ring))}\n{needs}",
                $"{Loc.T("base.cost")}: {Cost(unlock.Cost, wallet)}",
                verdict == RingRejectReason.None ? string.Empty : Loc.Reason(verdict.ToString()),
                Loc.T("base.expand.open"),
                verdict == RingRejectReason.None,
                () => UnlockRing(ring));
        }

        private static string Check(bool ok) => ok ? "<color=#38D6F2>•</color> " : "<color=#FF7369>•</color> ";

        private void RenderBuildMenu()
        {
            _title.text = Loc.F("base.build.title", _ring);
            _subtitle.text = string.Empty;

            var wallet = Wallet();
            var occupied = new HashSet<(int ring, int sector)>(Buildings().Select(b => (b.cellRing, b.cellSector)));
            var modifiers = Modifiers();
            var builderBusy = ActiveConstructions() >= Math.Max(1, _catalog.Progression.builderSlots) + modifiers.ExtraBuilders;
            var owned = OwnedCounts();
            var reactor = ReactorLevel();
            var any = false;

            // Buildable first, then by the reactor level that unlocks them, then by price.
            foreach (var definition in _catalog.Buildings.OrderBy(b => b.RequiredReactorLevel).ThenBy(b => b.BuildCost.Sum(c => c.Value)))
            {
                var verdict = PlacementRules.Validate(_catalog, definition.Id, _ring, _sector, _grid.UnlockedRingCount, occupied, wallet, owned, reactor, modifiers);
                // Buildings that can never go on this slot are not listed; the rest show why they are blocked.
                if (verdict is PlacementRejectReason.RingNotAllowed or PlacementRejectReason.SlotTypeMismatch
                    or PlacementRejectReason.SectorRestricted or PlacementRejectReason.SlotDoesNotExist or PlacementRejectReason.UnknownBuilding)
                    continue;

                any = true;
                owned.TryGetValue(definition.Id, out var count);
                var reason = verdict switch
                {
                    PlacementRejectReason.None => builderBusy ? Loc.Reason(nameof(ConstructionRejectReason.BuilderBusy)) : string.Empty,
                    PlacementRejectReason.ReactorLevelTooLow => Loc.F("base.needs.reactor", definition.RequiredReactorLevel),
                    PlacementRejectReason.LimitReached => Loc.F("base.limit", count, modifiers.MaxCount(definition)),
                    _ => Loc.Reason(verdict.ToString())
                };
                var id = definition.Id;
                AddCard(
                    definition.MaxCount > 0 ? $"{Loc.Building(id)}  <size=30>{count}/{modifiers.MaxCount(definition)}</size>" : Loc.Building(id),
                    $"{Loc.T("desc." + id)}\n{Rates(definition, 1)}",
                    $"{Loc.T("base.cost")}: {Cost(BuildingProgression.Cost(definition, 1, _catalog.Progression), wallet)}   {Loc.T("base.time")}: {Loc.Duration(modifiers.BuildSeconds(BuildingProgression.Seconds(definition, 1, _catalog.Progression)))}",
                    reason,
                    Loc.T("base.build"),
                    reason.Length == 0,
                    () => Place(id));
            }

            if (!any)
                UiKit.Label("Empty", _list, Loc.T("base.nothing"), 34, UiKit.Muted, TextAnchor.MiddleCenter).Row(160);
        }

        private void RenderBuilding(BuildingSaveEntry building)
        {
            if (!_catalog.TryGetBuilding(building.definitionId, out var definition))
                return;

            var rules = _catalog.Progression;
            var remaining = building.constructionEndsAt - Time.realtimeSinceStartup;
            _title.text = Loc.Building(definition.Id);
            _subtitle.text = building.level == 0
                ? Loc.F("base.constructing", Loc.Duration(remaining))
                : building.IsUnderConstruction
                    ? Loc.F("base.upgrading", building.level + 1, Loc.Duration(remaining))
                    : Loc.F("base.level", building.level) + (building.level >= definition.MaxLevel ? $" · {Loc.T("base.level.max")}" : string.Empty);

            var current = building.level > 0 ? Rates(definition, building.level) : string.Empty;
            UiKit.Label("Desc", _list, $"{Loc.T("desc." + definition.Id)}\n{current}", 34, UiKit.Text).Row(130);

            // The building's own function (research, boost, overdrive, salvage) once it works.
            if (building.level > 0)
                AddFunctionCards(building, definition);

            if (building.IsUnderConstruction)
            {
                var progress = building.constructionSeconds > 0 ? 1f - remaining / building.constructionSeconds : 1f;
                AddProgress(Mathf.Clamp01(progress));
                var free = remaining <= rules.freeFinishSeconds;
                var key = building.instanceKey;
                AddCard(Loc.T("base.finish"), string.Empty, string.Empty,
                    free ? string.Empty : Loc.Reason(nameof(ConstructionRejectReason.NotYetFree)),
                    Loc.T("base.finish"), free, () => Finish(key));

                // A first construction can be called off for everything back; upgrades must finish.
                if (building.level == 0)
                    AddRemoveCard(building, definition);
                return;
            }

            if (building.level >= definition.MaxLevel)
            {
                AddRemoveCard(building, definition);
                return;
            }

            var target = building.level + 1;
            var wallet = Wallet();
            var modifiers = Modifiers();
            var verdict = BuildingProgression.ValidateUpgrade(_catalog, definition.Id, building.level, false,
                ActiveConstructions(), ReactorLevel(), wallet, modifiers.ExtraBuilders);
            var instanceKey = building.instanceKey;
            AddCard(
                $"{Loc.T("base.next")}: {Loc.F("base.level", target)}",
                Rates(definition, target),
                $"{Loc.T("base.cost")}: {Cost(BuildingProgression.Cost(definition, target, rules), wallet)}   {Loc.T("base.time")}: {Loc.Duration(modifiers.BuildSeconds(BuildingProgression.Seconds(definition, target, rules)))}",
                verdict == ConstructionRejectReason.None ? string.Empty : Loc.Reason(verdict.ToString()),
                Loc.T("base.upgrade"),
                verdict == ConstructionRejectReason.None,
                () => Upgrade(instanceKey));

            AddRemoveCard(building, definition);
        }

        private void AddRemoveCard(BuildingSaveEntry building, BuildingDefinition definition)
        {
            if (BaseRules.ValidateRemove(definition.Id, building.level, building.IsUnderConstruction) != RemoveRejectReason.None)
                return;

            var cancel = building.level == 0;
            var refund = BaseRules.Refund(definition, building.level, _catalog.Progression);
            var confirming = _confirmRemove == building.instanceKey;
            var key = building.instanceKey;
            AddCard(
                Loc.T(cancel ? "base.cancel.title" : "base.remove.title"),
                string.Empty,
                Loc.F("base.refund", string.Join("  ", refund.Select(c => $"{c.Value} {Loc.Resource(c.Key.ToString())}"))),
                confirming ? Loc.T("base.confirm") : string.Empty,
                confirming ? Loc.T("base.confirm.yes") : Loc.T(cancel ? "base.cancel" : "base.remove"),
                true,
                () => Remove(key),
                UiKit.Danger);
        }

        private void AddCard(string title, string body, string cost, string reason, string action, bool enabled, Action onClick, Color? fill = null)
        {
            var card = UiKit.Box("Card", _list, UiKit.Field).Row(string.IsNullOrEmpty(body) ? 170 : 260);
            UiKit.Label("Title", card.transform, title, 40, UiKit.Text, TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.Place(0.03f, 0.62f, 0.68f, 0.95f);
            if (!string.IsNullOrEmpty(body))
                UiKit.Label("Body", card.transform, body, 28, UiKit.Muted, TextAnchor.UpperLeft).rectTransform.Place(0.03f, 0.3f, 0.68f, 0.66f);
            UiKit.Label("Cost", card.transform, string.IsNullOrEmpty(reason) ? cost : $"{cost}\n<color=#FF7369>{reason}</color>", 28, UiKit.Amber,
                TextAnchor.LowerLeft).rectTransform.Place(0.03f, 0.04f, 0.7f, 0.32f);
            var button = UiKit.Button("Action", card.transform, action, () => onClick(), enabled ? fill ?? UiKit.Accent : UiKit.Panel, 36);
            button.interactable = enabled;
            ((RectTransform)button.transform).Place(0.71f, 0.22f, 0.97f, 0.78f);
        }

        private void AddProgress(float progress)
        {
            var bar = UiKit.Box("Progress", _list, UiKit.Panel).Row(36);
            var fill = UiKit.Box("Fill", bar.transform, UiKit.Accent);
            fill.rectTransform.Place(0, 0, Mathf.Max(0.02f, progress), 1);
        }

        // --- Timers over buildings ----------------------------------------------------------

        private void UpdateTimers()
        {
            var camera = _grid?.Config != null ? Camera.main : null;
            var live = new HashSet<string>();
            foreach (var building in Buildings())
            {
                if (!building.IsUnderConstruction || camera == null)
                    continue;

                live.Add(building.instanceKey);
                if (!_timers.TryGetValue(building.instanceKey, out var timer))
                {
                    var root = UiKit.Rect("Timer", _overlay);
                    root.sizeDelta = new Vector2(220, 64);
                    UiKit.Box("Back", root, new Color(0, 0, 0, 0.6f)).rectTransform.Place(0, 0, 1, 0.35f);
                    var fill = UiKit.Box("Fill", root, UiKit.Accent);
                    var label = UiKit.Label("Time", root, string.Empty, 32, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    label.rectTransform.Place(0, 0.35f, 1, 1);
                    label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
                    timer = (root, fill, label);
                    _timers[building.instanceKey] = timer;
                }

                var world = _grid.GetWorldPosition(new PolarGridSlotId(building.cellRing, building.cellSector)) + Vector3.up * 4.5f;
                var screen = camera.WorldToScreenPoint(world);
                timer.Root.gameObject.SetActive(screen.z > 0f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlay, screen, null, out var local);
                timer.Root.anchoredPosition = local;
                var remaining = building.constructionEndsAt - Time.realtimeSinceStartup;
                var progress = building.constructionSeconds > 0 ? Mathf.Clamp01(1f - remaining / building.constructionSeconds) : 1f;
                timer.Fill.rectTransform.Place(0, 0, Mathf.Max(0.02f, progress), 0.35f);
                timer.Label.text = Loc.Duration(remaining);
                timer.Label.color = remaining <= _catalog.Progression.freeFinishSeconds ? UiKit.Accent : UiKit.Text;
            }

            foreach (var key in _timers.Keys.Where(k => !live.Contains(k)).ToList())
            {
                Destroy(_timers[key].Root.gameObject);
                _timers.Remove(key);
            }
        }

        // --- Commands ------------------------------------------------------------------------

        private void Place(string definitionId) =>
            _commands.Dispatch(new PlaceBuildingCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = ++_sequence,
                BuildingDefinitionId = definitionId,
                RingIndex = _ring,
                SectorIndex = _sector
            });

        private void Upgrade(string instanceKey) =>
            _commands.Dispatch(new UpgradeBuildingCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = ++_sequence,
                BuildingEntityId = instanceKey
            });

        /// <summary>First tap asks for confirmation, the second sends the command.</summary>
        private void Remove(string instanceKey)
        {
            if (_confirmRemove != instanceKey)
            {
                _confirmRemove = instanceKey;
                Render();
                return;
            }

            _confirmRemove = string.Empty;
            _commands.Dispatch(new RemoveBuildingCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = ++_sequence,
                BuildingEntityId = instanceKey
            });
        }

        private void UnlockRing(int ring) =>
            _commands.Dispatch(new UnlockRingCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = ++_sequence,
                Ring = ring
            });

        private void Finish(string instanceKey) =>
            _commands.Dispatch(new FinishConstructionCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = ++_sequence,
                BuildingEntityId = instanceKey
            });

        // --- State helpers -------------------------------------------------------------------

        private BuildingSaveEntry[] Buildings() => _stateSync?.View.Data.buildings ?? Array.Empty<BuildingSaveEntry>();

        private int ActiveConstructions() => Buildings().Count(b => b.IsUnderConstruction);

        private Dictionary<string, int> OwnedCounts() =>
            Buildings().GroupBy(b => b.definitionId).ToDictionary(g => g.Key, g => g.Count());

        private int ReactorLevel() =>
            Buildings().Where(b => b.definitionId == BuildingProgression.ReactorId).Select(b => b.level).DefaultIfEmpty(0).Max();

        private Dictionary<ResourceType, long> Wallet()
        {
            var wallet = new Dictionary<ResourceType, long>();
            foreach (var entry in _stateSync.View.Data.resources ?? Array.Empty<ResourceSaveEntry>())
                wallet[ResourceTypeMap.FromStoredValue(entry.resourceType)] = entry.amount;
            return wallet;
        }

        private static string Rates(BuildingDefinition definition, int level)
        {
            var text = new StringBuilder();
            void Append(string label, IReadOnlyList<KeyValuePair<ResourceType, long>> amounts, string sign)
            {
                if (amounts.Count == 0)
                    return;
                text.Append(text.Length > 0 ? "   " : string.Empty).Append(label).Append(": ");
                text.Append(string.Join(", ", amounts.Select(a =>
                    $"{sign}{ProductionFormulas.ScaleByLevelMilli(a.Value, level) * TicksPerMinute / ProductionFormulas.Milli:0} {Loc.T("resource." + a.Key)}{Loc.T("base.permin")}")));
            }

            Append(Loc.T("base.produces"), definition.Production, "+");
            Append(Loc.T("base.consumes"), definition.Consumption, "-");
            return text.ToString();
        }

        private static string Cost(IReadOnlyList<KeyValuePair<ResourceType, long>> cost, IReadOnlyDictionary<ResourceType, long> wallet) =>
            cost.Count == 0 ? "-" : string.Join("  ", cost.Select(c =>
            {
                var enough = wallet.TryGetValue(c.Key, out var have) && have >= c.Value;
                return enough ? $"{c.Value} {Loc.T("resource." + c.Key)}" : $"<color=#FF7369>{c.Value} {Loc.T("resource." + c.Key)}</color>";
            }));
    }
}
