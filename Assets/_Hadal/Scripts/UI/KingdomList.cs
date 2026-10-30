using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// The kingdom list (docs/39 §4), opened from the map's kingdom badge and the profile's kingdom row: a dialog over
    /// the blurred game with every kingdom's number, opening date, players and status; ours first, marked "Senin
    /// krallığın" in the gold of a current row. The server decides who lives where.
    /// </summary>
    public sealed class KingdomList : MonoBehaviour
    {
        private static KingdomList _instance;

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private ulong _sequence;
        private Canvas _canvas;
        private UiFrames.DialogSheet _window;
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
            _window?.Close();
            _window = null;
        }

        private void Update()
        {
            if (_window is { IsOpen: true } && _rendered != View.KingdomListVersion)
                Render();
        }

        /// <summary>A dialog of kingdom rows, ours first with the gold highlight of Last Asylum's current row.</summary>
        private void Render()
        {
            _rendered = View.KingdomListVersion;
            if (_window is not { IsOpen: true })
                _window = UiFrames.OpenSheet(_canvas.transform, Loc.T("kingdom.list"), Close);
            UiFrames.ClearSheet(_window);
            var content = _window.Content;
            UiFrames.Note(content, Loc.T("kingdom.hint")).Row(150);

            var kingdoms = View.KingdomList?.Kingdoms ?? Array.Empty<KingdomDto>();
            if (kingdoms.Length == 0)
            {
                UiFrames.SheetText(content, "…", UiKit.TypeScale.Title).Row(120);
                UiFrames.FitSheet(_window);
                return;
            }

            foreach (var k in kingdoms.OrderByDescending(k => k.Mine).ThenByDescending(k => k.Number))
            {
                var row = UiFrames.SheetBlock(content, k.Mine ? UiFrames.Highlight : UiFrames.Card, "Kingdom");
                row.Row(130);
                var title = string.IsNullOrEmpty(k.Name) ? Loc.F("kingdom.number", k.Number) : $"{Loc.F("kingdom.number", k.Number)} · {k.Name}";
                var name = UiKit.Label("Name", row.transform, title, UiKit.TypeScale.Subtitle, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.rectTransform.Place(0.04f, 0.5f, 0.64f, 0.95f);
                name.richText = false;
                var opened = DateTimeOffset.FromUnixTimeMilliseconds(k.OpenedAtMs).ToLocalTime().ToString("dd.MM.yyyy");
                UiKit.Label("Info", row.transform, $"{Loc.F("kingdom.opened", opened)}   ·   {Loc.F("kingdom.players", k.Players)}", UiKit.TypeScale.Caption + 2,
                    UiFrames.InkSoft, TextAnchor.MiddleLeft).rectTransform.Place(0.04f, 0.06f, 0.96f, 0.5f);
                var (status, color) = k.Status switch
                {
                    "open" => (Loc.T("kingdom.status.open"), UiFrames.InkGood),
                    "full" => (Loc.T("kingdom.status.full"), new Color(0.74f, 0.42f, 0.04f, 1f)),
                    _ => (Loc.T("kingdom.status.closed"), UiFrames.InkSoft)
                };
                UiKit.Label("Status", row.transform, k.Mine ? $"★ {Loc.T("kingdom.mine")}" : status, UiKit.TypeScale.Body, k.Mine ? UiFrames.Ink : color,
                    TextAnchor.MiddleRight, FontStyle.Bold).rectTransform.Place(0.56f, 0.5f, 0.96f, 0.95f);
            }

            UiFrames.FitSheet(_window);
        }
    }
}
