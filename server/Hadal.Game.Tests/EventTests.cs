using Hadal.Game.Events;
using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>The event engine (docs/29): definitions, scoring, stages, rankings, sign-in rewards, persistence.</summary>
    public sealed class EventTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryEventStore _store = new();
        private readonly GameWorld _world;

        public EventTests()
        {
            _world = new GameWorld(new CatalogRuntime(TestSupport.Catalog), new InMemoryPlayerRepository(), new Hadal.Game.Social.InMemorySocialStore(),
                new Hadal.Game.World.InMemoryWorldStore(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance, _store);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static string Id() => Guid.NewGuid().ToString("N");

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static T Latest<T>(RecordingConnection connection, string entityId) where T : class =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId).Payload);

        private static EventDto Event(RecordingConnection connection, string id) =>
            Latest<EventListDto>(connection, EntityIds.Events).Events.Single(e => e.Id == id);

        private static EventRewardConfig Titanium(long amount) => new() { Type = "resource", Id = "Titanium", Amount = amount };

        private EventDefinition Contest(string template = EventTemplates.Points) => new()
        {
            Template = template,
            Title = new() { ["tr"] = "Derinlik Yarışı", ["en"] = "Depth Race" },
            Banner = "abyss",
            StartsAt = _clock.UtcNow,
            EndsAt = _clock.UtcNow.AddDays(2),
            ClaimDays = 3,
            Actions = { new EventActionConfig { Action = EventActions.Gather, Points = 1, Per = 1000 }, new EventActionConfig { Action = EventActions.MonsterKill, Points = 50 } },
            Stages = { new EventTierConfig { Points = 10, Rewards = { Titanium(500) } }, new EventTierConfig { Points = 100, Rewards = { Titanium(5000) } } },
            RankBands = { new EventTierConfig { From = 1, To = 1, Rewards = { Titanium(9000) } }, new EventTierConfig { From = 2, To = 3, Rewards = { Titanium(3000) } } }
        };

        private string Publish(EventDefinition definition)
        {
            var (error, saved) = _world.Events.Save(definition, "test", _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.Equal(string.Empty, _world.Events.Publish(saved!.Id, _clock.UtcNow));
            return saved.Id;
        }

        /// <summary>Lets the world take the queued actions and run event housekeeping.</summary>
        private void Tick()
        {
            _clock.Advance(TimeSpan.FromSeconds(6));
            _world.Tick();
        }

        [Fact]
        public void Definitions_are_validated()
        {
            var catalog = TestSupport.Catalog;
            Assert.Equal(string.Empty, EventService.Validate(Contest(), catalog));

            var noTitle = Contest();
            noTitle.Title.Clear();
            Assert.Equal("InvalidTitle", EventService.Validate(noTitle, catalog));

            var backwards = Contest();
            backwards.EndsAt = backwards.StartsAt.AddHours(-1);
            Assert.Equal("InvalidWindow", EventService.Validate(backwards, catalog));

            var badAction = Contest();
            badAction.Actions.Add(new EventActionConfig { Action = "fly", Points = 1 });
            Assert.Equal("InvalidAction", EventService.Validate(badAction, catalog));

            var badReward = Contest();
            badReward.Stages[0].Rewards.Add(new EventRewardConfig { Type = "item", Id = "no_such_item", Amount = 1 });
            Assert.Equal("InvalidReward", EventService.Validate(badReward, catalog));

            var overlap = Contest();
            overlap.RankBands.Add(new EventTierConfig { From = 3, To = 10, Rewards = { Titanium(1) } });
            Assert.Equal("OverlappingBands", EventService.Validate(overlap, catalog));
        }

        [Fact]
        public async Task Drafts_stay_hidden_until_published()
        {
            var (_, connection) = await Connect("ev-draft");
            var (_, saved) = _world.Events.Save(Contest(), "test", _clock.UtcNow);
            Assert.Equal(EventStatus.Draft, saved!.Status);
            Assert.DoesNotContain(Latest<EventListDto>(connection, EntityIds.Events).Events, e => e.Id == saved.Id);

            _world.Events.Publish(saved.Id, _clock.UtcNow);
            Assert.Equal("Depth Race", EventRules.Text(Event(connection, saved.Id).Title, "en"));
        }

        [Fact]
        public async Task Actions_score_with_carry_and_stages_are_claimed_once()
        {
            var id = Publish(Contest());
            var (session, connection) = await Connect("ev-score");

            // 600 + 600 gathered at 1 point per 1000: one point, 200 carried.
            session.ScoreForTests(EventActions.Gather, 600);
            session.ScoreForTests(EventActions.Gather, 600);
            session.ScoreForTests(EventActions.Heal, 999); // not scored by this event
            Tick();
            Assert.Equal(1, Event(connection, id).MyPoints);

            Send(session, new ClaimEventStageCommand { CommandId = Id(), EventId = id, Index = 0 }, CommandType.ClaimEventStage);
            Assert.Equal("NotReached", LastAck(connection).Reason);

            session.ScoreForTests(EventActions.MonsterKill, 1);
            Tick();
            var dto = Event(connection, id);
            Assert.Equal(51, dto.MyPoints);
            Assert.Equal(1, dto.MyRank);
            Assert.True(EventRules.HasClaim(dto, _clock.UtcNow.ToUnixTimeMilliseconds()));

            Send(session, new ClaimEventStageCommand { CommandId = Id(), EventId = id, Index = 0 }, CommandType.ClaimEventStage);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Contains(session.StateForTests.Items, p => p.Key.StartsWith("cap_Titanium_") && p.Value > 0);
            Assert.Equal(new[] { 0 }, Event(connection, id).Claimed);

            Send(session, new ClaimEventStageCommand { CommandId = Id(), EventId = id, Index = 0 }, CommandType.ClaimEventStage);
            Assert.Equal("AlreadyClaimed", LastAck(connection).Reason);
        }

        [Fact]
        public async Task Rank_rewards_open_when_the_event_ends()
        {
            var id = Publish(Contest());
            var (first, firstConnection) = await Connect("ev-rank-1");
            var (second, secondConnection) = await Connect("ev-rank-2");
            var (fourth, fourthConnection) = await Connect("ev-rank-4");
            first.ScoreForTests(EventActions.MonsterKill, 3);
            second.ScoreForTests(EventActions.MonsterKill, 2);
            Tick();
            var third = await Connect("ev-rank-3");
            third.Session.ScoreForTests(EventActions.MonsterKill, 1);
            fourth.ScoreForTests(EventActions.Gather, 1000);
            Tick();

            Send(first, new ClaimEventRankCommand { CommandId = Id(), EventId = id }, CommandType.ClaimEventRank);
            Assert.Equal("NotEnded", LastAck(firstConnection).Reason);

            _clock.Advance(TimeSpan.FromDays(2));
            Tick();
            Assert.Equal(1, Event(firstConnection, id).MyRank);
            Assert.Equal(2, Event(secondConnection, id).MyRank);

            Send(first, new ClaimEventRankCommand { CommandId = Id(), EventId = id }, CommandType.ClaimEventRank);
            Assert.Equal(CommandResultCode.Accepted, LastAck(firstConnection).Result);
            var report = Latest<RewardReportDto>(firstConnection, firstConnection.Messages.Select(m => m.Delta != null ? m.Delta() : null)
                .Where(d => d != null).SelectMany(d => d!.Changes).Last(c => c.EntityId.StartsWith(EntityIds.RewardReport)).EntityId);
            Assert.Contains(report.Rewards, r => r.Id == "cap_Titanium_9000" || r.Amount == 9000);

            Send(first, new ClaimEventRankCommand { CommandId = Id(), EventId = id }, CommandType.ClaimEventRank);
            Assert.Equal("AlreadyClaimed", LastAck(firstConnection).Reason);

            // Fourth place is outside every band.
            Send(fourth, new ClaimEventRankCommand { CommandId = Id(), EventId = id }, CommandType.ClaimEventRank);
            Assert.Equal("NoRank", LastAck(fourthConnection).Reason);

            // After the claim window the event disappears.
            _clock.Advance(TimeSpan.FromDays(4));
            Tick();
            Assert.DoesNotContain(Latest<EventListDto>(secondConnection, EntityIds.Events).Events, e => e.Id == id);
        }

        [Fact]
        public async Task Phases_score_only_their_own_actions()
        {
            var contest = Contest();
            contest.Actions.Clear();
            contest.CyclePhases = true;
            contest.Phases.Add(new EventPhaseConfig { Title = { ["en"] = "Hunt" }, StartHour = 0, Hours = 4, Actions = { new EventActionConfig { Action = EventActions.MonsterKill, Points = 10 } } });
            contest.Phases.Add(new EventPhaseConfig { Title = { ["en"] = "Heal" }, StartHour = 4, Hours = 4, Actions = { new EventActionConfig { Action = EventActions.Heal, Points = 1 } } });
            var id = Publish(contest);
            var (session, connection) = await Connect("ev-phase");
            Assert.Equal(12, Event(connection, id).Phases.Length); // 2 days of 4-hour themes

            session.ScoreForTests(EventActions.Heal, 5);
            session.ScoreForTests(EventActions.MonsterKill, 1);
            Tick();
            Assert.Equal(10, Event(connection, id).MyPoints);

            _clock.Advance(TimeSpan.FromHours(4));
            session.ScoreForTests(EventActions.Heal, 5);
            session.ScoreForTests(EventActions.MonsterKill, 1);
            Tick();
            Assert.Equal(15, Event(connection, id).MyPoints);
        }

        [Fact]
        public async Task Sign_in_events_count_one_day_at_a_time()
        {
            var login = new EventDefinition
            {
                Template = EventTemplates.Login, Title = { ["tr"] = "Hoş geldin" }, Banner = "bloom",
                StartsAt = _clock.UtcNow.AddMinutes(-1), EndsAt = _clock.UtcNow.AddDays(7),
                Stages = Enumerable.Range(1, 7).Select(d => new EventTierConfig { Points = d, Rewards = { Titanium(100 * d) } }).ToList()
            };
            var id = Publish(login);
            var (session, connection) = await Connect("ev-login");
            Assert.Equal(1, Event(connection, id).LoginDays);

            Send(session, new ClaimEventStageCommand { CommandId = Id(), EventId = id, Index = 1 }, CommandType.ClaimEventStage);
            Assert.Equal("NotReached", LastAck(connection).Reason);
            Send(session, new ClaimEventStageCommand { CommandId = Id(), EventId = id, Index = 0 }, CommandType.ClaimEventStage);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);

            Tick();
            Assert.Equal(1, Event(connection, id).LoginDays);
            _clock.Advance(TimeSpan.FromDays(1));
            Tick();
            Assert.Equal(2, Event(connection, id).LoginDays);
            Send(session, new ClaimEventStageCommand { CommandId = Id(), EventId = id, Index = 1 }, CommandType.ClaimEventStage);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
        }

        [Fact]
        public async Task Alliance_contests_add_up_members_and_rank_alliances()
        {
            var id = Publish(Contest(EventTemplates.Alliance));
            var (leader, leaderConnection) = await Connect("ev-ally-1");
            leader.StateForTests.Wallet[ResourceType.Titanium] = 1_000_000;
            leader.StateForTests.Wallet[ResourceType.Hadalite] = 1_000_000;
            Send(leader, new CreateAllianceCommand { CommandId = Id(), Name = "House Abyss", Tag = "ABYS" }, CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leaderConnection).Result);
            var allianceId = Latest<AllianceDto>(leaderConnection, EntityIds.MyAlliance).Id;
            var (diver, diverConnection) = await Connect("ev-ally-2");
            Send(diver, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(diverConnection).Result);
            var (loner, lonerConnection) = await Connect("ev-ally-3");

            leader.ScoreForTests(EventActions.MonsterKill, 1);
            diver.ScoreForTests(EventActions.MonsterKill, 2);
            loner.ScoreForTests(EventActions.MonsterKill, 5);
            Tick();

            var dto = Event(diverConnection, id);
            Assert.Equal(100, dto.MyPoints);
            Assert.Equal(150, dto.AlliancePoints);
            Assert.Equal(1, dto.AllianceRank);
            Assert.Equal(0, Event(lonerConnection, id).AllianceRank);

            var ranking = _world.Events.Ranking(id, 10)!;
            Assert.True(ranking.Alliance);
            Assert.Equal("ABYS", Assert.Single(ranking.Entries).Tag);

            _clock.Advance(TimeSpan.FromDays(2));
            Tick();
            Send(diver, new ClaimEventRankCommand { CommandId = Id(), EventId = id }, CommandType.ClaimEventRank);
            Assert.Equal(CommandResultCode.Accepted, LastAck(diverConnection).Result);
            Send(loner, new ClaimEventRankCommand { CommandId = Id(), EventId = id }, CommandType.ClaimEventRank);
            Assert.Equal("NoRank", LastAck(lonerConnection).Reason);
        }

        [Fact]
        public async Task Test_runs_and_level_ranges_limit_who_sees_an_event()
        {
            var test = Contest();
            test.TestAccounts.Add("ev-tester");
            var testId = Publish(test);
            var high = Contest();
            high.MinLevel = 30;
            var highId = Publish(high);

            var (_, tester) = await Connect("ev-tester");
            var (_, other) = await Connect("ev-other");
            Assert.Contains(Latest<EventListDto>(tester, EntityIds.Events).Events, e => e.Id == testId);
            Assert.DoesNotContain(Latest<EventListDto>(other, EntityIds.Events).Events, e => e.Id == testId);
            Assert.DoesNotContain(Latest<EventListDto>(other, EntityIds.Events).Events, e => e.Id == highId);
        }

        [Fact]
        public async Task Overseer_controls_end_extend_cancel_and_delete()
        {
            var id = Publish(Contest());
            Assert.Equal(string.Empty, _world.Events.Extend(id, 24, _clock.UtcNow));
            Assert.Equal(_clock.UtcNow.AddDays(3).Date, _world.Events.Get(id, _clock.UtcNow)!.Definition.EndsAt.Date);
            Assert.Equal("CancelFirst", _world.Events.Delete(id));
            _clock.Advance(TimeSpan.FromHours(1));
            Assert.Equal(string.Empty, _world.Events.EndNow(id, _clock.UtcNow));
            Assert.Equal("ended", _world.Events.Get(id, _clock.UtcNow.AddSeconds(1))!.Phase);

            var (_, copy) = _world.Events.Duplicate(id, "test", _clock.UtcNow);
            Assert.Equal(EventStatus.Draft, copy!.Status);
            Assert.NotEqual(id, copy.Id);

            Assert.Equal(string.Empty, _world.Events.Cancel(id, _clock.UtcNow));
            var (_, connection) = await Connect("ev-cancel");
            Assert.DoesNotContain(Latest<EventListDto>(connection, EntityIds.Events).Events, e => e.Id == id);
            Assert.Equal(string.Empty, _world.Events.Delete(id));
            Assert.Null(_world.Events.Get(id, _clock.UtcNow));
        }

        [Fact]
        public async Task Scores_survive_a_restart()
        {
            var id = Publish(Contest());
            var (session, _) = await Connect("ev-persist");
            session.ScoreForTests(EventActions.MonsterKill, 2);
            Tick();
            await _world.Events.DrainAsync();

            var restarted = new EventService(_store, new CatalogRuntime(TestSupport.Catalog), TestSupport.Serializer, NullLogger.Instance);
            await restarted.InitializeAsync(_clock.UtcNow, default);
            var ranking = restarted.Ranking(id, 10)!;
            Assert.Equal(100, Assert.Single(ranking.Entries).Points);
        }
    }
}
