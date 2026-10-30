using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Network;
using Hadal.Core.Services;
using Hadal.Core.StateSync;
using TMPro;
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

            /// <summary>Asks the alliance for help: shares our base's position in a chat.</summary>
            public Action RequestHelp;
        }

        public static PvpCenter Instance { get; private set; }

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private Actions _actions;
        private ulong _sequence;

        private Canvas _canvas;
        private AlertVignette _vignette;
        private AttackBanner _banner;
        private readonly Dictionary<string, long> _firstSeenMs = new();
        private readonly HashSet<string> _landed = new();
        private float _nextPing;
        private AlarmMood _mood;
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
        private readonly List<(TMP_Text Label, long AtMs, string Format)> _clocks = new();

        private ClientStateView View => _stateSync.View;

        public bool IsOpen => _window != null;

        /// <summary>A hostile march is on its way to us: the onboarding guide steps aside (live feedback).</summary>
        public bool UnderAttack { get; private set; }

        private RectTransform _bannerActions;
        private ShieldDome _baseDome;
        private float _nextDomeCheck;

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
            // The edge vignette lives on its own canvas under the HUD (AlertVignette); the card and its actions sit in
            // the safe area at the top.
            _vignette = AlertVignette.Create(transform);
            var safe = UiKit.Rect("Safe", _canvas.transform).Place(0, 0, 1, 1);
            safe.gameObject.AddComponent<Hud.SafeArea>();
            _banner = AttackBanner.Create(safe, () => Open(0));

            // Under the banner: go to the attacker, raise a shield, ask the alliance for help.
            _bannerActions = UiKit.Rect("BannerActions", safe).Place(0.035f, 0.738f, 0.965f, 0.782f);
            var actions = new (string Key, Action Act, Color Color)[]
            {
                ("pvp.alarm.goto", GoToAttacker, UiKit.Field),
                ("pvp.alarm.shield", () => Open(0), UiKit.Primary),
                ("pvp.alarm.help", () => _actions.RequestHelp?.Invoke(), UiKit.Ok)
            };
            for (var i = 0; i < actions.Length; i++)
            {
                var (key, act, color) = actions[i];
                var button = UiKit.Button("Act" + i, _bannerActions, Loc.T(key), () => act(), color, UiKit.TypeScale.Body);
                ((RectTransform)button.transform).Place(i / 3f, 0f, (i + 1) / 3f, 1f, 5f);
            }

            _bannerActions.gameObject.SetActive(false);

            _windowCanvas = UiKit.Canvas("PvpWindow", 47);
            _windowCanvas.transform.SetParent(transform, false);
        }

        // --- Alarm -------------------------------------------------------------------------------

        private void Update()
        {
            if (_stateSync == null)
                return;

            TickShieldWindow();
            var now = View.ServerNowMs;
            var incoming = Incoming(now);
            var threats = incoming.Where(i => i.Kind != PvpRules.Scout).ToList();
            // A shield up when the first threat lands blocks it: the alarm turns calm teal instead of red.
            var shieldMs = AlarmDemo.Active ? AlarmDemo.ShieldUntilMs : View.Pvp?.ShieldUntilMs ?? 0;
            var blocked = threats.Count > 0 && threats.All(i => shieldMs > i.ArrivesAtMs);
            UnderAttack = threats.Count > 0;
            UpdateBaseDome(now);
            TickAlarm(now, incoming, threats, blocked, shieldMs);

            // A mate's team reaching the base to guard it (docs/32 §2.6); what was already there at login is not news.
            foreach (var g in View.Pvp?.Garrison ?? Array.Empty<GarrisonDto>())
            {
                if (g.ArrivesAtMs > now || !_arrivedGuards.Add(g.MarchId))
                    continue;
                if (_guardsKnown)
                    Toaster.Show(Loc.F("pvp.guard.arrived", g.OwnerName), UiKit.Ok);
            }

            _guardsKnown = true;

            // Each battle fought while we are here gets its result card; what happened while we were away gets one summary.
            while (View.TryTakeWarReport(out var report))
            {
                _results.Enqueue(report);
                Remember(report.AtMs);
            }

            if (_checkedLog != View.WarLogVersion && View.LiveSinceMs > 0 && !string.IsNullOrEmpty(View.PlayerId))
            {
                _checkedLog = View.WarLogVersion;
                SumUpMissed();
            }

            if (_result == null && _window == null && _results.Count > 0)
                ShowResult(_results.Dequeue());

            if (_window != null)
                TickWindow(now);
        }

        /// <summary>Marches on their way to us (or landed in the last two seconds); the adb demo stands in when it runs.</summary>
        private List<IncomingDto> Incoming(long now) =>
            (AlarmDemo.Poll(now) ?? View.Pvp?.Incoming ?? Array.Empty<IncomingDto>()).Where(i => i.ArrivesAtMs > now - 2000).ToList();

        /// <summary>
        /// The incoming-attack alarm (docs/40 §1): the edge vignette's mood and tempo, the alert card, a low sonar ping
        /// now and then, and on impact a flash, a short shake and a buzz. Everything eases in and returns to rest.
        /// </summary>
        private void TickAlarm(long now, List<IncomingDto> incoming, List<IncomingDto> threats, bool blocked, long shieldMs)
        {
            var dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            var firstThreat = threats.Where(i => i.ArrivesAtMs > now).OrderBy(i => i.ArrivesAtMs).FirstOrDefault();
            var threatLeft = firstThreat != null ? (firstThreat.ArrivesAtMs - now) / 1000f : 0f;
            var mood = threats.Count > 0 ? blocked ? AlarmMood.Shielded : AlarmMood.Threat : incoming.Count > 0 ? AlarmMood.Scout : AlarmMood.None;
            _vignette.Tick(firstThreat == null && mood == AlarmMood.Threat ? AlarmMood.None : mood, threatLeft, dt);

            // Impact: an unshielded attack reaching its arrival time while we watch.
            foreach (var i in threats)
            {
                if (i.ArrivesAtMs > now || shieldMs > i.ArrivesAtMs || !_landed.Add(i.MarchId))
                    continue;
                _vignette.Flash();
                _banner.Jolt();
                ScreenShake.Add(0.75f);
                GameAudio.Play(Sfx.BattleHit);
                Buzz();
            }

            if (incoming.Count == 0)
            {
                _banner.Show(false);
                _banner.Tick(dt, 2f, true);
                _bannerActions.gameObject.SetActive(false);
                _mood = AlarmMood.None;
                return;
            }

            var first = incoming.OrderBy(i => i.ArrivesAtMs).First();
            if (!_firstSeenMs.ContainsKey(first.MarchId))
                _firstSeenMs[first.MarchId] = now;
            var who = (string.IsNullOrEmpty(first.AttackerTag) ? string.Empty : $"[{first.AttackerTag}] ") + first.AttackerName;
            var what = first.Kind == PvpRules.Rally ? Loc.T("pvp.alarm.rally") : first.Kind == PvpRules.Scout ? Loc.T("pvp.alarm.scout")
                : first.TargetKind == PvpRules.TargetTeam ? Loc.T("pvp.alarm.team") : Loc.T("pvp.alarm.attack");
            var left = Math.Max(0, (first.ArrivesAtMs - now) / 1000f);
            var shielded = shieldMs > first.ArrivesAtMs && first.Kind != PvpRules.Scout;
            var cardMood = shielded ? AlarmMood.Shielded : first.Kind == PvpRules.Scout ? AlarmMood.Scout : AlarmMood.Threat;
            var total = Math.Max(1L, first.ArrivesAtMs - _firstSeenMs[first.MarchId]);
            var more = incoming.Count > 1 ? Loc.F("pvp.alarm.more", incoming.Count - 1) : string.Empty;
            _banner.Set(cardMood, shielded ? Loc.T("pvp.alarm.blocked") : what, who, more, left, (first.ArrivesAtMs - now) / (float)total);
            _banner.Show(_window == null);
            _banner.Tick(dt, cardMood == AlarmMood.Threat ? AlarmTempo.Threat(left).Period : 3f, cardMood != AlarmMood.Threat);
            _bannerActions.gameObject.SetActive(_window == null && _result == null && _banner.Showing);
            _bannerActions.Find("Act2").gameObject.SetActive(View.MyAlliance != null);

            // A new attack buzzes the phone once; a low sonar ping repeats, quicker as the impact nears.
            var fresh = false;
            foreach (var i in incoming.Where(i => _seenIncoming.Add(i.MarchId)))
                fresh |= i.Kind != PvpRules.Scout;
            if (fresh)
                Buzz();
            if (cardMood != _mood || Time.unscaledTime >= _nextPing && cardMood == AlarmMood.Threat)
            {
                GameAudio.Play(Sfx.SonarPing, cardMood == AlarmMood.Threat ? Mathf.Lerp(0.35f, 0.6f, AlarmTempo.Urgency(left)) : 0.3f);
                _nextPing = Time.unscaledTime + AlarmTempo.PingInterval(left);
            }

            _mood = cardMood;
        }

        private static void Buzz()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (GameSettings.Vibration)
                Handheld.Vibrate();
#endif
        }

        /// <summary>Centres the map on the base the first incoming march came from.</summary>
        private void GoToAttacker()
        {
            var now = View.ServerNowMs;
            var first = Incoming(now).OrderBy(i => i.ArrivesAtMs).FirstOrDefault();
            if (first != null)
                _actions.ShowOnMap?.Invoke(first.FromX, first.FromY);
        }

        /// <summary>The dome over our own base in the base view while a shield or newbie protection is up.</summary>
        private void UpdateBaseDome(long now)
        {
            if (Time.unscaledTime < _nextDomeCheck)
                return;
            _nextDomeCheck = Time.unscaledTime + 1f;
            if (_baseDome == null)
            {
                // The base grid's ring lines (CircularGridRenderer lives in the gameplay assembly) give the base's size.
                _nextDomeCheck = Time.unscaledTime + 5f;
                var grid = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).FirstOrDefault(m => m.GetType().Name == "CircularGridRenderer");
                var lines = grid != null ? grid.GetComponentsInChildren<LineRenderer>() : Array.Empty<LineRenderer>();
                if (lines.Length == 0)
                    return;
                var radius = lines.Max(l => Mathf.Max(l.bounds.extents.x, l.bounds.extents.z));
                _baseDome = ShieldDome.Create(grid.transform, radius * 1.08f);
            }

            var shield = View.Pvp?.ShieldUntilMs ?? 0;
            _baseDome.Show(shield > now ? ShieldDome.Kind.Shield : PvpRules.IsBeginner(ReactorLevel()) ? ShieldDome.Kind.Beginner : ShieldDome.Kind.None);
        }

        private int ReactorLevel() =>
            (View.Data.buildings ?? Array.Empty<Hadal.Data.Models.BuildingSaveEntry>()).Where(b => b.definitionId == BuildingProgression.ReactorId)
                .Select(b => b.level).DefaultIfEmpty(0).Max();

        private static bool Won(PvpReportDto r) => r.Outcome == "won" || r.Outcome == "scouted" || r.Outcome == "shielded" && r.Kind == "defence";

        /// <summary>We were the one hit: a defence, a gathering team, a guard post, a scout on our base.</summary>
        private static bool Hit(PvpReportDto r) => r.Kind is "defence" or "team" or "reinforce" or "scouted";

        // --- Results and the away summary (docs/30 §5.5) ------------------------------------------

        private readonly Queue<PvpReportDto> _results = new();
        private GameObject _result;
        private int _checkedLog = -1;

        private string SeenKey => "hadal.pvp.seen." + View.PlayerId;

        private long Seen()
        {
            try
            {
                return long.TryParse(PlayerPrefs.GetString(SeenKey, "0"), out var ms) ? ms : 0;
            }
            catch
            {
                return 0;
            }
        }

        private void Remember(long atMs)
        {
            if (string.IsNullOrEmpty(View.PlayerId) || atMs <= Seen())
                return;
            PlayerPrefs.SetString(SeenKey, atMs.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>Battles on us since we last looked, from before this login: one "your base was attacked" card.</summary>
        private void SumUpMissed()
        {
            var reports = View.WarLog?.Reports ?? Array.Empty<PvpReportDto>();
            if (reports.Length == 0)
                return;
            var seen = Seen();
            var since = Math.Max(seen, View.LiveSinceMs - 3L * 24 * 3600 * 1000);
            var missed = reports.Where(r => Hit(r) && r.AtMs > since && r.AtMs < View.LiveSinceMs).OrderByDescending(r => r.AtMs).ToList();
            Remember(reports.Max(r => r.AtMs));
            if (missed.Count == 0)
                return;

            var lines = new List<string> { Loc.F("pvp.away.count", missed.Count) };
            var dead = missed.Sum(r => r.Dead);
            var wounded = missed.Sum(r => r.Wounded);
            if (dead + wounded > 0)
                lines.Add(Loc.F("pvp.losses", dead.ToString("N0"), wounded.ToString("N0")));
            var lost = Loot(missed.SelectMany(r => r.Plunder ?? Array.Empty<ResourceAmountEntry>()), "−");
            if (lost.Length > 0)
                lines.Add(lost);
            lines.AddRange(missed.Take(4).Select(r => "• " + ReportLine(r)));
            lines.Add(Loc.T("pvp.result.mail"));
            Card(Loc.T("pvp.away.title"), string.Join("\n", lines), UiKit.Danger, Loc.T("pvp.tab.log"), () => Open(1));
        }

        /// <summary>One battle's result as it lands: "Zafer! +X Titanyum" or "Yenilgi", losses, and the report.</summary>
        private void ShowResult(PvpReportDto r)
        {
            var won = Won(r);
            GameAudio.Play(won ? Sfx.Reward : Sfx.Error);
            var lines = new List<string> { ReportLine(r) };
            if (r.Outcome == "shielded")
                lines.Add(Hit(r) ? Loc.F("pvp.result.blocked", r.OpponentName) : Loc.T("pvp.result.bounced"));
            var loot = Loot(r.Plunder ?? Array.Empty<ResourceAmountEntry>(), Hit(r) ? "−" : "+");
            if (loot.Length > 0 && r.Kind != "scout")
                lines.Add(loot);
            if (r.Dead + r.Wounded > 0)
                lines.Add(Loc.F("pvp.losses", r.Dead.ToString("N0"), r.Wounded.ToString("N0")));
            if (r.Kind == "scout")
                lines.Add(Loc.F("pvp.scout.result", r.DefenderPower.ToString("N0"), r.Reinforcements));
            lines.Add(Loc.T("pvp.result.mail"));
            var title = r.Kind is "scout" or "scouted" ? Loc.T("pvp.kind." + r.Kind)
                : r.Outcome is "won" or "lost" ? Loc.T(won ? "pvp.result.win" : "pvp.result.loss") : Loc.T("pvp.outcome." + r.Outcome);
            var report = r;
            Card(title, string.Join("\n", lines), won ? UiKit.Ok : UiKit.Danger, Loc.T("mail.pvp.open"), () =>
            {
                Open(1);
                ShowReport(report);
            });
        }

        private static string Loot(IEnumerable<ResourceAmountEntry> entries, string sign)
        {
            var sums = entries.Where(p => p.Value > 0).GroupBy(p => p.Key).Select(g => (Key: g.Key, Value: g.Sum(p => p.Value))).ToList();
            return string.Join("  ", sums.Select(p => $"{sign}{CostChips.Compact(p.Value)} {Loc.T("resource." + p.Key)}"));
        }

        /// <summary>A card over every screen with a title, text, an action and OK.</summary>
        private void Card(string title, string text, Color color, string action, Action onAction)
        {
            var dim = UiKit.Box("Result", _windowCanvas.transform, new Color(0, 0, 0, 0.55f));
            dim.rectTransform.Place(0, 0, 1, 1);
            _result = dim.gameObject;
            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.06f, 0.3f, 0.94f, 0.7f);
            card.gameObject.AddComponent<Button>();
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>();
            card.gameObject.AddComponent<Outline>().effectColor = color;
            UiKit.Label("Title", card.transform, title, 44, color, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.04f, 0.82f, 0.96f, 0.97f);
            UiKit.Label("Text", card.transform, text, 26, UiKit.Text, TextAnchor.UpperLeft).rectTransform.Place(0.06f, 0.22f, 0.94f, 0.8f);
            var go = UiKit.Button("Action", card.transform, action, () => { CloseResult(); onAction(); }, UiKit.Primary, 28);
            ((RectTransform)go.transform).Place(0.05f, 0.04f, 0.48f, 0.18f);
            var ok = UiKit.Button("Ok", card.transform, Loc.T("pvp.result.ok"), CloseResult, UiKit.Field, 28);
            ((RectTransform)ok.transform).Place(0.52f, 0.04f, 0.95f, 0.18f);
        }

        private void CloseResult()
        {
            if (_result != null)
                Destroy(_result);
            _result = null;
        }

        /// <summary>Opens the result screen of a battle in the war log (from its mail); the log itself when it has scrolled out.</summary>
        public void OpenReport(string battleId, string reportKind)
        {
            var reports = View.WarLog?.Reports ?? Array.Empty<PvpReportDto>();
            var report = reports.FirstOrDefault(r => r.Id == battleId && r.Kind == reportKind) ?? reports.FirstOrDefault(r => r.Id == battleId);
            Open(1);
            if (report != null)
                ShowReport(report);
        }

        // --- Shield window (docs/41) ------------------------------------------------------------

        private UiWindow _shield;
        private float _shieldRefresh;

        /// <summary>
        /// The base's shield in one place (the map's Shield button): whether it is up and for how long, then each shield
        /// with what the bag holds and a Use button (or its Hadalite price when the bag has none).
        /// </summary>
        public void OpenShield()
        {
            if (_shield == null)
                _shield = UiWindow.Open(_windowCanvas.transform, Loc.T("pvp.tab.shield"), string.Empty, () => _shield = null);
            RenderShieldWindow();
        }

        private void RenderShieldWindow()
        {
            _shieldRefresh = Time.unscaledTime + 1f;
            var window = _shield;
            window.Clear();
            var pvp = View.Pvp ?? new PvpStatusDto();
            var now = View.ServerNowMs;
            var shielded = pvp.ShieldUntilMs > now;
            window.SetTitle(Loc.T("pvp.tab.shield"), shielded
                ? Loc.F("pvp.shield.on", Loc.Duration((pvp.ShieldUntilMs - now) / 1000f))
                : Loc.T("pvp.shield.off"));
            window.Paragraph(PvpRules.IsBeginner(ReactorLevel()) ? Loc.F("pvp.beginner", PvpRules.BeginnerReactor) : Loc.T("pvp.shield.rules"),
                null, TextAnchor.MiddleLeft);

            var outgoing = (View.Marches?.Marches ?? Array.Empty<MarchDto>()).Any(m => m.Kind is PvpRules.Attack or PvpRules.Scout or PvpRules.Rally && m.ArrivesAtMs > now);
            var hadalite = (View.Data.resources ?? Array.Empty<Hadal.Data.Models.ResourceSaveEntry>())
                .Where(e => ResourceTypeMap.FromStoredValue(e.resourceType) == HADAL.Shared.Enums.ResourceType.Hadalite).Select(e => e.amount).DefaultIfEmpty(0).Sum();
            window.Section(Loc.T("pvp.tab.shield"));
            foreach (var id in PvpRules.ShieldItems)
            {
                var held = View.Items.TryGetValue(id, out var n) ? n : 0;
                var price = PvpRules.ShieldHadalite(id);
                var shieldId = id;
                window.Need("item_" + id, $"{Loc.T("item." + id)}  <size=22>{Loc.F("pvp.shield.have", held)}</size>", true,
                    held > 0 ? Loc.T("pvp.shield.use") : $"{price:N0} {Loc.Resource("Hadalite")}",
                    () => _commands.Dispatch(new UseShieldCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, ItemId = shieldId }),
                    !outgoing && (held > 0 || hadalite >= price), held > 0 ? UiKit.Ok : UiKit.Primary);
            }

            if (outgoing)
                window.Note($"<color=#{ColorUtility.ToHtmlStringRGB(UiWindow.InkBad)}>{Loc.T("pvp.shield.attackOut")}</color>");
            window.Fit();
        }

        private void TickShieldWindow()
        {
            if (_shield == null || _shield.IsClosing || Time.unscaledTime < _shieldRefresh)
                return;
            if (Input.touchCount > 0 || Input.GetMouseButton(0))
                return;
            RenderShieldWindow();
        }

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
            close.GetComponentInChildren<TMP_Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.84f, 0.15f, 0.96f, 0.85f);

            var tabs = new[] { Loc.T("pvp.tab.shield"), Loc.T("pvp.tab.log"), Loc.T("pvp.tab.alliance") };
            for (var i = 0; i < tabs.Length; i++)
            {
                var index = i;
                var x0 = 0.03f + i * 0.32f;
                var button = UiKit.Button("Tab" + i, card.transform, tabs[i], () => Open(index), i == _tab ? UiKit.TabOn : UiKit.TabOff, 28);
                button.GetComponentInChildren<TMP_Text>().color = i == _tab ? new Color(0.05f, 0.1f, 0.15f) : UiKit.Text;
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
                .rectTransform.Place(0.04f, 0.2f, 0.96f, 0.5f);

            // What an attack can never take (docs/33 §2): the storage share the Shield Generator raises.
            var guard = (pvp.Protected ?? Array.Empty<ResourceAmountEntry>()).Where(p => p.Value > 0).ToList();
            if (guard.Count > 0)
                UiKit.Label("Protected", status.transform, Loc.F("pvp.protected", string.Join("  ",
                        guard.Select(p => $"{CostChips.Compact(p.Value)} {Loc.T("resource." + p.Key)}"))), 22, UiKit.Ok, TextAnchor.MiddleLeft)
                    .rectTransform.Place(0.04f, 0.02f, 0.96f, 0.2f);

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
            else if ((pvp.Incoming ?? Array.Empty<IncomingDto>()).Any(x => x.ArrivesAtMs > now && x.Kind != PvpRules.Scout))
                UiKit.Label("Incoming", _body, shielded ? Loc.T("pvp.alarm.blocked") : Loc.T("pvp.shield.incoming"), 22, shielded ? UiKit.Ok : UiKit.Amber, TextAnchor.MiddleCenter)
                    .rectTransform.Place(0, 0.12f, 1, 0.2f);

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
                var report = r;
                row.gameObject.AddComponent<Button>().onClick.AddListener(() => ShowReport(report));
            }
        }

        /// <summary>
        /// The result screen (docs/33 §2): both armies with their start and losses by type, our dead and wounded, the loot
        /// and its share, our contribution, rounds, and the battle id, seed and rule version. The server computed all of it.
        /// </summary>
        private void ShowReport(PvpReportDto r)
        {
            ClearBody();
            _renderedLog = View.WarLogVersion; // stay on the report until the player goes back
            UiKit.Label("Title", _body, ReportLine(r), 28, Won(r) ? UiKit.Ok : UiKit.Danger, TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.Place(0, 0.88f, 1, 1);
            var lines = new List<string>
            {
                Loc.F("world.coords", r.X, r.Y) + "   " + Loc.F("pvp.ago", Loc.Duration(Math.Max(0, (View.ServerNowMs - r.AtMs) / 1000f)))
            };
            if (r.Rounds > 0)
                lines.Add(Loc.F("report.rounds", r.Rounds) + "   " + Loc.F("report.powers", r.AttackerPower.ToString("N0"), r.DefenderPower.ToString("N0")));
            if (r.Dead + r.Wounded > 0)
                lines.Add(Loc.F("pvp.losses", r.Dead.ToString("N0"), r.Wounded.ToString("N0")));
            var loot = (r.Plunder ?? Array.Empty<ResourceAmountEntry>()).Where(p => p.Value > 0).ToList();
            if (loot.Count > 0)
                lines.Add(Loc.T("report.loot") + " " + string.Join("  ", loot.Select(p => $"{CostChips.Compact(p.Value)} {Loc.T("resource." + p.Key)}")));
            if (r.PlunderShare < 100 && r.Kind is "attack" or "rally")
                lines.Add(Loc.F("report.fatigue", r.PlunderShare));
            if (r.Contribution > 0 && r.Contribution < 100)
                lines.Add(Loc.F("report.contribution", r.Contribution));
            if (r.Kind == "scout")
                lines.Add(Loc.F("pvp.scout.result", r.DefenderPower.ToString("N0"), r.Reinforcements));
            UiKit.Label("Lines", _body, string.Join("\n", lines), 22, UiKit.Text, TextAnchor.UpperLeft).rectTransform.Place(0, 0.62f, 1, 0.88f);

            // The two armies, type by type.
            Army(Loc.T("report.attacker"), r.Attacker, 0f, 0.5f);
            Army(Loc.T("report.defender"), r.Defender, 0.5f, 1f);
            if (r.RuleVersion > 0)
                UiKit.Label("Seed", _body, Loc.F("report.integrity", r.Id, r.Seed, r.RuleVersion), 16, UiKit.Muted, TextAnchor.LowerLeft)
                    .rectTransform.Place(0, 0.1f, 1, 0.15f);
            var back = UiKit.Button("Back", _body, Loc.T("report.back"), () => { _renderedLog = -1; }, UiKit.Field, 26);
            ((RectTransform)back.transform).Place(0, 0, 0.45f, 0.09f);
            var x = r.X;
            var y = r.Y;
            var go = UiKit.Button("Go", _body, Loc.T("pvp.go"), () => { Close(); _actions.ShowOnMap?.Invoke(x, y); }, UiKit.Primary, 26);
            ((RectTransform)go.transform).Place(0.55f, 0, 1, 0.09f);

            void Army(string title, BattleStackDto[] stacks, float x0, float x1)
            {
                UiKit.Label("Side", _body, title, 24, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(x0, 0.56f, x1, 0.61f);
                var text = string.Join("\n", (stacks ?? Array.Empty<BattleStackDto>()).Select(s =>
                    $"{Loc.T("troop." + s.Type)} T{s.Tier}: {CostChips.Compact(s.Start)} <color=#FF8A80>−{CostChips.Compact(s.Lost)}</color>"));
                UiKit.Label("Stacks", _body, text.Length == 0 ? "—" : text, 20, UiKit.Text, TextAnchor.UpperLeft).rectTransform.Place(x0, 0.16f, x1, 0.56f);
            }
        }

        /// <summary>One line for a report: what happened, against whom, and how it ended.</summary>
        private static string ReportLine(PvpReportDto r)
        {
            // A city ("k12"), an alliance building ("relay", "rig", "cannon") or a beacon.
            var place = AllianceLandRules.IsBuilding(r.StrongholdId) ? Loc.T("bld." + r.StrongholdId)
                : r.StrongholdId == AllianceLandRules.Beacon ? Loc.T("land.beacon") : Loc.T("city.name");
            var who = r.StrongholdId.Length > 0 ? $"{place} {Loc.F("base.level", r.StrongholdLevel)}"
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
