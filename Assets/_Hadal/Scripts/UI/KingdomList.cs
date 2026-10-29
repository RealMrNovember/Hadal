using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The kingdom list (docs/39 §4), opened from the map's kingdom badge: every kingdom with its number, opening date,
    /// players and status; ours is marked "Senin krallığın". The server decides who lives where.
    /// </summary>
    public sealed class KingdomList : MonoBehaviour
    {
        private static KingdomList _instance;

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private ulong _sequence;
        private Canvas _canvas;
        private GameObject _window;
        private int _rendered = -1;

        private ClientStateView View => _stateSync.View;

        /// <summary>Opens the list, creating the screen the first time.</summary>
        public static void Show(IStateSyncService stateSync, ICommandDispatcher commands)
        {
            if (_instance == null)
            {
                var go = new GameObject("KingdomList");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<KingdomList>();
                _instance._stateSync = stateSync;
                _instance._commands = commands;
                _instance._canvas = UiKit.Canvas("KingdomList", 48);
                _instance._canvas.transform.SetParent(go.transform, false);
            }

            _instance.Open();
        }

        private void Open()
        {
            _commands.Dispatch(new ViewKingdomsCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence });
            Render();
        }

        private void Close()
        {
            if (_window != null)
                Destroy(_window);
            _window = null;
        }

        private void Update()
        {
            if (_window != null && _rendered != View.KingdomListVersion)
                Render();
        }

        private void Render()
        {
            Close();
            _rendered = View.KingdomListVersion;
            var dim = UiKit.Box("Dim", _canvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _window = dim.gameObject;
            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.04f, 0.16f, 0.96f, 0.84f);
            card.gameObject.AddComponent<Button>();
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>();

            var header = UiKit.Box("Header", card.transform, new Color(0.05f, 0.16f, 0.22f, 1f));
            header.rectTransform.Place(0, 0.9f, 1, 1);
            UiKit.Label("Title", header.transform, Loc.T("kingdom.list"), 40, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0, 0.8f, 1);
            var close = UiKit.Button("Close", header.transform, "X", Close, UiKit.Field, 38);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.85f, 0.15f, 0.96f, 0.85f);
            UiKit.Label("Hint", card.transform, Loc.T("kingdom.hint"), 22, UiKit.Muted, TextAnchor.MiddleLeft).rectTransform.Place(0.04f, 0.82f, 0.96f, 0.89f);

            var kingdoms = View.KingdomList?.Kingdoms ?? Array.Empty<KingdomDto>();
            if (kingdoms.Length == 0)
            {
                UiKit.Label("Wait", card.transform, "…", 40, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0, 0.4f, 1, 0.6f);
                return;
            }

            var list = UiKit.ScrollList("List", card.transform, 10f);
            ((RectTransform)list.parent).Place(0.03f, 0.03f, 0.97f, 0.81f);
            foreach (var k in kingdoms.OrderByDescending(k => k.Mine).ThenByDescending(k => k.Number))
            {
                var row = UiKit.Box("Kingdom", list, k.Mine ? new Color(0.04f, 0.22f, 0.3f, 0.95f) : new Color(0, 0, 0, 0.3f)).Row(130f);
                var title = string.IsNullOrEmpty(k.Name) ? Loc.F("kingdom.number", k.Number) : $"{Loc.F("kingdom.number", k.Number)} · {k.Name}";
                UiKit.Label("Name", row.transform, title, 32, k.Mine ? UiKit.Accent : UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.04f, 0.5f, 0.66f, 0.95f);
                var opened = DateTimeOffset.FromUnixTimeMilliseconds(k.OpenedAtMs).ToLocalTime().ToString("dd.MM.yyyy");
                UiKit.Label("Info", row.transform, $"{Loc.F("kingdom.opened", opened)}   {Loc.F("kingdom.players", k.Players)}", 22, UiKit.Muted, TextAnchor.MiddleLeft)
                    .rectTransform.Place(0.04f, 0.06f, 0.96f, 0.5f);
                var (status, color) = k.Status switch
                {
                    "open" => (Loc.T("kingdom.status.open"), UiKit.Ok),
                    "full" => (Loc.T("kingdom.status.full"), UiKit.Amber),
                    _ => (Loc.T("kingdom.status.closed"), UiKit.Muted)
                };
                UiKit.Label("Status", row.transform, k.Mine ? $"★ {Loc.T("kingdom.mine")}" : status, 24, k.Mine ? UiKit.Accent : color, TextAnchor.MiddleRight, FontStyle.Bold)
                    .rectTransform.Place(0.6f, 0.5f, 0.96f, 0.95f);
            }
        }
    }
}
