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
    /// <summary>Trench Conquest on the server (docs/29 §4.6): registration, the draw, rounds on the clock and the view.</summary>
    public sealed class TrenchServiceTests
    {
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public TrenchServiceTests()
        {
            _world = new GameWorld(new CatalogRuntime(TestSupport.Catalog), new InMemoryPlayerRepository(), new Hadal.Game.Social.InMemorySocialStore(),
                new Hadal.Game.World.InMemoryWorldStore(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance, new InMemoryEventStore());
        }

        private static string Id() => Guid.NewGuid().ToString("N");

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static T Latest<T>(RecordingConnection connection, string entityId) where T : class =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId).Payload);

        private static TrenchDto View(RecordingConnection connection, string eventId) =>
            Latest<EventListDto>(connection, EntityIds.Events).Events.Single(e => e.Id == eventId).Trench!;

        /// <summary>An armed leader (reactor 10, five seasoned heroes, troops) and their new alliance.</summary>
        private async Task<(PlayerSession Session, RecordingConnection Connection)> Leader(string identity, string tag, long troops, int heroes = 5)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            session.ClearCrewForTests();
            var core = session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);
            core.Level = 10;
            foreach (var id in TestSupport.Catalog.Heroes.Select(h => h.Id).Where(id => session.StateForTests.Heroes.All(h => h.HeroId != id)).Take(heroes))
                session.StateForTests.Heroes.Add(new OwnedHeroState { HeroId = id, Level = 60, Stars = 5, Slot = -1 });
            session.StateForTests.Troops[TroopRules.Key("armored", 1)] = troops;
            session.StateForTests.Wallet[ResourceType.Titanium] = 1_000_000;
            session.StateForTests.Wallet[ResourceType.Hadalite] = 1_000_000;
            Send(session, new CreateAllianceCommand { CommandId = Id(), Name = "House " + tag, Tag = tag }, CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            return (session, connection);
        }

        private string League(int minReactor = 1)
        {
            var def = new EventDefinition
            {
                Template = EventTemplates.TrenchConquest, Title = { ["en"] = "Trench Conquest" }, Banner = "abyss",
                StartsAt = _clock.UtcNow, EndsAt = _clock.UtcNow.AddHours(30), ClaimDays = 1, MinRankPoints = 1,
                Trench = new TrenchConfig { MinRegistered = 1, MinReactor = minReactor, RegisterHours = 2, FirstRoundAfterHours = 1, RoundEveryHours = 4 }
            };
            var (error, saved) = _world.Events.Save(def, "test", _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.Equal(string.Empty, _world.Events.Publish(saved!.Id, _clock.UtcNow));
            return saved.Id;
        }

        private void Register(PlayerSession session, string eventId, int lane, string action = TrenchActions.Register) =>
            Send(session, new TrenchCommand { CommandId = Id(), EventId = eventId, Action = action, Lane = lane }, CommandType.TrenchConquest);

        private void Pass(TimeSpan time)
        {
            var end = _clock.UtcNow + time;
            while (_clock.UtcNow < end)
            {
                _clock.Advance(TimeSpan.FromMinutes(10));
                _world.Tick();
            }
        }

        [Fact]
        public async Task Teams_register_as_snapshots_then_the_league_draws_and_fights_on_the_clock()
        {
            var (strong, strongLink) = await Leader("trench-a", "TRA", 60000);
            var (weak, weakLink) = await Leader("trench-b", "TRB", 200, heroes: 1);
            var eventId = League();

            Register(strong, eventId, 0);
            Assert.Equal(CommandResultCode.Accepted, LastAck(strongLink).Result);
            Register(weak, eventId, 0);
            Assert.Equal(CommandResultCode.Accepted, LastAck(weakLink).Result);
            var view = View(strongLink, eventId);
            Assert.True(view.Registered);
            Assert.Equal(0, view.MyLane);
            Assert.True(view.MyPower > 0);
            Assert.False(view.InLeague);
            // The snapshot leaves the troops at home.
            Assert.Equal(60000, strong.StateForTests.Troops[TroopRules.Key("armored", 1)]);

            // Registration closes at 2 h: the draw puts both alliances in one group; the first round is at 3 h.
            Pass(TimeSpan.FromHours(2.1));
            Register(strong, eventId, 1);
            Assert.Equal("RegistrationClosed", LastAck(strongLink).Reason);
            Register(strong, eventId, 0, TrenchActions.Move);
            Assert.Equal(CommandResultCode.Accepted, LastAck(strongLink).Result);
            view = View(strongLink, eventId);
            Assert.True(view.InLeague);
            Assert.Equal(2, view.Group.Length);
            Assert.Empty(view.Rounds);

            Pass(TimeSpan.FromHours(1));
            view = View(strongLink, eventId);
            var first = Assert.Single(view.Rounds);
            Assert.Equal(TrenchRules.WinPoints, first.Points);
            Assert.Equal(1, first.Lanes[0]);
            Assert.Equal("TRA", view.Group[0].Tag);
            var theirs = View(weakLink, eventId).Rounds.Single();
            Assert.Equal("TRA", theirs.RivalTag);
            Assert.Equal(0, theirs.Points);
            Assert.Equal(2, theirs.Lanes[0]);

            // Every round that fits before the end is fought (rounds at 3, 7, 11, 15 and 19 h).
            Pass(TimeSpan.FromHours(20));
            view = View(strongLink, eventId);
            Assert.Equal(TrenchRules.Rounds, view.Rounds.Length);
            Assert.Equal(TrenchRules.Rounds, view.RoundsDone);
            Assert.Equal(0, view.NextRoundAtMs);
            Assert.Equal(TrenchRules.Rounds * TrenchRules.WinPoints, view.Group[0].Points);
        }

        [Fact]
        public async Task The_league_needs_the_reactor_level_and_an_alliance_and_a_lane()
        {
            var (low, lowLink) = await Leader("trench-c", "TRC", 3000);
            var eventId = League(minReactor: 20);
            Register(low, eventId, 0);
            Assert.Equal("ReactorTooLow", LastAck(lowLink).Reason);
            Register(low, eventId, 5);
            Assert.Equal("UnknownLane", LastAck(lowLink).Reason);
            Register(low, eventId, 0, TrenchActions.Move);
            Assert.Equal("NotRegistered", LastAck(lowLink).Reason);
        }

        [Fact]
        public async Task A_league_nobody_entered_still_moves_through_its_rounds()
        {
            var (_, link) = await Leader("trench-d", "TRD", 3000);
            var eventId = League();
            Pass(TimeSpan.FromHours(29));
            var view = View(link, eventId);
            Assert.False(view.InLeague);
            Assert.Equal(TrenchRules.Rounds, view.RoundsDone);
            Assert.Equal(0, view.NextRoundAtMs);
        }

        [Fact]
        public void League_definitions_need_room_for_the_rounds()
        {
            var def = new EventDefinition
            {
                Template = EventTemplates.TrenchConquest, Title = { ["en"] = "T" }, Banner = "abyss",
                StartsAt = _clock.UtcNow, EndsAt = _clock.UtcNow.AddHours(10),
                Trench = new TrenchConfig { RegisterHours = 48 }
            };
            Assert.Equal("InvalidTrench", EventService.Validate(def, TestSupport.Catalog));
            def.EndsAt = _clock.UtcNow.AddDays(7);
            Assert.Equal(string.Empty, EventService.Validate(def, TestSupport.Catalog));
        }
    }
}
