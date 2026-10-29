using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Hadal.Data.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Hadal.UI
{
    /// <summary>
    /// Training yards and the army (docs/rehber 19 §2, Last Asylum's training ground): pick a tier, set how many,
    /// see the price and the time, train; the Military Dome shows the army and its cap.
    /// </summary>
    public sealed partial class BaseManagementView
    {
        private GameObject _training;
        private string _trainingKey = string.Empty;
        private int _trainingTier;
        private int _trainingCount;
        private float _trainingRefresh;
        private bool _armyOpen;
        private bool _healOpen;

        private TroopCatalogData Troops => _catalog.Troops;

        private ArmyDto Army => _stateSync?.View.Army ?? new ArmyDto();

        private TrainingDto TrainingAt(string instanceKey) => Army.Training.FirstOrDefault(t => t.BuildingId == instanceKey);

        /// <summary>Round-menu entries for military buildings: Train on a yard, Army on the Military Dome.</summary>
        private void AddMilitaryActions(BuildingDefinition definition, BuildingSaveEntry building, List<TargetMenu.Item> items)
        {
            if (building.level <= 0)
                return;
            var key = building.instanceKey;
            if (TroopRules.IsTrainingBuilding(Troops, definition.Id))
                items.Add(new TargetMenu.Item("nav_heroes", Loc.T("train.action"), () => OpenTraining(key)));
            if (definition.Id == Troops.capacityBuilding)
                items.Add(new TargetMenu.Item("side_alliance", Loc.T("army.action"), () => OpenTraining(key, army: true)));
            if (definition.Id == Troops.infirmary.building)
                items.Add(new TargetMenu.Item("left_help", Loc.T("heal.action"), () => OpenTraining(key, heal: true)));
        }

        private void OpenTraining(string instanceKey, bool army = false, bool heal = false)
        {
            _trainingKey = instanceKey;
            _armyOpen = army;
            _healOpen = heal;
            _trainingTier = 0;
            _trainingCount = 0;
            Close();
            CloseUpgrade();
            RenderTraining();
        }

        private void CloseTraining()
        {
            if (_training != null)
                Destroy(_training);
            _training = null;
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

        private void RenderTraining()
        {
            _trainingRefresh = Time.unscaledTime + 1f;
            var building = Buildings().FirstOrDefault(b => b.instanceKey == _trainingKey);
            if (string.IsNullOrEmpty(building.instanceKey) || !_catalog.TryGetBuilding(building.definitionId, out var definition))
            {
                CloseTraining();
                return;
            }

            var fresh = _training == null;
            if (_training != null)
                Destroy(_training);

            var dim = UiKit.Box("Training", _canvas.transform, new Color(0f, 0.01f, 0.02f, 0.72f));
            dim.rectTransform.Place(0, 0, 1, 1);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(CloseTraining);
            _training = dim.gameObject;

            var card = UiKit.Box("Card", dim.transform, UiKit.Panel);
            card.rectTransform.Place(0.03f, 0.1f, 0.97f, 0.88f);
            card.gameObject.AddComponent<Button>();
            if (fresh)
            {
                card.gameObject.AddComponent<CanvasGroup>();
                card.gameObject.AddComponent<PopIn>();
            }

            var header = UiKit.Box("Header", card.transform, new Color(0.05f, 0.16f, 0.22f, 1f));
            header.rectTransform.Place(0, 0.91f, 1, 1);
            UiKit.Label("Title", header.transform, _armyOpen ? Loc.T("army.title") : _healOpen ? Loc.T("heal.title") : Loc.Building(definition.Id), 42, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.05f, 0.4f, 0.8f, 1f);
            UiKit.Label("Level", header.transform, Loc.F("base.level", building.level), 28, UiKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold)
                .rectTransform.Place(0.05f, 0.02f, 0.8f, 0.45f);
            var close = UiKit.Button("Close", header.transform, "X", CloseTraining, UiKit.Field, 40);
            close.GetComponentInChildren<Text>().color = UiKit.Text;
            ((RectTransform)close.transform).Place(0.84f, 0.18f, 0.96f, 0.82f);

            if (_armyOpen)
                RenderArmy(card.transform);
            else if (_healOpen)
                RenderInfirmary(card.transform);
            else
                RenderYard(card.transform, definition, building);
        }

        private void RenderYard(Transform card, BuildingDefinition definition, BuildingSaveEntry building)
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

            // Tier strip: ten portraits, five a row; locked ones show the yard level that opens them.
            UiKit.Label("Type", card, $"{Loc.T("troop." + type.id)}  <size=24><color=#8AA9B5>{Loc.T("troop.role." + type.id)}</color></size>", 32, UiKit.Amber,
                TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.04f, 0.855f, 0.96f, 0.905f);
            for (var i = 0; i < Troops.tiers.Length; i++)
            {
                var tier = Troops.tiers[i];
                var col = i % 5;
                var row = i / 5;
                var x0 = 0.04f + col * 0.186f;
                var y1 = 0.85f - row * 0.115f;
                var open = tier.tier <= maxTier;
                var chosen = tier.tier == _trainingTier;
                var cell = UiKit.Box("Tier", card, chosen ? new Color(0.13f, 0.52f, 0.86f, 1f) : open ? UiKit.Field : new Color(0.03f, 0.06f, 0.08f, 1f));
                cell.rectTransform.Place(x0, y1 - 0.105f, x0 + 0.176f, y1);
                Hud.HudIcons.Image(cell.transform, TroopIcon(type.id), 0.2f, 0.38f, 0.8f, 0.95f).color = open ? Color.white : new Color(1, 1, 1, 0.25f);
                UiKit.Label("Name", cell.transform, $"T{tier.tier}", 26, open ? UiKit.Text : UiKit.Muted, TextAnchor.UpperLeft, FontStyle.Bold)
                    .rectTransform.Place(0.06f, 0.6f, 0.6f, 0.97f);
                owned.TryGetValue(tier.tier, out var have);
                UiKit.Label("Have", cell.transform, open ? CostChips.Compact(have) : Loc.F("base.level", tier.buildingLevel), 22,
                    open ? UiKit.Amber : UiKit.Muted, TextAnchor.LowerCenter, FontStyle.Bold).rectTransform.Place(0, 0.03f, 1, 0.4f);
                var pick = tier.tier;
                if (open)
                    cell.gameObject.AddComponent<Button>().onClick.AddListener(() => { _trainingTier = pick; RenderTraining(); });
            }

            var chosenTier = TroopRules.Tier(Troops, _trainingTier)!;
            var running = TrainingAt(building.instanceKey);

            // The tier's numbers.
            var stats = UiKit.Box("Stats", card, new Color(0.04f, 0.11f, 0.15f, 1f));
            stats.rectTransform.Place(0.04f, 0.53f, 0.96f, 0.615f);
            UiKit.Label("Text", stats.transform,
                $"{Loc.T("train.power")} <b>{chosenTier.power}</b>    {Loc.T("train.load")} <b>{Math.Round(chosenTier.load * type.load)}</b>    " +
                $"{Loc.T("train.speed")} <b>{type.speed}</b>\n<size=22><color=#8AA9B5>{Loc.F("train.upkeep", chosenTier.upkeepFood, chosenTier.upkeepOxygen)}</color></size>",
                26, UiKit.Text, TextAnchor.MiddleCenter).rectTransform.Place(0.02f, 0, 0.98f, 1);

            // Army room.
            UiKit.Label("Army", card, Loc.F("train.army", CostChips.Compact(armyNow), CostChips.Compact(army.Capacity)), 26,
                armyNow >= army.Capacity ? UiKit.Danger : UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0.04f, 0.485f, 0.96f, 0.525f);

            if (running != null)
            {
                RenderRunning(card, running);
                return;
            }

            // How many: slider, minus/plus and the batch cap.
            var count = Mathf.Clamp(_trainingCount, 1, Math.Max(1, room));
            var minus = UiKit.Button("Minus", card, "-", () => { _trainingCount = Math.Max(1, _trainingCount - Math.Max(1, batch / 20)); RenderTraining(); }, UiKit.Field, 44);
            ((RectTransform)minus.transform).Place(0.04f, 0.41f, 0.14f, 0.47f);
            var slider = UiKit.Slider("Count", card, room <= 1 ? 1f : (count - 1f) / (room - 1f), v =>
            {
                _trainingCount = Mathf.Clamp(1 + Mathf.RoundToInt(v * Math.Max(0, room - 1)), 1, Math.Max(1, room));
                _countLabel.text = $"{_trainingCount} / {batch}";
                _trainingRefresh = Time.unscaledTime + 0.6f;
            });
            ((RectTransform)slider.transform).Place(0.16f, 0.41f, 0.66f, 0.47f);
            var plus = UiKit.Button("Plus", card, "+", () => { _trainingCount = Math.Min(Math.Max(1, room), _trainingCount + Math.Max(1, batch / 20)); RenderTraining(); }, UiKit.Field, 44);
            ((RectTransform)plus.transform).Place(0.68f, 0.41f, 0.78f, 0.47f);
            _countLabel = UiKit.Label("CountText", card, $"{count} / {batch}", 30, UiKit.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            _countLabel.rectTransform.Place(0.79f, 0.41f, 0.97f, 0.47f);

            // Price for this many, have / need.
            var wallet = Wallet();
            var cost = TroopRules.Cost(chosenTier, count);
            for (var i = 0; i < cost.Count; i++)
            {
                var c = cost[i];
                wallet.TryGetValue(c.Key, out var have);
                var ok = have >= c.Value;
                var chip = UiKit.Box("Cost", card, new Color(0.04f, 0.11f, 0.15f, 1f));
                var x0 = 0.04f + (i % 2) * 0.465f;
                var y1 = 0.395f - (i / 2) * 0.065f;
                chip.rectTransform.Place(x0, y1 - 0.058f, x0 + 0.455f, y1);
                Hud.HudIcons.Image(chip.transform, Hud.HudIcons.Resource(c.Key), 0.03f, 0.12f, 0.2f, 0.88f);
                UiKit.Label("Amount", chip.transform, $"<color={(ok ? "#E6F2F7" : "#FF5A5A")}>{CostChips.Compact(have)}</color> / {CostChips.Compact(c.Value)}",
                    26, UiKit.Text, TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.24f, 0, 0.98f, 1);
            }

            var seconds = TroopRules.Seconds(Troops, chosenTier, count, building.level);
            var verdict = TroopRules.ValidateTrain(Troops, definition.Id, building.level, building.IsUnderConstruction, type.id, chosenTier.tier, count,
                armyNow, army.Capacity, wallet);
            if (verdict != TroopRejectReason.None)
                UiKit.Label("Reason", card, Loc.Reason(verdict.ToString()), 26, UiKit.Danger, TextAnchor.MiddleCenter)
                    .rectTransform.Place(0.04f, 0.15f, 0.96f, 0.2f);
            var key = building.instanceKey;
            var typeId = type.id;
            var tierNumber = chosenTier.tier;
            var go = UiKit.Button("Train", card, $"{Loc.T("train.action")}\n<size=26>{Loc.Duration(seconds)}</size>", () =>
            {
                _commands.Dispatch(new TrainTroopsCommand
                {
                    CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, BuildingId = key, TroopType = typeId, Tier = tierNumber, Count = count
                });
            }, verdict == TroopRejectReason.None ? UiKit.Ok : UiKit.Panel, 40);
            go.interactable = verdict == TroopRejectReason.None;
            if (!go.interactable)
                go.GetComponentInChildren<Text>().color = UiKit.Muted;
            ((RectTransform)go.transform).Place(0.2f, 0.03f, 0.8f, 0.14f);
        }

        private Text _countLabel;

        /// <summary>The batch in progress: what, how many, the bar and the time left; Cancel refunds it all.</summary>
        private void RenderRunning(Transform card, TrainingDto running)
        {
            var now = _stateSync.View.ServerNowMs;
            var total = Math.Max(1, running.EndsAtMs - running.StartedAtMs);
            var progress = Mathf.Clamp01((now - running.StartedAtMs) / (float)total);
            UiKit.Label("Running", card, Loc.F("train.running", running.Count, $"T{running.Tier} {Loc.T("troop." + running.Type)}"), 30, UiKit.Text,
                TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0.04f, 0.38f, 0.96f, 0.45f);
            var bar = UiKit.Box("Bar", card, UiKit.Field);
            bar.rectTransform.Place(0.08f, 0.32f, 0.92f, 0.36f);
            UiKit.Box("Fill", bar.transform, UiKit.Amber).rectTransform.Place(0, 0, Mathf.Max(0.02f, progress), 1);
            UiKit.Label("Left", card, Loc.Duration(Math.Max(0, (running.EndsAtMs - now) / 1000f)), 32, UiKit.Amber, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(0.04f, 0.25f, 0.96f, 0.31f);
            var key = running.BuildingId;
            var cancel = UiKit.Button("Cancel", card, Loc.T("train.cancel"), () =>
                _commands.Dispatch(new CancelTrainingCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence, BuildingId = key }),
                UiKit.Danger, 34);
            ((RectTransform)cancel.transform).Place(0.04f, 0.04f, 0.48f, 0.13f);
            var speed = UiKit.Button("Speed", card, Loc.T("base.speedup"), () => SpeedupWindow.Open("train:" + key, SpeedupKind.Train,
                Loc.F("train.running", running.Count, $"T{running.Tier} {Loc.T("troop." + running.Type)}"), () =>
                {
                    var t = TrainingAt(key);
                    return t == null ? null : (t.StartedAtMs, t.EndsAtMs);
                }), UiKit.Primary, 34);
            ((RectTransform)speed.transform).Place(0.52f, 0.04f, 0.96f, 0.13f);
            UiKit.Label("Refund", card, Loc.T("train.refund"), 22, UiKit.Muted, TextAnchor.MiddleCenter).rectTransform.Place(0.04f, 0.14f, 0.96f, 0.19f);
        }

        /// <summary>The Military Dome: the cap, the upkeep, every stack by type and tier, and what is in training.</summary>
        private void RenderArmy(Transform card)
        {
            var army = Army;
            var total = army.Troops.Sum(t => t.Count) + army.Marching + (army.Wounded ?? Array.Empty<TroopStackDto>()).Sum(t => t.Count);
            var training = army.Training.Sum(t => (long)t.Count);
            var list = UiKit.ScrollList("List", card, 10f);
            ((RectTransform)list.parent).Place(0.04f, 0.03f, 0.96f, 0.89f);

            Section(list, Loc.T("army.capacity"));
            Row(list, Loc.T("army.home"), $"{total:N0} / {army.Capacity:N0}", total + training >= army.Capacity ? UiKit.Danger : UiKit.Ok);
            if (training > 0)
                Row(list, Loc.T("army.training"), training.ToString("N0"), UiKit.Amber);
            if (army.Marching > 0)
                Row(list, Loc.T("army.marching"), army.Marching.ToString("N0"), UiKit.Accent);
            var wounded = (army.Wounded ?? Array.Empty<TroopStackDto>()).Sum(t => t.Count) + (army.Healing?.Troops ?? Array.Empty<TroopStackDto>()).Sum(t => t.Count);
            if (wounded > 0)
                Row(list, Loc.T("army.wounded"), wounded.ToString("N0"), UiKit.Danger);
            Row(list, Loc.T("army.power"), TroopRules.Power(Troops, army.Troops.ToDictionary(t => TroopRules.Key(t.Type, t.Tier), t => t.Count)).ToString("N0"), UiKit.Text);
            Row(list, Loc.T("army.upkeep"), Loc.F("army.upkeep.value", Math.Round(army.UpkeepFood, 1), Math.Round(army.UpkeepOxygen, 1)), UiKit.Muted);

            foreach (var type in Troops.types)
            {
                Section(list, Loc.T("troop." + type.id));
                var stacks = army.Troops.Where(t => t.Type == type.id).OrderByDescending(t => t.Tier).ToList();
                if (stacks.Count == 0)
                    Row(list, Loc.T("army.none"), "—", UiKit.Muted);
                foreach (var s in stacks)
                    Row(list, $"T{s.Tier}", s.Count.ToString("N0"), UiKit.Text);
                foreach (var t in army.Training.Where(t => t.Type == type.id))
                    Row(list, $"T{t.Tier} · {Loc.T("army.training")}",
                        $"{t.Count:N0}  <color=#FFB74D>{Loc.Duration(Math.Max(0, (t.EndsAtMs - _stateSync.View.ServerNowMs) / 1000f))}</color>", UiKit.Text);
            }
        }

        /// <summary>
        /// The Med Bay (Last Asylum's hospital): wounded against the beds, each stack by type and tier, the price and time
        /// to heal them all, then Heal; while healing, the bar and Cancel.
        /// </summary>
        private void RenderInfirmary(Transform card)
        {
            var army = Army;
            var wounded = army.Wounded ?? Array.Empty<TroopStackDto>();
            var healing = army.Healing;
            var beds = wounded.Sum(t => t.Count) + (healing?.Troops ?? Array.Empty<TroopStackDto>()).Sum(t => t.Count);

            var bar = UiKit.Box("Beds", card, UiKit.Field);
            bar.rectTransform.Place(0.06f, 0.82f, 0.94f, 0.87f);
            UiKit.Box("Fill", bar.transform, beds >= army.InfirmaryCapacity && beds > 0 ? UiKit.Danger : UiKit.Ok).rectTransform
                .Place(0, 0, army.InfirmaryCapacity > 0 ? Mathf.Clamp01(beds / (float)army.InfirmaryCapacity) : 0f, 1);
            UiKit.Label("Text", bar.transform, Loc.F("heal.capacity", beds.ToString("N0"), army.InfirmaryCapacity.ToString("N0")), 24, Color.white,
                TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(0, 0, 1, 1);

            var list = UiKit.ScrollList("List", card, 8f);
            ((RectTransform)list.parent).Place(0.04f, 0.36f, 0.96f, 0.8f);
            if (wounded.Length == 0 && healing == null)
                Row(list, Loc.T("heal.none"), string.Empty, UiKit.Muted);
            foreach (var t in wounded)
                Row(list, $"T{t.Tier} {Loc.T("troop." + t.Type)}", t.Count.ToString("N0"), UiKit.Text);

            if (healing != null)
            {
                var now = _stateSync.View.ServerNowMs;
                var span = Math.Max(1, healing.EndsAtMs - healing.StartedAtMs);
                UiKit.Label("Running", card, Loc.F("heal.running", healing.Troops.Sum(t => t.Count).ToString("N0")), 30, UiKit.Text, TextAnchor.MiddleCenter,
                    FontStyle.Bold).rectTransform.Place(0.04f, 0.27f, 0.96f, 0.33f);
                var progress = UiKit.Box("Bar", card, UiKit.Field);
                progress.rectTransform.Place(0.08f, 0.22f, 0.92f, 0.26f);
                UiKit.Box("Fill", progress.transform, UiKit.Ok).rectTransform.Place(0, 0, Mathf.Max(0.02f, Mathf.Clamp01((now - healing.StartedAtMs) / (float)span)), 1);
                UiKit.Label("Left", card, Loc.Duration(Math.Max(0, (healing.EndsAtMs - now) / 1000f)), 30, UiKit.Ok, TextAnchor.MiddleCenter, FontStyle.Bold)
                    .rectTransform.Place(0.04f, 0.15f, 0.96f, 0.21f);
                // Cancel · Ask the alliance (in one) · Speed up.
                var inAlliance = _stateSync.View.MyAlliance != null;
                var cancel = UiKit.Button("Cancel", card, Loc.T("train.cancel"), () =>
                    _commands.Dispatch(new CancelHealCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence }), UiKit.Danger, 34);
                ((RectTransform)cancel.transform).Place(0.04f, 0.03f, inAlliance ? 0.32f : 0.48f, 0.13f);
                if (inAlliance)
                {
                    var helpKey = "heal:" + healing.StartedAtMs;
                    var asked = _helpRequested.Contains(helpKey);
                    var help = UiKit.Button("Help", card, asked ? Loc.T("help.asked") : Loc.T("help.ask"),
                        () => AskJobHelp(HelpKinds.Heal, string.Empty, helpKey), UiKit.Ok, 30);
                    help.interactable = !asked;
                    ((RectTransform)help.transform).Place(0.35f, 0.03f, 0.65f, 0.13f);
                }

                var speed = UiKit.Button("Speed", card, Loc.T("base.speedup"), () => SpeedupWindow.Open("heal", SpeedupKind.Heal, Loc.T("heal.title"), () =>
                    {
                        var h = Army.Healing;
                        return h == null ? null : (h.StartedAtMs, h.EndsAtMs);
                    }), UiKit.Primary, 34);
                ((RectTransform)speed.transform).Place(inAlliance ? 0.68f : 0.52f, 0.03f, 0.96f, 0.13f);
                return;
            }

            if (wounded.Length == 0)
                return;

            // Heal everyone: the price (a share of training) and the time, have / need.
            var request = wounded.ToDictionary(t => TroopRules.Key(t.Type, t.Tier), t => t.Count);
            var wallet = Wallet();
            var cost = TroopRules.HealCost(Troops, request);
            for (var i = 0; i < cost.Count; i++)
            {
                var c = cost[i];
                wallet.TryGetValue(c.Key, out var have);
                var chip = UiKit.Box("Cost", card, new Color(0.04f, 0.11f, 0.15f, 1f));
                var x0 = 0.04f + i * 0.31f;
                chip.rectTransform.Place(x0, 0.25f, x0 + 0.3f, 0.32f);
                Hud.HudIcons.Image(chip.transform, Hud.HudIcons.Resource(c.Key), 0.03f, 0.12f, 0.28f, 0.88f);
                UiKit.Label("Amount", chip.transform, $"<color={(have >= c.Value ? "#E6F2F7" : "#FF5A5A")}>{CostChips.Compact(c.Value)}</color>", 26, UiKit.Text,
                    TextAnchor.MiddleLeft, FontStyle.Bold).rectTransform.Place(0.32f, 0, 0.98f, 1);
            }

            var best = TroopRules.BestInfirmary(Troops, Buildings().Select(b => (b.definitionId, b.level)));
            var verdict = TroopRules.ValidateHeal(Troops, best, false, request, request, wallet);
            if (verdict != TroopRejectReason.None)
                UiKit.Label("Reason", card, Loc.Reason(verdict.ToString()), 26, UiKit.Danger, TextAnchor.MiddleCenter).rectTransform.Place(0.04f, 0.15f, 0.96f, 0.21f);
            var go = UiKit.Button("Heal", card, $"{Loc.T("heal.all")}\n<size=26>{Loc.Duration(TroopRules.HealSeconds(Troops, request, best))}</size>", () =>
                _commands.Dispatch(new HealTroopsCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = ++_sequence }),
                verdict == TroopRejectReason.None ? UiKit.Ok : UiKit.Panel, 40);
            go.interactable = verdict == TroopRejectReason.None;
            ((RectTransform)go.transform).Place(0.2f, 0.03f, 0.8f, 0.14f);
        }

        private static string TroopIcon(string type) => type switch
        {
            "harpoon" => "event_war",
            "arc" => "left_research",
            "driller" => "queue_build",
            _ => "power"
        };
    }
}
