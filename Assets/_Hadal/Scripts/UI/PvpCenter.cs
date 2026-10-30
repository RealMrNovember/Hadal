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
    /// and a banner counts down to the server's arrival time; each battle's result comes up as a dialog. The War Room
    /// (Last Asylum's dialog over the blurred game) has three tabs: the shield (the Boost dialog's shield panel), the war
    /// log as battle mail rows, and the alliance's attacks and defences with Go / Join / Reinforce. The Boost dialog
    /// itself (shield, war and economy bonuses) lives in PvpCenter.Boost.cs.
    /// </summary>
    public sealed partial class PvpCenter : MonoBehaviour
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
        private UiFrames.Sheet _room;
        private int _tab;
        private float _nextWarRequest;
        private int _renderedLog = -1;
        private int _renderedWar = -1;
        private string _renderedShield = string.Empty;
        private bool _showingReport;
        private readonly List<(TMP_Text Label, long AtMs, string Format)> _clocks = new();

        private ClientStateView View => _stateSync.View;

        /// <summary>The War Room or the Boost dialog is up.</summary>
        public bool IsOpen => _room is { IsOpen: true } || BoostOpen;

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
                ("pvp.alarm.shield", OpenShield, UiKit.Primary),
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

            TickBoost();
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

            if (_result == null && !IsOpen && _results.Count > 0)
                ShowResult(_results.Dequeue());

            if (_room is { IsOpen: true })
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
            _banner.Show(!IsOpen);
            _banner.Tick(dt, cardMood == AlarmMood.Threat ? AlarmTempo.Threat(left).Period : 3f, cardMood != AlarmMood.Threat);
            _bannerActions.gameObject.SetActive(!IsOpen && _result == null && _banner.Showing);
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
        private UiFrames.Sheet _result;
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

        /// <summary>Battles on us since we last looked, from before this login: one "your base was attacked" dialog.</summary>
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

            var lines = new List<string>();
            var dead = missed.Sum(r => r.Dead);
            var wounded = missed.Sum(r => r.Wounded);
            if (dead + wounded > 0)
                lines.Add(Loc.F("pvp.losses", dead.ToString("N0"), wounded.ToString("N0")));
            var lost = Loot(missed.SelectMany(r => r.Plunder ?? Array.Empty<ResourceAmountEntry>()), "−");
            if (lost.Length > 0)
                lines.Add(lost);
            ResultDialog(Loc.T("pvp.away.title"), null, Loc.F("pvp.away.count", missed.Count), UiFrames.InkBad, lines, missed.Take(4).ToList(),
                Loc.T("pvp.tab.log"), () => Open(1));
        }

        /// <summary>One battle's result as it lands: both sides face to face, what was won or lost, and the report.</summary>
        private void ShowResult(PvpReportDto r)
        {
            var won = Won(r);
            GameAudio.Play(won ? Sfx.Reward : Sfx.Error);
            var lines = new List<string>();
            if (r.Outcome == "shielded")
                lines.Add(Hit(r) ? Loc.F("pvp.result.blocked", r.OpponentName) : Loc.T("pvp.result.bounced"));
            var loot = Loot(r.Plunder ?? Array.Empty<ResourceAmountEntry>(), Hit(r) ? "−" : "+");
            if (loot.Length > 0 && r.Kind != "scout")
                lines.Add(loot);
            if (r.Dead + r.Wounded > 0)
                lines.Add(Loc.F("pvp.losses", r.Dead.ToString("N0"), r.Wounded.ToString("N0")));
            if (r.Kind == "scout")
                lines.Add(Loc.F("pvp.scout.result", r.DefenderPower.ToString("N0"), r.Reinforcements));
            var title = r.Kind is "scout" or "scouted" ? Loc.T("pvp.kind." + r.Kind)
                : r.Outcome is "won" or "lost" ? Loc.T(won ? "pvp.result.win" : "pvp.result.loss") : Loc.T("pvp.outcome." + r.Outcome);
            var report = r;
            ResultDialog(title, r, Summary(r), won ? UiFrames.InkGood : UiFrames.InkBad, lines, null, Loc.T("mail.pvp.open"), () =>
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

        /// <summary>
        /// A result dialog over every screen (Last Asylum's battle result): the two sides face to face when there was a
        /// battle, the headline, what it cost or brought, a few report rows, then the action and OK in the footer.
        /// </summary>
        private void ResultDialog(string title, PvpReportDto battle, string headline, Color color, List<string> lines, List<PvpReportDto> rows,
            string action, Action onAction)
        {
            CloseResult();
            var sheet = UiFrames.OpenSheet(_windowCanvas.transform, title, CloseResult, footer: 128f);
            _result = sheet;
            if (battle != null)
                Face(sheet.Content, battle).Row(230);

            var head = UiKit.Label("Headline", sheet.Content, headline, UiKit.TypeScale.Subtitle, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            head.richText = true;
            head.Row(70);
            foreach (var line in lines)
                UiFrames.SheetText(sheet.Content, line, UiKit.TypeScale.Body, UiFrames.Ink).Row(56);
            foreach (var r in rows ?? new List<PvpReportDto>())
                LogRow(sheet.Content, r, null);
            UiFrames.SheetText(sheet.Content, Loc.T("pvp.result.mail"), UiKit.TypeScale.Caption + 2).Row(50);

            UiFrames.FooterButton(sheet.Footer, action, () =>
            {
                CloseResult();
                onAction();
            }, UiKit.Primary, 0f, 0.5f);
            UiFrames.FooterButton(sheet.Footer, Loc.T("pvp.result.ok"), CloseResult, UiKit.Ok, 0.5f, 1f);
            UiFrames.FitSheet(sheet);
        }

        private void CloseResult()
        {
            _result?.Close();
            _result = null;
        }

        /// <summary>Opens the result of a battle in the war log (from its mail); the log itself when it has scrolled out.</summary>
        public void OpenReport(string battleId, string reportKind)
        {
            var reports = View.WarLog?.Reports ?? Array.Empty<PvpReportDto>();
            var report = reports.FirstOrDefault(r => r.Id == battleId && r.Kind == reportKind) ?? reports.FirstOrDefault(r => r.Id == battleId);
            Open(1);
            if (report != null)
                ShowReport(report, viaMail: false);
        }

        // --- Report wording (as the mailbox words it) -----------------------------------------

        /// <summary>The other side: "[TAG] Name", or the city or alliance building fought over.</summary>
        private static string Opponent(PvpReportDto r)
        {
            if (!string.IsNullOrEmpty(r.StrongholdId))
            {
                var place = AllianceLandRules.IsBuilding(r.StrongholdId) ? Loc.T("bld." + r.StrongholdId)
                    : r.StrongholdId == AllianceLandRules.Beacon ? Loc.T("land.beacon") : Loc.T("city.name");
                return r.StrongholdLevel > 0 ? $"{place} {Loc.F("base.level", r.StrongholdLevel)}" : place;
            }

            return (string.IsNullOrEmpty(r.OpponentTag) ? string.Empty : $"[{r.OpponentTag}] ") + r.OpponentName;
        }

        private static bool WeAttacked(PvpReportDto r) => r.Kind is "attack" or "rally" or "scout";

        private static bool IsScout(PvpReportDto r) => r.Kind is "scout" or "scouted";

        /// <summary>Victory / Defeat / Shield blocked / Scout report / You were scouted.</summary>
        private static string Headline(PvpReportDto r) =>
            r.Kind == "scouted" ? Loc.T("mail.head.scouted")
            : r.Kind == "scout" ? Loc.T("mail.head.scout")
            : r.Outcome is "won" or "lost" or "shielded" ? Loc.T("mail.head." + r.Outcome)
            : Loc.T("pvp.outcome." + r.Outcome);

        private static Color HeadlineColor(PvpReportDto r) =>
            IsScout(r) ? UiFrames.Ink : r.Outcome == "won" ? UiFrames.InkGood : r.Outcome == "lost" ? UiFrames.InkBad : new Color(0.72f, 0.42f, 0.06f, 1f);

        /// <summary>"Attack · [TAG] Name" with the enemy in red, as Last Asylum's battle mail rows say it.</summary>
        private static string Summary(PvpReportDto r) =>
            $"{Loc.T("pvp.kind." + r.Kind)} · <color=#D0392F>{Opponent(r).Replace("<", "‹").Replace(">", "›")}</color>" +
            (r.Captured ? " · " + Loc.T("pvp.captured") : string.Empty);

        private static string ReportIcon(PvpReportDto r) => IsScout(r) ? "left_search" : r.Outcome == "shielded" ? "item_shield_8h" : "event_war";

        /// <summary>The report's head: the attacker blue on the left, the defender red on the right, the result over each.</summary>
        private static RectTransform Face(Transform parent, PvpReportDto r)
        {
            var we = WeAttacked(r);
            var us = Loc.T("mail.report.you");
            var them = Opponent(r);
            var attackerWon = r.Outcome == "won" ? we : r.Outcome == "lost" ? !we : (bool?)null;
            if (IsScout(r) || r.Outcome is "shielded" or "moved" or "gone")
                attackerWon = null;
            string Result(bool attacker) => IsScout(r) ? Loc.T(attacker ? "mail.report.scoutSide" : "mail.report.scoutedSide")
                : attackerWon == null ? string.Empty : Loc.T(attackerWon.Value == attacker ? "battle.won" : "battle.lost");
            var attackerPower = r.AttackerPower > 0 ? Loc.F("hero.power", r.AttackerPower.ToString("N0")) : string.Empty;
            var defenderPower = r.DefenderPower > 0 && r.Kind != "scouted" ? Loc.F("hero.power", r.DefenderPower.ToString("N0")) : string.Empty;
            return UiFrames.Versus(parent,
                (we ? us : them, Result(true), attackerWon == true, attackerPower),
                (we ? them : us, Result(false), attackerWon == false, defenderPower),
                IsScout(r) || r.Outcome == "shielded" ? ReportIcon(r) : null);
        }

        // --- War Room -----------------------------------------------------------------------------

        private const int TabShield = 0;
        private const int TabLog = 1;
        private const int TabWar = 2;

        /// <summary>Opens the War Room on a tab: 0 shield, 1 war log, 2 alliance war.</summary>
        public void Open(int tab)
        {
            _tab = Mathf.Clamp(tab, TabShield, TabWar);
            CloseBoost();
            if (_room is not { IsOpen: true })
                _room = UiFrames.OpenSheet(_windowCanvas.transform, Loc.T("pvp.title"), Close, height: 0.74f, top: 100f);
            _renderedLog = _renderedWar = -1;
            _renderedShield = string.Empty;
            _showingReport = false;
            _nextWarRequest = 0f;
            Refill();
        }

        public void Close()
        {
            _room?.Close();
            _room = null;
            _clocks.Clear();
        }

        /// <summary>Empties the room and draws its tabs again (the body follows in <see cref="TickWindow"/>).</summary>
        private void Refill()
        {
            _clocks.Clear();
            UiFrames.ClearSheet(_room);
            var labels = new[] { Loc.T("pvp.tab.shield"), Loc.T("pvp.tab.log"), Loc.T("pvp.tab.alliance") };
            UiFrames.Tabs(_room.Top, labels, _tab, i => Open(i)).Place(0, 0, 1, 1);
        }

        private void TickWindow(long now)
        {
            switch (_tab)
            {
                case TabShield:
                    var key = ShieldKey(now);
                    if (key != _renderedShield)
                    {
                        Refill();
                        _renderedShield = key;
                        ShieldPanel(_room.Content, _clocks);
                    }

                    break;
                case TabLog:
                    if (!_showingReport && _renderedLog != View.WarLogVersion)
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

            Tick(_clocks, now);
        }

        private static void Tick(List<(TMP_Text Label, long AtMs, string Format)> clocks, long now)
        {
            foreach (var (label, at, format) in clocks)
                if (label != null)
                    label.text = string.Format(format, Loc.Duration(Math.Max(0, (at - now) / 1000f)));
        }

        /// <summary>The war log as Last Asylum's battle mail list: a disc with the kind, the headline, the enemy in red, the age.</summary>
        private void RenderLog()
        {
            _renderedLog = View.WarLogVersion;
            Refill();
            var reports = View.WarLog?.Reports ?? Array.Empty<PvpReportDto>();
            if (reports.Length == 0)
            {
                UiFrames.Note(_room.Content, Loc.T("pvp.log.empty")).Row(160);
                return;
            }

            foreach (var r in reports)
            {
                var report = r;
                LogRow(_room.Content, r, () => ShowReport(report));
            }
        }

        /// <summary>One battle mail row; tapping it opens the report.</summary>
        private void LogRow(Transform parent, PvpReportDto r, Action open)
        {
            var row = UiFrames.SheetBlock(parent, Won(r) || IsScout(r) ? UiFrames.Card : new Color(0.93f, 0.86f, 0.86f, 1f), "Report");
            row.Row(150);
            if (open != null)
            {
                row.raycastTarget = true;
                row.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    GameAudio.Play(Sfx.UiTap);
                    open();
                });
                row.gameObject.AddComponent<PressFeedback>();
            }

            var disc = UiKit.Box("Disc", row.transform, new Color(0.78f, 0.85f, 0.93f, 1f));
            disc.sprite = UiKit.Circle;
            disc.type = Image.Type.Simple;
            disc.preserveAspect = true;
            disc.raycastTarget = false;
            disc.rectTransform.Place(0.02f, 0.1f, 0.17f, 0.9f);
            Hud.HudIcons.Image(disc.transform, ReportIcon(r), 0.16f, 0.16f, 0.84f, 0.84f);

            var head = UiKit.Label("Headline", row.transform, Headline(r), UiKit.TypeScale.Body + 2, HeadlineColor(r), TextAnchor.MiddleLeft, FontStyle.Bold);
            head.rectTransform.Place(0.2f, 0.6f, 0.97f, 0.95f);
            var summary = UiKit.Label("Summary", row.transform, Summary(r), UiKit.TypeScale.Caption + 2, UiFrames.Ink, TextAnchor.MiddleLeft);
            summary.rectTransform.Place(0.2f, 0.32f, 0.97f, 0.62f);
            summary.textWrappingMode = TextWrappingModes.NoWrap;
            summary.overflowMode = TextOverflowModes.Ellipsis;
            var detail = new List<string> { Loc.F("pvp.ago", Loc.Duration(Math.Max(0, (View.ServerNowMs - r.AtMs) / 1000f))) };
            if (r.Dead + r.Wounded > 0)
                detail.Add(Loc.F("pvp.losses", r.Dead.ToString("N0"), r.Wounded.ToString("N0")));
            var loot = Loot(r.Plunder ?? Array.Empty<ResourceAmountEntry>(), Hit(r) ? "−" : "+");
            if (loot.Length > 0 && r.Kind != "scout")
                detail.Add(loot);
            var age = UiKit.Label("Detail", row.transform, string.Join("  ·  ", detail), UiKit.TypeScale.Caption, UiFrames.InkSoft, TextAnchor.MiddleLeft);
            age.rectTransform.Place(0.2f, 0.05f, 0.97f, 0.34f);
            age.textWrappingMode = TextWrappingModes.NoWrap;
            age.overflowMode = TextOverflowModes.Ellipsis;
        }

        /// <summary>
        /// A battle's report. The mailbox's report card is the one report screen (Last Asylum's), so a report whose mail
        /// is still in the box opens there; an older one is drawn here in the same order: place and time, the two
        /// sides face to face, the overview, both armies stack by stack, and the battle's id with its seed.
        /// </summary>
        private void ShowReport(PvpReportDto r, bool viaMail = true)
        {
            if (viaMail && FindFirstObjectByType<SocialView>() is { } social && social.OpenBattleMail(r.Id, r.Kind))
            {
                Close();
                return;
            }

            _tab = TabLog;
            Refill();
            _showingReport = true;
            var content = _room.Content;
            var at = DateTimeOffset.FromUnixTimeMilliseconds(r.AtMs).ToLocalTime();
            var top = UiKit.Rect("Place", content).Row(56);
            UiKit.Label("Where", top, Loc.F("mail.report.place", r.X, r.Y), UiKit.TypeScale.Body, UiFrames.InkGood, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.02f, 0, 0.6f, 1);
            UiKit.Label("When", top, at.ToString("yyyy-MM-dd HH:mm"), UiKit.TypeScale.Caption + 2, UiFrames.InkSoft, TextAnchor.MiddleRight)
                .rectTransform.Place(0.5f, 0, 0.98f, 1);
            Face(content, r).Row(230);

            UiFrames.Section(content, Loc.T("mail.report.overview")).Row(60);
            var lines = new List<string>();
            if (r.Outcome == "shielded")
                lines.Add(WeAttacked(r) ? Loc.T("mail.pvp.bounced") : Loc.F("mail.pvp.blocked", Opponent(r)));
            else if (r.Outcome is "moved" or "gone")
                lines.Add(Loc.T("mail.report." + r.Outcome));
            if (r.Rounds > 0)
                lines.Add(Loc.F("report.rounds", r.Rounds) + "   " + Loc.F("report.powers", r.AttackerPower.ToString("N0"), r.DefenderPower.ToString("N0")));
            if (r.Dead + r.Wounded > 0)
                lines.Add(Loc.F("mail.report.ourLosses", r.Dead.ToString("N0"), r.Wounded.ToString("N0")));
            var loot = (r.Plunder ?? Array.Empty<ResourceAmountEntry>()).Where(p => p.Value > 0).ToList();
            if (loot.Count > 0)
                lines.Add(Loc.T("report.loot") + " " + string.Join("  ", loot.Select(p => $"{CostChips.Compact(p.Value)} {Loc.T("resource." + p.Key)}")));
            if (r.PlunderShare < 100 && r.Kind is "attack" or "rally")
                lines.Add(Loc.F("report.fatigue", r.PlunderShare));
            if (r.Contribution > 0 && r.Contribution < 100)
                lines.Add(Loc.F("report.contribution", r.Contribution));
            if (r.Kind == "scout")
                lines.Add(Loc.F("pvp.scout.result", r.DefenderPower.ToString("N0"), r.Reinforcements));
            for (var i = 0; i < lines.Count; i++)
                UiFrames.TableRow(content, new[] { lines[i] }, index: i).Row(64);

            if (!(r.Outcome is "shielded" or "moved" or "gone") && r.Kind != "scouted")
            {
                Army(Loc.T("report.attacker"), r.Attacker, WeAttacked(r));
                Army(Loc.T("report.defender"), r.Defender, !WeAttacked(r));
            }

            UiFrames.SheetText(content, r.RuleVersion > 0 ? Loc.F("report.integrity", r.Id, r.Seed, r.RuleVersion) : Loc.F("mail.pvp.ref", r.Id),
                UiKit.TypeScale.Micro).Row(44);
            var buttons = UiKit.Rect("Buttons", content).Row(112);
            UiFrames.FooterButton(buttons, Loc.T("report.back"), () =>
            {
                _showingReport = false;
                _renderedLog = -1;
            }, UiKit.Field, 0f, 0.5f);
            var x = r.X;
            var y = r.Y;
            UiFrames.FooterButton(buttons, Loc.T("pvp.go"), () =>
            {
                Close();
                _actions.ShowOnMap?.Invoke(x, y);
            }, UiKit.Primary, 0.5f, 1f);

            void Army(string side, BattleStackDto[] stacks, bool ours)
            {
                UiFrames.Section(content, ours ? $"{side} · {Loc.T("mail.report.you")}" : side).Row(60);
                var all = (stacks ?? Array.Empty<BattleStackDto>()).OrderByDescending(s => s.Start).ToList();
                if (all.Count == 0)
                {
                    UiFrames.TableRow(content, new[] { Loc.T("mail.report.noTroops") }).Row(64);
                    return;
                }

                UiFrames.TableRow(content, new[] { string.Empty, Loc.T("mail.report.start"), Loc.T("mail.report.lost"), Loc.T("mail.report.left") }, header: true).Row(52);
                for (var i = 0; i < all.Count; i++)
                {
                    var s = all[i];
                    UiFrames.TableRow(content, new[]
                    {
                        $"T{s.Tier} {Loc.T("troop." + s.Type)}", s.Start.ToString("N0"),
                        s.Lost > 0 ? $"<color=#C8352B>−{s.Lost:N0}</color>" : "0", Math.Max(0, s.Start - s.Lost).ToString("N0")
                    }, index: i).Row(64);
                }
            }
        }

        /// <summary>The alliance's marches in and out: who goes where, the clock, and Go / Join / Reinforce.</summary>
        private void RenderWar()
        {
            _renderedWar = View.WarListVersion;
            Refill();
            if (View.MyAlliance == null)
            {
                UiFrames.Note(_room.Content, Loc.T("pvp.war.noAlliance")).Row(160);
                return;
            }

            var rows = View.WarList?.Rows ?? Array.Empty<WarRowDto>();
            if (rows.Length == 0)
            {
                UiFrames.Note(_room.Content, Loc.T("pvp.war.empty")).Row(160);
                return;
            }

            var now = View.ServerNowMs;
            foreach (var w in rows)
            {
                var row = UiFrames.SheetBlock(_room.Content, w.Incoming ? new Color(0.94f, 0.86f, 0.86f, 1f) : UiFrames.Card, "War");
                row.Row(176);
                var attacker = (string.IsNullOrEmpty(w.AttackerTag) ? string.Empty : $"[{w.AttackerTag}] ") + w.AttackerName;
                var target = w.TargetKind == PvpRules.TargetStronghold ? Loc.T("pvp.stronghold")
                    : (string.IsNullOrEmpty(w.TargetTag) ? string.Empty : $"[{w.TargetTag}] ") + w.TargetName;
                var title = $"{(w.Incoming ? "⬇ " + Loc.T("pvp.war.in") : "⬆ " + Loc.T("pvp.war.out"))} · {Loc.T("pvp.kind." + w.Kind)}";
                UiKit.Label("Title", row.transform, title, UiKit.TypeScale.Body, w.Incoming ? UiFrames.InkBad : UiKit.Primary, TextAnchor.MiddleLeft, FontStyle.Bold)
                    .rectTransform.Place(0.03f, 0.66f, 0.7f, 0.96f);
                var who = UiKit.Label("Who", row.transform, $"{attacker} → {target}", UiKit.TypeScale.Caption + 2, UiFrames.Ink, TextAnchor.MiddleLeft);
                who.rectTransform.Place(0.03f, 0.36f, 0.7f, 0.66f);
                who.richText = false;
                var clock = UiKit.Label("Clock", row.transform, string.Empty, UiKit.TypeScale.Caption + 2, UiFrames.InkSoft, TextAnchor.MiddleLeft);
                clock.rectTransform.Place(0.03f, 0.04f, 0.7f, 0.36f);
                if (w.DepartsAtMs > now)
                    _clocks.Add((clock, w.DepartsAtMs, Loc.T("pvp.war.departs") + $"  ({w.Teams}/{w.MaxTeams})"));
                else
                    _clocks.Add((clock, w.ArrivesAtMs, Loc.T("pvp.war.arrives")));

                var row0 = w;
                var go = UiKit.Button("Go", row.transform, Loc.T("pvp.go"), () => { Close(); _actions.ShowOnMap?.Invoke(row0.ToX, row0.ToY); }, UiKit.Primary,
                    UiKit.TypeScale.Body);
                ((RectTransform)go.transform).Place(0.72f, 0.54f, 0.97f, 0.94f);
                if (w.Kind == PvpRules.Rally && !w.Incoming && !w.Joined && w.DepartsAtMs > now && w.Teams < w.MaxTeams)
                {
                    var join = UiKit.Button("Join", row.transform, Loc.T("pvp.rally.join"), () => { Close(); _actions.JoinRally?.Invoke(row0); }, UiKit.Ok,
                        UiKit.TypeScale.Body);
                    ((RectTransform)join.transform).Place(0.72f, 0.08f, 0.97f, 0.48f);
                }
                else if (w.Incoming && w.TargetKind == PvpRules.TargetBase && w.TargetId != View.PlayerId)
                {
                    var help = UiKit.Button("Help", row.transform, Loc.T("pvp.reinforce"), () =>
                    {
                        Close();
                        _actions.Reinforce?.Invoke(row0.TargetId, target, row0.ToX, row0.ToY);
                    }, UiKit.Gold, UiKit.TypeScale.Body);
                    ((RectTransform)help.transform).Place(0.72f, 0.08f, 0.97f, 0.48f);
                }
            }
        }
    }
}
