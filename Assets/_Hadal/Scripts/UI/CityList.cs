using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Last Asylum's City List (docs/32 §2.4), in the genre's dialog over the blurred game: our cities and the others,
    /// filtered by level, each on a light row with its coordinates (tap to see it on the map), distance in km, owner and
    /// whether it neighbours our land; the owned count and the City Bonus at the bottom. Opened from the alliance hub.
    /// </summary>
    public sealed class CityList : MonoBehaviour
    {
        public static CityList Instance { get; private set; }

        private IStateSyncService _stateSync;
        private ICommandDispatcher _commands;
        private Action<int, int> _showOnMap;
        private ulong _sequence;
        private Canvas _canvas;
        private GameObject _window;
        private RectTransform _body;
        private int _tab;
        private int _level;
        private int _rendered = -1;

        private ClientStateView View => _stateSync.View;

        public void Initialize(IStateSyncService stateSync, ICommandDispatcher commands, Action<int, int> showOnMap)
        {
            _stateSync = stateSync;
            _commands = commands;
            _showOnMap = showOnMap;
            Instance = this;
            _canvas = UiKit.Canvas("CityList", 47);
            _canvas.transform.SetParent(transform, false);
        }

        public void Open()
        {
            _commands.Dispatch(new ViewTerritoryCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence });
            Render();
        }

        private void Close()
        {
            if (_window != null)
                Destroy(_window);
            _window = null;
            _body = null;
        }

        private void Update()
        {
            if (_window != null && _rendered != View.TerritoryVersion)
                Render();
        }

        /// <summary>The backdrop and dialog are made once; tabs, filters and new data redraw only the body.</summary>
        private void Render()
        {
            _rendered = View.TerritoryVersion;
            if (_window == null)
            {
                var dim = UiFrames.Dim(_canvas.transform, Close);
                _window = dim.gameObject;
                var card = UiKit.Box("Card", dim, UiFrames.Navy);
                card.rectTransform.Place(0.03f, 0.08f, 0.97f, 0.92f);
                card.gameObject.AddComponent<CanvasGroup>();
                card.gameObject.AddComponent<PopIn>().From = 0.94f;
                var parts = UiFrames.Dialog(card.rectTransform, Close);
                parts.Title.text = Loc.T("city.list");
                _body = parts.Body;
            }

            for (var i = _body.childCount - 1; i >= 0; i--)
                if (_body.GetChild(i).name != "Subtitle")
                    Destroy(_body.GetChild(i).gameObject);
            var t = View.Territory;

            // Tabs: ours / open to take; then the level filter.
            UiFrames.Tabs(_body, new[] { Loc.T("city.tab.ours"), Loc.T("city.tab.open") }, _tab, i => { _tab = i; Render(); }).Place(0.03f, 0.915f, 0.97f, 0.985f);
            for (var l = 0; l <= 6; l++)
            {
                var level = l;
                var b = UiKit.Button("Lv" + l, _body, l == 0 ? Loc.T("city.all") : Loc.F("base.level", l), () => { _level = level; Render(); },
                    l == _level ? UiKit.Primary : UiKit.TabOn, 20);
                ((RectTransform)b.transform).Place(0.03f + l * 0.135f, 0.85f, 0.155f + l * 0.135f, 0.905f);
            }

            if (t == null)
            {
                UiKit.Label("Wait", _body, "…", UiKit.TypeScale.Title, UiFrames.InkSoft, TextAnchor.MiddleCenter).rectTransform.Place(0, 0.4f, 1, 0.6f);
                return;
            }

            var mine = t.MyAllianceId ?? string.Empty;
            var ourLand = OurLand(t);
            var cities = (t.Strongholds ?? Array.Empty<StrongholdDto>())
                .Where(c => c.Level < TerritoryRules.ThroneLevel || _tab == 1)
                .Where(c => _tab == 0 ? mine.Length > 0 && c.AllianceId == mine : c.AllianceId != mine)
                .Where(c => _level == 0 || c.Level == _level)
                .OrderBy(c => GatherRules.Km(t.MyX, t.MyY, c.X, c.Y)).Take(60).ToList();

            var list = UiKit.ScrollList("List", _body, 10f);
            ((RectTransform)list.parent).Place(0.02f, 0.15f, 0.98f, 0.84f);
            if (cities.Count == 0)
                UiFrames.Note(list, Loc.T("city.none")).Row(120f);
            for (var i = 0; i < cities.Count; i++)
            {
                var c = cities[i];
                var row = UiFrames.CardBox(list, i % 2 == 0 ? UiFrames.Card : UiFrames.CardAlt, "City").Row(150f);
                Hud.HudIcons.Image(row.rectTransform, "left_home", 0.02f, 0.12f, 0.16f, 0.88f).raycastTarget = false;
                var name = c.Level >= TerritoryRules.ThroneLevel ? Loc.T("pvp.throne") : Loc.T("city.name");
                UiKit.Label("Name", row.transform, $"{name} {Loc.F("base.level", c.Level)}", UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold)
                    .rectTransform.Place(0.19f, 0.64f, 0.62f, 0.96f);
                var x = c.X;
                var y = c.Y;
                var coords = UiKit.Label("Coords", row.transform, $"<u>{Loc.F("world.coords", x, y)}</u>", UiKit.TypeScale.Caption + 2, UiFrames.InkGood,
                    TextAnchor.MiddleLeft, FontStyle.Bold);
                coords.rectTransform.Place(0.19f, 0.34f, 0.6f, 0.64f);
                coords.raycastTarget = true;
                coords.gameObject.AddComponent<Button>().onClick.AddListener(() => { Close(); _showOnMap?.Invoke(x, y); });
                UiKit.Label("Km", row.transform, Loc.F("city.distance", GatherRules.Km(t.MyX, t.MyY, x, y).ToString("N0")), UiKit.TypeScale.Caption, UiFrames.InkSoft,
                    TextAnchor.MiddleLeft).rectTransform.Place(0.19f, 0.05f, 0.6f, 0.34f);
                var owner = string.IsNullOrEmpty(c.AllianceTag) ? Loc.T("pvp.unowned") : $"[{c.AllianceTag}]";
                var neighbour = _tab == 1 && Near(ourLand, x, y, AllianceLandRules.CityReach);
                var who = UiKit.Label("Owner", row.transform, owner + (neighbour ? "\n" + Loc.T("city.neighbour") : string.Empty), UiKit.TypeScale.Caption,
                    c.AllianceId == mine ? UiFrames.InkGood : string.IsNullOrEmpty(c.AllianceId) ? UiFrames.InkSoft : UiFrames.InkBad, TextAnchor.MiddleRight, FontStyle.Bold);
                who.rectTransform.Place(0.6f, 0.08f, 0.97f, 0.92f);
                who.richText = false;
            }

            UiKit.Label("Held", _body, Loc.F("city.owned", t.Held, t.Allowed), UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(0.05f, 0.085f, 0.95f, 0.145f);
            UiKit.Label("Rule", _body, Loc.T("city.rule"), UiKit.TypeScale.Micro, UiFrames.InkSoft, TextAnchor.MiddleLeft).rectTransform.Place(0.04f, 0.01f, 0.6f, 0.08f);
            var held = t.Held;
            var bonus = UiKit.Button("Bonus", _body, Loc.T("city.bonus"), () => Toaster.Show(Loc.F("city.bonus.text", held * AllianceLandRules.CityGatherPercent), UiKit.Accent),
                UiKit.Gold, 26);
            ((RectTransform)bonus.transform).Place(0.62f, 0.012f, 0.97f, 0.078f);
        }

        private static HashSet<(int, int)> OurLand(TerritoryDto t)
        {
            var result = new HashSet<(int, int)>();
            var index = Array.IndexOf(t.LandAlliances ?? Array.Empty<string>(), t.MyAllianceId ?? string.Empty);
            if (index < 0)
                return result;
            foreach (var packed in t.Land ?? Array.Empty<int>())
                if (packed % 1_000 == index)
                    result.Add((packed / 1_000_000, packed / 1_000 % 1_000));
            return result;
        }

        private static bool Near(HashSet<(int, int)> land, int x, int y, int reach)
        {
            for (var dx = -reach; dx <= reach; dx++)
                for (var dy = -reach; dy <= reach; dy++)
                    if (land.Contains((x + dx, y + dy)))
                        return true;
            return false;
        }
    }
}
