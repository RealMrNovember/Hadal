using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Data.Models;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Each building's own function in its panel (docs/24_Building_Functions.md): Boost on producers,
    /// Overdrive and the next-level preview on the Core Reactor, salvage runs from the Drone Factory,
    /// the research screen of the Research Center and a "coming later" card elsewhere.
    /// </summary>
    public sealed partial class BaseManagementView
    {
        private bool _researchOpen;
        private string _researchTree = "development";

        /// <summary>Research plus the command crew's skills, as the server computes them.</summary>
        private PlayerModifiers Modifiers()
        {
            var modifiers = ResearchRules.Modifiers(_catalog, _stateSync.View.ResearchLevels);
            HeroRules.AddCrew(_catalog, modifiers, _stateSync.View.Heroes.Select(h => new OwnedHero(h.HeroId, h.Level, h.Stars, h.Shards, h.Slot)));
            return modifiers;
        }

        private static float Left(float endsAt) => endsAt > 0f ? Mathf.Max(0f, endsAt - Time.realtimeSinceStartup) : 0f;

        private void AddFunctionCards(BuildingSaveEntry building, BuildingDefinition definition)
        {
            if (definition.Id == ResearchRules.CenterId)
                AddResearchEntryCard();
            else if (BuildingFunctions.CanOverdrive(definition))
                AddOverdriveCards(building);
            else if (BuildingFunctions.CanSalvage(definition))
                AddSalvageCards(building);

            if (BuildingFunctions.CanBoost(definition))
                AddBoostCard(building, definition);

            if (definition.Id != ResearchRules.CenterId && !BuildingFunctions.CanOverdrive(definition)
                && !BuildingFunctions.CanSalvage(definition) && !BuildingFunctions.CanBoost(definition))
                AddCard(Loc.T("func.soon.title"), Loc.Has("func.soon." + definition.Id) ? Loc.T("func.soon." + definition.Id) : string.Empty,
                    string.Empty, string.Empty, Loc.T("func.soon.button"), false, () => { });
        }

        // --- Boost -------------------------------------------------------------------------------

        private void AddBoostCard(BuildingSaveEntry building, BuildingDefinition definition)
        {
            var rules = _catalog.Boost;
            var left = Left(building.boostEndsAt);
            var verdict = BuildingFunctions.ValidateBoost(_catalog, definition, building.level, left > 0f, Wallet());
            var key = building.instanceKey;
            AddCard(
                Loc.T("func.boost.title"),
                left > 0f ? Loc.F("func.boost.running", Loc.Duration(left)) : Loc.F("func.boost.body", rules.percent, Loc.Duration(rules.seconds)),
                string.Empty,
                verdict is FunctionRejectReason.None or FunctionRejectReason.AlreadyRunning ? string.Empty : Loc.Reason(verdict.ToString()),
                Loc.T("func.boost.button"),
                verdict == FunctionRejectReason.None,
                () => Send(new BoostBuildingCommand { BuildingEntityId = key }),
                costs: _catalog.BoostCost, wallet: Wallet());
        }

        // --- Core Reactor -------------------------------------------------------------------------

        private void AddOverdriveCards(BuildingSaveEntry building)
        {
            var rules = _catalog.Overdrive;
            var running = Left(building.overdriveEndsAt);
            var cooldown = Left(building.overdriveReadyAt);
            _catalog.TryGetBuilding(building.definitionId, out var definition);
            var verdict = BuildingFunctions.ValidateOverdrive(definition, building.level, running > 0f, cooldown > 0f);
            var key = building.instanceKey;
            var body = running > 0f ? Loc.F("func.overdrive.running", Loc.Duration(running))
                : cooldown > 0f ? Loc.F("func.overdrive.cooldown", Loc.Duration(cooldown))
                : Loc.F("func.overdrive.body", rules.percent, Loc.Duration(rules.seconds), Loc.Duration(rules.cooldownSeconds));
            AddCard(Loc.T("func.overdrive.title"), body, string.Empty, string.Empty, Loc.T("func.overdrive.button"),
                verdict == FunctionRejectReason.None, () => Send(new OverdriveCommand { BuildingEntityId = key }));

            // What the next reactor level opens: the reason to upgrade it.
            var next = building.level + 1;
            var opens = _catalog.Buildings.Where(b => b.RequiredReactorLevel == next).Select(b => Loc.Building(b.Id)).ToList();
            for (var ring = _catalog.Grid.startingUnlockedRings + 1; ring <= _catalog.Grid.maxRings; ring++)
            {
                if (_catalog.TryGetRingUnlock(ring, out var unlock) && unlock.ReactorLevel == next)
                    opens.Add(Loc.F("base.expand.ring", ring));
            }

            if (opens.Count > 0)
                UiKit.Label("Opens", _list, Loc.F("func.reactor.next", next, string.Join(", ", opens)), 30, UiKit.Accent).Row(100);
        }

        // --- Drone Factory --------------------------------------------------------------------------

        private void AddSalvageCards(BuildingSaveEntry building)
        {
            var key = building.instanceKey;
            var left = Left(building.missionEndsAt);
            if (left > 0f && _catalog.TryGetSalvageMission(building.missionId, out var running))
            {
                AddCard(Loc.F("func.salvage.running", Loc.T("mission." + running.Id), Loc.Duration(left)),
                    RewardText(running, building.level), string.Empty, string.Empty, Loc.T("func.salvage.send"), false, () => { });
                AddProgress(building.missionSeconds > 0f ? Mathf.Clamp01(1f - left / building.missionSeconds) : 1f);
                return;
            }

            UiKit.Label("SalvageTitle", _list, Loc.T("func.salvage.title"), 34, UiKit.Muted).Row(60);
            foreach (var mission in _catalog.SalvageMissions)
            {
                var id = mission.Id;
                AddCard(Loc.T("mission." + id), RewardText(mission, building.level),
                    Loc.F("func.salvage.time", Loc.Duration(mission.Seconds)), string.Empty, Loc.T("func.salvage.send"), true,
                    () => Send(new StartSalvageCommand { BuildingEntityId = key, MissionId = id }));
            }
        }

        private static string RewardText(SalvageMission mission, int level)
        {
            var rewards = string.Join("  ", BuildingFunctions.SalvageRewards(mission, level).Select(r => $"{r.Value} {Loc.Resource(r.Key.ToString())}"));
            var text = Loc.F("func.salvage.rewards", rewards);
            if (mission.HadaliteChancePercent > 0)
                text += "\n" + Loc.F("func.salvage.hadalite", mission.HadaliteChancePercent, mission.HadaliteMin, mission.HadaliteMax);
            return text;
        }

        // --- Research --------------------------------------------------------------------------------

        private void AddResearchEntryCard()
        {
            var active = _stateSync.View.ActiveResearch;
            var body = active.Count == 0
                ? Loc.T("func.research.idle")
                : string.Join("\n", active.Select(a => Loc.F("func.research.running", Loc.T("research.node." + a.NodeId), a.TargetLevel, Loc.Duration(Left(a.EndsAt)))));
            AddCard(Loc.T("func.research.title"), body, string.Empty, string.Empty, Loc.T("func.research.open"), true, OpenResearch);
        }

        public void OpenResearch()
        {
            _researchOpen = true;
            _expandOpen = false;
            _sheet.gameObject.SetActive(true);
            Render();
        }

        private void RenderResearch()
        {
            var center = Buildings().Where(b => b.definitionId == ResearchRules.CenterId).Select(b => b.level).DefaultIfEmpty(0).Max();
            var modifiers = Modifiers();
            var levels = _stateSync.View.ResearchLevels;
            var active = _stateSync.View.ActiveResearch;
            var rules = _catalog.Research;
            _title.text = Loc.T("research.title");
            _subtitle.text = $"{Loc.F("base.level", center)}   ·   {Loc.F("research.queue", active.Count, ResearchRules.Slots(rules, modifiers))}";

            var trees = _catalog.ResearchNodes.Select(n => n.Tree).Distinct().ToArray();
            if (Array.IndexOf(trees, _researchTree) < 0 && trees.Length > 0)
                _researchTree = trees[0];
            UiKit.Choice("Trees", _list, trees.Select(t => Loc.T("research.tree." + t)).ToArray(),
                () => Array.IndexOf(trees, _researchTree), i => { _researchTree = trees[i]; Render(); }).Row(100);

            foreach (var running in active)
            {
                var left = Left(running.EndsAt);
                var free = left <= _catalog.Progression.freeFinishSeconds;
                var nodeId = running.NodeId;
                AddCard(Loc.F("func.research.running", Loc.T("research.node." + nodeId), running.TargetLevel, Loc.Duration(left)), string.Empty,
                    string.Empty, free ? string.Empty : Loc.Reason(nameof(ResearchRejectReason.NotYetFree)), Loc.T("base.finish"), free,
                    () => Send(new FinishResearchCommand { NodeId = nodeId }));
                AddProgress(running.TotalSeconds > 0f ? Mathf.Clamp01(1f - left / running.TotalSeconds) : 1f);
            }

            var wallet = Wallet();
            var inProgress = new HashSet<string>(active.Select(a => a.NodeId));
            foreach (var node in _catalog.ResearchNodes.Where(n => n.Tree == _researchTree))
            {
                var current = levels.TryGetValue(node.Id, out var l) ? l : 0;
                var title = $"{Loc.T("research.node." + node.Id)}  <size=30>{Loc.F("research.level", current, node.MaxLevel)}</size>";
                if (current >= node.MaxLevel)
                {
                    AddCard(title, EffectText(node, current, current), string.Empty, string.Empty, Loc.T("research.max"), false, () => { });
                    continue;
                }

                var target = current + 1;
                var verdict = ResearchRules.Validate(_catalog, node.Id, levels, inProgress, center, modifiers, wallet);
                var reason = verdict switch
                {
                    ResearchRejectReason.None => string.Empty,
                    ResearchRejectReason.CenterLevelTooLow => Loc.F("research.needs.center", ResearchRules.CenterLevelFor(node, target, rules)),
                    ResearchRejectReason.PrerequisiteMissing => string.Join(", ", node.Requires
                        .Where(r => !levels.TryGetValue(r.Key, out var have) || have < r.Value)
                        .Select(r => Loc.F("research.needs.node", Loc.T("research.node." + r.Key), r.Value))),
                    _ => Loc.Reason(verdict.ToString())
                };
                var id = node.Id;
                AddCard(
                    title,
                    EffectText(node, current, target),
                    string.Empty,
                    reason,
                    Loc.T("research.start"),
                    verdict == ResearchRejectReason.None,
                    () => Send(new StartResearchCommand { NodeId = id }),
                    costs: ResearchRules.Cost(node, target, rules), wallet: wallet,
                    seconds: modifiers.ResearchSeconds(ResearchRules.BaseSeconds(node, target, rules)));
            }

            UiKit.Button("Back", _list, Loc.T("account.back"), () => { _researchOpen = false; Render(); }, UiKit.Field).Row(100)
                .GetComponentInChildren<UnityEngine.UI.Text>().color = UiKit.Text;
        }

        /// <summary>"Construction time -6% → -9%" style line for a node.</summary>
        private static string EffectText(ResearchNode node, int current, int target)
        {
            var colon = node.Effect.IndexOf(':');
            var kind = colon < 0 ? node.Effect : node.Effect.Substring(0, colon);
            var subject = colon < 0 ? string.Empty : node.Effect.Substring(colon + 1);
            var percent = kind is "buildTime" or "researchTime" or "production" or "capacity";

            string Value(int level)
            {
                var v = node.ValuePerLevel * level;
                var sign = v > 0 ? "+" : v < 0 ? "-" : string.Empty;
                return percent ? $"{sign}%{Math.Abs(v)}" : $"{sign}{Math.Abs(v)}";
            }

            var subjectName = kind switch
            {
                "production" => Loc.Resource(subject),
                "limit" => Loc.Building(subject),
                _ => string.Empty
            };
            var change = current == target ? Value(current) : $"{Value(current)} → {Value(target)}";
            return Loc.F("effect." + kind, change, subjectName);
        }

        // --- Commands and reports ---------------------------------------------------------------------

        private void Send(ICommand command)
        {
            switch (command)
            {
                case BoostBuildingCommand c: c.CommandId = Guid.NewGuid().ToString("N"); c.ClientSequence = ++_sequence; _commands.Dispatch(c); break;
                case OverdriveCommand c: c.CommandId = Guid.NewGuid().ToString("N"); c.ClientSequence = ++_sequence; _commands.Dispatch(c); break;
                case StartSalvageCommand c: c.CommandId = Guid.NewGuid().ToString("N"); c.ClientSequence = ++_sequence; _commands.Dispatch(c); break;
                case StartResearchCommand c: c.CommandId = Guid.NewGuid().ToString("N"); c.ClientSequence = ++_sequence; _commands.Dispatch(c); break;
                case FinishResearchCommand c: c.CommandId = Guid.NewGuid().ToString("N"); c.ClientSequence = ++_sequence; _commands.Dispatch(c); break;
            }
        }

        private static string FunctionToast(CommandType type) => type switch
        {
            CommandType.StartResearch => Loc.T("research.started"),
            CommandType.FinishResearch => Loc.T("research.done"),
            CommandType.BoostBuilding => Loc.T("func.boost.started"),
            CommandType.Overdrive => Loc.T("func.overdrive.started"),
            CommandType.StartSalvage => Loc.T("func.salvage.started"),
            _ => string.Empty
        };

        private void ShowSalvageReports()
        {
            while (_stateSync != null && _stateSync.View.TryTakeSalvageReport(out var report))
            {
                var rewards = ResourceAmountEntry.ToMap(report.Rewards)
                    .Where(p => p.Value > 0)
                    .Select(p => $"+{p.Value} {Loc.Resource(p.Key.ToString())}");
                Toast(Loc.F("func.salvage.done", string.Join("  ", rewards)), UiKit.Accent);
            }
        }
    }
}
