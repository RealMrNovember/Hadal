using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.Events;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Last Asylum's speed-up window, in the genre's centred dialog: the job and its timer bar on top (with Finish Free
    /// once a construction or research is short enough), then every fitting speed-up in the bag as an item card with
    /// its rarity tile, what it takes off, how many are held and Use. Opens over any screen for construction, training,
    /// healing, research and marches.
    /// </summary>
    public sealed class SpeedupWindow : MonoBehaviour
    {
        public static SpeedupWindow Instance { get; private set; }

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private ILocalEventBus _localBus;
        private Canvas _canvas;
        private UiFrames.Sheet _sheet;
        private ulong _sequence;

        private string _target = string.Empty;
        private SpeedupKind _kind;
        private string _title = string.Empty;
        private Func<(long StartMs, long EndMs)?> _timer;
        private RectTransform _bar;
        private int _itemsVersion = -1;
        private bool _freeShown;

        private ClientStateView View => _stateSync.View;

        public bool IsOpen => _sheet != null;

        public void Initialize(IStateSyncService stateSync, ICommandDispatcher commands, ILocalEventBus localBus)
        {
            Instance = this;
            _stateSync = stateSync;
            _commands = commands;
            _localBus = localBus;
            _canvas = UiKit.Canvas("SpeedupUi", 48);
            _canvas.transform.SetParent(transform, false);
            _localBus?.Subscribe<CommandResolvedEvent>(OnCommandResolved);
        }

        private void OnDestroy()
        {
            _localBus?.Unsubscribe<CommandResolvedEvent>(OnCommandResolved);
            if (Instance == this)
                Instance = null;
        }

        /// <param name="target">"build:{id}", "train:{id}", "heal", "research:{node}", "march:{id}" (the server's target).</param>
        /// <param name="timer">Start and end of the timer in server milliseconds, or null once it is over.</param>
        public static void Open(string target, SpeedupKind kind, string title, Func<(long StartMs, long EndMs)?> timer)
        {
            if (Instance == null)
                return;
            Instance._target = target;
            Instance._kind = kind;
            Instance._title = title;
            Instance._timer = timer;
            Instance.Render();
        }

        public void Close()
        {
            if (_sheet != null)
                Destroy(_sheet.Root.gameObject);
            _sheet = null;
            _bar = null;
        }

        private void OnCommandResolved(CommandResolvedEvent result)
        {
            if (result.Type != CommandType.UseSpeedup)
                return;
            Toaster.Show(result.Result == CommandResultCode.Rejected ? Loc.Reason(result.Reason) : Loc.T("speedup.used"),
                result.Result == CommandResultCode.Rejected ? UiKit.Danger : UiKit.Ok);
        }

        private void Update()
        {
            if (_sheet == null)
                return;
            var timer = _timer?.Invoke();
            var now = View.ServerNowMs;
            if (timer == null || timer.Value.EndMs <= now)
            {
                Close();
                return;
            }

            var (start, end) = timer.Value;
            var left = (end - now) / 1000f;
            UiFrames.SetProgress(_bar, (now - start) / (float)Math.Max(1, end - start), Loc.Duration(left));
            var refill = _itemsVersion != View.QuestVersion || CanFinishFree(left) != _freeShown;
            if (refill && !(Input.touchCount > 0 || Input.GetMouseButton(0)))
                Render();
        }

        /// <summary>Constructions and research end for free in their last minutes (the server's free-finish rule).</summary>
        private bool CanFinishFree(float left) =>
            (_target.StartsWith("build:", StringComparison.Ordinal) || _target.StartsWith("research:", StringComparison.Ordinal))
            && left <= ClientCatalog.Catalog.Progression.freeFinishSeconds;

        /// <summary>Builds the dialog once, then refills its content in place (counts change, Finish Free appears).</summary>
        private void Render()
        {
            if (_sheet == null)
                _sheet = UiFrames.SizedDialog(_canvas.transform, Close, 0.8f);
            UiFrames.Clear(_sheet.List);
            _itemsVersion = View.QuestVersion;
            _sheet.Parts.Title.text = Loc.T("speedup.title");

            var timer = _timer?.Invoke();
            var now = View.ServerNowMs;
            var (start, end) = timer ?? (now, now);
            var left = Math.Max(0, (end - now) / 1000f);
            _freeShown = CanFinishFree(left);
            var job = UiFrames.Job(_sheet.List, _title, (now - start) / (float)Math.Max(1, end - start), Loc.Duration(left), "blue", _freeShown);
            _bar = job.Bar;
            if (_freeShown)
                UiKit.Button("Finish", job.Buttons, Loc.T("base.finish"), FinishFree, UiKit.Ok, 32);

            var fitting = View.Items.Where(p => p.Value > 0)
                .Select(p => (Id: p.Key, Count: p.Value, Ok: Speedups.TryParse(p.Key, out var kind, out var value, out var percent), Kind: kind, Value: value, Percent: percent))
                .Where(e => e.Ok && (Speedups.Fits(e.Kind, _kind) || (e.Kind == SpeedupKind.March && _kind == SpeedupKind.March)))
                .OrderBy(e => e.Kind == SpeedupKind.Any ? 1 : 0).ThenBy(e => e.Value)
                .ToList();
            if (fitting.Count == 0)
            {
                UiFrames.Note(_sheet.List, Loc.T("speedup.none")).Row(130);
                return;
            }

            var grid = UiFrames.Grid(_sheet.List, 4, 0.56f, 10f);
            grid.GetComponent<UnityEngine.UI.GridLayoutGroup>().padding = new RectOffset(0, 0, 4, 4);
            foreach (var e in fitting)
            {
                var id = e.Id;
                UiFrames.ItemCard(grid, QuestText.Item(id), "item_speed_" + e.Kind.ToString().ToLowerInvariant(),
                    Tone(e.Value, e.Percent), QuestText.SpeedupAmount(e.Value, e.Percent), Loc.F("speedup.have", e.Count), Loc.T("bag.use"), () => Use(id));
            }
        }

        /// <summary>The tile's frame by what it takes off (as in the bag): up to an hour green, 8 hours blue, a day purple, more gold.</summary>
        private static string Tone(int minutes, bool percent) =>
            percent ? minutes >= 50 ? "epic" : "rare"
            : minutes > 1440 ? "legendary" : minutes > 480 ? "epic" : minutes > 60 ? "rare" : "common";

        private void Use(string itemId) =>
            _commands.Dispatch(new UseSpeedupCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, ItemId = itemId, Count = 1, Target = _target });

        private void FinishFree()
        {
            var colon = _target.IndexOf(':');
            var id = colon < 0 ? string.Empty : _target.Substring(colon + 1);
            if (_target.StartsWith("build:", StringComparison.Ordinal))
                _commands.Dispatch(new FinishConstructionCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, BuildingEntityId = id });
            else
                _commands.Dispatch(new FinishResearchCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, NodeId = id });
            Close();
        }
    }
}
