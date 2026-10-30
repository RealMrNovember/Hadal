using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.State;
using Hadal.Data.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Tapping a building (Last Asylum pattern): the camera glides to it, round actions open around it, and
    /// Upgrade shows a centred window with what the level brings, what it needs and what it costs.
    /// </summary>
    public sealed partial class BaseManagementView
    {
        private GameObject _upgrade;
        private string _upgradeKey = string.Empty;
        private float _upgradeRefresh;

        /// <summary>A modal or the round menu is up (the HUD steps aside).</summary>
        public bool ModalOpen => (_upgrade != null && _upgrade.activeSelf) || (_training != null && _training.activeSelf) || TargetMenu.IsOpen;

        private Vector3 SlotWorld(int ring, int sector) => _grid.GetWorldPosition(new PolarGridSlotId(ring, sector));

        private void ShowBuildingActions(BuildingSaveEntry building)
        {
            if (!_catalog.TryGetBuilding(building.definitionId, out var definition))
                return;

            var key = building.instanceKey;
            var ring = building.cellRing;
            var sector = building.cellSector;
            var world = SlotWorld(ring, sector);
            CameraFocus.Request(world, 34f);

            var items = new List<TargetMenu.Item>
            {
                new("left_search", Loc.T("base.details"), () => OpenBuilding(key))
            };

            if (building.IsUnderConstruction)
            {
                var remaining = building.constructionEndsAt - Time.realtimeSinceStartup;
                var free = remaining <= _catalog.Progression.freeFinishSeconds;
                items.Add(new TargetMenu.Item("queue_build", free ? Loc.T("base.finish") : Loc.T("base.speedup"), () =>
                {
                    if (free)
                        Finish(key);
                    else
                        SpeedUpBuilding(key, definition.Id);
                }));
            }
            else if (building.level < definition.MaxLevel)
            {
                items.Add(new TargetMenu.Item("queue_build", Loc.T("base.upgrade"), () => OpenUpgrade(key)));
            }

            AddMilitaryActions(definition, building, items);

            if (definition.Id == ResearchRules.CenterId && building.level > 0)
                items.Add(new TargetMenu.Item("left_research", Loc.T("hud.research"), () =>
                {
                    OpenBuilding(key);
                    OpenResearch();
                }));

            var training = TrainingAt(key);
            var status = building.level == 0 ? Loc.F("base.constructing", Loc.Duration(building.constructionEndsAt - Time.realtimeSinceStartup))
                : training != null ? Loc.F("train.running.short", training.Count, $"T{training.Tier}",
                    Loc.Duration(Math.Max(0, (training.EndsAtMs - _stateSync.View.ServerNowMs) / 1000f)))
                : building.IsUnderConstruction ? Loc.F("base.upgrading", building.level + 1, Loc.Duration(building.constructionEndsAt - Time.realtimeSinceStartup))
                : Rates(definition, building.level);
            TargetMenu.Show(() => Camera.main, () => SlotWorld(ring, sector) + Vector3.up * 3f,
                $"{Loc.F("base.level", building.level)}  {Loc.Building(definition.Id)}", status, items);
        }

        /// <summary>The speed-up window on a construction (its timer read live from the building).</summary>
        private void SpeedUpBuilding(string key, string definitionId)
        {
            TargetMenu.Hide();
            SpeedupWindow.Open("build:" + key, SpeedupKind.Build, Loc.Building(definitionId), () =>
            {
                var b = Buildings().FirstOrDefault(x => x.instanceKey == key);
                if (string.IsNullOrEmpty(b.instanceKey) || !b.IsUnderConstruction)
                    return null;
                var end = _stateSync.View.ServerNowMs + (long)((b.constructionEndsAt - Time.realtimeSinceStartup) * 1000f);
                return (end - (long)(b.constructionSeconds * 1000f), end);
            });
        }

        // --- Upgrade window ---------------------------------------------------------------------

        private void OpenUpgrade(string instanceKey)
        {
            _upgradeKey = instanceKey;
            Close();
            RenderUpgrade();
        }

        private void CloseUpgrade()
        {
            if (_upgrade != null)
                Destroy(_upgrade);
            _upgrade = null;
            _upgradeKey = string.Empty;
        }

        /// <summary>Once a second while open, so costs turn green as resources come in and the window closes when work starts.</summary>
        private void TickUpgrade()
        {
            if (_upgrade == null || Time.unscaledTime < _upgradeRefresh)
                return;
            if (Input.touchCount > 0 || Input.GetMouseButton(0))
                return;
            RenderUpgrade();
        }

        private void RenderUpgrade()
        {
            _upgradeRefresh = Time.unscaledTime + 1f;
            var building = Buildings().FirstOrDefault(b => b.instanceKey == _upgradeKey);
            if (string.IsNullOrEmpty(building.instanceKey) || building.IsUnderConstruction
                || !_catalog.TryGetBuilding(building.definitionId, out var definition) || building.level >= definition.MaxLevel)
            {
                CloseUpgrade();
                return;
            }

            var fresh = _upgrade == null;
            if (_upgrade != null)
                Destroy(_upgrade);

            var rules = _catalog.Progression;
            var target = building.level + 1;
            var wallet = Wallet();
            var modifiers = Modifiers();
            var verdict = BuildingProgression.ValidateUpgrade(_catalog, definition.Id, building.level, false,
                ActiveConstructions(), ReactorLevel(), wallet, modifiers.ExtraBuilders, HighestLevel);

            // Dim behind a centred window; tapping the dim closes it.
            var dim = UiKit.Box("Upgrade", _canvas.transform, new Color(0f, 0.01f, 0.02f, 0.72f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(CloseUpgrade);
            _upgrade = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.04f, 0.17f, 0.96f, 0.83f);
            card.gameObject.AddComponent<Button>();
            if (fresh)
            {
                card.gameObject.AddComponent<CanvasGroup>();
                card.gameObject.AddComponent<PopIn>();
            }

            // Header strip: name, level, close.
            var header = UiKit.Box("Header", card.transform, new Color(0.05f, 0.16f, 0.22f, 1f));
            header.rectTransform.Place(0, 0.88f, 1, 1);
            UiKit.Label("Title", header.transform, Loc.Building(definition.Id), 44, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.05f, 0.4f, 0.8f, 1f);
            UiKit.Label("Level", header.transform, Loc.F("base.level", building.level), 30, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.05f, 0.02f, 0.8f, 0.45f);
            var close = UiKit.Button("Close", header.transform, "X", CloseUpgrade, UiKit.Field, 40);
            close.GetComponentInChildren<TMP_Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.84f, 0.18f, 0.96f, 0.82f);

            var list = UiKit.ScrollList("List", card.transform, 10f);
            ((RectTransform)list.parent).Place(0.04f, 0.2f, 0.96f, 0.86f);

            // What the level brings.
            Section(list, Loc.T("upgrade.effect"));
            Row(list, Loc.T("base.next"), Loc.F("base.level", target), UiKit.Ok);
            var power = BuildingProgression.Power(definition, building.level);
            var nextPower = BuildingProgression.Power(definition, target);
            Row(list, Loc.T("upgrade.power"), $"{power:N0}  <color=#5BE37A>+{Math.Max(0, nextPower - power):N0}</color>", UiKit.Text);
            var now = Rates(definition, building.level);
            var next = Rates(definition, target);
            if (next.Length > 0 && next != now)
                Row(list, Loc.T("upgrade.output"), next, UiKit.Ok, 90);

            // What it needs besides resources.
            var needs = new List<(string Text, bool Ok)>();
            var reactorNeed = BuildingProgression.ReactorFor(definition, target);
            if (definition.Id != BuildingProgression.ReactorId && reactorNeed > 0)
                needs.Add(($"{Loc.Building(BuildingProgression.ReactorId)} {Loc.F("base.level", reactorNeed)}", ReactorLevel() >= reactorNeed));
            if (definition.Id == BuildingProgression.ReactorId)
                needs.AddRange(_catalog.ReactorRequirements(target).Select(n => ($"{Loc.Building(n.Key)} {Loc.F("base.level", n.Value)}", HighestLevel(n.Key) >= n.Value)));
            if (needs.Count > 0)
            {
                Section(list, Loc.T("base.needs.title"));
                foreach (var (text, ok) in needs)
                    Check(list, text, ok);
            }

            // What it costs: have / need, red when short.
            Section(list, Loc.T("upgrade.spend"));
            foreach (var c in BuildingProgression.Cost(definition, target, rules))
            {
                wallet.TryGetValue(c.Key, out var have);
                var ok = have >= c.Value;
                var row = UiKit.Box("Cost", list, new Color(0.04f, 0.11f, 0.15f, 1f)).Row(84);
                Hud.HudIcons.Image(row.transform, Hud.HudIcons.Resource(c.Key), 0.02f, 0.1f, 0.12f, 0.9f);
                UiKit.Label("Amount", row.transform, $"<color={(ok ? "#E6F2F7" : "#FF5A5A")}>{CostChips.Compact(have)}</color> / {CostChips.Compact(c.Value)}",
                    32, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.15f, 0, 0.8f, 1);
                UiKit.Label("Mark", row.transform, ok ? "✓" : "✗", 40, ok ? UiKit.Ok : UiKit.Danger, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .rectTransform.Place(0.84f, 0, 0.98f, 1);
            }

            // Why not, then one big green button with the time on it.
            var seconds = modifiers.BuildSeconds(BuildingProgression.Seconds(definition, target, rules));
            if (verdict != ConstructionRejectReason.None)
                UiKit.Label("Reason", card.transform, Loc.Reason(verdict.ToString()), 28, UiKit.Danger, TextAnchor.MiddleCenter)
                    .rectTransform.Place(0.05f, 0.14f, 0.95f, 0.2f);
            var key = building.instanceKey;
            var go = UiKit.Button("Upgrade", card.transform, $"{Loc.T("base.upgrade")}\n<size=26>{Loc.Duration(seconds)}</size>", () =>
            {
                Upgrade(key);
                CloseUpgrade();
            }, verdict == ConstructionRejectReason.None ? UiKit.Ok : UiKit.Panel, 40);
            go.interactable = verdict == ConstructionRejectReason.None;
            if (!go.interactable)
                go.GetComponentInChildren<TMP_Text>().color = UiKit.Muted;
            ((RectTransform)go.transform).Place(0.2f, 0.025f, 0.8f, 0.13f);
        }

        private static void Section(Transform list, string title)
        {
            var bar = UiKit.Box("Section", list, new Color(0.07f, 0.2f, 0.27f, 1f)).Row(64);
            UiKit.Label("Text", bar.transform, title, 30, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);
        }

        private static void Row(Transform list, string label, string value, Color valueColor, float height = 70)
        {
            var row = UiKit.Rect("Row", list).Row(height);
            UiKit.Label("Label", row, label, 28, UiKit.Muted, TextAnchor.MiddleLeft).rectTransform.Place(0.02f, 0, 0.45f, 1);
            UiKit.Label("Value", row, value, 28, valueColor, TextAnchor.MiddleRight, FontStyle.Bold).rectTransform.Place(0.4f, 0, 0.98f, 1);
        }

        private static void Check(Transform list, string text, bool ok)
        {
            var row = UiKit.Box("Need", list, new Color(0.04f, 0.11f, 0.15f, 1f)).Row(76);
            UiKit.Label("Text", row.transform, text, 30, ok ? UiKit.Text : UiKit.Danger, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.04f, 0, 0.8f, 1);
            UiKit.Label("Mark", row.transform, ok ? "✓" : "✗", 40, ok ? UiKit.Ok : UiKit.Danger, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(0.84f, 0, 0.98f, 1);
        }
    }
}
