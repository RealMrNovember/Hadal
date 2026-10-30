using System;
using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Tests.EditMode
{
    /// <summary>Mobilization, the Alliance Duel and event rewards by mail on the client (docs/29 §4.3, §4.5, roadmap §7).</summary>
    public sealed class AllianceEventClientTests
    {
        private static void Apply(ClientStateView view, string entityId, object payload)
        {
            var serializer = new ProtobufNetNetworkSerializer();
            ClientStateViewMutator.ApplyDelta(view, new StateDelta
            {
                ServerTick = 2,
                Changes = new List<EntityChangeDto> { new() { EntityId = entityId, Kind = EntityChangeKind.Update, Payload = serializer.Serialize(payload) } }
            }, new NetworkSerializationLayer(), new List<CommandAckDto>());
        }

        [Test]
        public void Pool_and_duel_arrive_inside_the_event_list_and_the_command_is_mapped()
        {
            var view = new ClientStateView();
            view.ResetSession();
            Apply(view, EntityIds.Events, new EventListDto
            {
                Events = new[]
                {
                    new EventDto
                    {
                        Id = "m", Template = EventTemplates.Mobilization, PaidOut = true,
                        Mobilization = new MobilizationDto
                        {
                            InAlliance = true, Members = 16, MinMembers = 15, TasksLeftToday = 3, TasksPerDay = 8,
                            Pool = new[] { new MobilizationTaskDto { Kind = "gather", Tier = 4, Target = 750_000, Points = 560, Slot = 0 }, new MobilizationTaskDto { Slot = 1, ReadyAtMs = 99 } },
                            Mine = new MobilizationTaskDto { Kind = "train", Tier = 2, Target = 120, Points = 200, Slot = -1, Progress = 40 }
                        }
                    },
                    new EventDto
                    {
                        Id = "d", Template = EventTemplates.AllianceDuel,
                        Duel = new DuelDto
                        {
                            InDuel = true, Mine = new DuelSideDto { AllianceId = "a", Tag = "AAA", Wins = 3, DayPoints = new long[] { 5, 0, 0, 0, 0, 0 } },
                            Rival = new DuelSideDto { AllianceId = AllianceDuelRules.Echo, Wins = 2 }, DayWinners = new[] { 1, 2, 0, 0, 0, 0 },
                            History = new[] { new DuelHistoryDto { RivalTag = "BBB", MyWins = 9, RivalWins = 4, Result = AllianceDuelRules.Won } }
                        }
                    }
                }
            });

            var m = view.Events.Events.Single(e => e.Id == "m");
            Assert.IsTrue(m.PaidOut);
            Assert.AreEqual(2, m.Mobilization.Pool.Length);
            Assert.AreEqual(40, m.Mobilization.Mine.Progress);
            var d = view.Events.Events.Single(e => e.Id == "d").Duel;
            Assert.AreEqual(AllianceDuelRules.Echo, d.Rival.AllianceId);
            Assert.AreEqual(9, d.History[0].MyWins);
            Assert.AreEqual(CommandType.MobilizationTask, CommandDispatcher.ResolveCommandType(new MobilizationTaskCommand()));
        }

        [Test]
        public void Taking_a_task_is_blocked_with_a_reason()
        {
            Loc.Use("en");
            var ready = new MobilizationDto { InAlliance = true, Members = 15, MinMembers = 15, TasksLeftToday = 1 };
            Assert.AreEqual(string.Empty, AllianceEventText.TakeBlock(ready));
            Assert.AreEqual(Loc.T("mob.noAlliance"), AllianceEventText.TakeBlock(new MobilizationDto()));
            StringAssert.Contains("15", AllianceEventText.TakeBlock(new MobilizationDto { InAlliance = true, Members = 9, MinMembers = 15, TasksLeftToday = 1 }));
            StringAssert.Contains("9", AllianceEventText.TakeBlock(new MobilizationDto { InAlliance = true, Members = 9, MinMembers = 15, TasksLeftToday = 1 }));
            Assert.AreEqual(Loc.T("mob.holding"), AllianceEventText.TakeBlock(new MobilizationDto
                { InAlliance = true, Members = 15, MinMembers = 15, TasksLeftToday = 1, Mine = new MobilizationTaskDto { Kind = "heal" } }));
            Assert.AreEqual(Loc.T("mob.noneLeft"), AllianceEventText.TakeBlock(new MobilizationDto { InAlliance = true, Members = 15, MinMembers = 15 }));

            Assert.AreEqual("Gather 500 resources", AllianceEventText.TaskText(new MobilizationTaskDto { Kind = "gather", Target = 500 }));
            Assert.AreEqual("legendary", AllianceEventText.TierFrame(MobilizationRules.Gold));
            Assert.AreNotEqual(AllianceEventText.TierColor(MobilizationRules.Green), AllianceEventText.TierColor(MobilizationRules.Gold));
            foreach (var kind in MobilizationRules.Kinds)
                Assert.AreNotEqual("quest", AllianceEventText.KindIcon(kind.Id), kind.Id);
        }

        [Test]
        public void Duel_chests_show_today_and_earlier_days_still_waiting()
        {
            var stages = new[] { new EventTierDto { Points = 20 }, new EventTierDto { Points = 100 } };
            var duel = new DuelDto { InDuel = true, MyDayPoints = new long[] { 30, 150, 5, 0, 0, 0 }, ClaimedChests = new[] { AllianceDuelRules.ChestKey(1, 0) } };
            var chests = AllianceEventText.Chests(duel, stages, 2);
            // Today's two stages first, then day 1's first chest and day 2's both chests minus the taken one.
            CollectionAssert.AreEqual(new[] { (2, 0), (2, 1), (0, 0), (1, 1) }, chests);
            Assert.AreEqual(2, AllianceDuelRules.ChestsWaiting(duel, stages));

            var live = new EventDto
            {
                Template = EventTemplates.AllianceDuel, StartsAtMs = 0, EndsAtMs = 1000, ClaimUntilMs = 2000, Stages = stages, Duel = duel
            };
            Assert.IsTrue(EventRules.HasClaim(live, 500));
            Assert.IsFalse(EventRules.HasClaim(live, 1500)); // over: the rest comes by mail
        }

        [Test]
        public void Rank_notes_and_duel_names_read_right()
        {
            Loc.Use("en");
            var e = new EventDto { MinRankPoints = 600 };
            StringAssert.Contains("600", AllianceEventText.RankNote(e, EventPhase.Live));
            Assert.AreEqual(Loc.T("event.rankSending"), AllianceEventText.RankNote(e, EventPhase.Ended));
            e.PaidOut = true;
            Assert.AreEqual(Loc.T("event.rankNone"), AllianceEventText.RankNote(e, EventPhase.Ended));
            e.RankClaimed = true;
            Assert.AreEqual(Loc.T("event.rankMailed"), AllianceEventText.RankNote(e, EventPhase.Ended));

            Assert.AreEqual(Loc.T("duel.echo"), AllianceEventText.SideName(new DuelSideDto { AllianceId = AllianceDuelRules.Echo }));
            Assert.AreEqual("[AAA] Abyss", AllianceEventText.SideName(new DuelSideDto { AllianceId = "x", Tag = "AAA", Name = "Abyss" }));
            Assert.AreEqual("duel.result.won", AllianceEventText.ResultKey(AllianceDuelRules.Won));
            Assert.AreNotEqual(AllianceEventText.DayColor(AllianceDuelRules.First, false), AllianceEventText.DayColor(AllianceDuelRules.Second, false));

            // A reward mail reads in the player's language, the default event's name translated too.
            Assert.AreEqual("Depth Race is over. You finished #1 with 500 points. Your reward is attached.",
                MailText.Show("loc:mail.event.rank.body|event.name.depth-race|1|500", "event"));
            Assert.AreEqual("This week's duel against Abyss Echo ended 9–4. You scored 230 points for your alliance. Any reward is attached.",
                MailText.Show("loc:mail.duel.result.body|duel.echo|9|4|230", "event"));
        }

        [Test]
        public void Every_language_has_the_alliance_event_texts_with_their_placeholders()
        {
            var placeholders = new Dictionary<string, int>
            {
                ["mail.event.rank.title"] = 1, ["mail.event.rank.body"] = 3, ["mail.event.allianceRank.body"] = 3, ["mail.event.stages.title"] = 1,
                ["mail.event.stages.body"] = 2, ["mail.duel.chests.body"] = 1, ["mail.duel.result.body"] = 4, ["event.rankAtEndMailMin"] = 1,
                ["mob.fewMembers"] = 2, ["mob.left"] = 2, ["mob.done"] = 1, ["mob.points"] = 1, ["mob.refresh"] = 1, ["mob.refreshIn"] = 1,
                ["mob.refilling"] = 1, ["duel.firstTo"] = 1, ["duel.today"] = 2, ["duel.myToday"] = 1, ["duel.week"] = 1
            };
            var plain = new[]
            {
                "event.template.mobilization", "event.template.alliance_duel", "event.byMail", "event.rankMailed", "event.rankSending", "event.rankAtEndMail",
                "event.notRunning", "mob.myTask", "mob.takeOne", "mob.noAlliance", "mob.holding", "mob.noneLeft", "mob.pool", "mob.drop", "mob.take",
                "duel.echo", "duel.pairingSoon", "duel.notIn", "duel.resultMail", "duel.chests", "duel.history", "mail.duel.won.title", "mail.duel.lost.title",
                "mail.duel.draw.title", "alliance.tile.duel", "alliance.tile.mobilization"
            }
                .Concat(MobilizationRules.Kinds.Select(k => "mob.kind." + k.Id))
                .Concat(MobilizationRules.TierNames.Where(t => t.Length > 0).Select(t => "mob.tier." + t))
                .Concat(AllianceDuelRules.Themes.Select(t => "duel.theme." + t))
                .Concat(new[] { "won", "lost", "draw", "running" }.Select(r => "duel.result." + r))
                .Concat(new[] { "depth-race", "alliance-week", "login-week", "mobilization", "alliance-duel" }.Select(s => "event.name." + s))
                .Concat(new[] { "TaskHeld", "NoTasksLeft", "TaskGone", "NoTask", "NoRefreshesLeft", "RefreshCooldown", "SentByMail", "NotInAlliance", "TooFewForEvent" }
                    .Select(r => "reason." + r));
            foreach (var (code, _) in Loc.Languages)
            {
                var asset = Resources.Load<TextAsset>("Localization/" + code);
                Assert.IsNotNull(asset, code);
                var table = Loc.Parse(asset.text);
                foreach (var key in plain.Concat(placeholders.Keys))
                    Assert.IsTrue(table.ContainsKey(key) && table[key].Length > 0, $"{code}: {key}");
                foreach (var (key, count) in placeholders)
                    for (var i = 0; i < count; i++)
                        StringAssert.Contains("{" + i + "}", table[key], $"{code}: {key}");
            }
        }
    }
}
