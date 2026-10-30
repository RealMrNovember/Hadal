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
using TMPro;
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
        private TMP_Text _title;
        private TMP_Text _subtitle;
        private RectTransform _list;
        private RectTransform _footer;
        private TMP_Text _toast;
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
        private readonly Dictionary<string, (RectTransform Root, Image Fill, TMP_Text Label)> _timers = new();

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

            // Labels pinned to buildings (levels, timers) belong to the world: their own canvas under the HUD, so a
            // level plate never sits on top of a HUD button.
            var labels = UiKit.Canvas("BaseLabels", 30);
            labels.transform.SetParent(transform, false);
            _overlay = UiKit.Rect("Timers", labels.transform).Place(0, 0, 1, 1);

            // Building details, the build list, expand and research: the genre's centred dialog over the blurred base,
            // as tall as its content, with the page's main button pinned in the footer.
            var sheet = UiFrames.SizedDialog(_canvas.transform, Close, 0.78f, footer: true);
            _sheet = sheet.Root.gameObject.AddComponent<CanvasGroup>();
            _title = sheet.Parts.Title;
            _subtitle = sheet.Parts.Subtitle;
            _list = sheet.List;
            _footer = sheet.Footer;

            _expand = UiKit.Button("Expand", _canvas.transform, Loc.T("base.expand"), OpenExpand, new Color(0.03f, 0.10f, 0.14f, 0.85f), 36);
            _expand.GetComponentInChildren<TMP_Text>().color = UiKit.Accent;
            ((RectTransform)_expand.transform).Place(0.03f, 0.02f, 0.4f, 0.07f);

            _toast = UiKit.Label("Toast", _canvas.transform, string.Empty, 38, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _toast.rectTransform.Place(0.05f, 0.55f, 0.95f, 0.6f);
            UiText.Outline(_toast, new Color(0, 0, 0, 0.8f));

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
                Reinforce = (id, name, x, y) => { Close(); _social.Close(); _map.Reinforce(id, name, x, y); },
                RequestHelp = () =>
                {
                    var view = _stateSync.View.WorldView;
                    if (view != null)
                        _social.ShareLocation(view.MyX, view.MyY, "me", Loc.T("pvp.help.request"));
                }
            });

            // Alliance rally point card with the instant alliance teleport (docs/30 §6.3.1).
            var rally = new GameObject("RallyPoint");
            rally.transform.SetParent(transform, false);
            rally.AddComponent<RallyPointPrompt>().Initialize(_stateSync, _commands, new RallyPointPrompt.Actions
            {
                ShowOnMap = (x, y) => _social.ShowOnMap(x, y),
                OpenMap = () => { Close(); _social.Close(); _map.Open(); }
            });

            // Last Asylum's City List, opened from the alliance hub (docs/32 §2.4).
            var cities = new GameObject("CityList");
            cities.transform.SetParent(transform, false);
            cities.AddComponent<CityList>().Initialize(_stateSync, _commands, (x, y) => _social.ShowOnMap(x, y));

            var speedups = new GameObject("SpeedupWindow");
            speedups.transform.SetParent(transform, false);
            speedups.AddComponent<SpeedupWindow>().Initialize(_stateSync, _commands, _localBus);
            speedups.AddComponent<BattleReportWindow>().Initialize(_stateSync);

            // Music for the screen on show, effects for what the server confirms.
            var audio = new GameObject("AudioDirector");
            audio.transform.SetParent(transform, false);
            audio.AddComponent<AudioDirector>().Initialize(_stateSync, _localBus);

            // Phone notifications for the timers above while the game is closed; a tap opens its screen (docs/36).
            var phone = new GameObject("PhoneNotifications");
            phone.transform.SetParent(transform, false);
            phone.AddComponent<PhoneNotifications>().Initialize(_stateSync, _commands, new PhoneNotifications.Actions
            {
                Base = () => { Close(); _social.Close(); _map.Close(); },
                Research = OpenResearchFromHud,
                Map = () => { Close(); _social.Close(); _map.Open(); },
                Vip = () => { Close(); _map.Close(); _social.OpenVip(); },
                Events = () => { Close(); _map.Close(); _social.OpenEvents(); },
                Mail = () => { Close(); _map.Close(); _social.OpenMail(); },
                Alliance = () => { Close(); _map.Close(); _social.OpenAlliance(); },
                Chat = () => { Close(); _map.Close(); _social.OpenChat(); }
            });

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
                Guide = () => OnboardingGuide.Instance?.Go(),
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
                InWorld = () => _map.IsOpen,
                RepairEmergency = id => _commands.Dispatch(new RepairEmergencyCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, EmergencyId = id ?? string.Empty
                }),
                AskRepairHelp = id => _commands.Dispatch(new RequestHelpCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, Kind = HelpKinds.Repair, Target = id ?? string.Empty
                })
            });

            // The first-session guide over the HUD (docs/34).
            CreateGuide();

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
            if (SpeedupWindow.Instance != null && SpeedupWindow.Instance.IsOpen)
                SpeedupWindow.Instance.Close();
            else if (_upgrade != null)
                CloseUpgrade();
            else if (_training != null)
                CloseTraining();
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
                or CommandType.TrainTroops or CommandType.CancelTraining or CommandType.HealTroops or CommandType.CancelHeal
                or CommandType.RepairEmergency))
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
                CommandType.RepairEmergency => Loc.T("survival.toast.repairing"),
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

            UiFrames.Clear(_list);
            UiFrames.Clear(_footer);

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

        /// <summary>
        /// The next ring (Last Asylum's expand): the ring as a picture card, what it needs as ticked lines, and the
        /// price with Open in the footer.
        /// </summary>
        private void RenderExpand()
        {
            _title.text = Loc.T("base.expand");
            var maxRings = _catalog.Grid.maxRings;
            var ring = _grid.UnlockedRingCount + 1;
            _subtitle.text = Loc.F("base.expand.open.rings", _grid.UnlockedRingCount, maxRings);

            if (ring > maxRings || !_catalog.TryGetRingUnlock(ring, out var unlock))
            {
                UiFrames.Note(_list, Loc.T("base.expand.all"), UiFrames.Ink).Row(140);
                return;
            }

            var wallet = Wallet();
            var finished = new HashSet<string>(Buildings().Where(b => b.level > 0).Select(b => b.definitionId));
            var verdict = BaseRules.ValidateUnlock(_catalog, ring, _grid.UnlockedRingCount, ReactorLevel(), finished, wallet);

            var card = UiKit.Box("Ring", _list, UiFrames.Card);
            card.sprite = UiKit.Rounded;
            card.raycastTarget = false;
            card.Row(190);
            Hud.HudIcons.Image(card.transform, "left_expand", 0.03f, 0.1f, 0.22f, 0.9f);
            UiKit.Label("Name", card.transform, Loc.F("base.expand.ring", ring), UiKit.TypeScale.Title, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.26f, 0.5f, 0.97f, 0.95f);
            UiKit.Label("Text", card.transform, Loc.F("base.expand.body", _catalog.GetSectorCount(ring)), UiKit.TypeScale.Body, UiFrames.InkSoft)
                .rectTransform.Place(0.26f, 0.06f, 0.97f, 0.5f);

            UiFrames.Section(_list, Loc.T("upgrade.spend")).Row(60);
            UiFrames.NeedRow(_list, "power", Loc.F("base.needs.reactor", unlock.ReactorLevel), ReactorLevel() >= unlock.ReactorLevel).Row(84);
            if (!string.IsNullOrEmpty(unlock.RequiresBuilding) && _catalog.TryGetBuilding(unlock.RequiresBuilding, out var required))
                UiFrames.NeedRow(_list, BuildingIcon(required), Loc.Building(required.Id), finished.Contains(required.Id)).Row(84);

            AddFooter(null, null, unlock.Cost, wallet, -1f, verdict == RingRejectReason.None ? string.Empty : Loc.Reason(verdict.ToString()),
                Loc.T("base.expand.open"), null, verdict == RingRejectReason.None, () => UnlockRing(ring));
        }

        private string _buildPick = string.Empty;

        /// <summary>
        /// The build list for a free slot (Last Asylum's): a grid of building cards with their icon, name and time, or
        /// why they are locked; the chosen card's description, price and Build button sit in the footer.
        /// </summary>
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
            var entries = new List<(BuildingDefinition Definition, PlacementRejectReason Verdict, string Reason, int Count)>();

            // Buildable first, then by the reactor level that unlocks them, then by price.
            foreach (var definition in _catalog.Buildings.OrderBy(b => b.RequiredReactorLevel).ThenBy(b => b.BuildCost.Sum(c => c.Value)))
            {
                var verdict = PlacementRules.Validate(_catalog, definition.Id, _ring, _sector, _grid.UnlockedRingCount, occupied, wallet, owned, reactor, modifiers);
                // Buildings that can never go on this slot are not listed; the rest show why they are blocked.
                if (verdict is PlacementRejectReason.RingNotAllowed or PlacementRejectReason.SlotTypeMismatch
                    or PlacementRejectReason.SectorRestricted or PlacementRejectReason.SlotDoesNotExist or PlacementRejectReason.UnknownBuilding)
                    continue;

                owned.TryGetValue(definition.Id, out var count);
                var reason = verdict switch
                {
                    PlacementRejectReason.None => builderBusy ? Loc.Reason(nameof(ConstructionRejectReason.BuilderBusy)) : string.Empty,
                    PlacementRejectReason.ReactorLevelTooLow => Loc.F("base.needs.reactor", definition.UnlockFor(count)),
                    PlacementRejectReason.LimitReached => Loc.F("base.limit", count, modifiers.MaxCount(definition)),
                    _ => Loc.Reason(verdict.ToString())
                };
                entries.Add((definition, verdict, reason, count));
            }

            if (entries.Count == 0)
            {
                UiFrames.Note(_list, Loc.T("base.nothing"), UiFrames.Ink).Row(140);
                return;
            }

            if (!entries.Any(e => e.Definition.Id == _buildPick))
                _buildPick = (entries.FirstOrDefault(e => e.Reason.Length == 0).Definition ?? entries[0].Definition).Id;

            var grid = UiFrames.Grid(_list, 3, 0.8f, 12f);
            grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(0, 0, 4, 4);
            foreach (var (definition, verdict, reason, count) in entries)
            {
                var id = definition.Id;
                var locked = verdict is PlacementRejectReason.ReactorLevelTooLow or PlacementRejectReason.LimitReached;
                var seconds = modifiers.BuildSeconds(BuildingProgression.Seconds(definition, 1, _catalog.Progression));
                UiFrames.Tile(grid, BuildingIcon(definition), Loc.Building(id),
                    reason.Length == 0 ? Loc.Duration(seconds) : reason, reason.Length == 0 ? UiFrames.InkSoft : UiFrames.InkBad,
                    () =>
                    {
                        _buildPick = id;
                        Render();
                    }, id == _buildPick, locked, definition.MaxCount > 0 ? $"{count}/{modifiers.MaxCount(definition)}" : null);
            }

            var pick = entries.First(e => e.Definition.Id == _buildPick);
            var pickId = pick.Definition.Id;
            var rates = Rates(pick.Definition, 1);
            AddFooter(Loc.Building(pickId), rates.Length > 0 ? $"{Loc.T("desc." + pickId)}\n{rates}" : Loc.T("desc." + pickId),
                BuildingProgression.Cost(pick.Definition, 1, _catalog.Progression), wallet,
                modifiers.BuildSeconds(BuildingProgression.Seconds(pick.Definition, 1, _catalog.Progression)),
                pick.Reason, Loc.T("base.build"), null, pick.Reason.Length == 0, () => Place(pickId));
        }

        /// <summary>
        /// A building's details (Last Asylum's): the description card, the running job with its buttons, the level table
        /// with the current level in gold, the building's own functions, Remove at the bottom and Upgrade in the footer.
        /// </summary>
        private void RenderBuilding(BuildingSaveEntry building)
        {
            if (!_catalog.TryGetBuilding(building.definitionId, out var definition))
                return;

            _title.text = Loc.Building(definition.Id);
            _subtitle.text = building.level == 0
                ? Loc.T("base.build")
                : Loc.F("base.level", building.level) + (building.level >= definition.MaxLevel ? $" · {Loc.T("base.level.max")}" : string.Empty);

            UiFrames.Note(_list, Loc.T("desc." + definition.Id), UiFrames.Ink).Row(130);

            if (building.IsUnderConstruction)
                AddConstructionJob(building, definition);

            AddLevelTable(definition, building.level);

            // The building's own function (research, boost, overdrive, salvage) once it works.
            if (building.level > 0)
                AddFunctionCards(building, definition);

            AddRemoveCard(building, definition);

            // Upgrade opens the upgrade window (what the level brings, what it needs, Go buttons).
            if (!building.IsUnderConstruction && building.level < definition.MaxLevel)
            {
                var key = building.instanceKey;
                AddFooter(null, null, null, null, -1f, string.Empty, Loc.T("base.upgrade"), Loc.F("base.level", building.level + 1), true,
                    () => OpenUpgrade(key));
            }
        }

        /// <summary>The construction or upgrade under way: the bar with the time left, Ask help, Speed up or Finish Free.</summary>
        private void AddConstructionJob(BuildingSaveEntry building, BuildingDefinition definition)
        {
            var remaining = building.constructionEndsAt - Time.realtimeSinceStartup;
            var progress = building.constructionSeconds > 0 ? Mathf.Clamp01(1f - remaining / building.constructionSeconds) : 1f;
            var free = remaining <= _catalog.Progression.freeFinishSeconds;
            var key = building.instanceKey;
            var title = building.level == 0 ? Loc.T("base.build") : $"{Loc.T("base.upgrade")} → {Loc.F("base.level", building.level + 1)}";
            var job = UiFrames.Job(_list, title, progress, Loc.Duration(remaining));

            // Alliance mates can shave time off a running construction (docs/25).
            if (_stateSync.View.MyAlliance != null && !free)
            {
                var asked = _helpRequested.Contains(key + ":" + building.level);
                var level = building.level;
                var help = UiKit.Button("Help", job.Buttons, asked ? Loc.T("alliance.help.asked") : Loc.T("alliance.help.ask"), () => AskHelp(key, level), UiKit.Ok, 32);
                help.interactable = !asked;
            }

            if (free)
                UiKit.Button("Finish", job.Buttons, Loc.T("base.finish"), () => Finish(key), UiKit.Ok, 32);
            else
                UiKit.Button("Speed", job.Buttons, Loc.T("base.speedup"), () => SpeedUpBuilding(key, definition.Id), UiKit.Primary, 32);
        }

        /// <summary>
        /// The level table (Last Asylum's details): level, the stats that matter for this building and its power, a few
        /// levels around the current one, which is gold.
        /// </summary>
        private void AddLevelTable(BuildingDefinition definition, int level)
        {
            var columns = LevelColumns(definition);
            var table = UiKit.Column("Table", _list, 0f);
            var head = new List<string> { Loc.T("table.level") };
            head.AddRange(columns.Select(c => c.Head));
            head.Add(Loc.T("upgrade.power"));
            UiFrames.TableRow(table, head.ToArray(), header: true).Row(80);

            var first = Mathf.Clamp(level - 2, 1, Math.Max(1, definition.MaxLevel - 8));
            var last = Math.Min(definition.MaxLevel, first + 8);
            for (var l = first; l <= last; l++)
            {
                var cells = new List<string> { l.ToString() };
                var at = l;
                cells.AddRange(columns.Select(c => c.Value(at)));
                cells.Add(CostChips.Compact(BuildingProgression.Power(definition, l)));
                UiFrames.TableRow(table, cells.ToArray(), highlight: l == level, index: l - first).Row(64);
            }
        }

        /// <summary>The columns between level and power: troops for military buildings, otherwise the hourly output (or upkeep).</summary>
        private List<(string Head, Func<int, string> Value)> LevelColumns(BuildingDefinition definition)
        {
            var columns = new List<(string Head, Func<int, string> Value)>();
            if (TroopRules.IsTrainingBuilding(Troops, definition.Id))
            {
                columns.Add((Loc.T("table.batch"), l => CostChips.Compact(TroopRules.BatchSize(Troops, l))));
                columns.Add((Loc.T("table.tier"), l => $"T{TroopRules.MaxTier(Troops, l)}"));
                return columns;
            }

            if (definition.Id == Troops.capacityBuilding)
            {
                columns.Add((Loc.T("army.capacity"), l => CostChips.Compact(TroopRules.Interpolate(Troops.capacity, l))));
                return columns;
            }

            if (definition.Id == Troops.infirmary.building)
            {
                columns.Add((Loc.T("table.beds"), l => CostChips.Compact(TroopRules.Interpolate(Troops.infirmary.capacity, l))));
                return columns;
            }

            var production = HourlyRates(definition, 1, true).Count > 0;
            foreach (var resource in HourlyRates(definition, 1, production).Keys)
            {
                var r = resource;
                columns.Add(($"{Loc.Resource(r.ToString())}{Loc.T("base.perhour")}",
                    l => (production ? string.Empty : "-") + CostChips.Compact(HourlyRates(definition, l, production).TryGetValue(r, out var v) ? v : 0)));
            }

            return columns;
        }

        /// <summary>A level's hourly output (or upkeep), from the balance table or the base rate scaled by level.</summary>
        private static Dictionary<ResourceType, long> HourlyRates(BuildingDefinition definition, int level, bool production)
        {
            var row = definition.Level(level);
            if (row != null)
                return (production ? row.ProductionMilliPerTick : row.ConsumptionMilliPerTick)
                    .ToDictionary(p => p.Key, p => p.Value * SimulationTick.TicksPerHour / ProductionFormulas.Milli);
            return (production ? definition.Production : definition.Consumption)
                .ToDictionary(p => p.Key, p => ProductionFormulas.ScaleByLevelMilli(p.Value, level) * SimulationTick.TicksPerHour / ProductionFormulas.Milli);
        }

        /// <summary>The icon a building's card shows (no building portraits yet: what it makes or what it is for).</summary>
        private string BuildingIcon(BuildingDefinition definition)
        {
            var troop = TroopRules.TypeOfBuilding(Troops, definition.Id);
            if (troop != null)
                return "item_troops_" + troop.id;
            if (definition.Id == BuildingProgression.ReactorId)
                return "power";
            if (definition.Id == ResearchRules.CenterId)
                return "left_research";
            if (definition.Id == Troops.capacityBuilding)
                return "event_war";
            if (definition.Id == Troops.infirmary.building)
                return "left_help";
            if (definition.Production.Count > 0)
                return Hud.HudIcons.Resource(definition.Production[0].Key);
            return definition.Id switch
            {
                "submarine_dock" => "nav_world",
                "radar_center" => "left_search",
                "shield_generator" => "item_shield_24h",
                "alliance_hub" => "side_alliance",
                "decoration_spire" => "vip",
                _ => "queue_build"
            };
        }

        /// <summary>Remove (or cancel a first construction) as a quiet line at the bottom; the first tap asks to confirm.</summary>
        private void AddRemoveCard(BuildingSaveEntry building, BuildingDefinition definition)
        {
            if (BaseRules.ValidateRemove(definition.Id, building.level, building.IsUnderConstruction) != RemoveRejectReason.None)
                return;

            var cancel = building.level == 0;
            var refund = BaseRules.Refund(definition, building.level, _catalog.Progression);
            var confirming = _confirmRemove == building.instanceKey;
            var key = building.instanceKey;

            var row = UiKit.Box("Remove", _list, UiFrames.CardAlt);
            row.sprite = UiKit.Rounded;
            row.raycastTarget = false;
            row.Row(120);
            var small = Mathf.RoundToInt(UiKit.TypeScale.Caption * UiKit.Scale);
            var text = confirming
                ? $"<color=#{ColorUtility.ToHtmlStringRGB(UiFrames.InkBad)}>{Loc.T("base.confirm")}</color>"
                : $"{Loc.T(cancel ? "base.cancel.title" : "base.remove.title")}\n<size={small}>{Loc.F("base.refund", string.Join("  ", refund.Select(c => $"{CostChips.Compact(c.Value)} {Loc.Resource(c.Key.ToString())}")))}</size>";
            UiKit.Label("Text", row.transform, text, UiKit.TypeScale.Body, UiFrames.InkSoft, TextAnchor.MiddleLeft).Fit(14, UiKit.TypeScale.Body)
                .rectTransform.Place(0.04f, 0.05f, 0.68f, 0.95f);
            var button = UiKit.Button("Action", row.transform, confirming ? Loc.T("base.confirm.yes") : Loc.T(cancel ? "base.cancel" : "base.remove"),
                () => Remove(key), confirming ? UiKit.Danger : new Color(0.95f, 0.97f, 1f, 1f), 30);
            if (!confirming)
                button.GetComponentInChildren<TMP_Text>().color = UiFrames.InkBad;
            ((RectTransform)button.transform).Place(0.71f, 0.16f, 0.97f, 0.84f);
        }

        /// <summary>One action card in the list (boost, overdrive, salvage…): <see cref="UiFrames.ActionCard"/>.</summary>
        private void AddCard(string title, string body, string cost, string reason, string action, bool enabled, Action onClick, Color? fill = null,
            IReadOnlyList<KeyValuePair<ResourceType, long>> costs = null, IReadOnlyDictionary<ResourceType, long> wallet = null, float seconds = -1f) =>
            UiFrames.ActionCard(_list, title, body, reason, action, enabled, onClick, fill, costs, wallet, seconds, cost);

        /// <summary>
        /// The page's call to action in the pinned footer (Last Asylum's bottom button): an optional heading and line,
        /// the price with the time, why it is blocked in red, then the button with its detail (a time, a level) and an
        /// optional light secondary button on its left.
        /// </summary>
        private void AddFooter(string heading, string body, IReadOnlyList<KeyValuePair<ResourceType, long>> costs, IReadOnlyDictionary<ResourceType, long> wallet,
            float seconds, string reason, string action, string detail, bool enabled, Action onClick, Color? fill = null, string secondary = null,
            Action onSecondary = null)
        {
            if (!string.IsNullOrEmpty(heading))
                UiKit.Label("Heading", _footer, heading, UiKit.TypeScale.Body + 2, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold)
                    .Fit(18, UiKit.TypeScale.Body + 2).Row(48);
            if (!string.IsNullOrEmpty(body))
                UiKit.Label("Body", _footer, body, UiKit.TypeScale.Caption + 2, UiFrames.InkSoft, TextAnchor.UpperLeft)
                    .Fit(14, UiKit.TypeScale.Caption + 2).Row(92);
            if (costs != null && (costs.Count > 0 || seconds >= 0f))
                UiFrames.Costs(_footer, costs, wallet, seconds).Row(54);
            if (!string.IsNullOrEmpty(reason))
                UiKit.Label("Reason", _footer, reason, UiKit.TypeScale.Caption + 2, UiFrames.InkBad, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .Fit(14, UiKit.TypeScale.Caption + 2).Row(44);

            var strip = UiFrames.ButtonStrip(_footer, 26f);
            strip.Row(116);
            var side = onSecondary == null ? 150 : 20;
            strip.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(side, side, 0, 0);
            if (onSecondary != null)
                UiKit.Button("Secondary", strip, secondary, () => onSecondary(), new Color(0.95f, 0.97f, 1f, 1f), 34);
            var size = Mathf.RoundToInt(UiKit.TypeScale.Caption * UiKit.Scale);
            var text = string.IsNullOrEmpty(detail) ? action : $"{action}\n<size={size}>{detail}</size>";
            var button = UiKit.Button("Action", strip, text, () => onClick(), enabled ? fill ?? UiKit.Ok : UiKit.Panel, 36);
            button.interactable = enabled;
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
                    UiText.Outline(label, new Color(0, 0, 0, 0.8f));
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

        private readonly Dictionary<string, (RectTransform Root, TMP_Text Label)> _badges = new();

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
    }
}
