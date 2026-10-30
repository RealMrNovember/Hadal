using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using UnityEngine;
using UnityEngine.UI;

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

        /// <summary>Research, the command crew, VIP and alliance research, as the server computes them.</summary>
        private PlayerModifiers Modifiers() => PlayerBonuses.Of(_stateSync.View, _catalog);

        /// <summary>A sample trip for the propulsion tree's summary: 20 minutes at base speed.</summary>
        private const int SampleTripSeconds = 20 * 60;

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
                UiFrames.Note(_list, Loc.F("func.reactor.next", next, string.Join(", ", opens)), UiFrames.InkGood).Row(110);
        }

        // --- Drone Factory --------------------------------------------------------------------------

        private void AddSalvageCards(BuildingSaveEntry building)
        {
            var key = building.instanceKey;
            var left = Left(building.missionEndsAt);
            if (left > 0f && _catalog.TryGetSalvageMission(building.missionId, out var running))
            {
                UiFrames.Job(_list, Loc.T("mission." + running.Id), building.missionSeconds > 0f ? Mathf.Clamp01(1f - left / building.missionSeconds) : 1f,
                    Loc.Duration(left), "gold", buttons: false);
                UiFrames.Note(_list, RewardText(running, building.level)).Row(110);
                return;
            }

            UiFrames.Section(_list, Loc.T("func.salvage.title")).Row(60);
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

        private string _researchPick = string.Empty;

        /// <summary>
        /// The research tree (Last Asylum's): the trees as tabs, the running research as job cards, the nodes as a grid
        /// of cards with their level bar; the chosen node's effect, price and Research button sit in the footer.
        /// </summary>
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
            UiFrames.Tabs(_list, trees.Select(t => Loc.T("research.tree." + t)).ToArray(), Array.IndexOf(trees, _researchTree), i =>
            {
                _researchTree = trees[i];
                _researchPick = string.Empty;
                Render();
            }).Row(92);

            foreach (var running in active)
                AddResearchJob(running);

            // The propulsion tree opens with what every bonus adds up to (research, VIP, alliance) and what it means
            // for a trip, so each level is felt: "March speed +28% · a 20 min trip takes 15m 38s".
            if (_researchTree == "propulsion")
            {
                var total = Loc.F("research.march.total", modifiers.MarchSpeedPercent, Loc.Duration(modifiers.MarchSeconds(SampleTripSeconds)));
                if (modifiers.ReturnSpeedPercent > 0)
                    total += "\n" + Loc.F("research.march.return", modifiers.ReturnSpeedPercent);
                UiFrames.Note(_list, total, UiFrames.InkGood).Row(modifiers.ReturnSpeedPercent > 0 ? 130 : 96);
            }

            var wallet = Wallet();
            var inProgress = new HashSet<string>(active.Select(a => a.NodeId));
            var nodes = _catalog.ResearchNodes.Where(n => n.Tree == _researchTree).ToList();
            ResearchRejectReason Verdict(ResearchNode n) => ResearchRules.Validate(_catalog, n.Id, levels, inProgress, center, modifiers, wallet);
            int Level(ResearchNode n) => levels.TryGetValue(n.Id, out var l) ? l : 0;
            static bool Locked(ResearchRejectReason v) => v is ResearchRejectReason.CenterLevelTooLow or ResearchRejectReason.PrerequisiteMissing;

            if (nodes.All(n => n.Id != _researchPick))
                _researchPick = (nodes.FirstOrDefault(n => Level(n) < n.MaxLevel && !Locked(Verdict(n))) ?? nodes.FirstOrDefault())?.Id ?? string.Empty;

            var grid = UiFrames.Grid(_list, 3, 0.78f, 12f);
            grid.GetComponent<GridLayoutGroup>().padding = new RectOffset(0, 0, 4, 4);
            foreach (var node in nodes)
            {
                var current = Level(node);
                var maxed = current >= node.MaxLevel;
                var running = active.Where(a => a.NodeId == node.Id).Select(a => (float?)Left(a.EndsAt)).FirstOrDefault();
                var locked = !maxed && running == null && Locked(Verdict(node));
                var line = maxed ? Loc.T("research.max") : running != null ? Loc.Duration(running.Value) : Loc.F("research.level", current, node.MaxLevel);
                var color = maxed ? UiFrames.InkGood : locked ? UiFrames.InkBad : UiFrames.InkSoft;
                var id = node.Id;
                UiFrames.Tile(grid, ResearchIcon(node), Loc.T("research.node." + id), line, color, () =>
                {
                    _researchPick = id;
                    Render();
                }, id == _researchPick, locked, progress: node.MaxLevel > 0 ? current / (float)node.MaxLevel : 0f);
            }

            var back = Loc.T("account.back");
            void Back()
            {
                _researchOpen = false;
                Render();
            }

            var pick = nodes.FirstOrDefault(n => n.Id == _researchPick);
            if (pick == null)
            {
                AddFooter(null, null, null, null, -1f, string.Empty, back, null, true, Back, new Color(0.95f, 0.97f, 1f, 1f));
                return;
            }

            var level = Level(pick);
            var heading = $"{Loc.T("research.node." + pick.Id)}   {Loc.F("research.level", level, pick.MaxLevel)}";
            if (level >= pick.MaxLevel)
            {
                AddFooter(heading, EffectText(pick, level, level), null, null, -1f, string.Empty, Loc.T("research.max"), null, false, () => { }, null, back, Back);
                return;
            }

            var target = level + 1;
            var verdict = Verdict(pick);
            var reason = verdict switch
            {
                ResearchRejectReason.None => string.Empty,
                ResearchRejectReason.CenterLevelTooLow => Loc.F("research.needs.center", ResearchRules.CenterLevelFor(pick, target, rules)),
                ResearchRejectReason.PrerequisiteMissing => string.Join(", ", pick.Requires
                    .Where(r => !levels.TryGetValue(r.Key, out var have) || have < r.Value)
                    .Select(r => Loc.F("research.needs.node", Loc.T("research.node." + r.Key), r.Value))),
                _ => Loc.Reason(verdict.ToString())
            };
            var pickId = pick.Id;
            AddFooter(heading, EffectText(pick, level, target), ResearchRules.Cost(pick, target, rules), wallet,
                modifiers.ResearchSeconds(ResearchRules.BaseSeconds(pick, target, rules)), reason, Loc.T("research.start"), null,
                verdict == ResearchRejectReason.None, () => Send(new StartResearchCommand { NodeId = pickId }), null, back, Back);
        }

        /// <summary>A running research: the bar, Ask help, then Speed up or Finish Free.</summary>
        private void AddResearchJob(ActiveResearchView running)
        {
            var left = Left(running.EndsAt);
            var free = left <= _catalog.Progression.freeFinishSeconds;
            var nodeId = running.NodeId;
            var name = Loc.T("research.node." + nodeId);
            var max = _catalog.ResearchNodes.FirstOrDefault(n => n.Id == nodeId)?.MaxLevel ?? running.TargetLevel;
            var job = UiFrames.Job(_list, $"{name}  →  {Loc.F("research.level", running.TargetLevel, max)}",
                running.TotalSeconds > 0f ? Mathf.Clamp01(1f - left / running.TotalSeconds) : 1f, Loc.Duration(left), "blue");

            // Alliance mates can shave time off a running research too (docs/25 §4).
            if (_stateSync.View.MyAlliance != null && !free)
            {
                var helpKey = "research:" + nodeId + ":" + running.TargetLevel;
                var asked = _helpRequested.Contains(helpKey);
                var help = UiKit.Button("Help", job.Buttons, asked ? Loc.T("help.asked") : Loc.T("help.ask"),
                    () => AskJobHelp(HelpKinds.Research, nodeId, helpKey), UiKit.Ok, 32);
                help.interactable = !asked;
            }

            if (free)
                UiKit.Button("Finish", job.Buttons, Loc.T("base.finish"), () => Send(new FinishResearchCommand { NodeId = nodeId }), UiKit.Ok, 32);
            else
                UiKit.Button("Speed", job.Buttons, Loc.T("base.speedup"), () => SpeedupWindow.Open("research:" + nodeId, SpeedupKind.Research, name, () =>
                {
                    var r = _stateSync.View.ActiveResearch.FirstOrDefault(a => a.NodeId == nodeId);
                    if (string.IsNullOrEmpty(r.NodeId))
                        return null;
                    var end = _stateSync.View.ServerNowMs + (long)(Left(r.EndsAt) * 1000f);
                    return (end - (long)(r.TotalSeconds * 1000f), end);
                }), UiKit.Primary, 32);
        }

        /// <summary>A node's card icon: the resource it raises, the building it allows more of, or its field.</summary>
        private string ResearchIcon(ResearchNode node)
        {
            var colon = node.Effect.IndexOf(':');
            var kind = colon < 0 ? node.Effect : node.Effect.Substring(0, colon);
            var subject = colon < 0 ? string.Empty : node.Effect.Substring(colon + 1);
            if (kind == "limit" && _catalog.TryGetBuilding(subject, out var building))
                return BuildingIcon(building);
            return kind switch
            {
                "production" => "res_" + subject.ToLowerInvariant(),
                "capacity" => "item_cap_titanium",
                "buildTime" or "builderSlots" or "limit" => "queue_build",
                "marchSpeed" or "returnSpeed" => "item_speed_march",
                _ => "left_research"
            };
        }

        /// <summary>"Construction time -6% → -9%" style line for a node.</summary>
        private static string EffectText(ResearchNode node, int current, int target)
        {
            var colon = node.Effect.IndexOf(':');
            var kind = colon < 0 ? node.Effect : node.Effect.Substring(0, colon);
            var subject = colon < 0 ? string.Empty : node.Effect.Substring(colon + 1);
            var percent = kind is "buildTime" or "researchTime" or "production" or "capacity" or "marchSpeed" or "returnSpeed";

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
