using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>Fractional production, research, Boost, Overdrive and salvage runs (docs/24).</summary>
    public sealed class BuildingFunctionTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public BuildingFunctionTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity = "device-a")
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default);
            session.ClearCrewForTests(); // exact timings below; heroes have their own tests
            return (session, connection);
        }

        private static void Rich(PlayerSession session)
        {
            foreach (var type in new[] { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Energy })
                session.StateForTests.Wallet[type] = 5000;
        }

        private static PlacedBuilding Building(PlayerSession session, string definitionId) =>
            session.StateForTests.Buildings.Single(b => b.DefinitionId == definitionId);

        private void Pass(int seconds)
        {
            for (var i = 0; i < seconds; i++)
            {
                _clock.Advance(TimeSpan.FromSeconds(1));
                _world.Tick();
            }
        }

        private static CommandAckDto LastAck(RecordingConnection connection) => connection.Messages.Last().Delta().Ack()!;

        private static CommandEnvelope Command<T>(T command, CommandType type) where T : ICommand =>
            EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command));

        private static CommandEnvelope Research(string node) =>
            Command(new StartResearchCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = 1, NodeId = node }, CommandType.StartResearch);

        private static CommandEnvelope Boost(string id) =>
            Command(new BoostBuildingCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = 1, BuildingEntityId = id }, CommandType.BoostBuilding);

        private static CommandEnvelope Overdrive(string id) =>
            Command(new OverdriveCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = 1, BuildingEntityId = id }, CommandType.Overdrive);

        private static CommandEnvelope Salvage(string id, string mission) =>
            Command(new StartSalvageCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = 1, BuildingEntityId = id, MissionId = mission },
                CommandType.StartSalvage);

        [Fact]
        public async Task SmallProducers_GainFromEveryLevel()
        {
            var (session, connection) = await Connect();
            Rich(session);
            Building(session, BuildingProgression.ReactorId).Level = 5;
            var lab = _world.PlaceBuilt(session, connection, "food_lab", 1, 0);
            _world.HandleCommand(session, TestSupport.Upgrade(lab));
            _world.HandleCommand(session, TestSupport.Finish(lab));
            Assert.Equal(2, Building(session, "food_lab").Level);
            var food = session.StateForTests.Wallet[ResourceType.Food];

            Pass(10);

            // 2 per tick at level 2 is 2.5 per tick, not the 2 integer maths used to give.
            Assert.Equal(food + 25, session.StateForTests.Wallet[ResourceType.Food]);
        }

        [Fact]
        public async Task Research_TakesTime_NeedsTheCenter_AndSpeedsUpBuilding()
        {
            var (session, connection) = await Connect();
            Rich(session);

            _world.HandleCommand(session, Research("construction_mastery"));
            Assert.Equal(ResearchRejectReason.NoResearchCenter.ToString(), LastAck(connection).Reason);

            Building(session, BuildingProgression.ReactorId).Level = 3;
            _world.PlaceBuilt(session, connection, "research_center", 2, 0);
            Rich(session);

            _world.HandleCommand(session, Research("second_builder"));
            Assert.Equal(ResearchRejectReason.CenterLevelTooLow.ToString(), LastAck(connection).Reason);

            _world.HandleCommand(session, Research("construction_mastery"));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(5000 - 80, session.StateForTests.Wallet[ResourceType.Titanium]);
            var research = connection.Messages.Last().Delta().Changes.Single(c => c.EntityId == EntityIds.Research);
            var active = Assert.Single(TestSupport.Serializer.Deserialize<ResearchStateDto>(research.Payload).Active);
            Assert.Equal(60_000, active.TotalMs);

            // Level 2 needs Research Center level 3 (1 + 2 per level).
            _world.HandleCommand(session, Research("research_methods"));
            Assert.Equal(ResearchRejectReason.ResearchBusy.ToString(), LastAck(connection).Reason);

            Pass(60);
            Assert.Equal(1, session.StateForTests.ResearchLevels["construction_mastery"]);

            // 3% faster: a 10 s Oxygen Plant takes 10 s x 0.97 -> 10 s; a 30 s upgrade of the reactor 34 s -> 33 s.
            _world.HandleCommand(session, TestSupport.Upgrade(Building(session, BuildingProgression.ReactorId).BuildingId));
            var reactor = Assert.Single(connection.Messages.Last().Delta().UpdatedBuildings());
            Assert.Equal(PlayerModifiers.ApplyPercent(BuildingProgression.Seconds(TestSupport.Catalog.Buildings.Single(b => b.Id == "core_reactor"), 4, TestSupport.Catalog.Progression), -3) * 1000L,
                reactor.ConstructionTotalMs);
        }

        [Fact]
        public async Task ExtraLine_Research_RaisesTheLimit_And_SurvivesASave()
        {
            var (session, connection) = await Connect();
            Rich(session);
            Building(session, BuildingProgression.ReactorId).Level = 10;
            session.StateForTests.ResearchLevels["extra_oxygen_line"] = 1;
            _world.HandleCommand(session, TestSupport.Upgrade(Building(session, BuildingProgression.ReactorId).BuildingId)); // refreshes nothing; limits read live
            await _world.SaveAllAsync(default);
            await _world.DisconnectAsync(session, connection, default);

            var again = new RecordingConnection();
            var resumed = await _world.ConnectAsync("device-a", again, default);
            Assert.Equal(1, resumed.StateForTests.ResearchLevels["extra_oxygen_line"]);
            var snapshot = again.Messages.First().Snapshot();
            Assert.Contains(snapshot.Research!.Levels, l => l.NodeId == "extra_oxygen_line" && l.Level == 1);
        }

        [Fact]
        public async Task Boost_RaisesOneBuildingsOutput_ForItsDuration()
        {
            var (session, connection) = await Connect();
            Rich(session);
            var plant = _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            session.StateForTests.Wallet[ResourceType.Oxygen] = 0;

            _world.HandleCommand(session, Boost(plant));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(5000 - 150, session.StateForTests.Wallet[ResourceType.Energy]);
            Assert.True(connection.Messages.Last().Delta().UpdatedBuildings().Single().BoostRemainingMs > 0);

            _world.HandleCommand(session, Boost(plant));
            Assert.Equal(FunctionRejectReason.AlreadyRunning.ToString(), LastAck(connection).Reason);

            Pass(100);
            Assert.Equal(390, session.StateForTests.Wallet[ResourceType.Oxygen]); // 3 x 1.3 per tick

            _clock.Advance(TimeSpan.FromSeconds(1800));
            _world.Tick();
            Assert.Null(Building(session, "oxygen_plant").BoostEndsAt);
            session.StateForTests.Wallet[ResourceType.Oxygen] = 0; // it filled the store meanwhile
            Pass(10);
            Assert.Equal(30, session.StateForTests.Wallet[ResourceType.Oxygen]);
        }

        [Fact]
        public async Task Overdrive_RunsThenCoolsDown()
        {
            var (session, connection) = await Connect();
            var reactor = Building(session, BuildingProgression.ReactorId).BuildingId;
            session.StateForTests.Wallet[ResourceType.Energy] = 0;

            _world.HandleCommand(session, Overdrive(reactor));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Pass(10);
            Assert.Equal(120, session.StateForTests.Wallet[ResourceType.Energy]); // 8 x 1.5 per tick

            _world.HandleCommand(session, Overdrive(reactor));
            Assert.Equal(FunctionRejectReason.AlreadyRunning.ToString(), LastAck(connection).Reason);

            _clock.Advance(TimeSpan.FromSeconds(3600));
            _world.Tick();
            _world.HandleCommand(session, Overdrive(reactor));
            Assert.Equal(FunctionRejectReason.OnCooldown.ToString(), LastAck(connection).Reason);

            _clock.Advance(TimeSpan.FromHours(8));
            _world.Tick();
            _world.HandleCommand(session, Overdrive(reactor));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
        }

        [Fact]
        public async Task Salvage_PaysWhenItReturns_EvenAfterARestart()
        {
            var (session, connection) = await Connect();
            Rich(session);
            var reactor = Building(session, BuildingProgression.ReactorId);
            reactor.Level = 2;
            session.StateForTests.UnsavedBuildings.Add(reactor); // saved, so the restart keeps reactor 2 (the drone factory needs it)
            session.StateForTests.IsDirty = true;
            var factory = _world.PlaceBuilt(session, connection, "drone_factory", 2, 1);

            _world.HandleCommand(session, Salvage(factory, "nope"));
            Assert.Equal(FunctionRejectReason.UnknownMission.ToString(), LastAck(connection).Reason);
            _world.HandleCommand(session, Salvage(factory, "quick"));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            _world.HandleCommand(session, Salvage(factory, "quick"));
            Assert.Equal(FunctionRejectReason.AlreadyRunning.ToString(), LastAck(connection).Reason);

            await _world.SaveAllAsync(default);
            await _world.DisconnectAsync(session, connection, default);
            _clock.Advance(TimeSpan.FromSeconds(901));

            var again = new RecordingConnection();
            var resumed = await _world.ConnectAsync("device-a", again, default);
            var state = resumed.StateForTests;
            Assert.Null(state.Buildings.Single(b => b.BuildingId == factory).MissionEndsAt);
            // Paid while offline: 1500 titanium on top of what the factory itself produced.
            var produced = 2L * 901; // 2 per tick at level 1
            Assert.Equal(Math.Min(25000L, 5000 - 120 + 1500 + produced), state.Wallet[ResourceType.Titanium]);
        }
    }
}
