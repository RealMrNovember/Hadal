using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Events;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Last Asylum's speed-up window: what is being sped up, the time left on a bar, and every fitting speed-up in the
    /// bag as a row (what it takes off, how many are held, Use). Opens over any screen for construction, training,
    /// healing, research and marches.
    /// </summary>
    public sealed class SpeedupWindow : MonoBehaviour
    {
        public static SpeedupWindow Instance { get; private set; }

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private ILocalEventBus _localBus;
        private Canvas _canvas;
        private GameObject _window;
        private ulong _sequence;

        private string _target = string.Empty;
        private SpeedupKind _kind;
        private string _title = string.Empty;
        private Func<(long StartMs, long EndMs)?> _timer;
        private TMP_Text _left;
        private Image _bar;
        private int _itemsVersion = -1;

        private ClientStateView View => _stateSync.View;

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
            if (_window != null)
                Destroy(_window);
            _window = null;
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
            if (_window == null)
                return;
            var timer = _timer?.Invoke();
            var now = View.ServerNowMs;
            if (timer == null || timer.Value.EndMs <= now)
            {
                Close();
                return;
            }

            var (start, end) = timer.Value;
            _left.text = Loc.Duration((end - now) / 1000f);
            _bar.rectTransform.Place(0, 0, Mathf.Clamp01((now - start) / (float)Math.Max(1, end - start)), 1);
            if (_itemsVersion != View.QuestVersion && !(Input.touchCount > 0 || Input.GetMouseButton(0)))
                Render();
        }

        private void Render()
        {
            var fresh = _window == null;
            Close();
            _itemsVersion = View.QuestVersion;

            var dim = UiKit.Box("Speedup", _canvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _window = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.04f, 0.2f, 0.96f, 0.8f);
            card.gameObject.AddComponent<Button>();
            if (fresh)
            {
                card.gameObject.AddComponent<CanvasGroup>();
                card.gameObject.AddComponent<PopIn>();
            }

            var header = UiKit.Box("Header", card.transform, new Color(0.05f, 0.16f, 0.22f, 1f));
            header.rectTransform.Place(0, 0.88f, 1, 1);
            UiKit.Label("Title", header.transform, Loc.T("speedup.title"), 40, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0.45f, 0.8f, 1f);
            UiKit.Label("What", header.transform, _title, 26, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0.02f, 0.8f, 0.5f);
            var close = UiKit.Button("Close", header.transform, "X", Close, UiKit.Field, 40);
            ((RectTransform)close.transform).Place(0.84f, 0.18f, 0.96f, 0.82f);

            var track = UiKit.Box("Track", card.transform, new Color(0, 0, 0, 0.6f));
            track.rectTransform.Place(0.06f, 0.79f, 0.94f, 0.84f);
            _bar = UiKit.Box("Bar", track.transform, UiKit.Ok);
            _left = UiKit.Label("Left", track.transform, string.Empty, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _left.rectTransform.Place(0, 0, 1, 1);
            UiText.Outline(_left, new Color(0, 0, 0, 0.7f));

            var list = UiKit.ScrollList("List", card.transform, 10f);
            ((RectTransform)list.parent).Place(0.04f, 0.03f, 0.96f, 0.76f);

            var fitting = View.Items.Where(p => p.Value > 0)
                .Select(p => (Id: p.Key, Count: p.Value, Ok: Speedups.TryParse(p.Key, out var kind, out var value, out var percent), Kind: kind, Value: value, Percent: percent))
                .Where(e => e.Ok && (Speedups.Fits(e.Kind, _kind) || (e.Kind == SpeedupKind.March && _kind == SpeedupKind.March)))
                .OrderBy(e => e.Kind == SpeedupKind.Any ? 1 : 0).ThenBy(e => e.Value)
                .ToList();
            if (fitting.Count == 0)
            {
                UiKit.Label("None", list, Loc.T("speedup.none"), 28, UiKit.Muted, TextAnchor.MiddleCenter).Row(160);
                return;
            }

            foreach (var e in fitting)
            {
                var row = UiKit.Box("Row", list, new Color(0.04f, 0.11f, 0.15f, 1f)).Row(140);
                var icon = UiKit.Box("Icon", row.transform, e.Kind == SpeedupKind.March ? new Color(0.24f, 0.52f, 0.9f, 1f) : e.Kind == SpeedupKind.Any ? new Color(0.6f, 0.33f, 0.86f, 1f) : new Color(0.27f, 0.66f, 0.33f, 1f));
                icon.rectTransform.Place(0.02f, 0.1f, 0.17f, 0.9f);
                icon.raycastTarget = false;
                UiKit.Label("Amount", icon.transform, e.Percent ? $"%{e.Value}" : Loc.Duration(e.Value * 60f), 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .rectTransform.Place(0, 0, 1, 1);
                UiKit.Label("Name", row.transform, QuestText.Item(e.Id), 28, UiKit.Text, TextAnchor.UpperLeft, FontStyle.Bold).rectTransform.Place(0.2f, 0.5f, 0.66f, 0.92f);
                UiKit.Label("Have", row.transform, Loc.F("speedup.have", e.Count), 24, UiKit.Muted, TextAnchor.LowerLeft).rectTransform.Place(0.2f, 0.08f, 0.66f, 0.5f);
                var id = e.Id;
                var use = UiKit.Button("Use", row.transform, Loc.T("bag.use"), () => Use(id), UiKit.Primary, 32);
                ((RectTransform)use.transform).Place(0.68f, 0.18f, 0.98f, 0.82f);
            }
        }

        private void Use(string itemId) =>
            _commands.Dispatch(new UseSpeedupCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, ItemId = itemId, Count = 1, Target = _target });
    }
}
