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
    /// Last Asylum's City List (docs/32 §2.4): our cities and the others, filtered by level, each with its coordinates
    /// (tap to see it on the map), distance in km, owner and whether it neighbours our land; the owned count and the City
    /// Bonus. Opened from the alliance hub.
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
        }

        private void Update()
        {
            if (_window != null && _rendered != View.TerritoryVersion)
                Render();
        }

        private void Render()
        {
            Close();
            _rendered = View.TerritoryVersion;
            var t = View.Territory;
            var dim = UiKit.Box("Dim", _canvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            _window = dim.gameObject;
            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.03f, 0.08f, 0.97f, 0.92f);
            card.gameObject.AddComponent<Button>();

            var header = UiKit.Box("Header", card.transform, new Color(0.05f, 0.16f, 0.22f, 1f));
            header.rectTransform.Place(0, 0.93f, 1, 1);
            UiKit.Label("Title", header.transform, Loc.T("city.list"), 40, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.05f, 0, 0.8f, 1);
            var close = UiKit.Button("Close", header.transform, "X", Close, UiKit.Field, 38);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.85f, 0.15f, 0.96f, 0.85f);

            // Tabs: ours / open to take; then the level filter.
            var tabs = new[] { Loc.T("city.tab.ours"), Loc.T("city.tab.open") };
            for (var i = 0; i < tabs.Length; i++)
            {
                var index = i;
                var b = UiKit.Button("Tab" + i, card.transform, tabs[i], () => { _tab = index; Render(); }, i == _tab ? UiKit.TabOn : UiKit.TabOff, 28);
                b.GetComponentInChildren<Text>().color = i == _tab ? new Color(0.05f, 0.1f, 0.15f) : UiKit.Text;
                ((RectTransform)b.transform).Place(0.03f + i * 0.475f, 0.865f, 0.495f + i * 0.475f, 0.92f);
            }

            for (var l = 0; l <= 6; l++)
            {
                var level = l;
                var b = UiKit.Button("Lv" + l, card.transform, l == 0 ? Loc.T("city.all") : Loc.F("base.level", l), () => { _level = level; Render(); },
                    l == _level ? UiKit.Primary : UiKit.Field, 20);
                ((RectTransform)b.transform).Place(0.03f + l * 0.135f, 0.81f, 0.155f + l * 0.135f, 0.855f);
            }

            if (t == null)
            {
                UiKit.Label("Wait", card.transform, "…", 40, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0, 0.4f, 1, 0.6f);
                return;
            }

            var mine = t.MyAllianceId ?? string.Empty;
            var ourLand = OurLand(t);
            var cities = (t.Strongholds ?? Array.Empty<StrongholdDto>())
                .Where(c => c.Level < TerritoryRules.ThroneLevel || _tab == 1)
                .Where(c => _tab == 0 ? mine.Length > 0 && c.AllianceId == mine : c.AllianceId != mine)
                .Where(c => _level == 0 || c.Level == _level)
                .OrderBy(c => GatherRules.Km(t.MyX, t.MyY, c.X, c.Y)).Take(60).ToList();

            var list = UiKit.ScrollList("List", card.transform, 10f);
            ((RectTransform)list.parent).Place(0.03f, 0.14f, 0.97f, 0.8f);
            if (cities.Count == 0)
                UiKit.Label("None", list, Loc.T("city.none"), 26, UiKit.Muted, TextAnchor.MiddleCenter).Row(120f);
            foreach (var c in cities)
            {
                var row = UiKit.Box("City", list, UiKit.Field).Row(150f);
                Hud.HudIcons.Image(row.rectTransform, "left_home", 0.02f, 0.15f, 0.16f, 0.85f).raycastTarget = false;
                var name = c.Level >= TerritoryRules.ThroneLevel ? Loc.T("pvp.throne") : Loc.T("city.name");
                UiKit.Label("Name", row.transform, $"{name} {Loc.F("base.level", c.Level)}", 28, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold)
                    .rectTransform.Place(0.19f, 0.66f, 0.7f, 0.96f);
                var x = c.X;
                var y = c.Y;
                var coords = UiKit.Button("Coords", row.transform, Loc.F("world.coords", x, y), () => { Close(); _showOnMap?.Invoke(x, y); }, new Color(0, 0, 0, 0), 22);
                coords.GetComponentInChildren<Text>().color = UiKit.Ok;
                coords.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                ((RectTransform)coords.transform).Place(0.19f, 0.36f, 0.6f, 0.66f);
                UiKit.Label("Km", row.transform, Loc.F("city.distance", GatherRules.Km(t.MyX, t.MyY, x, y).ToString("N0")), 22, UiKit.Muted, TextAnchor.MiddleLeft)
                    .rectTransform.Place(0.19f, 0.06f, 0.6f, 0.36f);
                var owner = string.IsNullOrEmpty(c.AllianceTag) ? Loc.T("pvp.unowned") : $"[{c.AllianceTag}]";
                var neighbour = _tab == 1 && Near(ourLand, x, y, AllianceLandRules.CityReach);
                UiKit.Label("Owner", row.transform, owner + (neighbour ? "\n" + Loc.T("city.neighbour") : string.Empty), 22,
                    c.AllianceId == mine ? UiKit.Ok : string.IsNullOrEmpty(c.AllianceId) ? UiKit.Muted : UiKit.Danger, TextAnchor.MiddleRight, FontStyle.Bold)
                    .rectTransform.Place(0.6f, 0.1f, 0.97f, 0.9f);
            }

            UiKit.Label("Held", card.transform, Loc.F("city.owned", t.Held, t.Allowed), 28, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(0.05f, 0.08f, 0.95f, 0.13f);
            UiKit.Label("Rule", card.transform, Loc.T("city.rule"), 20, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0.05f, 0.005f, 0.6f, 0.075f);
            var bonus = UiKit.Button("Bonus", card.transform, Loc.T("city.bonus"), () => Toaster.Show(Loc.F("city.bonus.text", t.Held * AllianceLandRules.CityGatherPercent), UiKit.Accent),
                UiKit.Gold, 26);
            ((RectTransform)bonus.transform).Place(0.62f, 0.01f, 0.97f, 0.075f);
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
