using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Alliance buildings on the map (docs/32 §2.3): the picker on our land (what each gives, what unlocks it), and the
    /// building's menu — send a team to build or guard it, donate, remove; enemies attack or rally it.
    /// </summary>
    public sealed partial class WorldMapView
    {
        private int _landVersion = -1;
        private readonly HashSet<(int, int)> _ourLand = new();

        /// <summary>Is (x, y) our alliance's land (from the last territory view)?</summary>
        private bool OnOurLand(int x, int y)
        {
            var t = View.Territory;
            if (t == null || string.IsNullOrEmpty(t.MyAllianceId))
                return false;
            if (_landVersion != View.TerritoryVersion)
            {
                _landVersion = View.TerritoryVersion;
                _ourLand.Clear();
                var index = Array.IndexOf(t.LandAlliances ?? Array.Empty<string>(), t.MyAllianceId);
                if (index >= 0)
                    foreach (var packed in t.Land ?? Array.Empty<int>())
                        if (packed % 1_000 == index)
                            _ourLand.Add((packed / 1_000_000, packed / 1_000 % 1_000));
            }

            return _ourLand.Contains((x, y));
        }

        public static string BuildingIcon(string kind) => kind switch
        {
            AllianceLandRules.Relay => "power",
            AllianceLandRules.Rig => "res_titanium",
            AllianceLandRules.Cannon => "event_war",
            _ => "side_alliance"
        };

        /// <summary>Open water on our land: the alliance building picker (officers).</summary>
        private void AddBuildingItem(List<TargetMenu.Item> items, int x, int y)
        {
            if (MyHub() == null || !OnOurLand(x, y))
                return;
            items.Add(new TargetMenu.Item("power", Loc.T("bld.place"), () => OpenBuildingPicker(x, y), View.MyRank() >= AlliancePermissions.Officer));
        }

        private void OpenBuildingPicker(int x, int y)
        {
            TargetMenu.Hide();
            ClosePopup();
            var t = View.Territory;
            var dim = UiKit.Box("Popup", _popupCanvas.transform, new Color(0, 0, 0, 0.6f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(ClosePopup);
            _popup = dim.gameObject;
            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.04f, 0.26f, 0.96f, 0.74f);
            card.gameObject.AddComponent<Button>();
            UiKit.Label("Title", card.transform, Loc.T("bld.place"), 38, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.05f, 0.87f, 0.95f, 0.98f);
            var kinds = AllianceLandRules.Buildings;
            for (var i = 0; i < kinds.Length; i++)
            {
                var kind = kinds[i];
                var y1 = 0.85f - i * 0.28f;
                var row = UiKit.Box("Row" + kind, card.transform, new Color(0, 0, 0, 0.3f));
                row.rectTransform.Place(0.03f, y1 - 0.26f, 0.97f, y1);
                Hud.HudIcons.Image(row.rectTransform, BuildingIcon(kind), 0.02f, 0.2f, 0.14f, 0.8f).raycastTarget = false;
                var (members, power) = AllianceLandRules.Unlock(kind);
                var built = (t?.Structures ?? Array.Empty<StructureDto>()).Any(s => s.AllianceId == t.MyAllianceId && s.Kind == kind);
                var unlocked = t != null && t.Members >= members && t.AlliancePower >= power;
                UiKit.Label("Name", row.transform, Loc.T("bld." + kind), 28, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.17f, 0.55f, 0.7f, 0.95f);
                UiKit.Label("Desc", row.transform, Loc.F("bld." + kind + ".desc", AllianceLandRules.BonusPercent(kind, 1)) +
                    (unlocked ? string.Empty : "\n" + Loc.F("bld.unlock", members, CostChips.Compact(power))), 20, unlocked ? UiKit.Muted : UiKit.Amber, TextAnchor.UpperLeft)
                    .rectTransform.Place(0.17f, 0.04f, 0.7f, 0.55f);
                var can = unlocked && !built;
                var go = UiKit.Button("Go", row.transform, built ? Loc.T("bld.built") : Loc.T("bld.build"), () =>
                {
                    ClosePopup();
                    LandCommand(AllianceLandRules.PlaceBuilding, x: x, y: y, resource: kind);
                }, can ? UiKit.Ok : UiKit.Panel, 26);
                go.interactable = can;
                ((RectTransform)go.transform).Place(0.72f, 0.2f, 0.97f, 0.8f);
            }
        }

        /// <summary>The building lines and buttons of a structure's menu (buildings and beacons).</summary>
        private void AddBuildingMenu(StructureDto s, bool ours, List<string> lines, List<TargetMenu.Item> items, string title)
        {
            var building = AllianceLandRules.IsBuilding(s.Kind);
            if (building)
            {
                lines.Add(Loc.F("bld." + s.Kind + ".desc", s.Bonus));
                if (s.UnderConstruction)
                    lines.Add(Loc.F("bld.work", s.WorkNeeded > 0 ? (int)(100L * s.Work / s.WorkNeeded) : 0));
            }

            if (!s.UnderConstruction)
                lines.Add(s.Durability <= 0 ? $"<color=#FF8A80>{Loc.T("bld.damaged")}</color>" : Loc.F("bld.durability", s.Durability.ToString("N0"), s.MaxDurability.ToString("N0")));
            if (s.Guards > 0)
                lines.Add(Loc.F("bld.guards", s.Guards));

            var target = s;
            if (ours && s.Kind != AllianceLandRules.Hub)
            {
                items.Insert(0, new TargetMenu.Item("nav_heroes", s.UnderConstruction ? Loc.T("bld.build") : Loc.T("bld.guard"), () => OpenPvpSetup(new PvpPlan
                {
                    Title = title, X = target.X, Y = target.Y, Info = s.UnderConstruction ? Loc.T("bld.build.info") : Loc.T("pvp.reinforce.info"),
                    GoLabel = s.UnderConstruction ? Loc.T("bld.build") : Loc.T("bld.guard"), GoColor = AllyBlue,
                    Send = heroes => SendPvp(PvpRules.Reinforce, PvpRules.TargetStructure, target.Id, heroes)
                })));
                if (building && !s.UnderConstruction)
                    items.Insert(1, new TargetMenu.Item("res_titanium", Loc.T("land.donate"), () => OpenDonate(target), s.PointsNext > 0));
            }
            else if (!ours && s.Kind != AllianceLandRules.Hub)
            {
                var open = !IBeginner;
                items.Insert(0, new TargetMenu.Item("event_war", Loc.T("world.attack"), () => OpenPvpSetup(new PvpPlan
                {
                    Title = title, X = target.X, Y = target.Y, Info = Loc.F("bld.durability", target.Durability.ToString("N0"), target.MaxDurability.ToString("N0")),
                    GoLabel = Loc.T("world.attack"), GoColor = AttackRed, Send = heroes => SendPvp(PvpRules.Attack, PvpRules.TargetStructure, target.Id, heroes)
                }), open));
                items.Insert(1, new TargetMenu.Item("side_alliance", Loc.T("pvp.rally"), () => OpenMuster(title, target.X, target.Y, PvpRules.TargetStructure, target.Id),
                    open && View.MyAlliance != null));
            }
        }
    }
}
