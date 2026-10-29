using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The PvP layer over every screen (docs/30 §5): while an attack or a rally is on its way the screen edge pulses red
    /// and a banner counts down to the server's arrival time; new battle reports pop up as toasts. The war window has
    /// three tabs: the shield (8 h to 7 days, item or Hadalite), the war log, and the alliance's attacks and defences
    /// with Go / Join / Reinforce.
    /// </summary>
    public sealed class PvpCenter : MonoBehaviour
    {
        public sealed class Actions
        {
            public Action<int, int> ShowOnMap;
            public Action<WarRowDto> JoinRally;
            public Action<string, string, int, int> Reinforce;
        }

        public static PvpCenter Instance { get; private set; }

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private Actions _actions;
        private ulong _sequence;

        private Canvas _canvas;
        private readonly List<Image> _edges = new();
        private RectTransform _banner;
        private Image _bannerFill;
        private Text _bannerText;
        private readonly HashSet<string> _seenIncoming = new();
        private readonly HashSet<string> _arrivedGuards = new();
        private bool _guardsKnown;

        private Canvas _windowCanvas;
        private GameObject _window;
        private int _tab;
        private float _nextWarRequest;
        private int _renderedLog = -1;
        private int _renderedWar = -1;
        private int _renderedPvp = -1;
        private int _renderedInventory = -1;
        private RectTransform _body;
        private readonly List<(Text Label, long AtMs, string Format)> _clocks = new();

        private ClientStateView View => _stateSync.View;

        public bool IsOpen => _window != null;

        public void Initialize(IStateSyncService stateSync, ICommandDispatcher commands, Actions actions)
        {
            _stateSync = stateSync;
            _commands = commands;
            _actions = actions;
            Instance = this;
            Build();
        }

        private void Build()
        {
            _canvas = UiKit.Canvas("PvpAlarm", 45);
            _canvas.transform.SetParent(transform, false);
            var edge = new Color(1f, 0.1f, 0.08f, 0f);
            foreach (var (x0, y0, x1, y1) in new[] { (0f, 0.96f, 1f, 1f), (0f, 0f, 1f, 0.04f), (0f, 0f, 0.05f, 1f), (0.95f, 0f, 1f, 1f) })
            {
                var image = UiKit.Box("Edge", _canvas.transform, edge);
                image.rectTransform.Place(x0, y0, x1, y1);
                image.raycastTarget = false;
                _edges.Add(image);
            }

            _bannerFill = UiKit.Box("Banner", _canvas.transform, new Color(0.55f, 0.06f, 0.05f, 0.95f));
            _banner = _bannerFill.rectTransform;
            _banner.Place(0.08f, 0.835f, 0.92f, 0.875f);
            _bannerFill.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 0.4f, 0.3f, 0.9f);
            _bannerFill.gameObject.AddComponent<Button>().onClick.AddListener(() => Open(0));
            _bannerFill.gameObject.AddComponent<PressFeedback>();
            _bannerText = UiKit.Label("Text", _banner, string.Empty, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _bannerText.rectTransform.Place(0.02f, 0f, 0.98f, 1f);
            _banner.gameObject.SetActive(false);

            _windowCanvas = UiKit.Canvas("PvpWindow", 47);
            _windowCanvas.transform.SetParent(transform, false);
        }

        // --- Alarm -------------------------------------------------------------------------------

        private void Update()
        {
            if (_stateSync == null)
                return;

            var now = View.ServerNowMs;
            var incoming = (View.Pvp?.Incoming ?? Array.Empty<IncomingDto>()).Where(i => i.ArrivesAtMs > now - 2000).ToList();
            var threats = incoming.Where(i => i.Kind != PvpRules.Scout).ToList();
            var pulse = threats.Count > 0 ? 0.25f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f) : 0f;
            foreach (var e in _edges)
                e.color = new Color(1f, 0.1f, 0.08f, pulse);

            if (incoming.Count > 0)
            {
                var first = incoming.OrderBy(i => i.ArrivesAtMs).First();
                var who = (string.IsNullOrEmpty(first.AttackerTag) ? string.Empty : $"[{first.AttackerTag}] ") + first.AttackerName;
                var what = first.Kind == PvpRules.Rally ? Loc.T("pvp.alarm.rally") : first.Kind == PvpRules.Scout ? Loc.T("pvp.alarm.scout")
                    : first.TargetKind == PvpRules.TargetTeam ? Loc.T("pvp.alarm.team") : Loc.T("pvp.alarm.attack");
                var left = Math.Max(0, (first.ArrivesAtMs - now) / 1000f);
                var more = incoming.Count > 1 ? $"  (+{incoming.Count - 1})" : string.Empty;
                _bannerText.text = $"⚠ {what}: {who} — {Loc.Duration(left)}{more}";
                _bannerFill.color = first.Kind == PvpRules.Scout ? new Color(0.55f, 0.4f, 0.05f, 0.95f) : new Color(0.55f, 0.06f, 0.05f, 0.95f);
                _banner.gameObject.SetActive(_window == null);

                // A new attack shakes the phone once.
                foreach (var i in incoming.Where(i => _seenIncoming.Add(i.MarchId)))
                {
                    if (i.Kind != PvpRules.Scout)
                    {
#if UNITY_ANDROID || UNITY_IOS
                        Handheld.Vibrate();
#endif
                        Toaster.Show($"⚠ {what}: {who}", UiKit.Danger);
                    }
                }
            }
            else
            {
                _banner.gameObject.SetActive(false);
            }

            // A mate's team reaching the base to guard it (docs/32 §2.6); what was already there at login is not news.
            foreach (var g in View.Pvp?.Garrison ?? Array.Empty<GarrisonDto>())
            {
                if (g.ArrivesAtMs > now || !_arrivedGuards.Add(g.MarchId))
                    continue;
                if (_guardsKnown)
                    Toaster.Show(Loc.F("pvp.guard.arrived", g.OwnerName), UiKit.Ok);
            }

            _guardsKnown = true;

            while (View.TryTakeWarReport(out var report))
                Toaster.Show(ReportLine(report), Won(report) ? UiKit.Ok : UiKit.Danger);

            if (_window != null)
                TickWindow(now);
        }

        private int ReactorLevel() =>
            (View.Data.buildings ?? Array.Empty<Hadal.Data.Models.BuildingSaveEntry>()).Where(b => b.definitionId == BuildingProgression.ReactorId)
                .Select(b => b.level).DefaultIfEmpty(0).Max();

        private static bool Won(PvpReportDto r) => r.Outcome == "won" || r.Outcome == "scouted" || r.Outcome == "shielded" && r.Kind == "defence";

        // --- Window ------------------------------------------------------------------------------

        /// <summary>Opens the war window on a tab: 0 shield and alarms, 1 war log, 2 alliance war.</summary>
        public void Open(int tab)
        {
            _tab = tab;
            Close();
            var dim = UiKit.Box("Dim", _windowCanvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _window = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.03f, 0.1f, 0.97f, 0.9f);
            card.gameObject.AddComponent<Button>();
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>();

            var header = UiKit.Box("Header", card.transform, new Color(0.05f, 0.16f, 0.22f, 1f));
            header.rectTransform.Place(0, 0.92f, 1, 1);
            UiKit.Label("Title", header.transform, Loc.T("pvp.title"), 42, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0, 0.8f, 1);
            var close = UiKit.Button("Close", header.transform, "X", Close, UiKit.Field, 40);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.84f, 0.15f, 0.96f, 0.85f);

            var tabs = new[] { Loc.T("pvp.tab.shield"), Loc.T("pvp.tab.log"), Loc.T("pvp.tab.alliance") };
            for (var i = 0; i < tabs.Length; i++)
            {
                var index = i;
                var x0 = 0.03f + i * 0.32f;
                var button = UiKit.Button("Tab" + i, card.transform, tabs[i], () => Open(index), i == _tab ? UiKit.TabOn : UiKit.TabOff, 28);
                button.GetComponentInChildren<Text>().color = i == _tab ? new Color(0.05f, 0.1f, 0.15f) : UiKit.Text;
                ((RectTransform)button.transform).Place(x0, 0.85f, x0 + 0.3f, 0.905f);
            }

            _body = UiKit.Rect("Body", card.transform).Place(0.03f, 0.02f, 0.97f, 0.84f);
            _renderedLog = _renderedWar = _renderedPvp = _renderedInventory = -1;
            _nextWarRequest = 0f;
        }

        public void Close()
        {
            if (_window != null)
                Destroy(_window);
            _window = null;
            _clocks.Clear();
        }

        private void TickWindow(long now)
        {
            switch (_tab)
            {
                case 0:
                    if (_renderedPvp != View.PvpVersion || _renderedInventory != View.QuestVersion)
                        RenderShield();
                    break;
                case 1:
                    if (_renderedLog != View.WarLogVersion)
                        RenderLog();
                    break;
                default:
                    if (Time.unscaledTime >= _nextWarRequest)
                    {
                        _nextWarRequest = Time.unscaledTime + 5f;
                        _commands.Dispatch(new ViewWarCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence });
                    }

                    if (_renderedWar != View.WarListVersion)
                        RenderWar();
                    break;
            }

            foreach (var (label, at, format) in _clocks)
                if (label != null)
                    label.text = string.Format(format, Loc.Duration(Math.Max(0, (at - now) / 1000f)));
        }

        private void ClearBody()
        {
            _clocks.Clear();
            for (var i = _body.childCount - 1; i >= 0; i--)
                Destroy(_body.GetChild(i).gameObject);
        }

        private void RenderShield()
        {
            _renderedPvp = View.PvpVersion;
            _renderedInventory = View.QuestVersion;
            ClearBody();
            var pvp = View.Pvp ?? new PvpStatusDto();
            var now = View.ServerNowMs;

            var status = UiKit.Box("Status", _body, new Color(0.04f, 0.11f, 0.15f, 1f));
            status.rectTransform.Place(0, 0.8f, 1, 1);
            var shielded = pvp.ShieldUntilMs > now;
            var line = UiKit.Label("Shield", status.transform, string.Empty, 32, shielded ? UiKit.Ok : UiKit.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            line.rectTransform.Place(0.04f, 0.5f, 0.96f, 0.95f);
            if (shielded)
                _clocks.Add((line, pvp.ShieldUntilMs, "🛡 " + Loc.T("pvp.shield.on")));
            else
                line.text = Loc.T("pvp.shield.off");
            var beginner = PvpRules.IsBeginner(ReactorLevel());
            UiKit.Label("Rules", status.transform, beginner ? Loc.F("pvp.beginner", PvpRules.BeginnerReactor) : Loc.T("pvp.shield.rules"), 22, UiKit.Muted, TextAnchor.UpperLeft)
                .rectTransform.Place(0.04f, 0.04f, 0.96f, 0.5f);

            var outgoing = (View.Marches?.Marches ?? Array.Empty<MarchDto>()).Any(m => m.Kind is PvpRules.Attack or PvpRules.Scout or PvpRules.Rally && m.ArrivesAtMs > now);
            var hadalite = (View.Data.resources ?? Array.Empty<Hadal.Data.Models.ResourceSaveEntry>())
                .Where(e => ResourceTypeMap.FromStoredValue(e.resourceType) == HADAL.Shared.Enums.ResourceType.Hadalite).Select(e => e.amount).DefaultIfEmpty(0).Sum();
            for (var i = 0; i < PvpRules.ShieldItems.Length; i++)
            {
                var id = PvpRules.ShieldItems[i];
                var y1 = 0.77f - i * 0.135f;
                var row = UiKit.Box("Row" + id, _body, new Color(0, 0, 0, 0.3f));
                row.rectTransform.Place(0, y1 - 0.12f, 1, y1);
                var held = View.Items.TryGetValue(id, out var n) ? n : 0;
                UiKit.Label("Name", row.transform, Loc.T("item." + id), 30, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.04f, 0.5f, 0.6f, 0.95f);
                UiKit.Label("Have", row.transform, Loc.F("pvp.shield.have", held), 22, UiKit.Muted, TextAnchor.MiddleLeft).rectTransform.Place(0.04f, 0.05f, 0.6f, 0.5f);
                var price = PvpRules.ShieldHadalite(id);
                var can = !outgoing && (held > 0 || hadalite >= price);
                var shieldId = id;
                var use = UiKit.Button("Use", row.transform, held > 0 ? Loc.T("pvp.shield.use") : $"💎 {price:N0}", () =>
                    _commands.Dispatch(new UseShieldCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, ItemId = shieldId }),
                    can ? (held > 0 ? UiKit.Ok : UiKit.Primary) : UiKit.Panel, 28);
                use.interactable = can;
                ((RectTransform)use.transform).Place(0.62f, 0.15f, 0.96f, 0.85f);
            }

            if (outgoing)
                UiKit.Label("Out", _body, Loc.T("pvp.shield.attackOut"), 22, UiKit.Amber, TextAnchor.MiddleCenter).rectTransform.Place(0, 0.12f, 1, 0.2f);

            // Who guards the base: alliance mates' teams, here or on their way.
            var guards = pvp.Garrison ?? Array.Empty<GarrisonDto>();
            if (guards.Length > 0)
            {
                var names = string.Join(", ", guards.Select(g => g.ArrivesAtMs > now
                    ? $"{g.OwnerName} ({CostChips.Compact(g.Troops)}, {Loc.Duration((g.ArrivesAtMs - now) / 1000f)})"
                    : $"{g.OwnerName} ({CostChips.Compact(g.Troops)})"));
                UiKit.Label("Guards", _body, Loc.F("pvp.guards", names), 22, UiKit.Ok, TextAnchor.MiddleLeft).rectTransform.Place(0, 0.2f, 1, 0.245f);
            }

            // What is coming, with Go buttons.
            var incoming = (pvp.Incoming ?? Array.Empty<IncomingDto>()).Where(x => x.ArrivesAtMs > now).OrderBy(x => x.ArrivesAtMs).Take(2).ToList();
            for (var i = 0; i < incoming.Count; i++)
            {
                var inc = incoming[i];
                var y1 = 0.11f - i * 0.055f;
                var label = UiKit.Label("Incoming" + i, _body, string.Empty, 22, UiKit.Danger, TextAnchor.MiddleLeft, FontStyle.Bold);
                label.rectTransform.Place(0, y1 - 0.05f, 0.75f, y1);
                var who = (string.IsNullOrEmpty(inc.AttackerTag) ? string.Empty : $"[{inc.AttackerTag}] ") + inc.AttackerName;
                _clocks.Add((label, inc.ArrivesAtMs, $"⚠ {who.Replace("{", "{{").Replace("}", "}}")} — {{0}}"));
                var go = UiKit.Button("Go" + i, _body, Loc.T("pvp.go"), () => { Close(); _actions.ShowOnMap?.Invoke(inc.FromX, inc.FromY); }, UiKit.Field, 22);
                ((RectTransform)go.transform).Place(0.77f, y1 - 0.05f, 1f, y1);
            }
        }

        private void RenderLog()
        {
            _renderedLog = View.WarLogVersion;
            ClearBody();
            var reports = View.WarLog?.Reports ?? Array.Empty<PvpReportDto>();
            if (reports.Length == 0)
            {
                UiKit.Label("Empty", _body, Loc.T("pvp.log.empty"), 28, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0, 0.4f, 1, 0.6f);
                return;
            }

            var list = UiKit.ScrollList("List", _body, 10f);
            ((RectTransform)list.parent).Place(0, 0, 1, 1);
            var now = View.ServerNowMs;
            foreach (var r in reports)
            {
                var row = UiKit.Box("Report", list, Won(r) ? new Color(0.05f, 0.2f, 0.12f, 0.9f) : new Color(0.25f, 0.06f, 0.06f, 0.9f)).Row(150f);
                UiKit.Label("Line", row.transform, ReportLine(r), 26, UiKit.Text, TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.Place(0.03f, 0.5f, 0.97f, 0.95f);
                var detail = new List<string> { Loc.F("pvp.ago", Loc.Duration(Math.Max(0, (now - r.AtMs) / 1000f))), Loc.F("world.coords", r.X, r.Y) };
                if (r.Dead + r.Wounded > 0)
                    detail.Add(Loc.F("pvp.losses", r.Dead.ToString("N0"), r.Wounded.ToString("N0")));
                var loot = (r.Plunder ?? Array.Empty<ResourceAmountEntry>()).Where(p => p.Value > 0).ToList();
                if (loot.Count > 0)
                    detail.Add((r.Kind == "defence" || r.Kind == "team" ? "−" : "+") + string.Join(" ", loot.Select(p => $"{CostChips.Compact(p.Value)} {Loc.T("resource." + p.Key)}")));
                if (r.Kind == "scout")
                    detail.Add(Loc.F("pvp.scout.result", r.DefenderPower.ToString("N0"), r.Reinforcements));
                UiKit.Label("Detail", row.transform, string.Join("   ", detail), 22, UiKit.Muted, TextAnchor.UpperLeft).rectTransform.Place(0.03f, 0.04f, 0.97f, 0.5f);
                var x = r.X;
                var y = r.Y;
                row.gameObject.AddComponent<Button>().onClick.AddListener(() => { Close(); _actions.ShowOnMap?.Invoke(x, y); });
            }
        }

        /// <summary>One line for a report: what happened, against whom, and how it ended.</summary>
        private static string ReportLine(PvpReportDto r)
        {
            var who = r.StrongholdId.Length > 0 ? $"{Loc.T("pvp.stronghold")} {Loc.F("base.level", r.StrongholdLevel)}"
                : (string.IsNullOrEmpty(r.OpponentTag) ? string.Empty : $"[{r.OpponentTag}] ") + r.OpponentName;
            var kind = Loc.T("pvp.kind." + r.Kind);
            var outcome = Loc.T("pvp.outcome." + r.Outcome);
            var captured = r.Captured ? " " + Loc.T("pvp.captured") : string.Empty;
            return $"{kind}: {who} — {outcome}{captured}";
        }

        private void RenderWar()
        {
            _renderedWar = View.WarListVersion;
            ClearBody();
            if (View.MyAlliance == null)
            {
                UiKit.Label("None", _body, Loc.T("pvp.war.noAlliance"), 28, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0, 0.4f, 1, 0.6f);
                return;
            }

            var rows = View.WarList?.Rows ?? Array.Empty<WarRowDto>();
            if (rows.Length == 0)
            {
                UiKit.Label("Empty", _body, Loc.T("pvp.war.empty"), 28, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0, 0.4f, 1, 0.6f);
                return;
            }

            var list = UiKit.ScrollList("List", _body, 10f);
            ((RectTransform)list.parent).Place(0, 0, 1, 1);
            var now = View.ServerNowMs;
            foreach (var w in rows)
            {
                var row = UiKit.Box("War", list, w.Incoming ? new Color(0.28f, 0.06f, 0.06f, 0.92f) : new Color(0.05f, 0.14f, 0.24f, 0.92f)).Row(170f);
                var attacker = (string.IsNullOrEmpty(w.AttackerTag) ? string.Empty : $"[{w.AttackerTag}] ") + w.AttackerName;
                var target = w.TargetKind == PvpRules.TargetStronghold ? Loc.T("pvp.stronghold")
                    : (string.IsNullOrEmpty(w.TargetTag) ? string.Empty : $"[{w.TargetTag}] ") + w.TargetName;
                var title = $"{(w.Incoming ? "⬇ " + Loc.T("pvp.war.in") : "⬆ " + Loc.T("pvp.war.out"))} · {Loc.T("pvp.kind." + w.Kind)}";
                UiKit.Label("Title", row.transform, title, 24, w.Incoming ? UiKit.Danger : UiKit.Accent, TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.Place(0.03f, 0.66f, 0.7f, 0.96f);
                UiKit.Label("Who", row.transform, $"{attacker} → {target}", 24, UiKit.Text, TextAnchor.MiddleLeft).rectTransform.Place(0.03f, 0.36f, 0.97f, 0.66f);
                var clock = UiKit.Label("Clock", row.transform, string.Empty, 22, UiKit.Muted, TextAnchor.MiddleLeft);
                clock.rectTransform.Place(0.03f, 0.04f, 0.6f, 0.36f);
                if (w.DepartsAtMs > now)
                    _clocks.Add((clock, w.DepartsAtMs, Loc.T("pvp.war.departs") + $"  ({w.Teams}/{w.MaxTeams})"));
                else
                    _clocks.Add((clock, w.ArrivesAtMs, Loc.T("pvp.war.arrives")));

                var row0 = w;
                var go = UiKit.Button("Go", row.transform, Loc.T("pvp.go"), () => { Close(); _actions.ShowOnMap?.Invoke(row0.ToX, row0.ToY); }, UiKit.Field, 22);
                ((RectTransform)go.transform).Place(0.72f, 0.62f, 0.97f, 0.94f);
                if (w.Kind == PvpRules.Rally && !w.Incoming && !w.Joined && w.DepartsAtMs > now && w.Teams < w.MaxTeams)
                {
                    var join = UiKit.Button("Join", row.transform, Loc.T("pvp.rally.join"), () => { Close(); _actions.JoinRally?.Invoke(row0); }, UiKit.Ok, 22);
                    ((RectTransform)join.transform).Place(0.72f, 0.08f, 0.97f, 0.4f);
                }
                else if (w.Incoming && w.TargetKind == PvpRules.TargetBase && w.TargetId != View.PlayerId)
                {
                    var help = UiKit.Button("Help", row.transform, Loc.T("pvp.reinforce"), () =>
                    {
                        Close();
                        _actions.Reinforce?.Invoke(row0.TargetId, target, row0.ToX, row0.ToY);
                    }, UiKit.Primary, 22);
                    ((RectTransform)help.transform).Place(0.72f, 0.08f, 0.97f, 0.4f);
                }
            }
        }
    }
}
