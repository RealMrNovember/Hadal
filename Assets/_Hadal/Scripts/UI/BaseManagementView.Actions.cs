using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.State;
using Hadal.Data.Models;
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
        private UiWindow _upgradeWindow;
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
                new("g_details", Loc.T("base.details"), () => OpenBuilding(key))
            };

            if (building.IsUnderConstruction)
            {
                var remaining = building.constructionEndsAt - Time.realtimeSinceStartup;
                var free = remaining <= _catalog.Progression.freeFinishSeconds;
                items.Add(new TargetMenu.Item("g_speed", free ? Loc.T("base.finish") : Loc.T("base.speedup"), () =>
                {
                    if (free)
                        Finish(key);
                    else
                        SpeedUpBuilding(key, definition.Id);
                }));
            }
            else if (building.level < definition.MaxLevel)
            {
                items.Add(new TargetMenu.Item("g_up", Loc.T("base.upgrade"), () => OpenUpgrade(key)));
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
            var window = _upgradeWindow;
            _upgradeWindow = null;
            _upgrade = null;
            _upgradeKey = string.Empty;
            if (window != null)
                window.Close();
        }

        /// <summary>Once a second while open, so costs turn green as resources come in and the window closes when work starts.</summary>
        private void TickUpgrade()
        {
            if (_upgradeWindow == null || _upgradeWindow.IsClosing || Time.unscaledTime < _upgradeRefresh)
                return;
            if (Input.touchCount > 0 || Input.GetMouseButton(0))
                return;
            RenderUpgrade();
        }

        /// <summary>
        /// The upgrade window (docs/41, Last Asylum's): next level and what it brings, then one "Spend" list where a
        /// missing building has a Go button to it and a short resource a Go to the bag, and the Upgrade button with
        /// its time. It refills in place each second rather than re-opening.
        /// </summary>
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

            var rules = _catalog.Progression;
            var target = building.level + 1;
            var wallet = Wallet();
            var modifiers = Modifiers();
            var verdict = BuildingProgression.ValidateUpgrade(_catalog, definition.Id, building.level, false,
                ActiveConstructions(), ReactorLevel(), wallet, modifiers.ExtraBuilders, HighestLevel);

            var title = Loc.Building(definition.Id);
            var subtitle = Loc.F("base.level", building.level);
            if (_upgradeWindow == null)
            {
                UiWindow opened = null;
                opened = UiWindow.Open(_canvas.transform, title, subtitle, () =>
                {
                    // Closed by its own cross or the backdrop: forget it here too.
                    if (_upgradeWindow != opened)
                        return;
                    _upgradeWindow = null;
                    _upgrade = null;
                    _upgradeKey = string.Empty;
                });
                _upgradeWindow = opened;
                _upgrade = opened.gameObject;
            }
            else
            {
                _upgradeWindow.Clear();
                _upgradeWindow.SetTitle(title, subtitle);
            }

            var window = _upgradeWindow;
            var green = ColorUtility.ToHtmlStringRGB(UiWindow.InkGood);

            // What the level brings.
            window.Row(Loc.T("base.next"), Loc.F("base.level", target), UiWindow.InkGood);
            window.Section(Loc.T("upgrade.effect"));
            var power = BuildingProgression.Power(definition, building.level);
            var nextPower = BuildingProgression.Power(definition, target);
            window.Row(Loc.T("upgrade.power"), $"{power:N0}  <color=#{green}>+{Math.Max(0, nextPower - power):N0}</color>");
            var now = Rates(definition, building.level);
            var next = Rates(definition, target);
            if (next.Length > 0 && next != now)
                window.Row(Loc.T("upgrade.output"), WithoutLabels(next), UiWindow.InkGood);

            // One list of what it takes: buildings first (Go to the one to raise), then resources (Go to the bag).
            window.Section(Loc.T("upgrade.spend"));
            var reactorNeed = BuildingProgression.ReactorFor(definition, target);
            if (definition.Id != BuildingProgression.ReactorId && reactorNeed > 0)
                NeedBuilding(window, BuildingProgression.ReactorId, reactorNeed);
            if (definition.Id == BuildingProgression.ReactorId)
                foreach (var n in _catalog.ReactorRequirements(target))
                    NeedBuilding(window, n.Key, n.Value);

            foreach (var c in BuildingProgression.Cost(definition, target, rules))
            {
                wallet.TryGetValue(c.Key, out var have);
                var ok = have >= c.Value;
                var bad = ColorUtility.ToHtmlStringRGB(UiWindow.InkBad);
                var amount = ok ? $"{CostChips.Compact(have)} / {CostChips.Compact(c.Value)}"
                    : $"<color=#{bad}>{CostChips.Compact(have)}</color> / {CostChips.Compact(c.Value)}";
                if (ok)
                    window.Need(Hud.HudIcons.Resource(c.Key), amount, true);
                else
                    window.Need(Hud.HudIcons.Resource(c.Key), amount, true, Loc.T("guide.go"), () =>
                    {
                        CloseUpgrade();
                        _social.OpenBag();
                    });
            }

            // Why not (in the note), then the Upgrade button with its time.
            var seconds = modifiers.BuildSeconds(BuildingProgression.Seconds(definition, target, rules));
            if (verdict != ConstructionRejectReason.None)
                window.Note($"<color=#{ColorUtility.ToHtmlStringRGB(UiWindow.InkBad)}>{Loc.Reason(verdict.ToString())}</color>");
            var key = building.instanceKey;
            window.AddButton(Loc.T("base.upgrade"), Loc.Duration(seconds), UiKit.Ok, () =>
            {
                Upgrade(key);
                CloseUpgrade();
            }, verdict == ConstructionRejectReason.None);
            window.Fit();
        }

        /// <summary>Opens a building's round menu once the tap that asked for it has ended (else the release closes it).</summary>
        private System.Collections.IEnumerator ShowActionsAfterTap(BuildingSaveEntry building)
        {
            yield return new WaitForSecondsRealtime(0.25f);
            ShowBuildingActions(building);
        }

        /// <summary>A building the upgrade waits on; while it is short, Go flies to the highest one of that kind.</summary>
        private void NeedBuilding(UiWindow window, string definitionId, int level)
        {
            var met = HighestLevel(definitionId) >= level;
            var text = $"{Loc.Building(definitionId)} {Loc.F("base.level", level)}";
            if (met)
            {
                window.Need("queue_build", text, true);
                return;
            }

            window.Need("queue_build", text, false, Loc.T("guide.go"), () =>
            {
                var best = Buildings().Where(b => b.definitionId == definitionId).OrderByDescending(b => b.level).FirstOrDefault();
                CloseUpgrade();
                // Not built yet: the build list on a free slot that takes it (the genre's Go to an empty plot).
                if (string.IsNullOrEmpty(best.instanceKey))
                    OpenBuildFor(definitionId);
                else
                    StartCoroutine(ShowActionsAfterTap(best));
            });
        }

        /// <summary>"Output: +22 Energy/h" lines without their "Output:" (the row already says it).</summary>
        private static string WithoutLabels(string lines) =>
            string.Join("\n", lines.Split('\n').Select(l => l.IndexOf(": ", StringComparison.Ordinal) is var c and > 0 ? l.Substring(c + 2) : l));

        // Rows for the windows still on the old frame (training); new windows use UiWindow.
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
    }
}
