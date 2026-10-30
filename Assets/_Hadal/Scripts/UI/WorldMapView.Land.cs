using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Alliance land on the map (docs/32): the hub and beacons as flags in the alliance's colour, their menu (level and
    /// donations, building countdown, cut off from the hub, donate, remove), and the open-water menu's "plant the hub",
    /// "raise a beacon" and "move the hub here". The server decides what is allowed.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private readonly Dictionary<string, RectTransform> _structurePlates = new();

        private void LandCommand(string action, string id = "", int x = 0, int y = 0, string resource = "", long amount = 0)
        {
            TargetMenu.Hide();
            _commands.Dispatch(new TerritoryCommand
            {
                CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, Action = action, StrongholdId = id ?? string.Empty, X = x, Y = y,
                Resource = resource ?? string.Empty, Amount = amount
            });
        }

        private StructureDto MyHub()
        {
            var t = View.Territory;
            return t == null || string.IsNullOrEmpty(t.MyAllianceId)
                ? null
                : (t.Structures ?? Array.Empty<StructureDto>()).FirstOrDefault(s => s.AllianceId == t.MyAllianceId && s.Kind == AllianceLandRules.Hub);
        }

        /// <summary>Open water: plant the hub, raise a beacon or move the hub here (officers, leader).</summary>
        private void AddLandItems(List<TargetMenu.Item> items, int x, int y)
        {
            if (View.MyAlliance == null || View.Territory == null)
                return;
            var rank = View.MyRank();
            var hub = MyHub();
            if (hub == null)
            {
                items.Add(new TargetMenu.Item("side_alliance", Loc.T("land.plant"), () => LandCommand(AllianceLandRules.PlaceHub, x: x, y: y),
                    rank >= AlliancePermissions.Officer));
                return;
            }

            var t = View.Territory;
            items.Add(new TargetMenu.Item("side_alliance", Loc.F("land.beacon.place", AllianceLandRules.BeaconCost(t.Beacons).ToString("N0")),
                () => LandCommand(AllianceLandRules.PlaceBeacon, x: x, y: y), rank >= AlliancePermissions.Officer && t.Beacons < t.BeaconsAllowed));
            items.Add(new TargetMenu.Item("nav_world", Loc.T("land.moveHub"), () => LandCommand(AllianceLandRules.MoveHub, x: x, y: y), rank >= AlliancePermissions.Leader));
            AddBuildingItem(items, x, y);
        }

        /// <summary>Hub and beacon flags: the hub always, beacons when zoomed in or our own.</summary>
        private void UpdateStructures(TerritoryDto territory, bool visible)
        {
            var live = new HashSet<string>();
            var myAlliance = territory.MyAllianceId ?? string.Empty;
            var now = View.ServerNowMs;
            foreach (var s in territory.Structures ?? Array.Empty<StructureDto>())
            {
                live.Add(s.Id);
                if (!_structurePlates.TryGetValue(s.Id, out var plate))
                {
                    plate = BuildStructurePlate(s.Id);
                    _structurePlates[s.Id] = plate;
                }

                var hub = s.Kind == AllianceLandRules.Hub;
                var ours = myAlliance.Length > 0 && s.AllianceId == myAlliance;
                var top = Project(TileToWorld(s.X, s.Y) + Vector3.up * 2f, out var onScreen);
                plate.gameObject.SetActive(visible && onScreen && (hub || ours || !Strategic));
                plate.anchoredPosition = top;
                var color = s.Color >= 0 ? AllianceColor(s.Color, 1f) : Color.white;
                var flag = plate.Find("Flag").GetComponent<Image>();
                flag.sprite = Hud.HudIcons.Get(BuildingIcon(s.Kind));
                flag.color = s.Cut || s.UnderConstruction || (AllianceLandRules.IsBuilding(s.Kind) && s.Durability <= 0) ? new Color(0.5f, 0.5f, 0.5f, 0.9f) : color;
                var label = plate.Find("Name").GetComponent<TMP_Text>();
                var tag = string.IsNullOrEmpty(s.AllianceTag) ? string.Empty : $"[{s.AllianceTag}] ";
                label.text = hub ? $"{tag}{Loc.T("land.hub")} {Loc.F("base.level", s.Level)}"
                    : AllianceLandRules.IsBuilding(s.Kind) ? $"{tag}{Loc.T("bld." + s.Kind)} {Loc.F("base.level", s.Level)}"
                    : s.ReadyAtMs > now ? $"{tag}{Loc.Duration(Math.Max(0, (s.ReadyAtMs - now) / 1000f))}" : tag.Trim();
                label.gameObject.SetActive(hub || !Strategic);
                plate.localScale = Vector3.one * (hub ? 1.25f : 0.85f);
                var info = s;
                var button = plate.GetComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => ShowStructure(info));
            }

            foreach (var id in _structurePlates.Keys.Where(k => !live.Contains(k)).ToList())
            {
                Destroy(_structurePlates[id].gameObject);
                _structurePlates.Remove(id);
            }
        }

        private RectTransform BuildStructurePlate(string id)
        {
            var root = UiKit.Rect("Structure " + id, _labels);
            root.sizeDelta = new Vector2(300f, 110f);
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0.001f);
            root.gameObject.AddComponent<Button>();
            var flag = Hud.HudIcons.Image(root, "side_alliance", 0.36f, 0.38f, 0.64f, 1f);
            flag.name = "Flag";
            flag.raycastTarget = false;
            var label = UiKit.Label("Name", root, string.Empty, 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.rectTransform.Place(0, 0, 1, 0.38f);
            UiText.Outline(label, new Color(0, 0, 0, 0.8f));
            root.SetAsFirstSibling();
            return root;
        }

        /// <summary>A hub or beacon: owner, level, donations, building countdown or cut off; donate, remove.</summary>
        private void ShowStructure(StructureDto s)
        {
            var t = View.Territory;
            var ours = t != null && !string.IsNullOrEmpty(t.MyAllianceId) && s.AllianceId == t.MyAllianceId;
            var hub = s.Kind == AllianceLandRules.Hub;
            var tag = string.IsNullOrEmpty(s.AllianceTag) ? string.Empty : $"[{s.AllianceTag}] ";
            var title = tag + (hub ? Loc.T("land.hub") : AllianceLandRules.IsBuilding(s.Kind) ? Loc.T("bld." + s.Kind) : Loc.T("land.beacon"));
            var lines = new List<string> { $"{Loc.F("base.level", s.Level)}   {Loc.F("land.radius", s.Radius)}   {Loc.F("world.coords", s.X, s.Y)}" };
            if (hub && s.PointsNext > 0)
                lines.Add(Loc.F("land.points", s.Points.ToString("N0"), s.PointsNext.ToString("N0")));
            if (s.ReadyAtMs > View.ServerNowMs)
                lines.Add(Loc.F("land.building", Loc.Duration((s.ReadyAtMs - View.ServerNowMs) / 1000f)));
            if (s.Cut)
                lines.Add($"<color=#FFB347>{Loc.T("land.cut")}</color>");
            if (ours && hub && t != null)
                lines.Add(Loc.F("land.beacons", t.Beacons, t.BeaconsAllowed));
            if (AllianceLandRules.IsBuilding(s.Kind) && s.PointsNext > 0)
                lines.Add(Loc.F("land.points", s.Points.ToString("N0"), s.PointsNext.ToString("N0")));

            var items = new List<TargetMenu.Item>();
            var target = s;
            if (ours && hub)
                items.Add(new TargetMenu.Item("res_titanium", Loc.T("land.donate"), () => OpenDonate(target), s.PointsNext > 0));
            if (ours && !hub)
                items.Add(new TargetMenu.Item("left_home", Loc.T(AllianceLandRules.IsBuilding(target.Kind) ? "bld.remove" : "land.remove"), () => LandCommand(AllianceLandRules.Remove, target.Id),
                    View.MyRank() >= AlliancePermissions.Officer));
            items.Add(new TargetMenu.Item("chat", Loc.T("share.title"), () => ShareLocation?.Invoke(target.X, target.Y, ours ? "base" : "enemy", title)));
            AddBuildingMenu(s, ours, lines, items, title);
            GlideTo(s.X, s.Y);
            var x = s.X;
            var y = s.Y;
            TargetMenu.Show(() => IsOpen ? _camera : null, () => TileToWorld(x, y) + Vector3.up * 2f, title, string.Join("\n", lines), items);
        }

        /// <summary>Donating to the hub or a building as a dialog: the points, then a row per resource with three amounts.</summary>
        private void OpenDonate(StructureDto hub)
        {
            TargetMenu.Hide();
            var sheet = MapDialog("donate", Loc.T("land.donate"));
            var content = sheet.Content;
            var next = Math.Max(1, hub.PointsNext);
            ShareBar(UiKit.Rect("Points", content).Row(56), hub.Points / (float)next, Loc.F("land.points", hub.Points.ToString("N0"), hub.PointsNext.ToString("N0")),
                UiKit.Gold);
            var resources = new[] { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Food };
            var amounts = new long[] { 10_000, 50_000, 200_000 };
            foreach (var type in resources)
            {
                var row = UiFrames.SheetBlock(content, UiFrames.Card, "Res" + type);
                row.Row(120);
                Hud.HudIcons.Image(row.transform, Hud.HudIcons.Resource(type), 0.02f, 0.12f, 0.12f, 0.88f);
                var name = UiKit.Label("Name", row.transform, Loc.T("resource." + type), UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                name.rectTransform.Place(0.13f, 0f, 0.34f, 1f);
                name.Fit(14, UiKit.TypeScale.Body);
                for (var a = 0; a < amounts.Length; a++)
                {
                    var resource = type.ToString();
                    var amount = amounts[a];
                    var x0 = 0.35f + a * 0.215f;
                    var button = UiKit.Button($"Give{a}", row.transform, CostChips.Compact(amount), () =>
                    {
                        ClosePopup();
                        LandCommand(AllianceLandRules.Donate, hub.Id, resource: resource, amount: amount);
                    }, UiKit.Primary, UiKit.TypeScale.Body);
                    ((RectTransform)button.transform).Place(x0, 0.14f, x0 + 0.2f, 0.86f);
                }
            }

            UiFrames.FitSheet(sheet);
        }
    }
}
