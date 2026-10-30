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
    /// <summary>Alliance polls, time-slot votes and duties on the client (docs/29 §2.3 and §5, roadmap §7 E2).</summary>
    public sealed class AlliancePollClientTests
    {
        private static readonly string[] Names = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private const long Hour = 3_600_000L;

        private static long Utc(int y, int m, int d, int h = 0, int min = 0) => new DateTimeOffset(y, m, d, h, min, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

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
        public void Votes_arrive_through_their_entity_and_the_commands_are_mapped()
        {
            var view = new ClientStateView();
            view.ResetSession();
            Apply(view, EntityIds.AlliancePolls, new AlliancePollsDto
            {
                Polls = new[]
                {
                    new AlliancePollDto
                    {
                        Id = "v1", Kind = AlliancePollRules.KindPoll, Question = "Q?", ClosesAtMs = Utc(2026, 11, 2),
                        Options = new[] { new AlliancePollOptionDto { Text = "a", Votes = 2, VoterNames = new[] { "Ana", "Ben" } }, new AlliancePollOptionDto { Text = "b" } }
                    }
                }
            });

            Assert.AreEqual(1, view.AlliancePollsVersion);
            Assert.AreEqual("v1", view.AlliancePolls.Polls[0].Id);
            Assert.AreEqual(new[] { "Ana", "Ben" }, view.AlliancePolls.Polls[0].Options[0].VoterNames);
            Assert.AreEqual(CommandType.CreateAlliancePoll, CommandDispatcher.ResolveCommandType(new CreateAlliancePollCommand()));
            Assert.AreEqual(CommandType.VoteAlliancePoll, CommandDispatcher.ResolveCommandType(new VoteAlliancePollCommand()));
            Assert.AreEqual(CommandType.CloseAlliancePoll, CommandDispatcher.ResolveCommandType(new CloseAlliancePollCommand()));
        }

        [Test]
        public void The_red_dot_shows_an_open_vote_the_player_has_not_answered()
        {
            var now = Utc(2026, 11, 1, 12);
            var open = new AlliancePollDto { ClosesAtMs = now + Hour };
            Assert.IsTrue(AlliancePollRules.AwaitsVote(open, now));
            open.MyVotes = new[] { 1 };
            Assert.IsFalse(AlliancePollRules.AwaitsVote(open, now));
            Assert.IsFalse(AlliancePollRules.AwaitsVote(new AlliancePollDto { ClosesAtMs = now - 1 }, now), "time is up");
            Assert.IsFalse(AlliancePollRules.AwaitsVote(new AlliancePollDto { ClosesAtMs = now + Hour, ClosedAtMs = now - 5 }, now), "closed early");
        }

        [Test]
        public void The_viewers_duty_comes_from_the_member_list()
        {
            var view = new ClientStateView();
            view.ResetSession();
            view.PlayerId = "me";
            Assert.AreEqual(string.Empty, view.MyRole());
            Apply(view, EntityIds.MyAlliance, new AllianceDto
            {
                Id = "a1",
                Members = new[] { new AllianceMemberDto { PlayerId = "me", Rank = 4, Role = AlliancePermissions.Logistics } }
            });
            Assert.AreEqual(AlliancePermissions.Logistics, view.MyRole());
            Assert.IsTrue(AlliancePermissions.CanBuild(view.MyRank(), view.MyRole()));
            Assert.IsFalse(AlliancePermissions.CanRunPolls(view.MyRank(), view.MyRole()));
        }

        [Test]
        public void A_new_poll_form_needs_a_question_and_two_answers()
        {
            var now = Utc(2026, 11, 1, 10);
            var draft = new PollDraft();
            draft.Reset(AlliancePollRules.KindPoll, now, 0);
            Assert.AreEqual(SocialRejectReason.InvalidPoll, draft.Problem(now));
            draft.Question = "  Raid tonight? ";
            draft.Options[0] = "Yes";
            draft.Options[1] = "No";
            Assert.AreEqual(SocialRejectReason.None, draft.Problem(now));

            for (var i = 0; i < 10; i++)
                draft.AddOption();
            Assert.AreEqual(AlliancePollRules.MaxOptions, draft.Options.Count);
            Assert.AreEqual(SocialRejectReason.InvalidPoll, draft.Problem(now), "empty extra answers");
            while (draft.CanRemoveOption)
                draft.RemoveOption(draft.Options.Count - 1);
            Assert.AreEqual(AlliancePollRules.MinOptions, draft.Options.Count);

            draft.StepHours(10);
            Assert.AreEqual(AlliancePollRules.MaxHours, draft.Hours);
            draft.StepHours(-10);
            Assert.AreEqual(AlliancePollRules.MinHours, draft.Hours);

            draft.Anonymous = true;
            var command = draft.Command("c1", 7);
            Assert.AreEqual(("c1", 7UL, AlliancePollRules.KindPoll, "Raid tonight?"), (command.CommandId, command.ClientSequence, command.Kind, command.Question));
            Assert.AreEqual(new[] { "Yes", "No" }, command.Options);
            Assert.IsTrue(command.Anonymous);
            Assert.IsEmpty(command.SlotsMs);
        }

        [Test]
        public void A_time_vote_offers_evening_slots_after_the_vote_in_the_phones_time()
        {
            var now = Utc(2026, 11, 1, 10); // Sunday 10:00 UTC = 13:00 at UTC+3
            var plus3 = 3 * Hour;
            var draft = new PollDraft();
            draft.Reset(AlliancePollRules.KindSlots, now, plus3);
            Assert.AreEqual(AlliancePollRules.SlotVoteHours, draft.Hours);
            Assert.AreEqual(new[] { Utc(2026, 11, 3, 17), Utc(2026, 11, 3, 18) }, draft.Slots.ToArray(), "20:00 and 21:00 at UTC+3, the day after tomorrow");
            Assert.AreEqual(SocialRejectReason.None, draft.Problem(now));

            // A slot moved before the vote ends is refused; one more lands an hour after the last.
            draft.ShiftSlot(0, -2 * PollDraft.DayMs);
            Assert.AreEqual(SocialRejectReason.InvalidPoll, draft.Problem(now));
            draft.ShiftSlot(0, 2 * PollDraft.DayMs + PollDraft.SlotStepMs);
            draft.AddOption();
            Assert.AreEqual(Utc(2026, 11, 3, 19), draft.Slots[2]);
            Assert.AreEqual(SocialRejectReason.None, draft.Problem(now));

            draft.StepPurpose();
            var command = draft.Command("c2", 1);
            Assert.AreEqual(AlliancePollRules.Purposes[1], command.Purpose);
            Assert.AreEqual(3, command.SlotsMs.Length);
            Assert.IsFalse(command.Anonymous);

            Assert.AreEqual("Tue 03.11 20:00 · 17:00 UTC", AlliancePollText.Slot(Utc(2026, 11, 3, 17), plus3, d => Names[d]));
            Assert.AreEqual("Mon 02.11 19:00 · 00:00 UTC", AlliancePollText.Slot(Utc(2026, 11, 3), -5 * Hour, d => Names[d]));
        }

        [Test]
        public void Results_read_as_votes_share_and_a_few_names()
        {
            Assert.AreEqual("3 · 60%", AlliancePollText.Share(3, 5));
            Assert.AreEqual("0 · 0%", AlliancePollText.Share(0, 0));
            Assert.AreEqual("Ana, Ben, Cem +2", AlliancePollText.Names(new[] { "Ana", "Ben", "Cem", "Dov", "Eda" }));
            Assert.AreEqual("Ana", AlliancePollText.Names(new[] { "Ana" }));
            Assert.AreEqual(string.Empty, AlliancePollText.Names(Array.Empty<string>()));
            Assert.AreEqual(2, AlliancePollRules.Winner(new[] { 1, 3, 3 }));
            Assert.AreEqual(0, AlliancePollRules.Winner(new[] { 0, 0 }));
        }

        [Test]
        public void Chat_notices_translate_keys_and_escape_player_text()
        {
            var previous = Loc.Current;
            try
            {
                Loc.Use("en");
                var args = SocialView.SystemArgs("alliance.poll.purpose.lure_trap|<b>Boss</b>|2");
                Assert.AreEqual("Lure Trap", args[0]);
                Assert.IsFalse(((string)args[1]).Contains("<b>"), "a question cannot carry rich text");
                Assert.AreEqual("2", args[2]);
                StringAssert.Contains("Lure Trap", Loc.F("chat.sys.slotsend", SocialView.SystemArgs("alliance.poll.purpose.lure_trap|2026-11-03 17:00 UTC|2|3")));
            }
            finally
            {
                Loc.Use(previous);
            }
        }

        private static readonly string[] Keys =
        {
            "alliance.tile.polls", "alliance.polls.intro", "alliance.polls.new", "alliance.poll.vote", "alliance.poll.change", "alliance.poll.close",
            "alliance.poll.kind.poll", "alliance.poll.kind.slots", "alliance.poll.slots.rule", "alliance.duty.pick", "alliance.duty.none",
            "alliance.duty.grants.none", "mail.alliance.leader.title"
        };

        [Test]
        public void Every_language_has_the_vote_texts_with_their_placeholders()
        {
            var placeholders = new Dictionary<string, int>
            {
                ["chat.sys.poll"] = 2, ["chat.sys.pollend"] = 4, ["chat.sys.pollnone"] = 1, ["chat.sys.slots"] = 2, ["chat.sys.slotsend"] = 4,
                ["chat.sys.slotsnone"] = 1, ["chat.sys.inactive"] = 3, ["mail.alliance.leader.body"] = 3, ["alliance.poll.ends"] = 1,
                ["alliance.poll.winner"] = 1, ["alliance.poll.fixed"] = 1, ["alliance.poll.voters"] = 1, ["alliance.poll.hoursValue"] = 1
            };
            foreach (var (code, _) in Loc.Languages)
            {
                var asset = Resources.Load<TextAsset>("Localization/" + code);
                Assert.IsNotNull(asset, code);
                var table = Loc.Parse(asset.text);
                var keys = Keys.Concat(placeholders.Keys)
                    .Concat(AlliancePollRules.Purposes.Select(p => "alliance.poll.purpose." + p))
                    .Concat(AlliancePermissions.Roles.Where(r => r.Length > 0).Select(r => "alliance.duty.grants." + r))
                    .Concat(new[] { "InvalidPoll", "UnknownPoll", "PollClosed", "TooManyPolls", "InvalidVote" }.Select(r => "reason." + r));
                foreach (var key in keys)
                    Assert.IsTrue(table.ContainsKey(key) && table[key].Length > 0, $"{code}: {key}");
                foreach (var (key, count) in placeholders)
                    for (var i = 0; i < count; i++)
                        StringAssert.Contains("{" + i + "}", table[key], $"{code}: {key}");
            }
        }
    }
}
