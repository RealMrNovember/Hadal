using Hadal.Game.Persistence;
using Hadal.Game.Social;
using Hadal.Game.World;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Duty permissions, polls, event time-slot votes and the inactive leader's handover (docs/29 §2.3 and §5, roadmap §7 E2).
    /// </summary>
    public sealed class AlliancePollTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemorySocialStore _store = new();
        private readonly GameWorld _world;

        public AlliancePollTests()
        {
            _world = World(_store);
        }

        private GameWorld World(InMemorySocialStore store) => new(new CatalogRuntime(TestSupport.Catalog), new InMemoryPlayerRepository(), store,
            new InMemoryWorldStore(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance);

        private sealed record Player(PlayerSession S, RecordingConnection C);

        private async Task<Player> Connect(GameWorld world, string identity)
        {
            var connection = new RecordingConnection();
            var session = await world.ConnectAsync(identity, connection, default, identity);
            foreach (var type in new[] { ResourceType.Titanium, ResourceType.Biomass })
                session.StateForTests.Wallet[type] = 1_000_000;
            return new Player(session, connection);
        }

        private static void Send<T>(GameWorld world, Player p, T command, CommandType type) where T : ICommand =>
            world.HandleCommand(p.S, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private void Send<T>(Player p, T command, CommandType type) where T : ICommand => Send(_world, p, command, type);

        private static string Id() => Guid.NewGuid().ToString("N");

        private static CommandAckDto LastAck(Player p) =>
            p.C.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static string? Refusal(Player p) => LastAck(p).Result == CommandResultCode.Accepted ? null : LastAck(p).Reason;

        private static AllianceDto Mine(Player p) =>
            TestSupport.Serializer.Deserialize<AllianceDto>(p.C.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.MyAlliance && c.Kind != EntityChangeKind.Delete).Payload);

        private static AlliancePollDto[] Polls(Player p) =>
            TestSupport.Serializer.Deserialize<AlliancePollsDto>(p.C.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.AlliancePolls).Payload).Polls;

        /// <summary>A leader and <paramref name="members"/> divers (R2, so they may be made officers).</summary>
        private async Task<(Player Leader, List<Player> Members, string AllianceId)> Alliance(GameWorld world, string name, int members)
        {
            var leader = await Connect(world, name + "-l");
            Send(world, leader, new CreateAllianceCommand { CommandId = Id(), Name = "House " + name, Tag = name[..4].ToUpperInvariant() }, CommandType.CreateAlliance);
            Assert.Null(Refusal(leader));
            var id = Mine(leader).Id;
            var list = new List<Player>();
            for (var i = 0; i < members; i++)
            {
                var m = await Connect(world, $"{name}-{i}");
                Send(world, m, new JoinAllianceCommand { CommandId = Id(), AllianceId = id }, CommandType.JoinAlliance);
                Assert.Null(Refusal(m));
                list.Add(m);
            }

            return (leader, list, id);
        }

        private Task<(Player Leader, List<Player> Members, string AllianceId)> Alliance(string name, int members) => Alliance(_world, name, members);

        private static void Officer(GameWorld world, Player leader, Player member, string duty)
        {
            Send(world, leader, new SetMemberRankCommand { CommandId = Id(), MemberId = member.S.PublicId, Rank = AlliancePermissions.Officer, Role = duty },
                CommandType.SetMemberRank);
            Assert.Null(Refusal(leader));
        }

        private static CreateAlliancePollCommand Poll(string question, params string[] options) => new()
        {
            CommandId = Id(), Kind = AlliancePollRules.KindPoll, Question = question, Options = options, Hours = 24
        };

        private CreateAlliancePollCommand Slots(params long[] hoursAhead) => new()
        {
            CommandId = Id(), Kind = AlliancePollRules.KindSlots, Purpose = "lure_trap", Hours = 24,
            SlotsMs = hoursAhead.Select(h => AlliancePollRules.RoundSlot(_clock.UtcNow.AddHours(h).ToUnixTimeMilliseconds())).ToArray()
        };

        private void Vote(Player p, string pollId, params int[] choices) =>
            Send(p, new VoteAlliancePollCommand { CommandId = Id(), PollId = pollId, Choices = choices }, CommandType.VoteAlliancePoll);

        private List<string> AllianceChat(GameWorld world, string allianceId) =>
            world.Social.RecentChat("alliance:" + allianceId).Where(l => l.IsSystem).Select(l => l.Text).ToList();

        // --- Duties ------------------------------------------------------------------------------

        [Fact]
        public void The_leader_holds_every_duty_and_an_officer_only_their_own()
        {
            foreach (var duty in AlliancePermissions.Roles.Where(r => r.Length > 0))
            {
                Assert.True(AlliancePermissions.HasDuty(AlliancePermissions.Leader, string.Empty, duty));
                Assert.True(AlliancePermissions.HasDuty(AlliancePermissions.Officer, duty, duty));
                Assert.False(AlliancePermissions.HasDuty(AlliancePermissions.Officer, string.Empty, duty));
                Assert.False(AlliancePermissions.HasDuty(AlliancePermissions.Veteran, duty, duty)); // a stale duty tag below R4 grants nothing
                foreach (var other in AlliancePermissions.Roles.Where(r => r.Length > 0 && r != duty))
                    Assert.False(AlliancePermissions.HasDuty(AlliancePermissions.Officer, other, duty));
            }

            Assert.True(AlliancePermissions.CanRunEvents(AlliancePermissions.Officer, AlliancePermissions.War));
            Assert.True(AlliancePermissions.CanBuild(AlliancePermissions.Officer, AlliancePermissions.Logistics));
            Assert.True(AlliancePermissions.CanRunPolls(AlliancePermissions.Officer, AlliancePermissions.Diplomacy));
            Assert.True(AlliancePermissions.CanEditAnnouncement(AlliancePermissions.Officer, AlliancePermissions.Diplomacy));
            Assert.True(AlliancePermissions.CanMarkTargets(AlliancePermissions.Officer, AlliancePermissions.Scout));
            Assert.False(AlliancePermissions.CanBuild(AlliancePermissions.Officer, AlliancePermissions.War));
            Assert.True(AlliancePermissions.CanSetResearchFocus(AlliancePermissions.Officer));
            Assert.False(AlliancePermissions.CanSetResearchFocus(AlliancePermissions.Veteran));
        }

        [Fact]
        public async Task Polls_need_diplomacy_and_time_votes_need_war()
        {
            var (leader, m, _) = await Alliance("duty", 3);
            Officer(_world, leader, m[0], AlliancePermissions.Diplomacy);
            Officer(_world, leader, m[1], AlliancePermissions.War);

            Send(m[2], Poll("Q?", "a", "b"), CommandType.CreateAlliancePoll);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), Refusal(m[2]));
            Send(m[1], Poll("Q?", "a", "b"), CommandType.CreateAlliancePoll);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), Refusal(m[1]));
            Send(m[0], Slots(30, 40), CommandType.CreateAlliancePoll);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), Refusal(m[0]));

            Send(m[0], Poll("Q?", "a", "b"), CommandType.CreateAlliancePoll);
            Assert.Null(Refusal(m[0]));
            Send(m[1], Slots(30, 40), CommandType.CreateAlliancePoll);
            Assert.Null(Refusal(m[1]));
            Send(leader, Poll("Leader?", "a", "b"), CommandType.CreateAlliancePoll);
            Assert.Null(Refusal(leader));
            Assert.Equal(3, Polls(m[2]).Length);
        }

        // --- Polls -------------------------------------------------------------------------------

        [Fact]
        public async Task Bad_polls_are_refused_and_open_ones_are_capped()
        {
            var (leader, _, _) = await Alliance("rule", 0);
            var refused = new[]
            {
                Poll("Q?", "only one"),
                Poll("Q?", "a", "b", "c", "d", "e", "f", "g"),
                Poll("", "a", "b"),
                Poll("Q?", "same", "SAME"),
                Poll("Q?", "a", " "),
                new CreateAlliancePollCommand { CommandId = Id(), Kind = AlliancePollRules.KindPoll, Question = "Q", Options = new[] { "a", "b" }, Hours = 73 },
                new CreateAlliancePollCommand { CommandId = Id(), Kind = "referendum", Question = "Q", Options = new[] { "a", "b" } }
            };
            foreach (var poll in refused)
            {
                Send(leader, poll, CommandType.CreateAlliancePoll);
                Assert.Equal(nameof(SocialRejectReason.InvalidPoll), Refusal(leader));
            }

            for (var i = 0; i < AlliancePollRules.MaxOpen; i++)
            {
                Send(leader, Poll("Q" + i, "a", "b"), CommandType.CreateAlliancePoll);
                Assert.Null(Refusal(leader));
            }

            Send(leader, Poll("One too many", "a", "b"), CommandType.CreateAlliancePoll);
            Assert.Equal(nameof(SocialRejectReason.TooManyPolls), Refusal(leader));
        }

        [Fact]
        public async Task Members_vote_once_may_change_it_and_see_live_results_until_the_poll_closes_by_time()
        {
            var (leader, m, allianceId) = await Alliance("vote", 3);
            Send(leader, Poll("Raid | when?", "Tonight", "Tomorrow", "Never"), CommandType.CreateAlliancePoll);
            var pollId = Polls(leader).Single().Id;
            Assert.Contains(AllianceChat(_world, allianceId), t => t.StartsWith("sys.poll|", StringComparison.Ordinal));

            // A single-choice poll takes exactly one known option.
            Vote(m[0], pollId, 0, 1);
            Assert.Equal(nameof(SocialRejectReason.InvalidVote), Refusal(m[0]));
            Vote(m[0], pollId, 3);
            Assert.Equal(nameof(SocialRejectReason.InvalidVote), Refusal(m[0]));
            Vote(m[0], pollId);
            Assert.Equal(nameof(SocialRejectReason.InvalidVote), Refusal(m[0]));

            Vote(m[0], pollId, 2);
            Vote(m[1], pollId, 1);
            Vote(m[2], pollId, 1);
            Assert.Null(Refusal(m[2]));
            // The latest ballot counts: one vote per member.
            Vote(m[0], pollId, 1);
            var live = Polls(leader).Single();
            Assert.Equal(new[] { 0, 3, 0 }, live.Options.Select(o => o.Votes));
            Assert.Equal(3, live.Voters);
            Assert.Empty(live.MyVotes);
            Assert.Equal(new[] { 1 }, Polls(m[0]).Single().MyVotes);
            Assert.Equal(3, live.Options[1].VoterNames.Length); // not anonymous: names are shown
            Assert.True(AlliancePollRules.AwaitsVote(live, _clock.UtcNow.ToUnixTimeMilliseconds()));
            Assert.Equal("Raid / when?", live.Question); // the chat separator cannot be smuggled in

            _clock.Advance(TimeSpan.FromHours(24) + TimeSpan.FromMinutes(1));
            _world.Tick();
            var closed = Polls(m[1]).Single();
            Assert.True(closed.ClosedAtMs > 0);
            Assert.Equal(AlliancePollRules.ClosedByTime, closed.ClosedBy);
            Assert.Equal(2, closed.Winner);
            Assert.Contains("sys.pollend|Raid / when?|Tomorrow|3|3", AllianceChat(_world, allianceId));

            Vote(m[1], pollId, 0);
            Assert.Equal(nameof(SocialRejectReason.PollClosed), Refusal(m[1]));
        }

        [Fact]
        public async Task Anonymous_multiple_choice_polls_hide_names_and_count_every_choice()
        {
            var (leader, m, _) = await Alliance("anon", 2);
            var poll = Poll("Which days?", "Mon", "Tue", "Wed");
            poll.Multiple = true;
            poll.Anonymous = true;
            Send(leader, poll, CommandType.CreateAlliancePoll);
            var pollId = Polls(leader).Single().Id;
            Vote(m[0], pollId, 0, 2);
            Assert.Null(Refusal(m[0]));
            Vote(m[1], pollId, 2, 2);
            Assert.Equal(nameof(SocialRejectReason.InvalidVote), Refusal(m[1]));
            Vote(m[1], pollId, 2);

            var seen = Polls(leader).Single();
            Assert.Equal(new[] { 1, 0, 2 }, seen.Options.Select(o => o.Votes));
            Assert.All(seen.Options, o => Assert.Empty(o.VoterNames));
            Assert.Equal(100, AlliancePollRules.Percent(seen.Options[2].Votes, seen.Voters));
        }

        [Fact]
        public async Task The_creator_or_a_diplomat_closes_early_and_a_poll_without_votes_has_no_winner()
        {
            var (leader, m, allianceId) = await Alliance("shut", 2);
            Officer(_world, leader, m[0], AlliancePermissions.Diplomacy);
            Send(m[0], Poll("Early?", "yes", "no"), CommandType.CreateAlliancePoll);
            var pollId = Polls(m[0]).Single().Id;

            Send(m[1], new CloseAlliancePollCommand { CommandId = Id(), PollId = pollId }, CommandType.CloseAlliancePoll);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), Refusal(m[1]));
            Send(m[0], new CloseAlliancePollCommand { CommandId = Id(), PollId = pollId }, CommandType.CloseAlliancePoll);
            Assert.Null(Refusal(m[0]));

            var closed = Polls(m[1]).Single();
            Assert.Equal(AlliancePollRules.ClosedByOfficer, closed.ClosedBy);
            Assert.Equal(0, closed.Winner);
            Assert.Contains("sys.pollnone|Early?", AllianceChat(_world, allianceId));
            Send(m[0], new CloseAlliancePollCommand { CommandId = Id(), PollId = pollId }, CommandType.CloseAlliancePoll);
            Assert.Equal(nameof(SocialRejectReason.PollClosed), Refusal(m[0]));
        }

        [Fact]
        public async Task Only_the_last_closed_polls_are_kept()
        {
            var (leader, _, _) = await Alliance("hist", 0);
            for (var i = 0; i < AlliancePollRules.History + 2; i++)
            {
                Send(leader, Poll("Q" + i, "a", "b"), CommandType.CreateAlliancePoll);
                var id = Polls(leader).First(p => p.ClosedAtMs == 0).Id;
                _clock.Advance(TimeSpan.FromMinutes(1));
                Send(leader, new CloseAlliancePollCommand { CommandId = Id(), PollId = id }, CommandType.CloseAlliancePoll);
            }

            var kept = Polls(leader);
            Assert.Equal(AlliancePollRules.History, kept.Length);
            Assert.Equal("Q" + (AlliancePollRules.History + 1), kept[0].Question); // newest first
            await _world.Social.DrainAsyncForTests();
            Assert.Equal(AlliancePollRules.History, (await _store.LoadPollsAsync(default)).Count);
        }

        [Fact]
        public async Task Votes_survive_a_restart_and_close_on_time_afterwards()
        {
            var store = new InMemorySocialStore();
            var first = World(store);
            await first.Social.InitializeAsync(default);
            var (leader, m, allianceId) = await Alliance(first, "rest", 2);
            Send(first, leader, Poll("Survive?", "yes", "no"), CommandType.CreateAlliancePoll);
            var pollId = Polls(leader).Single().Id;
            Send(first, m[0], new VoteAlliancePollCommand { CommandId = Id(), PollId = pollId, Choices = new[] { 1 } }, CommandType.VoteAlliancePoll);
            Send(first, m[1], new VoteAlliancePollCommand { CommandId = Id(), PollId = pollId, Choices = new[] { 1 } }, CommandType.VoteAlliancePoll);
            await first.Social.DrainAsyncForTests();

            var second = World(store);
            await second.Social.InitializeAsync(default);
            var loaded = second.Social.PollsForOverseer(null).Single();
            Assert.Equal((pollId, 2, (DateTimeOffset?)null), (loaded.Id, loaded.Voters, loaded.ClosedAt));

            _clock.Advance(TimeSpan.FromHours(25));
            second.Tick();
            Assert.Equal(2, second.Social.PollsForOverseer(null).Single().Winner);
            Assert.Contains("sys.pollend|Survive?|no|2|2", AllianceChat(second, allianceId));
        }

        [Fact]
        public async Task The_overseer_closes_or_deletes_an_abusive_poll()
        {
            var (leader, m, allianceId) = await Alliance("over", 1);
            Send(leader, Poll("Bad words", "a", "b"), CommandType.CreateAlliancePoll);
            Send(leader, Poll("Also bad", "a", "b"), CommandType.CreateAlliancePoll);
            var ids = Polls(leader).Select(p => p.Id).ToList();

            Assert.True(_world.Social.CloseByOverseer(ids[0], _clock.UtcNow));
            Assert.False(_world.Social.CloseByOverseer(ids[0], _clock.UtcNow));
            Assert.Equal(AlliancePollRules.ClosedByOverseer, Polls(m[0]).Single(p => p.Id == ids[0]).ClosedBy);

            Assert.True(_world.Social.DeletePoll(ids[1], _clock.UtcNow));
            Assert.DoesNotContain(Polls(m[0]), p => p.Id == ids[1]);
            Assert.False(_world.Social.DeletePoll("nope", _clock.UtcNow));
            Assert.Single(_world.Social.PollsForOverseer("OVER"));
            Assert.Empty(_world.Social.PollsForOverseer("no such alliance"));
        }

        // --- Time-slot votes ---------------------------------------------------------------------

        [Fact]
        public async Task Time_slots_must_follow_the_vote_and_the_winner_is_fixed_for_the_event()
        {
            var (leader, m, allianceId) = await Alliance("slot", 3);
            Officer(_world, leader, m[0], AlliancePermissions.War);

            // Slots must start after the 24-h vote, on the quarter hour, within 14 days, 2–5 of them, for a known event.
            var bad = new[]
            {
                Slots(30),
                Slots(10, 30),
                Slots(30, 30),
                Slots(30, 40, 50, 60, 70, 80),
                Slots(30, 24 * 15),
                new CreateAlliancePollCommand
                {
                    CommandId = Id(), Kind = AlliancePollRules.KindSlots, Purpose = "lure_trap", Hours = 24,
                    SlotsMs = new[] { AlliancePollRules.RoundSlot(_clock.UtcNow.AddHours(30).ToUnixTimeMilliseconds()) + 60_000, AlliancePollRules.RoundSlot(_clock.UtcNow.AddHours(40).ToUnixTimeMilliseconds()) }
                }
            };
            var unknown = Slots(30, 40);
            unknown.Purpose = "picnic";
            foreach (var command in bad.Append(unknown))
            {
                Send(m[0], command, CommandType.CreateAlliancePoll);
                Assert.Equal(nameof(SocialRejectReason.InvalidPoll), Refusal(m[0]));
            }

            // Given out of order, the slots are kept earliest first.
            var vote = Slots(48, 30, 40);
            Send(m[0], vote, CommandType.CreateAlliancePoll);
            Assert.Null(Refusal(m[0]));
            var poll = Polls(m[1]).Single();
            Assert.Equal(vote.SlotsMs.OrderBy(t => t), poll.Options.Select(o => o.AtMs));
            Assert.Equal(_clock.UtcNow.AddHours(AlliancePollRules.SlotVoteHours).ToUnixTimeMilliseconds(), poll.ClosesAtMs);
            Assert.Null(_world.Social.DecidedSlot(allianceId, "lure_trap"));

            // A tie goes to the earlier time.
            Vote(m[1], poll.Id, 2);
            Vote(m[2], poll.Id, 1);
            _clock.Advance(TimeSpan.FromHours(24));
            _world.Tick();

            var closed = Polls(m[1]).Single();
            Assert.Equal(2, closed.Winner);
            Assert.Equal(closed.Options[1].AtMs, closed.WinnerAtMs);
            var decided = _world.Social.DecidedSlot(allianceId, "lure_trap");
            Assert.NotNull(decided);
            Assert.Equal(closed.WinnerAtMs, decided!.At.ToUnixTimeMilliseconds());
            Assert.Null(_world.Social.DecidedSlot(allianceId, "trench_conquest"));
            Assert.Contains($"sys.slotsend|alliance.poll.purpose.lure_trap|{SocialHub.ServerTime(closed.WinnerAtMs)}|1|2", AllianceChat(_world, allianceId));
        }

        [Fact]
        public async Task A_time_vote_nobody_answered_fixes_nothing()
        {
            var (leader, _, allianceId) = await Alliance("none", 1);
            var vote = Slots(30, 40);
            vote.Purpose = "meeting";
            Send(leader, vote, CommandType.CreateAlliancePoll);
            _clock.Advance(TimeSpan.FromHours(25));
            _world.Tick();
            Assert.Null(_world.Social.DecidedSlot(allianceId, "meeting"));
            Assert.Contains("sys.slotsnone|alliance.poll.purpose.meeting", AllianceChat(_world, allianceId));
        }

        // --- Leader inactivity -------------------------------------------------------------------

        [Fact]
        public void The_heir_is_the_strongest_active_officer_then_the_next_rank()
        {
            var members = new List<(string, int, long, bool)>
            {
                ("lead", 5, 900, false), ("weak", 4, 100, true), ("gone", 4, 800, false), ("strong", 4, 300, true), ("vet", 3, 999, true)
            };
            Assert.Equal("strong", AlliancePermissions.InactiveLeaderHeir(members, "lead"));
            members.RemoveAll(x => x.Item2 == 4 && x.Item4);
            Assert.Equal("vet", AlliancePermissions.InactiveLeaderHeir(members, "lead"));
            Assert.Null(AlliancePermissions.InactiveLeaderHeir(new[] { ("lead", 5, 1L, false), ("gone", 4, 1L, false) }, "lead"));
        }

        [Fact]
        public async Task A_leader_away_fourteen_days_hands_over_to_the_active_officer_and_the_alliance_is_told()
        {
            var (leader, m, allianceId) = await Alliance("idle", 3);
            Officer(_world, leader, m[0], AlliancePermissions.War);
            Officer(_world, leader, m[1], AlliancePermissions.Logistics);
            await _world.DisconnectAsync(m[1].S, m[1].C, default); // an officer who is away too is passed over
            await _world.DisconnectAsync(leader.S, leader.C, default);

            _clock.Advance(TimeSpan.FromDays(AlliancePermissions.InactiveLeaderDays) - TimeSpan.FromHours(1));
            _world.Tick();
            Assert.Equal(leader.S.PublicId, Mine(m[0]).LeaderId);

            _clock.Advance(TimeSpan.FromHours(2));
            _world.Tick();
            var alliance = Mine(m[2]);
            Assert.Equal(m[0].S.PublicId, alliance.LeaderId);
            Assert.Equal(AlliancePermissions.Leader, alliance.Members.Single(x => x.PlayerId == m[0].S.PublicId).Rank);
            Assert.Equal((AlliancePermissions.Officer, string.Empty), (alliance.Members.Single(x => x.PlayerId == leader.S.PublicId).Rank,
                alliance.Members.Single(x => x.PlayerId == leader.S.PublicId).Role));
            Assert.Contains(AllianceChat(_world, allianceId), t => t.StartsWith("sys.inactive|", StringComparison.Ordinal) && t.EndsWith("|14", StringComparison.Ordinal));

            // Every member gets the notice by mail (sent off the hub's lock).
            for (var i = 0; i < 100 && !_world.Mail.MailsOf(m[2].S.PublicId, _clock.UtcNow).Any(); i++)
                await Task.Delay(20);
            var mail = _world.Mail.MailsOf(m[2].S.PublicId, _clock.UtcNow).Single();
            Assert.Equal("loc:mail.alliance.leader.title", mail.Title);
            Assert.StartsWith("loc:mail.alliance.leader.body|", mail.Body);
        }
    }
}
