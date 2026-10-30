using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.Events;
using Hadal.Core.Services;
using Hadal.Data.Models;
using TMPro;
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
            if (result.Type is not (CommandType.MarchGather or CommandType.RecallMarch or CommandType.InviteToAlliance or CommandType.MarchAttack
                or CommandType.Teleport or CommandType.MarchPvp or CommandType.Territory or CommandType.UseShield))
                return;
            if (result.Result == CommandResultCode.Rejected)
            {
                _toast?.Invoke(Loc.Reason(result.Reason), UiKit.Danger);
                return;
            }

            _toast?.Invoke(Loc.T(result.Type switch
            {
                CommandType.MarchGather => "world.march.sent",
                CommandType.MarchAttack => "world.attack.sent",
                CommandType.RecallMarch => "world.march.recalled",
                CommandType.Teleport => "world.teleport.done",
                CommandType.MarchPvp => "pvp.sent",
                CommandType.Territory => "pvp.territory.done",
                CommandType.UseShield => "pvp.shield.raised",
                _ => "alliance.invited"
            }), UiKit.Accent);
            ClosePopup();
            if (result.Type == CommandType.Teleport)
            {
                AfterTeleport();
                return;
            }

            if (result.Type == CommandType.Territory)
                RequestTerritory();
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
            name.textWrappingMode = TextWrappingModes.NoWrap;
            return plate.rectTransform;
        }

        private void StyleNodePlate(RectTransform plate, WorldNodeDto n)
        {
            var color = NodeLook.TryGetValue(n.Type, out var look) ? look.Color : Color.white;
            plate.Find("Level").GetComponent<Image>().color = color;
            plate.Find("Level/Text").GetComponent<TMP_Text>().text = n.Level.ToString();
            var state = n.Mine ? " <color=#38D6F2>◆</color>" : n.Busy ? " <color=#FFB347>●</color>" : string.Empty;
            plate.Find("Name").GetComponent<TMP_Text>().text = NodeName(n.Type) + state;
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
                var homeTroops = (View.Army?.Troops ?? Array.Empty<TroopStackDto>()).ToDictionary(t => TroopRules.Key(t.Type, t.Tier), t => t.Count);
                var load = Math.Min(catalog.Troops.types.Length > 0 ? TroopRules.MarchLoad(catalog.Troops, homeTroops) : GatherRules.Load(catalog.Gather, reactor), n.Remaining);
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
            var level = n.Level;
            var type = n.Type;
            var items = new List<TargetMenu.Item>
            {
                new("queue_drone", Loc.T("world.gather"), () => OpenMarchSetup(n), blocked == null),
                new("nav_world", Loc.T("share.title"), () => ShareLocation?.Invoke(n.X, n.Y, "node", $"{level}:{type}"))
            };
            GlideTo(n.X, n.Y);
            var x = n.X;
            var y = n.Y;
            TargetMenu.Show(() => IsOpen ? _camera : null, () => TileToWorld(x, y) + Vector3.up * 2f,
                $"{Loc.F("base.level", n.Level)}  {NodeName(n.Type)}", string.Join("\n", lines), items,
                n.Capacity > 0 ? n.Remaining / (float)n.Capacity : -1f, $"{CostChips.Compact(n.Remaining)} / {CostChips.Compact(n.Capacity)}");
        }

        private void Gather(string nodeId, string[] heroIds) =>
            _commands.Dispatch(new MarchGatherCommand
            {
                CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, NodeId = nodeId,
                HeroId = heroIds.FirstOrDefault() ?? string.Empty, HeroIds = heroIds
            });

        private void Recall(string marchId) =>
            _commands.Dispatch(new RecallMarchCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, MarchId = marchId });

        // --- Marches: setup, the queue at the top left, submarines on the map -------------------

        private List<string> _setupTeam = new();

        private void BuildMarches(Transform safe)
        {
            // Same place and logic as the base's build queue (top left, under the header): "Sefer 1/3" and a row per march.
            _marchPanel = UiKit.Rect("Marches", safe).Place(0.012f, 0.56f, 0.5f, 0.885f);
        }

        private HashSet<string> MarchingHeroes() =>
            new((View.Marches?.Marches ?? Array.Empty<MarchDto>())
                .SelectMany(m => m.HeroIds != null && m.HeroIds.Length > 0 ? m.HeroIds : new[] { m.HeroId })
                .Where(h => !string.IsNullOrEmpty(h)));

        /// <summary>"Sable +2" for a team of three.</summary>
        private static string TeamName(MarchDto m)
        {
            var hero = ClientCatalog.Catalog.TryGetHero(m.HeroId, out var def) ? Loc.T("hero." + def.Id) : Loc.T("march.divers");
            var more = (m.HeroIds?.Length ?? 0) - 1;
            return more > 0 ? $"{hero} +{more}" : hero;
        }

        /// <summary>The same setup in attack mode: the swarm's power against the team's, the Sonar it costs.</summary>
        private void OpenAttackSetup(WorldMonsterDto monster) => OpenMarchSetup(null, null, monster);

        /// <summary>The "+X% speed" tag the open march setup shows after its trip time (empty without a bonus).</summary>
        private string _tripTag = string.Empty;

        /// <summary>A trip time in the march setup with the team's speed bonus beside it.</summary>
        private string TripText(int trip) => Loc.Duration(trip) + _tripTag;

        /// <summary>
        /// The genre's march setup (Last Asylum's troop and hero selection): up to five heroes, each leading their command
        /// of troops from home; the troops under every portrait and type by type, the team's load, trip and gathering
        /// time, then March with the travel time.
        /// </summary>
        private void OpenMarchSetup(WorldNodeDto n, List<string> team = null, WorldMonsterDto monster = null)
        {
            // Neither a node nor a monster: a PvP target (WorldMapView.Pvp).
            var pvp = n == null && monster == null ? _pvpPlan : null;
            TargetMenu.Hide();
            var catalog = ClientCatalog.Catalog;
            var troops = catalog.Troops;
            var world = catalog.World;
            var reactor = ReactorLevel();
            var view = View.WorldView;
            var marching = MarchingHeroes();
            var army = View.Army ?? new ArmyDto();
            var command = (army.Command ?? Array.Empty<HeroCommandDto>()).ToDictionary(c => c.HeroId, c => c.Capacity);
            var heroes = View.Heroes
                .Select(h => (View: h, Ok: catalog.TryGetHero(h.HeroId, out var d), Def: d))
                .Where(h => h.Ok)
                .Select(h => (h.View, h.Def, Power: HeroRules.Power(h.Def, h.View.Level, h.View.Stars), Busy: marching.Contains(h.View.HeroId),
                    Command: command.TryGetValue(h.View.HeroId, out var c) ? c : TroopRules.CommandCapacity(troops, h.View.Level, h.View.Stars, 0)))
                .OrderBy(h => h.Busy).ThenByDescending(h => h.Power).ToList();
            var maxHeroes = Math.Max(1, troops.command.maxHeroes);
            _setupTeam = team ?? heroes.Where(h => !h.Busy).Take(maxHeroes).Select(h => h.View.HeroId).ToList();

            // Auto-fill exactly as the server does: each hero's own troop type first, highest tier first.
            var home = (army.Troops ?? Array.Empty<TroopStackDto>()).ToDictionary(t => TroopRules.Key(t.Type, t.Tier), t => t.Count);
            var members = _setupTeam.Select(id => heroes.First(h => h.View.HeroId == id)).ToList();
            var shares = TroopRules.FillTeam(troops, home, members.Select(m => (TroopRules.Affinity(troops, m.Def.HeroClass), m.Command)).ToList());

            // The first suggestion keeps only heroes who get troops; a hero with nobody to lead stays free.
            if (team == null && shares.Any(sh => sh.Values.Sum() > 0))
            {
                var keep = shares.Select((sh, i) => (sh, i)).Where(x => x.sh.Values.Sum() > 0).Select(x => x.i).ToList();
                members = keep.Select(i => members[i]).ToList();
                shares = keep.Select(i => shares[i]).ToList();
                _setupTeam = members.Select(m => m.View.HeroId).ToList();
            }
            var aboard = new Dictionary<string, long>();
            foreach (var share in shares)
                foreach (var pair in share)
                    aboard[pair.Key] = (aboard.TryGetValue(pair.Key, out var c) ? c : 0) + pair.Value;
            var troopCount = aboard.Values.Sum();
            var capacity = members.Sum(m => (long)m.Command);

            // Load, trip and gathering time for this team; in attack mode the two powers and the Sonar it costs.
            var speed = TroopRules.MarchSpeed(troops, aboard.Keys);
            var tx = pvp?.X ?? monster?.X ?? n.X;
            var ty = pvp?.Y ?? monster?.Y ?? n.Y;
            // Every march-speed bonus the server applies (research, VIP, alliance research), shown as a "+X% speed" tag.
            var bonuses = PlayerBonuses.Of(View, catalog);
            var trip = view == null ? 0 : bonuses.MarchSeconds((int)Math.Ceiling(GatherRules.TravelSeconds(world, view.MyX, view.MyY, tx, ty) * 100.0 / speed));
            _tripTag = bonuses.MarchSpeedPercent > 0 ? $"  <color=#5BE37A>{Loc.F("march.speedbonus", bonuses.MarchSpeedPercent)}</color>" : string.Empty;
            string text;
            var sonarShort = false;
            if (troopCount <= 0)
                text = $"<color=#FF8A80>{Loc.T("march.notroops")}</color>";
            else if (pvp != null)
                text = PvpSetupText(pvp, aboard, members.Select(m => m.Power).ToList(), trip);
            else if (monster != null)
                text = MonsterSetupText(monster, aboard, members.Select(m => m.Power).ToList(), trip, out sonarShort);
            else
            {
                var rates = GatherRules.Rates(world, catalog.Gather, n.Type, n.Level, reactor);
                var load = Math.Min(TroopRules.MarchLoad(troops, aboard), n.Remaining);
                text = $"{Loc.F("march.load", CostChips.Compact(load))}\n{Loc.F("march.trip", TripText(trip), Loc.Duration(GatherRules.GatherSeconds(world, rates, load)))}";
            }

            // Last Asylum's troop and hero selection: the team's seats on top, the troops aboard type by type, what the
            // march will do, the heroes to pick from, and March with its travel time at the bottom.
            var sheet = MapDialog("march", Loc.T("march.setup"), 150f);
            var content = sheet.Content;
            var target = pvp != null ? pvp.Title : monster != null ? $"{Loc.F("base.level", monster.Level)}  {MonsterName(monster.Type)}" : $"{Loc.F("base.level", n.Level)}  {NodeName(n.Type)}";
            var head = UiFrames.SheetText(content, $"{target}   <color=#5A6B87>{Loc.F("world.coords", tx, ty)}</color>", UiKit.TypeScale.Body + 2, UiFrames.Ink,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            head.Row(56);

            // The team: five seats, each with the troops that hero leads (Last Asylum's 320 · 320 · 293 …); a tap frees it.
            UiFrames.Section(content, Loc.T("march.team")).Row(56);
            var seats = UiKit.Rect("Seats", content).Row(220);
            for (var i = 0; i < maxHeroes; i++)
            {
                var cell = UiKit.Rect("Cell", seats).Place((float)i / maxHeroes, 0f, (float)(i + 1) / maxHeroes, 1f, 5f);
                if (i >= members.Count)
                {
                    Seat(cell, null, 0, null).Place(0, 0, 1, 1);
                    continue;
                }

                var drop = members[i].View.HeroId;
                Seat(cell, members[i].Def, shares[i].Values.Sum(), () => OpenMarchSetup(n, _setupTeam.Where(id => id != drop).ToList(), monster)).Place(0, 0, 1, 1);
            }

            // Troops aboard against the team's command, then each type aboard out of what is home (the server fills the
            // team from the heroes' own types, highest tier first).
            ShareBar(UiKit.Rect("Command", content).Row(52), capacity > 0 ? troopCount / (float)capacity : 0f,
                Loc.F("march.troops", troopCount.ToString("N0"), capacity.ToString("N0")), troopCount >= capacity && capacity > 0 ? UiKit.Ok : UiKit.Gold);
            foreach (var stack in home.Where(p => p.Value > 0).OrderByDescending(p => aboard.TryGetValue(p.Key, out var sent) ? sent : 0).ThenByDescending(p => p.Key))
            {
                if (!TroopRules.TryParseKey(stack.Key, out var type, out var tier))
                    continue;
                var going = aboard.TryGetValue(stack.Key, out var a) ? a : 0;
                var row = UiFrames.SheetBlock(content, going > 0 ? UiFrames.Card : UiFrames.CardAlt, "Troop");
                row.Row(92);
                Hud.HudIcons.Image(row.transform, "item_troops_" + type, 0.02f, 0.08f, 0.12f, 0.92f);
                var name = UiKit.Label("Name", row.transform, $"T{tier} {Loc.T("troop." + type)}", UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.rectTransform.Place(0.14f, 0f, 0.46f, 1f);
                name.Fit(14, UiKit.TypeScale.Body);
                ShareBar(UiKit.Rect("Share", row.transform).Place(0.47f, 0.3f, 0.76f, 0.7f), going / (float)Math.Max(1, stack.Value), null, UiKit.Primary);
                UiKit.Label("Count", row.transform, $"{CostChips.Compact(going)} / {CostChips.Compact(stack.Value)}", UiKit.TypeScale.Body, going > 0 ? UiFrames.Ink : UiFrames.InkSoft,
                    TextAnchor.MiddleRight, FontStyle.Bold).rectTransform.Place(0.76f, 0f, 0.97f, 1f);
            }

            InfoBlock(content, text);

            // Every hero: tap to put in or take out (at most five); heroes already out are dimmed.
            UiFrames.Section(content, Loc.F("march.pick", maxHeroes)).Row(70);
            if (heroes.Count == 0)
                UiFrames.Note(content, Loc.T("march.noheroes")).Row(110);
            var grid = UiFrames.Grid(content, 5, 0.78f, 10f);
            foreach (var h in heroes.Take(15))
            {
                var picked = _setupTeam.Contains(h.View.HeroId);
                var slot = UiFrames.SheetBlock(grid, picked ? UiFrames.Highlight : UiFrames.CardAlt, "Hero");
                SocialView.Portrait(slot.transform, h.Def, !h.Busy, 0.06f, 0.24f, 0.94f, 0.96f);
                UiKit.Label("Cmd", slot.transform, h.Busy ? Loc.T("march.busy") : $"⚔ {h.Command}", UiKit.TypeScale.Caption, h.Busy ? UiFrames.InkBad : UiFrames.Ink,
                    TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0.02f, 1, 0.24f);
                if (picked)
                    UiFrames.Dot(slot.transform);
                var pick = h.View.HeroId;
                if (h.Busy)
                    continue;
                slot.raycastTarget = true;
                slot.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    var next = _setupTeam.ToList();
                    if (next.Contains(pick))
                        next.Remove(pick);
                    else if (next.Count < maxHeroes)
                        next.Add(pick);
                    OpenMarchSetup(n, next, monster);
                });
            }

            // March, with the travel time under its name.
            var can = _setupTeam.Count > 0 && troopCount > 0 && !sonarShort;
            var label = pvp != null ? pvp.GoLabel : monster != null ? Loc.T("world.attack") : Loc.T("march.go");
            var go = UiKit.Button("Go", sheet.Footer, $"{label}\n<size={Mathf.RoundToInt(UiKit.TypeScale.Body * UiKit.Scale)}>⏱ {Loc.Duration(trip)}</size>", () =>
            {
                if (pvp != null)
                    pvp.Send(_setupTeam.ToArray());
                else if (monster != null)
                    Attack(monster.Id, _setupTeam.ToArray());
                else
                    Gather(n.Id, _setupTeam.ToArray());
                ClosePopup();
            }, can ? (pvp != null ? pvp.GoColor : monster != null ? new Color(0.86f, 0.3f, 0.27f, 1f) : UiKit.Ok) : UiKit.Panel, UiKit.TypeScale.Subtitle + 4);
            go.interactable = can;
            ((RectTransform)go.transform).Place(0.2f, 0f, 0.8f, 1f, 4f);
            UiFrames.FitSheet(sheet);
        }

        /// <summary>Where a march is now, in tiles (outbound, at the node, or on the way back).</summary>
        private Vector3 MarchPosition(MarchDto m, long now)
        {
            return Position(m.FromX, m.FromY, m.X, m.Y, m.StartedAtMs, m.ArrivesAtMs, m.GatheredAtMs, m.HomeAtMs, now);
        }

        /// <summary>Every team on the map: the player's own as a team with its plate, all of them on dashed routes.</summary>
        private void UpdateSubmarines()
        {
            var marches = View.Marches?.Marches ?? Array.Empty<MarchDto>();
            var now = View.ServerNowMs;
            UpdateTeams(marches, now);
            UpdateRoutes(marches, now);
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
                row.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenTeamDetails(march.Id));
                if (catalog.TryGetHero(m.HeroId, out var def))
                    SocialView.Portrait(row.transform, def, true, 0.01f, 0.06f, 0.2f, 0.94f);
                else
                    Hud.HudIcons.Image(row.transform, "queue_drone", 0.02f, 0.1f, 0.18f, 0.9f);

                var phase = now < m.ArrivesAtMs ? "world.march.outbound" : now < m.GatheredAtMs ? "world.march.gathering" : "world.march.returning";
                var until = now < m.ArrivesAtMs ? m.ArrivesAtMs : now < m.GatheredAtMs ? m.GatheredAtMs : m.HomeAtMs;
                UiKit.Label("Who", row.transform, $"{TeamName(m)} · {Loc.F("march.crew.short", m.Crew)}", 22, UiKit.Text, TextAnchor.UpperLeft, FontStyle.Bold)
                    .rectTransform.Place(0.23f, 0.52f, 0.98f, 0.95f);
                UiKit.Label("Where", row.transform, $"{(m.Kind == "attack" ? MonsterName(m.NodeType) : NodeName(m.NodeType))} {m.NodeLevel} · <color=#FFB347>{Loc.F(phase, Loc.Duration(Math.Max(0, (until - now) / 1000f)))}</color>",
                    20, UiKit.Muted, TextAnchor.LowerLeft).rectTransform.Place(0.23f, 0.06f, 0.72f, 0.5f);
                if (now < m.GatheredAtMs)
                {
                    var id = m.Id;
                    var recall = UiKit.Button("Recall", row.transform, Loc.T("world.recall"), () => Recall(id), UiKit.Field, 20);
                    recall.GetComponentInChildren<TMP_Text>().color = UiKit.Text;
                    ((RectTransform)recall.transform).Place(0.73f, 0.08f, 0.98f, 0.5f);
                }
            }
        }
    }
}
