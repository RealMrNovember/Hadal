using Hadal.Game.Events;
using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Roadmap §1, research §14 "Zamanlayıcı": builds, research, training, healing, marches, shields and events finish
    /// by the server's clock only — also while the player is offline — a client cannot claim early, and a server clock
    /// that jumps between ticks neither pays twice nor finishes anything twice.
    /// </summary>
    public sealed class TimerSafetyTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public TimerSafetyTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            return (session, connection);
        }

        private static CommandEnvelope Envelope<T>(T command, CommandType type) where T : ICommand =>
            EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command));

        private static string Id() => Guid.NewGuid().ToString("N");

        private static CommandAckDto Ack(RecordingConnection connection, string commandId) =>
            connection.Messages.Where(m => m.Delta != null).SelectMany(m => m.Delta().Changes)
                .Where(c => c.EntityId == EntityIds.Command + commandId)
                .Select(c => TestSupport.Serializer.Deserialize<CommandAckDto>(c.Payload)).Last();

        private static void Add(PlayerSession session, PlacedBuilding building)
        {
            session.StateForTests.Buildings.Add(building);
            session.StateForTests.UnsavedBuildings.Add(building);
            session.StateForTests.IsDirty = true;
        }

        private static long Troops(PlayerSession session, string key) =>
            session.StateForTests.Troops.TryGetValue(key, out var n) ? n : 0;

        /// <summary>One of everything that runs on a timer, each ending at a different time within the next three hours.</summary>
        private void StartEverything(PlayerSession session)
        {
            var now = _clock.UtcNow;
            var state = session.StateForTests;
            Add(session, new PlacedBuilding { BuildingId = "plant", DefinitionId = "oxygen_plant", Ring = 1, Sector = 0, Level = 1,
                ConstructionEndsAt = now.AddHours(2), ConstructionSeconds = 7200 });
            Add(session, new PlacedBuilding { BuildingId = "center", DefinitionId = ResearchRules.CenterId, Ring = 1, Sector = 1, Level = 1 });
            Add(session, new PlacedBuilding { BuildingId = "yard", DefinitionId = "armor_yard", Ring = 4, Sector = 0, Level = 1 });
            Add(session, new PlacedBuilding { BuildingId = "dome", DefinitionId = "military_dome", Ring = 4, Sector = 1, Level = 1 });
            Add(session, new PlacedBuilding { BuildingId = "medbay", DefinitionId = TestSupport.Catalog.Troops.infirmary.building, Ring = 4, Sector = 2, Level = 1 });
            state.ActiveResearch.Add(new ActiveResearch { NodeId = "construction_mastery", TargetLevel = 1, EndsAt = now.AddHours(3), Seconds = 10800 });
            state.Training.Add(new TrainingState { BuildingId = "yard", Type = "armored", Tier = 1, Count = 10, StartedAt = now, EndsAt = now.AddHours(1),
                Cost = new Dictionary<ResourceType, long> { [ResourceType.Food] = 100 } });
            state.Healing = new HealState { Troops = new Dictionary<string, long>(StringComparer.Ordinal) { ["armored:1"] = 5 }, StartedAt = now,
                EndsAt = now.AddMinutes(90), Cost = new Dictionary<ResourceType, long>() };
            state.Marches.Add(new MarchState { Id = "m1", Kind = "gather", StartedAt = now.AddHours(-1), ArrivesAt = now.AddMinutes(-40),
                GatheredAt = now.AddMinutes(-10), HomeAt = now.AddMinutes(30),
                Troops = new Dictionary<string, long>(StringComparer.Ordinal) { ["armored:1"] = 3 },
                Cargo = new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 700 } });
            state.IsDirty = true;
        }

        private static void AssertEverythingDone(PlayerSession session)
        {
            var state = session.StateForTests;
            Assert.Equal(2, state.Buildings.Single(b => b.BuildingId == "plant").Level);
            Assert.Null(state.Buildings.Single(b => b.BuildingId == "plant").ConstructionEndsAt);
            Assert.Equal(1, state.ResearchLevels["construction_mastery"]);
            Assert.Empty(state.ActiveResearch);
            Assert.Empty(state.Training);
            Assert.Null(state.Healing);
            Assert.Empty(state.Marches);
            Assert.Equal(10 + 5 + 3, Troops(session, "armored:1")); // each batch once, never twice
        }

        [Fact]
        public async Task Everything_finishes_on_server_time_while_the_player_is_offline()
        {
            var (session, connection) = await Connect("timer-offline");
            StartEverything(session);
            var titanium = session.StateForTests.Wallet[ResourceType.Titanium];
            var shield = new UseShieldCommand { CommandId = Id(), ItemId = PvpRules.Shield8h };
            session.StateForTests.Items[PvpRules.Shield8h] = 1;
            _world.HandleCommand(session, Envelope(shield, CommandType.UseShield));
            Assert.Equal(_clock.UtcNow.AddHours(8), session.StateForTests.ShieldUntil);

            await _world.DisconnectAsync(session, connection, default);
            _clock.Advance(TimeSpan.FromHours(4));
            _world.Tick(); // the player is not loaded: nothing to tick

            var (again, againConnection) = await Connect("timer-offline");
            AssertEverythingDone(again);
            Assert.True(again.StateForTests.Wallet[ResourceType.Titanium] >= titanium + 700); // the cargo, past any cap
            var snapshot = againConnection.Messages.First(m => m.Snapshot != null).Snapshot();
            Assert.Equal(2, snapshot.Buildings.Single(b => b.EntityId == "plant").Level);
            Assert.Equal(0, snapshot.Buildings.Single(b => b.EntityId == "plant").ConstructionRemainingMs);

            // The shield still holds four hours later and ends on the server's clock.
            Assert.True(again.ShieldedAt(_clock.UtcNow));
            Assert.False(again.ShieldedAt(_clock.UtcNow.AddHours(4).AddSeconds(1)));

            // Nothing finishes a second time on later ticks or logins.
            _clock.Advance(TimeSpan.FromHours(1));
            _world.Tick();
            await _world.DisconnectAsync(again, againConnection, default);
            var (third, _) = await Connect("timer-offline");
            AssertEverythingDone(third);
        }

        [Fact]
        public async Task A_client_cannot_finish_anything_early()
        {
            var (session, connection) = await Connect("timer-early");
            StartEverything(session);

            // The envelope's ServerTick is the client's word and counts for nothing.
            var finish = TestSupport.Finish("plant", Id());
            finish.ServerTick = ulong.MaxValue;
            _world.HandleCommand(session, finish);
            Assert.Equal(nameof(ConstructionRejectReason.NotYetFree), Ack(connection, finish.CommandId).Reason);

            var research = new FinishResearchCommand { CommandId = Id(), NodeId = "construction_mastery" };
            _world.HandleCommand(session, Envelope(research, CommandType.FinishResearch));
            Assert.Equal(nameof(ResearchRejectReason.NotYetFree), Ack(connection, research.CommandId).Reason);

            // Speed-ups the player does not hold do nothing.
            var speed = new UseSpeedupCommand { CommandId = Id(), ItemId = Speedups.Id(SpeedupKind.Any, 60), Target = "train:yard", Count = 99 };
            _world.HandleCommand(session, Envelope(speed, CommandType.UseSpeedup));
            Assert.Equal(nameof(SpeedupRejectReason.NotEnoughItems), Ack(connection, speed.CommandId).Reason);

            _clock.Advance(TimeSpan.FromMinutes(59));
            _world.Tick();
            Assert.Single(session.StateForTests.Training);
            Assert.Equal(1, session.StateForTests.Buildings.Single(b => b.BuildingId == "plant").Level);
            Assert.Equal(3, Troops(session, "armored:1")); // only the march that came home at 30 minutes

            // Inside the free-finish window (catalog freeFinishSeconds) the server allows it; once, however often it is asked.
            _clock.Advance(TimeSpan.FromHours(2) - TimeSpan.FromMinutes(59) - TimeSpan.FromSeconds(TestSupport.Catalog.Progression.freeFinishSeconds - 10));
            var free = TestSupport.Finish("plant", Id());
            _world.HandleCommand(session, free);
            Assert.Equal(CommandResultCode.Accepted, Ack(connection, free.CommandId).Result);
            _world.HandleCommand(session, TestSupport.Finish("plant", Id()));
            Assert.Equal(2, session.StateForTests.Buildings.Single(b => b.BuildingId == "plant").Level);
        }

        [Fact]
        public async Task A_clock_that_jumps_back_pays_nothing_and_finishes_nothing_early_or_twice()
        {
            var (session, _) = await Connect("timer-back");
            StartEverything(session);
            _clock.Advance(TimeSpan.FromSeconds(10));
            _world.Tick();
            var wallet = new Dictionary<ResourceType, long>(session.StateForTests.Wallet);

            _clock.Advance(TimeSpan.FromHours(-3));
            _world.Tick();
            _world.HandleCommand(session, Envelope(new ViewWorldCommand { CommandId = Id(), X = -1, Y = -1 }, CommandType.ViewWorld));
            Assert.Equal(wallet, session.StateForTests.Wallet);
            Assert.Single(session.StateForTests.Training);
            Assert.NotNull(session.StateForTests.Healing);
            Assert.Single(session.StateForTests.Marches);

            // Timers keep their absolute end: they finish when the server clock reaches it, each exactly once.
            _clock.Advance(TimeSpan.FromHours(3) + TimeSpan.FromHours(3));
            _world.Tick();
            _world.Tick();
            AssertEverythingDone(session);
            Assert.All(session.StateForTests.Wallet, p => Assert.True(p.Value >= 0));
        }

        [Fact]
        public async Task A_clock_that_jumps_far_ahead_pays_at_most_the_offline_cap_and_finishes_each_timer_once()
        {
            var (session, _) = await Connect("timer-ahead");
            StartEverything(session);
            var before = session.StateForTests.ServerTick;

            _clock.Advance(TimeSpan.FromDays(30));
            _world.Tick();
            _world.Tick();
            AssertEverythingDone(session);
            Assert.True(session.StateForTests.ServerTick - before <= (ulong)SimulationTick.MaxOfflineAccrualTicks);
            Assert.All(session.StateForTests.Wallet, p => Assert.True(p.Value >= 0));
        }

        [Fact]
        public async Task Event_rewards_follow_the_server_clock_even_when_it_steps_back()
        {
            var (session, connection) = await Connect("timer-event");
            var (error, saved) = _world.Events.Save(new EventDefinition
            {
                Template = EventTemplates.Points,
                Title = new() { ["tr"] = "Saat Testi", ["en"] = "Clock Test" },
                Banner = "abyss",
                StartsAt = _clock.UtcNow,
                EndsAt = _clock.UtcNow.AddHours(2),
                ClaimDays = 3,
                Actions = { new EventActionConfig { Action = EventActions.Gather, Points = 1, Per = 1 } },
                RankBands = { new EventTierConfig { From = 1, To = 10, Rewards = { new EventRewardConfig { Type = "resource", Id = "Titanium", Amount = 500 } } } }
            }, "test", _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.Equal(string.Empty, _world.Events.Publish(saved!.Id, _clock.UtcNow));
            session.ScoreForTests(EventActions.Gather, 50);
            _clock.Advance(TimeSpan.FromSeconds(6));
            _world.Tick();

            var early = new ClaimEventRankCommand { CommandId = Id(), EventId = saved.Id };
            _world.HandleCommand(session, Envelope(early, CommandType.ClaimEventRank));
            Assert.Equal("NotEnded", Ack(connection, early.CommandId).Reason);

            _clock.Advance(TimeSpan.FromHours(3));
            _world.Tick();
            // The rank reward is mailed once at the end; claiming on the page afterwards has nothing left.
            Assert.Single(_world.Mail.MailsOf(session.PublicId, _clock.UtcNow), m => m.Body.StartsWith("loc:mail.event.rank.body|", StringComparison.Ordinal));
            var claim = new ClaimEventRankCommand { CommandId = Id(), EventId = saved.Id };
            _world.HandleCommand(session, Envelope(claim, CommandType.ClaimEventRank));
            Assert.Equal("AlreadyClaimed", Ack(connection, claim.CommandId).Reason);

            // A clock step back into the running event does not reopen the claim.
            _clock.Advance(TimeSpan.FromHours(-2));
            _world.Tick();
            var again = new ClaimEventRankCommand { CommandId = Id(), EventId = saved.Id };
            _world.HandleCommand(session, Envelope(again, CommandType.ClaimEventRank));
            Assert.Equal(CommandResultCode.Rejected, Ack(connection, again.CommandId).Result);
            _clock.Advance(TimeSpan.FromHours(3));
            _world.Tick();
            var late = new ClaimEventRankCommand { CommandId = Id(), EventId = saved.Id };
            _world.HandleCommand(session, Envelope(late, CommandType.ClaimEventRank));
            Assert.Equal("AlreadyClaimed", Ack(connection, late.CommandId).Reason);
            Assert.Single(_world.Mail.MailsOf(session.PublicId, _clock.UtcNow), m => m.Body.StartsWith("loc:mail.event.rank.body|", StringComparison.Ordinal));
        }
    }
}
