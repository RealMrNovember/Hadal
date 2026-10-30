using System;
using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using UnityEngine;

namespace Hadal.UI
{
    /// <summary>
    /// Texts, colours and small decisions of the Mobilization and Alliance Duel pages (docs/29 §4.3, §4.5), kept out of
    /// the view so EditMode tests can check them.
    /// </summary>
    public static class AllianceEventText
    {
        /// <summary>The card colour of a Mobilization tier (1 green … 4 gold).</summary>
        public static Color TierColor(int tier) => tier switch
        {
            MobilizationRules.Blue => new Color(0.24f, 0.5f, 0.86f, 1f),
            MobilizationRules.Purple => new Color(0.58f, 0.34f, 0.8f, 1f),
            MobilizationRules.Gold => new Color(0.92f, 0.68f, 0.18f, 1f),
            _ => new Color(0.3f, 0.68f, 0.36f, 1f)
        };

        /// <summary>The item-tile frame of a tier: common, rare, epic, legendary.</summary>
        public static string TierFrame(int tier) => tier switch
        {
            MobilizationRules.Blue => "rare",
            MobilizationRules.Purple => "epic",
            MobilizationRules.Gold => "legendary",
            _ => "common"
        };

        /// <summary>The icon of a task kind.</summary>
        public static string KindIcon(string kind) => kind switch
        {
            "gather" => "res_titanium",
            "train" => "g_train",
            "research" => "g_research",
            "build_power" => "queue_build",
            "monster_kill" => "g_attack",
            "heal" => "g_heal",
            "speedup_minutes" => "g_speed",
            "hero_level" => "nav_heroes",
            "help" => "left_help",
            "dive" => "event_war",
            _ => "quest"
        };

        /// <summary>"Gather 150,000 resources": the task as one line.</summary>
        public static string TaskText(MobilizationTaskDto task) =>
            task == null || string.IsNullOrEmpty(task.Kind) ? string.Empty : Loc.F("mob.kind." + task.Kind, task.Target.ToString("N0"));

        /// <summary>Why the player cannot take a task now (empty = they can).</summary>
        public static string TakeBlock(MobilizationDto m)
        {
            if (m == null || !m.InAlliance)
                return Loc.T("mob.noAlliance");
            if (m.Members < m.MinMembers)
                return Loc.F("mob.fewMembers", m.MinMembers, m.Members);
            if (m.Mine != null && !string.IsNullOrEmpty(m.Mine.Kind))
                return Loc.T("mob.holding");
            if (m.TasksLeftToday <= 0)
                return Loc.T("mob.noneLeft");
            return string.Empty;
        }

        /// <summary>The rank section's note: the reward is on its way by mail, came by mail, or there is none.</summary>
        public static string RankNote(EventDto e, EventPhase phase)
        {
            if (e.RankClaimed)
                return Loc.T("event.rankMailed");
            if (phase is EventPhase.Ended or EventPhase.Closed)
                return Loc.T(e.PaidOut ? "event.rankNone" : "event.rankSending");
            return e.MinRankPoints > 0 ? Loc.F("event.rankAtEndMailMin", e.MinRankPoints.ToString("N0")) : Loc.T("event.rankAtEndMail");
        }

        /// <summary>A side's name for the banner: the Abyss Echo by its own name, an alliance as "[TAG] Name".</summary>
        public static string SideName(DuelSideDto side) =>
            side == null ? "—" : side.AllianceId == AllianceDuelRules.Echo ? Loc.T("duel.echo") : $"[{side.Tag}] {side.Name}";

        /// <summary>The week result for the player's alliance as a label key.</summary>
        public static string ResultKey(int result) => result switch
        {
            AllianceDuelRules.Won => "duel.result.won",
            AllianceDuelRules.Lost => "duel.result.lost",
            AllianceDuelRules.Draw => "duel.result.draw",
            _ => "duel.result.running"
        };

        /// <summary>A day cell's colour: won green, lost red, tie grey, today gold, still to come pale.</summary>
        public static Color DayColor(int winner, bool today) => winner switch
        {
            AllianceDuelRules.First => new Color(0.36f, 0.72f, 0.42f, 1f),
            AllianceDuelRules.Second => new Color(0.84f, 0.38f, 0.34f, 1f),
            AllianceDuelRules.Tie => new Color(0.62f, 0.66f, 0.72f, 1f),
            _ => today ? new Color(0.98f, 0.82f, 0.4f, 1f) : new Color(0.84f, 0.88f, 0.93f, 1f)
        };

        /// <summary>
        /// The daily chests to show: every stage of today, and those of earlier days reached but not taken, as (day,
        /// stage) pairs, today first.
        /// </summary>
        public static List<(int Day, int Stage)> Chests(DuelDto duel, EventTierDto[] stages, int today)
        {
            var list = new List<(int, int)>();
            if (duel == null || stages == null)
                return list;
            var claimed = new HashSet<int>(duel.ClaimedChests ?? Array.Empty<int>());
            if (today >= 0)
            {
                for (var s = 0; s < stages.Length; s++)
                    list.Add((today, s));
            }

            var days = duel.MyDayPoints ?? Array.Empty<long>();
            for (var d = 0; d < Math.Min(today < 0 ? AllianceDuelRules.Days : today, days.Length); d++)
            {
                for (var s = 0; s < stages.Length; s++)
                {
                    if (days[d] >= stages[s].Points && !claimed.Contains(AllianceDuelRules.ChestKey(d, s)))
                        list.Add((d, s));
                }
            }

            return list;
        }
    }
}
