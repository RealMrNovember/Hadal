using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Data.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Training yards and the army (docs/rehber 19 §2, Last Asylum's training ground), in the genre's centred dialog:
    /// the tiers as a strip of rarity tiles, the tier's numbers, a count slider, the price as chips and Train with its
    /// time; the Military Dome shows the army, the Med Bay the wounded and Heal.
    /// </summary>
    public sealed partial class BaseManagementView
    {
        private GameObject _training;
        private UiFrames.Sheet _trainingSheet;
        private ScrollRect _tierStrip;
        private float _tierScroll = -1f;
        private string _trainingKey = string.Empty;
        private int _trainingTier;
        private int _trainingCount;
        private float _trainingRefresh;
        private bool _armyOpen;
        private bool _healOpen;

        private TroopCatalogData Troops => _catalog.Troops;

        private ArmyDto Army => _stateSync?.View.Army ?? new ArmyDto();

        private TrainingDto TrainingAt(string instanceKey) => Army.Training.FirstOrDefault(t => t.BuildingId == instanceKey);

        /// <summary>Round-menu entries for military buildings: Train on a yard, Army on the Military Dome, Heal on the Med Bay.</summary>
        private void AddMilitaryActions(BuildingDefinition definition, BuildingSaveEntry building, List<TargetMenu.Item> items)
        {
            if (building.level <= 0)
                return;
            var key = building.instanceKey;
            if (TroopRules.IsTrainingBuilding(Troops, definition.Id))
                items.Add(new TargetMenu.Item("g_train", Loc.T("train.action"), () => OpenTraining(key)));
            if (definition.Id == Troops.capacityBuilding)
                items.Add(new TargetMenu.Item("g_army", Loc.T("army.action"), () => OpenTraining(key, army: true)));
            if (definition.Id == Troops.infirmary.building)
                items.Add(new TargetMenu.Item("g_heal", Loc.T("heal.action"), () => OpenTraining(key, heal: true)));
        }

        private void OpenTraining(string instanceKey, bool army = false, bool heal = false)
        {
            CloseTraining();
            _trainingKey = instanceKey;
            _armyOpen = army;
            _healOpen = heal;
            _trainingTier = 0;
            _trainingCount = 0;
            _tierScroll = -1f;
            Close();
            CloseUpgrade();
            _trainingSheet = UiFrames.SizedDialog(_canvas.transform, CloseTraining, 0.86f, footer: true);
            _training = _trainingSheet.Root.gameObject;
            RenderTraining();
        }

        private void CloseTraining()
        {
            if (_training != null)
                Destroy(_training);
            _training = null;
            _trainingSheet = null;
            _tierStrip = null;
            _trainingKey = string.Empty;
        }

        private void TickTraining()
        {
            if (_training == null || Time.unscaledTime < _trainingRefresh)
                return;
            if (Input.touchCount > 0 || Input.GetMouseButton(0))
                return;
            RenderTraining();
        }

        /// <summary>Refills the dialog (the frame and its blurred backdrop stay; only the content is rebuilt).</summary>
        private void RenderTraining()
        {
            _trainingRefresh = Time.unscaledTime + 1f;
            var building = Buildings().FirstOrDefault(b => b.instanceKey == _trainingKey);
            if (_trainingSheet == null || string.IsNullOrEmpty(building.instanceKey) || !_catalog.TryGetBuilding(building.definitionId, out var definition))
            {
                CloseTraining();
                return;
            }

            if (_tierStrip != null)
                _tierScroll = _tierStrip.horizontalNormalizedPosition;
            _tierStrip = null;
            UiFrames.Clear(_trainingSheet.List);
            UiFrames.Clear(_trainingSheet.Footer);
            _trainingSheet.Parts.Title.text = _armyOpen ? Loc.T("army.title") : _healOpen ? Loc.T("heal.title") : Loc.Building(definition.Id);
            _trainingSheet.Parts.Subtitle.text = _armyOpen ? string.Empty : Loc.F("base.level", building.level);

            if (_armyOpen)
                RenderArmy(_trainingSheet.List);
            else if (_healOpen)
                RenderInfirmary(_trainingSheet.List, _trainingSheet.Footer);
            else
                RenderYard(_trainingSheet.List, _trainingSheet.Footer, definition, building);
        }

        private void RenderYard(Transform list, Transform footer, BuildingDefinition definition, BuildingSaveEntry building)
        {
            var type = TroopRules.TypeOfBuilding(Troops, definition.Id);
            if (type == null)
                return;
            var maxTier = TroopRules.MaxTier(Troops, building.level);
            if (_trainingTier <= 0 || _trainingTier > maxTier)
                _trainingTier = Math.Max(1, maxTier);
            var batch = TroopRules.BatchSize(Troops, building.level);
            var army = Army;
            var owned = army.Troops.Where(t => t.Type == type.id).ToDictionary(t => t.Tier, t => t.Count);
            var armyNow = army.Troops.Sum(t => t.Count) + army.Training.Sum(t => (long)t.Count);
            var room = (int)Math.Max(0, Math.Min(batch, army.Capacity - armyNow));
            if (_trainingCount <= 0 || _trainingCount > Math.Max(1, room))
                _trainingCount = Math.Max(1, room);

            // The troop and its role, then the tiers as a sideways strip of rarity tiles (locked ones say the yard level).
            var small = Mathf.RoundToInt(UiKit.TypeScale.Caption * UiKit.Scale);
            UiKit.Label("Type", list, $"{Loc.T("troop." + type.id)}  <size={small}><color=#{ColorUtility.ToHtmlStringRGB(UiFrames.InkSoft)}>{Loc.T("troop.role." + type.id)}</color></size>",
                UiKit.TypeScale.Subtitle, UiFrames.Ink, TextAnchor.MiddleLeft, FontStyle.Bold).Row(56);
            var (strip, scroll) = UiFrames.Strip(list);
            ((RectTransform)scroll.transform).Row(176);
            _tierStrip = scroll;
            foreach (var tier in Troops.tiers)
            {
                owned.TryGetValue(tier.tier, out var have);
                var pick = tier.tier;
                TierTile(strip, type.id, tier.tier, tier.tier <= maxTier, tier.tier == _trainingTier, have, tier.buildingLevel, () =>
                {
                    _trainingTier = pick;
                    RenderTraining();
                });
            }

            // Kept where the finger left it across refreshes; on opening, the chosen tier is brought into view.
            var position = _tierScroll >= 0f ? _tierScroll : Troops.tiers.Length > 5 ? Mathf.Clamp01((_trainingTier - 3f) / (Troops.tiers.Length - 5f)) : 0f;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)list);
            scroll.horizontalNormalizedPosition = position;
            if (_tierScroll < 0f)
                StartCoroutine(ScrollStrip(scroll, position));

            // The tier's numbers.
            var chosenTier = TroopRules.Tier(Troops, _trainingTier)!;
            var stats = UiKit.Box("Stats", list, UiFrames.Card);
            stats.sprite = UiKit.Rounded;
            stats.raycastTarget = false;
            stats.Row(170);
            Stat(stats.transform, 0, Loc.T("train.power"), chosenTier.power.ToString("N0"));
            Stat(stats.transform, 1, Loc.T("train.load"), Math.Round(chosenTier.load * type.load).ToString("N0"));
            Stat(stats.transform, 2, Loc.T("train.speed"), type.speed.ToString());
            UiKit.Label("Upkeep", stats.transform, Loc.F("train.upkeep", chosenTier.upkeepFood, chosenTier.upkeepOxygen), UiKit.TypeScale.Caption, UiFrames.InkSoft,
                TextAnchor.MiddleCenter).Fit(12, UiKit.TypeScale.Caption).rectTransform.Place(0.03f, 0.04f, 0.97f, 0.3f);

            // Army room.
            UiKit.Label("Army", list, Loc.F("train.army", CostChips.Compact(armyNow), CostChips.Compact(army.Capacity)), UiKit.TypeScale.Body,
                armyNow >= army.Capacity ? UiFrames.InkBad : UiFrames.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold).Row(48);

            var running = TrainingAt(building.instanceKey);
            if (running != null)
            {
                RenderRunning(list, running);
                return;
            }

            // How many: minus, slider, plus and "n / batch".
            var count = Mathf.Clamp(_trainingCount, 1, Math.Max(1, room));
            var row = UiKit.Rect("Count", list).Row(96);
            var step = Math.Max(1, batch / 20);
            var minus = UiKit.Button("Minus", row, "-", () => { _trainingCount = Math.Max(1, _trainingCount - step); RenderTraining(); }, UiKit.Primary, 44);
            ((RectTransform)minus.transform).Place(0f, 0.08f, 0.12f, 0.92f);
            var slider = UiKit.Slider("Slider", row, room <= 1 ? 1f : (count - 1f) / (room - 1f), v =>
            {
                _trainingCount = Mathf.Clamp(1 + Mathf.RoundToInt(v * Math.Max(0, room - 1)), 1, Math.Max(1, room));
                _countLabel.text = $"{_trainingCount} / {batch}";
                _trainingRefresh = Time.unscaledTime + 0.6f;
            });
            ((RectTransform)slider.transform).Place(0.14f, 0f, 0.66f, 1f);
            var plus = UiKit.Button("Plus", row, "+", () => { _trainingCount = Math.Min(Math.Max(1, room), _trainingCount + step); RenderTraining(); }, UiKit.Primary, 44);
            ((RectTransform)plus.transform).Place(0.68f, 0.08f, 0.8f, 0.92f);
            var box = UiKit.Box("CountBox", row, UiFrames.Card);
            box.sprite = UiKit.Rounded;
            box.raycastTarget = false;
            box.rectTransform.Place(0.82f, 0.08f, 1f, 0.92f);
            _countLabel = UiKit.Label("CountText", box.transform, $"{count} / {batch}", UiKit.TypeScale.Body, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _countLabel.Fit(14, UiKit.TypeScale.Body);
            _countLabel.rectTransform.Place(0, 0, 1, 1, 4);

            // The price for this many, have / need.
            var wallet = Wallet();
            var cost = TroopRules.Cost(chosenTier, count);
            var chips = UiFrames.Grid(list, 2, 5f, 10f);
            chips.GetComponent<GridLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
            foreach (var c in cost)
            {
                wallet.TryGetValue(c.Key, out var have);
                UiFrames.CostChip(chips, c.Key, have, c.Value);
            }

            // Why not, then Train with its time.
            var seconds = TroopRules.Seconds(Troops, chosenTier, count, building.level);
            var verdict = TroopRules.ValidateTrain(Troops, definition.Id, building.level, building.IsUnderConstruction, type.id, chosenTier.tier, count,
                armyNow, army.Capacity, wallet);
            var key = building.instanceKey;
            var typeId = type.id;
            var tierNumber = chosenTier.tier;
            FooterButton(footer, verdict == TroopRejectReason.None ? string.Empty : Loc.Reason(verdict.ToString()), Loc.T("train.action"), Loc.Duration(seconds),
                verdict == TroopRejectReason.None, () => _commands.Dispatch(new TrainTroopsCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, BuildingId = key, TroopType = typeId, Tier = tierNumber, Count = count
                }));
        }

        private TMP_Text _countLabel;

        /// <summary>One tier in the strip: its rarity tile (owned count, "T3"), gold when chosen, shaded with the yard level when locked.</summary>
        private static void TierTile(Transform strip, string type, int tier, bool open, bool chosen, long have, int yardLevel, Action pick)
        {
            var cell = UiKit.Rect("Tier", strip);
            var element = cell.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = element.minWidth = 150f;
            if (chosen)
            {
                var ring = UiKit.Box("Chosen", cell, UiKit.Gold);
                ring.sprite = UiKit.Rounded;
                ring.raycastTarget = false;
                ring.rectTransform.Place(0, 0, 1, 1);
            }

            UiFrames.ItemTile(cell, "item_troops_" + type, UiFrames.TierRarity(tier), open ? CostChips.Compact(have) : null, open ? pick : null, $"T{tier}")
                .Place(0, 0, 1, 1, chosen ? 7 : 0);
            if (open)
                return;
            var shade = UiKit.Box("Locked", cell, new Color(0.03f, 0.05f, 0.1f, 0.55f));
            shade.sprite = UiKit.Rounded;
            shade.raycastTarget = false;
            shade.rectTransform.Place(0, 0, 1, 1, 4);
            var level = UiKit.Label("Level", shade.transform, Loc.F("base.level", yardLevel), UiKit.TypeScale.Caption, Color.white, TextAnchor.LowerCenter, FontStyle.Bold);
            level.Fit(12, UiKit.TypeScale.Caption);
            level.rectTransform.Place(0.04f, 0.06f, 0.96f, 0.5f);
            UiText.Outline(level, new Color(0.03f, 0.05f, 0.1f, 0.95f));
        }

        /// <summary>A number of the stats card: the label over the value, in one of three columns.</summary>
        private static void Stat(Transform card, int column, string label, string value)
        {
            var x = column / 3f;
            UiKit.Label("Label", card, label, UiKit.TypeScale.Caption + 2, UiFrames.InkSoft, TextAnchor.MiddleCenter).Fit(12, UiKit.TypeScale.Caption + 2)
                .rectTransform.Place(x + 0.01f, 0.66f, x + 0.32f, 0.95f);
            UiKit.Label("Value", card, value, UiKit.TypeScale.Title, UiFrames.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).Fit(16, UiKit.TypeScale.Title)
                .rectTransform.Place(x + 0.01f, 0.3f, x + 0.32f, 0.68f);
        }

        /// <summary>Puts the tier strip at <paramref name="position"/> once it has been laid out.</summary>
        private static IEnumerator ScrollStrip(ScrollRect scroll, float position)
        {
            yield return null;
            if (scroll != null)
                scroll.horizontalNormalizedPosition = position;
        }

        /// <summary>The footer's call to action: why not in red, then one big button with its time.</summary>
        private static void FooterButton(Transform footer, string reason, string action, string detail, bool enabled, Action click)
        {
            if (!string.IsNullOrEmpty(reason))
                UiKit.Label("Reason", footer, reason, UiKit.TypeScale.Caption + 2, UiFrames.InkBad, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .Fit(14, UiKit.TypeScale.Caption + 2).Row(44);
            var strip = UiFrames.ButtonStrip(footer);
            strip.Row(116);
            strip.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(150, 150, 0, 0);
            var size = Mathf.RoundToInt(UiKit.TypeScale.Caption * UiKit.Scale);
            var go = UiKit.Button("Action", strip, string.IsNullOrEmpty(detail) ? action : $"{action}\n<size={size}>{detail}</size>", () => click(),
                enabled ? UiKit.Ok : UiKit.Panel, 36);
            go.interactable = enabled;
        }

        /// <summary>The batch in progress: what, how many, the bar and the time left; Cancel refunds it all.</summary>
        private void RenderRunning(Transform list, TrainingDto running)
        {
            var now = _stateSync.View.ServerNowMs;
            var total = Math.Max(1, running.EndsAtMs - running.StartedAtMs);
            var progress = Mathf.Clamp01((now - running.StartedAtMs) / (float)total);
            var what = Loc.F("train.running", running.Count, $"T{running.Tier} {Loc.T("troop." + running.Type)}");
            var job = UiFrames.Job(list, what, progress, Loc.Duration(Math.Max(0, (running.EndsAtMs - now) / 1000f)), "gold");
            var key = running.BuildingId;
            UiKit.Button("Cancel", job.Buttons, Loc.T("train.cancel"), () =>
                _commands.Dispatch(new CancelTrainingCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, BuildingId = key }),
                UiKit.Danger, 32);
            UiKit.Button("Speed", job.Buttons, Loc.T("base.speedup"), () => SpeedupWindow.Open("train:" + key, SpeedupKind.Train, what, () =>
            {
                var t = TrainingAt(key);
                return t == null ? null : (t.StartedAtMs, t.EndsAtMs);
            }), UiKit.Primary, 32);
            UiKit.Label("Refund", list, Loc.T("train.refund"), UiKit.TypeScale.Caption, UiFrames.InkSoft, TextAnchor.MiddleCenter).Row(40);
        }

        /// <summary>
        /// The Military Dome (Last Asylum's army overview): the totals as bonus lines, then each troop type with its
        /// tiers as rarity tiles and what is in training.
        /// </summary>
        private void RenderArmy(Transform list)
        {
            var army = Army;
            var wounded = (army.Wounded ?? Array.Empty<TroopStackDto>()).Sum(t => t.Count);
            var total = army.Troops.Sum(t => t.Count) + army.Marching + wounded;
            var training = army.Training.Sum(t => (long)t.Count);
            var bad = ColorUtility.ToHtmlStringRGB(UiFrames.InkBad);

            UiFrames.BonusRow(list, Loc.T("army.home"), total + training >= army.Capacity
                ? $"<color=#{bad}>{total:N0} / {army.Capacity:N0}</color>" : $"{total:N0} / {army.Capacity:N0}").Row(76);
            if (training > 0)
                UiFrames.BonusRow(list, Loc.T("army.training"), training.ToString("N0")).Row(76);
            if (army.Marching > 0)
                UiFrames.BonusRow(list, Loc.T("army.marching"), army.Marching.ToString("N0")).Row(76);
            var hurt = wounded + (army.Healing?.Troops ?? Array.Empty<TroopStackDto>()).Sum(t => t.Count);
            if (hurt > 0)
                UiFrames.BonusRow(list, Loc.T("army.wounded"), $"<color=#{bad}>{hurt:N0}</color>").Row(76);
            UiFrames.BonusRow(list, Loc.T("army.power"),
                TroopRules.Power(Troops, army.Troops.ToDictionary(t => TroopRules.Key(t.Type, t.Tier), t => t.Count)).ToString("N0")).Row(76);
            UiFrames.BonusRow(list, Loc.T("army.upkeep"), Loc.F("army.upkeep.value", Math.Round(army.UpkeepFood, 1), Math.Round(army.UpkeepOxygen, 1))).Row(76);

            foreach (var type in Troops.types)
            {
                UiFrames.Section(list, Loc.T("troop." + type.id)).Row(60);
                var stacks = army.Troops.Where(t => t.Type == type.id && t.Count > 0).OrderByDescending(t => t.Tier).ToList();
                if (stacks.Count == 0)
                    UiFrames.Note(list, Loc.T("army.none")).Row(76);
                else
                    StackGrid(list, stacks.Select(s => (s.Type, s.Tier, s.Count)));
                foreach (var t in army.Training.Where(t => t.Type == type.id))
                    UiFrames.BonusRow(list, $"T{t.Tier} · {Loc.T("army.training")}",
                        $"{t.Count:N0}   {Loc.Duration(Math.Max(0, (t.EndsAtMs - _stateSync.View.ServerNowMs) / 1000f))}").Row(76);
            }
        }

        /// <summary>Troop stacks as a five-column grid of rarity tiles with the count and the tier.</summary>
        private static void StackGrid(Transform list, IEnumerable<(string Type, int Tier, long Count)> stacks)
        {
            var grid = UiFrames.Grid(list, 5, 1f, 10f);
            grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
            foreach (var (type, tier, count) in stacks)
                UiFrames.ItemTile(grid, "item_troops_" + type, UiFrames.TierRarity(tier), CostChips.Compact(count), null, $"T{tier}");
        }

        /// <summary>
        /// The Med Bay (Last Asylum's hospital): the beds as a bar, the wounded as rarity tiles, the price and time to
        /// heal them all with Heal in the footer; while healing, the job card with Cancel, Ask help and Speed up.
        /// </summary>
        private void RenderInfirmary(Transform list, Transform footer)
        {
            var army = Army;
            var wounded = army.Wounded ?? Array.Empty<TroopStackDto>();
            var healing = army.Healing;
            var beds = wounded.Sum(t => t.Count) + (healing?.Troops ?? Array.Empty<TroopStackDto>()).Sum(t => t.Count);
            UiFrames.Progress(list, army.InfirmaryCapacity > 0 ? Mathf.Clamp01(beds / (float)army.InfirmaryCapacity) : 0f,
                Loc.F("heal.capacity", beds.ToString("N0"), army.InfirmaryCapacity.ToString("N0")),
                beds >= army.InfirmaryCapacity && beds > 0 ? "gold" : "green").Row(48);

            if (wounded.Length == 0 && healing == null)
            {
                UiFrames.Note(list, Loc.T("heal.none")).Row(96);
                return;
            }

            if (wounded.Length > 0)
                StackGrid(list, wounded.Select(t => (t.Type, t.Tier, t.Count)));

            if (healing != null)
            {
                var now = _stateSync.View.ServerNowMs;
                var span = Math.Max(1, healing.EndsAtMs - healing.StartedAtMs);
                var job = UiFrames.Job(list, Loc.F("heal.running", healing.Troops.Sum(t => t.Count).ToString("N0")),
                    Mathf.Clamp01((now - healing.StartedAtMs) / (float)span), Loc.Duration(Math.Max(0, (healing.EndsAtMs - now) / 1000f)));
                // Cancel · Ask the alliance (once) · Speed up.
                UiKit.Button("Cancel", job.Buttons, Loc.T("train.cancel"), () =>
                    _commands.Dispatch(new CancelHealCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence }), UiKit.Danger, 30);
                if (_stateSync.View.MyAlliance != null)
                {
                    var helpKey = "heal:" + healing.StartedAtMs;
                    var asked = _helpRequested.Contains(helpKey);
                    var help = UiKit.Button("Help", job.Buttons, asked ? Loc.T("help.asked") : Loc.T("help.ask"),
                        () => AskJobHelp(HelpKinds.Heal, string.Empty, helpKey), UiKit.Ok, 30);
                    help.interactable = !asked;
                }

                UiKit.Button("Speed", job.Buttons, Loc.T("base.speedup"), () => SpeedupWindow.Open("heal", SpeedupKind.Heal, Loc.T("heal.title"), () =>
                {
                    var h = Army.Healing;
                    return h == null ? null : (h.StartedAtMs, h.EndsAtMs);
                }), UiKit.Primary, 30);
                return;
            }

            // Heal everyone: the price (a share of training) and the time.
            var request = wounded.ToDictionary(t => TroopRules.Key(t.Type, t.Tier), t => t.Count);
            var wallet = Wallet();
            var chips = UiFrames.Grid(list, 2, 5f, 10f);
            chips.GetComponent<GridLayoutGroup>().padding = new RectOffset(0, 0, 0, 0);
            foreach (var c in TroopRules.HealCost(Troops, request))
            {
                wallet.TryGetValue(c.Key, out var have);
                UiFrames.CostChip(chips, c.Key, have, c.Value);
            }

            var best = TroopRules.BestInfirmary(Troops, Buildings().Select(b => (b.definitionId, b.level)));
            var verdict = TroopRules.ValidateHeal(Troops, best, false, request, request, wallet);
            FooterButton(footer, verdict == TroopRejectReason.None ? string.Empty : Loc.Reason(verdict.ToString()), Loc.T("heal.all"),
                Loc.Duration(TroopRules.HealSeconds(Troops, request, best)), verdict == TroopRejectReason.None,
                () => _commands.Dispatch(new HealTroopsCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence }));
        }
    }
}
