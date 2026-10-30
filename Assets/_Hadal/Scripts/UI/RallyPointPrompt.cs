using System;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Last Asylum's "Alliance Rally Point" card (docs/30 §6.3.1): the rally point's coordinates (the hub, or the leader's
    /// base while there is none), how far the base is, a map preview with the flag and the base, the Alliance Teleport
    /// item (have/need, or its Hadalite price) and Taşı. Shown by itself right after joining an alliance when the base is
    /// more than <see cref="FarKm"/> away; the alliance page opens it with <see cref="OpenRallyPoint"/>. The jump is the
    /// server's alliance teleport: instant, onto the free tile nearest the rally point.
    /// </summary>
    public sealed class RallyPointPrompt : MonoBehaviour
    {
        public sealed class Actions
        {
            /// <summary>Opens the world map on a tile.</summary>
            public Action<int, int> ShowOnMap;

            /// <summary>Opens the world map on the player's own (new) base.</summary>
            public Action OpenMap;
        }

        /// <summary>Farther than this from the rally point, joining an alliance shows the card by itself.</summary>
        public const double FarKm = 30;

        public static RallyPointPrompt Instance { get; private set; }

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private Actions _actions;
        private ulong _sequence;
        private Canvas _canvas;
        private GameObject _card;
        private Texture2D _texture;

        private string _alliance;
        private bool _started;
        private float _initAt;

        /// <summary>Waiting for a fresh territory view: show it (explicit) or show it only when far (after joining).</summary>
        private int _waitingFrom = -1;
        private bool _onlyWhenFar;

        private ClientStateView View => _stateSync.View;

        public void Initialize(IStateSyncService stateSync, ICommandDispatcher commands, Actions actions)
        {
            _stateSync = stateSync;
            _commands = commands;
            _actions = actions;
            Instance = this;
            _initAt = Time.unscaledTime;
            _canvas = UiKit.Canvas("RallyPoint", 48);
            _canvas.transform.SetParent(transform, false);
        }

        /// <summary>Opens the card (the alliance page's Rally Point tile calls this).</summary>
        public void OpenRallyPoint()
        {
            if (View.MyAlliance == null)
            {
                Toaster.Show(Loc.T("pvp.war.noAlliance"), UiKit.Amber);
                return;
            }

            Ask(onlyWhenFar: false);
        }

        private void Ask(bool onlyWhenFar)
        {
            _onlyWhenFar = onlyWhenFar;
            _waitingFrom = View.TerritoryVersion;
            _commands.Dispatch(new ViewTerritoryCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence });
        }

        private void Update()
        {
            if (_stateSync == null)
                return;

            // Joining an alliance (not the one the player already had when the game started) asks for the rally point.
            var alliance = View.MyAlliance?.Id ?? string.Empty;
            if (!_started)
            {
                // The login's alliance state has arrived (or there was none to send).
                if (View.AllianceVersion > 0 || Time.unscaledTime - _initAt > 8f)
                {
                    _started = true;
                    _alliance = alliance;
                }
            }
            else if (alliance != _alliance)
            {
                _alliance = alliance;
                if (alliance.Length > 0)
                    Ask(onlyWhenFar: true);
            }

            if (_waitingFrom >= 0 && View.TerritoryVersion > _waitingFrom && View.Territory is { } territory)
            {
                _waitingFrom = -1;
                if (territory.RallyX >= 0 && (!_onlyWhenFar || GatherRules.Km(territory.MyX, territory.MyY, territory.RallyX, territory.RallyY) > FarKm))
                    Show(territory);
            }
        }

        private void Close()
        {
            if (_card != null)
                Destroy(_card);
            _card = null;
        }

        private void Show(TerritoryDto t)
        {
            Close();
            var km = GatherRules.Km(t.MyX, t.MyY, t.RallyX, t.RallyY);
            // The genre's dialog over the blurred game: navy frame, title on the left, the white cross.
            var dim = UiFrames.Dim(_canvas.transform, Close);
            _card = dim.gameObject;
            var card = UiKit.Box("Card", dim, UiFrames.Navy);
            card.rectTransform.Place(0.04f, 0.2f, 0.96f, 0.8f);
            card.gameObject.AddComponent<CanvasGroup>();
            card.gameObject.AddComponent<PopIn>().From = 0.94f;
            var parts = UiFrames.Dialog(card.rectTransform, Close);
            parts.Title.text = Loc.T("rally.title");
            var body = parts.Body;

            // "Rally point: X:391 Y:695" — tapping the coordinates opens the map there.
            var rx = t.RallyX;
            var ry = t.RallyY;
            var coords = UiKit.Label("Coords", body, $"<u>{Loc.F("rally.coords", rx, ry)}</u>", UiKit.TypeScale.Body + 2, UiFrames.InkGood, TextAnchor.MiddleLeft, FontStyle.Bold);
            coords.rectTransform.Place(0.05f, 0.89f, 0.95f, 0.98f);
            coords.raycastTarget = true;
            coords.gameObject.AddComponent<Button>().onClick.AddListener(() =>
            {
                Close();
                _actions.ShowOnMap?.Invoke(rx, ry);
            });
            var note = km > FarKm ? Loc.F("rally.far", km.ToString("N0")) : Loc.T("rally.near");
            if (!t.RallyIsHub)
                note += "\n" + Loc.T("rally.leader");
            UiKit.Label("Note", body, note, UiKit.TypeScale.Caption + 2, UiFrames.Ink, TextAnchor.UpperLeft).rectTransform.Place(0.05f, 0.72f, 0.95f, 0.88f);

            BuildPreview(body, t, km);

            // The Alliance Teleport: have/need, or its Hadalite price, on a light item card.
            var held = View.Items.TryGetValue(TeleportRules.AllianceItem, out var n) ? n : 0;
            var price = TeleportRules.HadaliteFor(TeleportRules.Alliance);
            var hadalite = (View.Data.resources ?? Array.Empty<Hadal.Data.Models.ResourceSaveEntry>())
                .Where(e => ResourceTypeMap.FromStoredValue(e.resourceType) == ResourceType.Hadalite).Select(e => e.amount).DefaultIfEmpty(0).Sum();
            var row = UiFrames.CardBox(body, null, "Item");
            row.rectTransform.Place(0.04f, 0.15f, 0.96f, 0.28f);
            UiFrames.ItemTile(row.transform, "item_teleport_alliance", "epic", held > 0 ? held.ToString("N0") : null).Place(0.02f, 0.08f, 0.17f, 0.92f);
            UiKit.Label("Name", row.transform, Loc.T("item." + TeleportRules.AllianceItem), UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.2f, 0.5f, 0.96f, 0.95f);
            UiKit.Label("Count", row.transform, held > 0 ? Loc.F("rally.need", held, 1) : $"💎 {price:N0}", UiKit.TypeScale.Body,
                held > 0 || hadalite >= price ? UiFrames.InkGood : UiFrames.InkBad, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.2f, 0.05f, 0.96f, 0.5f);

            var can = held > 0 || hadalite >= price;
            var move = UiKit.Button("Move", body, Loc.T("rally.move"), () =>
            {
                _commands.Dispatch(new TeleportCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, Mode = TeleportRules.Alliance });
                Close();
                _actions.OpenMap?.Invoke();
            }, can ? UiKit.Primary : UiKit.Panel, 36);
            move.interactable = can;
            ((RectTransform)move.transform).Place(0.3f, 0.02f, 0.7f, 0.13f);
        }

        /// <summary>A crop of the strategic map around the rally point and the base, with the flag, the base and the distance.</summary>
        private void BuildPreview(Transform parent, TerritoryDto t, double km)
        {
            var frame = UiKit.Box("Preview", parent, new Color(0.01f, 0.04f, 0.06f, 1f));
            frame.rectTransform.Place(0.04f, 0.3f, 0.96f, 0.7f);
            frame.gameObject.AddComponent<RectMask2D>();
            _texture = TerritoryPainter.Paint(t, _texture);

            var size = Math.Max(10, t.MapSize);
            var span = Mathf.Clamp(Math.Max(Math.Abs(t.MyX - t.RallyX), Math.Abs(t.MyY - t.RallyY)) + 16, 30, size);
            var cx = Mathf.Clamp((t.MyX + t.RallyX) / 2f, span / 2f, size - span / 2f);
            var cy = Mathf.Clamp((t.MyY + t.RallyY) / 2f, span / 2f, size - span / 2f);
            // The frame is wider than tall: show more tiles across so the map is not stretched.
            var aspect = 0.92f / 0.4f * 0.6f;
            var spanX = Mathf.Min(size, span * aspect);
            cx = Mathf.Clamp(cx, spanX / 2f, size - spanX / 2f);
            var image = new GameObject("Map").AddComponent<RawImage>();
            image.transform.SetParent(frame.transform, false);
            ((RectTransform)image.transform).Place(0, 0, 1, 1);
            image.texture = _texture;
            image.uvRect = new Rect((cx - spanX / 2f) / size, (cy - span / 2f) / size, spanX / size, span / size);
            image.raycastTarget = false;

            Vector2 Anchor(int x, int y) => new((x + 0.5f - (cx - spanX / 2f)) / spanX, (y + 0.5f - (cy - span / 2f)) / span);

            var flagAt = Anchor(t.RallyX, t.RallyY);
            var glow = UiKit.Box("Glow", frame.transform, new Color(0.7f, 0.4f, 1f, 0.35f));
            glow.sprite = UiKit.Circle;
            glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = flagAt;
            glow.rectTransform.sizeDelta = new Vector2(150f, 150f);
            glow.raycastTarget = false;
            var flag = Hud.HudIcons.Image(frame.rectTransform, "side_alliance", flagAt.x - 0.06f, flagAt.y - 0.08f, flagAt.x + 0.06f, flagAt.y + 0.16f);
            flag.color = new Color(0.8f, 0.55f, 1f);
            flag.raycastTarget = false;

            // The base: clamped to the edge when it lies outside the preview (Last Asylum's avatar on the rim).
            var meAt = Anchor(t.MyX, t.MyY);
            meAt = new Vector2(Mathf.Clamp(meAt.x, 0.08f, 0.92f), Mathf.Clamp(meAt.y, 0.12f, 0.88f));
            var me = UiKit.Box("Me", frame.transform, UiKit.Accent);
            me.sprite = UiKit.Circle;
            me.rectTransform.anchorMin = me.rectTransform.anchorMax = meAt;
            me.rectTransform.sizeDelta = new Vector2(70f, 70f);
            me.raycastTarget = false;
            UiKit.Label("Km", frame.transform, $"{km:N0} km", 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(meAt.x - 0.15f, meAt.y - 0.2f, meAt.x + 0.15f, meAt.y - 0.08f);
        }
    }
}
