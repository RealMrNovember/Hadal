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
        private SocialView _social;
        private WorldMapView _map;
        private readonly HashSet<string> _helpRequested = new();
        private bool _expandOpen;
        private string _confirmRemove = string.Empty;

        private int _ring;
        private int _sector;
        private string _buildingKey = string.Empty;
        private float _nextRefresh;
        private float _lastPointerTime = -1f;
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
            sheetImage.gameObject.AddComponent<PopIn>().Slide = true;

            UiKit.TitleBar(sheetImage.transform, 0.87f);
            _title = UiKit.Stroked(UiKit.Label("Title", sheetImage.transform, string.Empty, 48, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold));
            _title.rectTransform.Place(0.05f, 0.885f, 0.8f, 0.985f);
            _subtitle = UiKit.Label("Subtitle", sheetImage.transform, string.Empty, 32, UiKit.Muted);
            _subtitle.rectTransform.Place(0.05f, 0.8f, 0.95f, 0.865f);

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

            // Heroes, alliance and chat share this view's services.
            var social = new GameObject("SocialUi");
            social.transform.SetParent(transform, false);
            _social = social.AddComponent<SocialView>();
            _social.Initialize(_stateSync, _commands, _localBus);

            var map = new GameObject("WorldMap");
            map.transform.SetParent(transform, false);
            _map = map.AddComponent<WorldMapView>();
            _map.Initialize(_stateSync, _commands, Toast, _localBus);
            _map.MessagePlayer = (id, name) =>
            {
                _map.Close();
                _social.OpenPrivateChat(id, name);
            };
            _map.ShareLocation = (x, y, kind, label) => _social.ShareLocation(x, y, kind, label);
            _social.ShowOnMap = (x, y) =>
            {
                Close();
                _social.Close();
                _map.OpenAt(x, y);
            };

            // PvP over every screen: the red alarm, battle toasts and the war window (docs/30).
            var pvp = new GameObject("PvpCenter");
            pvp.transform.SetParent(transform, false);
            pvp.AddComponent<PvpCenter>().Initialize(_stateSync, _commands, new PvpCenter.Actions
            {
                ShowOnMap = (x, y) => _social.ShowOnMap(x, y),
                JoinRally = row => { Close(); _social.Close(); _map.JoinRally(row); },
                Reinforce = (id, name, x, y) => { Close(); _social.Close(); _map.Reinforce(id, name, x, y); }
            });

            // Alliance rally point card with the instant alliance teleport (docs/30 §6.3.1).
            var rally = new GameObject("RallyPoint");
            rally.transform.SetParent(transform, false);
            rally.AddComponent<RallyPointPrompt>().Initialize(_stateSync, _commands, new RallyPointPrompt.Actions
            {
                ShowOnMap = (x, y) => _social.ShowOnMap(x, y),
                OpenMap = () => { Close(); _social.Close(); _map.Open(); }
            });

            var speedups = new GameObject("SpeedupWindow");
            speedups.transform.SetParent(transform, false);
            speedups.AddComponent<SpeedupWindow>().Initialize(_stateSync, _commands, _localBus);
            speedups.AddComponent<BattleReportWindow>().Initialize(_stateSync);

            // Music for the screen on show, effects for what the server confirms.
            var audio = new GameObject("AudioDirector");
            audio.transform.SetParent(transform, false);
            audio.AddComponent<AudioDirector>().Initialize(_stateSync, _localBus);

            // Profile cards over every screen: from chat faces, member lists and bases on the map.
            var profile = new GameObject("ProfileCard");
            profile.transform.SetParent(transform, false);
            profile.AddComponent<ProfileCard>().Initialize(_stateSync, _commands, _localBus, new ProfileCard.Actions
            {
                Message = (id, name) =>
                {
                    Close();
                    _map.Close();
                    _social.OpenPrivateChat(id, name);
                },
                Mail = (id, name) =>
                {
                    Close();
                    _map.Close();
                    _social.OpenMailCompose(id, name);
                },
                ShowOnMap = (x, y) => _social.ShowOnMap(x, y),
                Settings = () =>
                {
                    Close();
                    _social.Close();
                    FindFirstObjectByType<AccountPanelView>()?.Open();
                }
            });

            // The main screen frame: header, side columns, bottom bar (docs/26_HUD.md).
            var hud = new GameObject("MainHud");
            hud.transform.SetParent(transform, false);
            hud.AddComponent<Hud.MainHudView>().Initialize(_stateSync, new Hud.MainHudView.Actions
            {
                Heroes = () => { Close(); _social.OpenHeroes(); },
                Pod = () => { Close(); _social.OpenHeroes(); },
                Alliance = () => { Close(); _social.OpenAlliance(); },
                AllianceHelp = () => { Close(); _social.OpenAlliance(help: true); },
                HelpAll = kind => _commands.Dispatch(new HelpAllCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, Kind = kind ?? string.Empty
                }),
                Chat = () => { Close(); _social.OpenChat(); },
                Research = OpenResearchFromHud,
                Expand = () => { _social.Close(); _map.Close(); OpenExpand(); },
                Settings = () =>
                {
                    // One panel at a time: settings never open over a building or a social page.
                    Close();
                    _social.Close();
                    FindFirstObjectByType<AccountPanelView>()?.Open();
                },
                Profile = () => ProfileCard.Instance?.ShowMine(),
                Quests = () => { Close(); _social.OpenQuests(); },
                Bag = () => { Close(); _social.OpenBag(); },
                Expedition = () => { Close(); _map.Close(); _social.OpenExpedition(); },
                Events = () => { Close(); _social.OpenEvents(); },
                Vip = () => { Close(); _social.OpenVip(); },
                Shop = () => { Close(); _social.OpenShop(); },
                Mail = () => { Close(); _social.OpenMail(); },
                War = () => PvpCenter.Instance?.Open(0),
                ClaimQuest = id => _social.ClaimQuest(id),
                Building = key => { _social.Close(); _map.Close(); OpenBuilding(key); },
                Toast = Toast,
                SheetOpen = () => IsOpen || _social.IsOpen || ModalOpen,
                Territory = () =>
                {
                    Close();
                    _social.Close();
                    if (_map.IsOpen)
                        _map.CloseToBase();
                    else
                        _map.Open();
                },
                InWorld = () => _map.IsOpen
            });

            // The HUD replaces the old resource list, the gear and the Expand button.
            _expand.gameObject.SetActive(false);
            var oldHud = FindFirstObjectByType<ResourceHudView>();
            if (oldHud != null)
                oldHud.gameObject.SetActive(false);
        }

        private bool _gearHidden;
        private float _nextGearCheck;

        /// <summary>The account UI is built after this view, so the gear is hidden once it exists.</summary>
        private void HideGearWhenReady()
        {
            if (_gearHidden || Time.unscaledTime < _nextGearCheck)
                return;
            _nextGearCheck = Time.unscaledTime + 1f;
            var account = FindFirstObjectByType<AccountPanelView>();
            if (account == null)
                return;
            account.HideGear();
            _gearHidden = true;
        }

        /// <summary>Android back closes the top panel: settings, then the social sheet, then the building sheet.</summary>
        private void HandleBack()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;
            var account = FindFirstObjectByType<AccountPanelView>();
            if (_upgrade != null)
                CloseUpgrade();
            else if (TargetMenu.IsOpen)
                TargetMenu.Hide();
            else if (account != null && account.IsOpen)
                account.Close();
            else if (_social.IsOpen)
                _social.Close();
            else if (IsOpen)
                Close();
        }

        /// <summary>Opens a building's panel (HUD queue rows, the suggested task).</summary>
        public void OpenBuilding(string instanceKey)
        {
            var building = Buildings().FirstOrDefault(b => b.instanceKey == instanceKey);
            if (string.IsNullOrEmpty(building.instanceKey))
                return;

            _ring = building.cellRing;
            _sector = building.cellSector;
            _buildingKey = instanceKey;
            _expandOpen = false;
            _researchOpen = false;
            _confirmRemove = string.Empty;
            _sheet.gameObject.SetActive(true);
            Render();
        }

        private void OpenResearchFromHud()
        {
            _social.Close();
            _map.Close();
            var center = Buildings().FirstOrDefault(b => b.definitionId == ResearchRules.CenterId && b.level > 0);
            if (string.IsNullOrEmpty(center.instanceKey))
            {
                Toast(Loc.Reason(nameof(ResearchRejectReason.NoResearchCenter)), UiKit.Danger);
                return;
            }

            OpenBuilding(center.instanceKey);
            OpenResearch();
        }

        // --- Events --------------------------------------------------------------------------

        private void OnSlotTapped(BaseSlotTappedEvent tap)
        {
            // A building gets the round action menu (Last Asylum pattern); a free slot the build list.
            var tapped = Buildings().FirstOrDefault(b => !string.IsNullOrEmpty(tap.BuildingInstanceKey) && b.instanceKey == tap.BuildingInstanceKey);
            if (!string.IsNullOrEmpty(tapped.instanceKey))
            {
                Close();
                _social.Close();
                ShowBuildingActions(tapped);
                return;
            }

            TargetMenu.Hide();
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
                or CommandType.BoostBuilding or CommandType.Overdrive or CommandType.StartSalvage
                or CommandType.TrainTroops or CommandType.CancelTraining or CommandType.HealTroops or CommandType.CancelHeal))
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
                CommandType.TrainTroops => Loc.T("train.started"),
                CommandType.CancelTraining => Loc.T("train.cancelled"),
                CommandType.HealTroops => Loc.T("heal.started"),
                CommandType.CancelHeal => Loc.T("train.cancelled"),
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
            TickUpgrade();
            TickTraining();
            ShowSalvageReports();
            HideGearWhenReady();
            HandleBack();

            if (_toast.text.Length > 0 && Time.unscaledTime > _toastUntil)
                _toast.text = string.Empty;

            // Countdowns and affordability change every second. Rebuilding destroys the buttons, so a press
            // (or a scroll drag) in progress would be lost: wait until the finger has been up for a moment.
            if (Input.touchCount > 0 || Input.GetMouseButton(0))
                _lastPointerTime = Time.unscaledTime;
            if (IsOpen && Time.unscaledTime >= _nextRefresh && Time.unscaledTime - _lastPointerTime > 0.3f)
                Render();
        }

        public void Close() => _sheet.gameObject.SetActive(false);

        private void Toast(string text, Color color) => Toaster.Show(text, color);

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
                string.Empty,
                verdict == RingRejectReason.None ? string.Empty : Loc.Reason(verdict.ToString()),
                Loc.T("base.expand.open"),
                verdict == RingRejectReason.None,
                () => UnlockRing(ring),
                costs: unlock.Cost, wallet: wallet);
        }

        private static string Check(bool ok) => ok ? "<color=#5BE37A>●</color> " : "<color=#FF7369>●</color> ";

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
                    PlacementRejectReason.ReactorLevelTooLow => Loc.F("base.needs.reactor", definition.UnlockFor(count)),
                    PlacementRejectReason.LimitReached => Loc.F("base.limit", count, modifiers.MaxCount(definition)),
                    _ => Loc.Reason(verdict.ToString())
                };
                var id = definition.Id;
                AddCard(
                    definition.MaxCount > 0 ? $"{Loc.Building(id)}  <size=30>{count}/{modifiers.MaxCount(definition)}</size>" : Loc.Building(id),
                    $"{Loc.T("desc." + id)}\n{Rates(definition, 1)}",
                    string.Empty,
                    reason,
                    Loc.T("base.build"),
                    reason.Length == 0,
                    () => Place(id),
                    costs: BuildingProgression.Cost(definition, 1, _catalog.Progression), wallet: wallet,
                    seconds: modifiers.BuildSeconds(BuildingProgression.Seconds(definition, 1, _catalog.Progression)));
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
            UiKit.Label("Desc", _list, Loc.T("desc." + definition.Id), 30, UiKit.Muted).Row(80);
            if (current.Length > 0)
                UiKit.Label("Now", _list, current, 30, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold).Row(60);

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

                // Alliance mates can shave time off a running construction (docs/25).
                if (_stateSync.View.MyAlliance != null && !free)
                {
                    var asked = _helpRequested.Contains(key + ":" + building.level);
                    var level = building.level;
                    AddCard(Loc.T("alliance.help.ask.title"), string.Empty, string.Empty, string.Empty,
                        asked ? Loc.T("alliance.help.asked") : Loc.T("alliance.help.ask"), !asked, () => AskHelp(key, level));
                }

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
                ActiveConstructions(), ReactorLevel(), wallet, modifiers.ExtraBuilders, HighestLevel);
            var instanceKey = building.instanceKey;

            // The reactor's next level lists the buildings it needs (docs/rehber 02), met ones in cyan.
            var body = Rates(definition, target);
            var needs = definition.Id == BuildingProgression.ReactorId ? _catalog.ReactorRequirements(target) : Array.Empty<KeyValuePair<string, int>>();
            if (needs.Count > 0)
                body += "\n" + Loc.T("base.needs.title") + " " + string.Join(", ", needs.Select(n =>
                    $"{Check(HighestLevel(n.Key) >= n.Value)}{Loc.Building(n.Key)} {Loc.F("base.level", n.Value)}"));
            else if (definition.Id != BuildingProgression.ReactorId && BuildingProgression.ReactorFor(definition, target) > ReactorLevel())
                body += "\n" + Loc.F("base.needs.reactor", BuildingProgression.ReactorFor(definition, target));

            AddCard(
                $"{Loc.F("base.level", building.level)}  →  <color=#38D6F2>{Loc.F("base.level", target)}</color>",
                body,
                string.Empty,
                verdict == ConstructionRejectReason.None ? string.Empty : Loc.Reason(verdict.ToString()),
                Loc.T("base.upgrade"),
                verdict == ConstructionRejectReason.None,
                () => Upgrade(instanceKey),
                costs: BuildingProgression.Cost(definition, target, rules), wallet: wallet,
                seconds: modifiers.BuildSeconds(BuildingProgression.Seconds(definition, target, rules)));

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

            // A quiet row at the bottom: removing is rare and must not compete with Upgrade.
            var row = UiKit.Box("Remove", _list, new Color(0.03f, 0.08f, 0.11f, 0.9f)).Row(confirming ? 150 : 120);
            var text = confirming
                ? $"<color=#FF7369>{Loc.T("base.confirm")}</color>"
                : $"{Loc.T(cancel ? "base.cancel.title" : "base.remove.title")}\n<size=22>{Loc.F("base.refund", string.Join("  ", refund.Select(c => $"{CostChips.Compact(c.Value)} {Loc.Resource(c.Key.ToString())}")))}</size>";
            UiKit.Label("Text", row.transform, text, 28, UiKit.Muted, TextAnchor.MiddleLeft).rectTransform.Place(0.04f, 0.05f, 0.68f, 0.95f);
            var button = UiKit.Button("Action", row.transform, confirming ? Loc.T("base.confirm.yes") : Loc.T(cancel ? "base.cancel" : "base.remove"),
                () => Remove(key), confirming ? UiKit.Danger : UiKit.Field, 30);
            if (!confirming)
                button.GetComponentInChildren<Text>().color = UiKit.Danger;
            ((RectTransform)button.transform).Place(0.71f, 0.18f, 0.97f, 0.82f);
        }

        /// <summary>
        /// One action card: title, a muted body, the blocking reason in red, then the cost as resource chips
        /// (red when short) and the time; the action button sits on the right (genre layout, docs/26).
        /// </summary>
        private void AddCard(string title, string body, string cost, string reason, string action, bool enabled, Action onClick, Color? fill = null,
            IReadOnlyList<KeyValuePair<ResourceType, long>> costs = null, IReadOnlyDictionary<ResourceType, long> wallet = null, float seconds = -1f)
        {
            var hasBody = !string.IsNullOrEmpty(body) || !string.IsNullOrEmpty(reason);
            var hasCost = costs != null || !string.IsNullOrEmpty(cost);
            var height = 130f + (hasBody ? 100f : 0f) + (hasCost ? 70f : 0f);
            var card = UiKit.Box("Card", _list, UiKit.Field).Row(height);

            var top = 1f - 70f / height;
            UiKit.Label("Title", card.transform, title, 38, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.04f, top, 0.68f, 0.97f);
            if (hasBody)
            {
                var text = string.IsNullOrEmpty(reason) ? body : $"{body}{(string.IsNullOrEmpty(body) ? string.Empty : "\n")}<color=#FF7369>{reason}</color>";
                UiKit.Label("Body", card.transform, text, 26, UiKit.Muted, TextAnchor.UpperLeft).rectTransform.Place(0.04f, hasCost ? 70f / height + 0.02f : 0.06f, 0.68f, top);
            }

            if (costs != null)
                CostChips.Row(card.transform, costs, wallet ?? new Dictionary<ResourceType, long>(), seconds).Place(0.04f, 0.04f, 0.7f, 0.04f + 56f / height);
            else if (!string.IsNullOrEmpty(cost))
                UiKit.Label("Cost", card.transform, cost, 26, UiKit.Amber, TextAnchor.MiddleLeft).rectTransform.Place(0.04f, 0.04f, 0.7f, 0.04f + 56f / height);

            var button = UiKit.Button("Action", card.transform, action, () => onClick(), enabled ? fill ?? UiKit.Accent : UiKit.Panel, 36);
            button.interactable = enabled;
            if (!enabled)
                button.GetComponentInChildren<Text>().color = UiKit.Muted; // a dark label on a dark button would vanish
            var mid = 0.5f;
            var half = Mathf.Min(0.3f, 50f / height);
            ((RectTransform)button.transform).Place(0.71f, mid - half, 0.97f, mid + half);
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
                var training = building.IsUnderConstruction ? null : TrainingAt(building.instanceKey);
                if ((!building.IsUnderConstruction && training == null) || camera == null)
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
                if (training != null)
                {
                    // Training: amber bar and "count · time" (Last Asylum's countdown over the training ground).
                    var now = _stateSync.View.ServerNowMs;
                    var left = Math.Max(0, (training.EndsAtMs - now) / 1000f);
                    var span = Math.Max(1, training.EndsAtMs - training.StartedAtMs);
                    timer.Fill.color = UiKit.Amber;
                    timer.Fill.rectTransform.Place(0, 0, Mathf.Max(0.02f, Mathf.Clamp01((now - training.StartedAtMs) / (float)span)), 0.35f);
                    timer.Label.text = $"{training.Count} · {Loc.Duration(left)}";
                    timer.Label.color = UiKit.Amber;
                    continue;
                }

                var remaining = building.constructionEndsAt - Time.realtimeSinceStartup;
                var progress = building.constructionSeconds > 0 ? Mathf.Clamp01(1f - remaining / building.constructionSeconds) : 1f;
                timer.Fill.color = UiKit.Accent;
                timer.Fill.rectTransform.Place(0, 0, Mathf.Max(0.02f, progress), 0.35f);
                timer.Label.text = Loc.Duration(remaining);
                timer.Label.color = remaining <= _catalog.Progression.freeFinishSeconds ? UiKit.Accent : UiKit.Text;
            }

            foreach (var key in _timers.Keys.Where(k => !live.Contains(k)).ToList())
            {
                Destroy(_timers[key].Root.gameObject);
                _timers.Remove(key);
            }

            UpdateLevelBadges(camera);
        }

        private readonly Dictionary<string, (RectTransform Root, Text Label)> _badges = new();

        /// <summary>A small level plate at the foot of every building (Last Asylum shows one on each).</summary>
        private void UpdateLevelBadges(Camera camera)
        {
            var show = camera != null && !GameModeInWorld && !ModalOpen;
            var live = new HashSet<string>();
            foreach (var building in Buildings())
            {
                if (!show || building.level <= 0)
                    continue;
                live.Add(building.instanceKey);
                if (!_badges.TryGetValue(building.instanceKey, out var badge))
                {
                    var plate = UiKit.Box("Level", _overlay, new Color(0.02f, 0.06f, 0.09f, 0.82f));
                    plate.raycastTarget = false;
                    plate.rectTransform.sizeDelta = new Vector2(64f, 40f);
                    var label = UiKit.Label("Text", plate.transform, string.Empty, 30, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                    label.rectTransform.Place(0, 0, 1, 1);
                    badge = (plate.rectTransform, label);
                    _badges[building.instanceKey] = badge;
                }

                var world = _grid.GetWorldPosition(new PolarGridSlotId(building.cellRing, building.cellSector)) + new Vector3(0f, 0.2f, -2.2f);
                var screen = camera.WorldToScreenPoint(world);
                badge.Root.gameObject.SetActive(screen.z > 0f && !_timers.ContainsKey(building.instanceKey));
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlay, screen, null, out var local);
                badge.Root.anchoredPosition = local;
                badge.Label.text = building.level.ToString();
            }

            foreach (var key in _badges.Keys.Where(k => !live.Contains(k)).ToList())
            {
                Destroy(_badges[key].Root.gameObject);
                _badges.Remove(key);
            }
        }

        private static bool GameModeInWorld => Hadal.Core.State.GameMode.InWorld;

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

        private void AskHelp(string instanceKey, int level)
        {
            _helpRequested.Add(instanceKey + ":" + level);
            _commands.Dispatch(new RequestHelpCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = ++_sequence,
                BuildingEntityId = instanceKey
            });
            Render();
        }

        /// <summary>Asks the alliance to help the Med Bay's healing or a research (HelpKinds); one request per job.</summary>
        private void AskJobHelp(string kind, string target, string key)
        {
            _helpRequested.Add(key);
            _commands.Dispatch(new RequestHelpCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = ++_sequence,
                Kind = kind,
                Target = target ?? string.Empty
            });
            Render();
        }

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

        /// <summary>The highest level among the player's buildings of one kind.</summary>
        private int HighestLevel(string definitionId) =>
            Buildings().Where(b => b.definitionId == definitionId).Select(b => b.level).DefaultIfEmpty(0).Max();

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
            // Balance-table buildings show the guide's hourly rates (docs/rehber K-17).
            var row = definition.Level(level);
            if (row != null)
            {
                var hourly = new StringBuilder();
                void Hourly(string label, IReadOnlyDictionary<ResourceType, long> milli, string sign)
                {
                    if (milli.Count == 0)
                        return;
                    hourly.Append(hourly.Length > 0 ? "   " : string.Empty).Append(label).Append(": ");
                    hourly.Append(string.Join(", ", milli.Select(a =>
                        $"{sign}{a.Value * SimulationTick.TicksPerHour / ProductionFormulas.Milli:N0} {Loc.T("resource." + a.Key)}{Loc.T("base.perhour")}")));
                }

                Hourly(Loc.T("base.produces"), row.ProductionMilliPerTick, "+");
                Hourly(Loc.T("base.consumes"), row.ConsumptionMilliPerTick, "-");
                return hourly.ToString();
            }

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
