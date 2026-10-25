using Hadal.Game.Persistence;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>Construction timers, upgrades and the reactor gate (docs/23_Base_Management.md).</summary>
    public sealed class ConstructionTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public ConstructionTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity = "device-a")
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default);
            return (session, connection);
        }

        private string ReactorId(PlayerSession session) =>
            session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId).BuildingId;

        private void Pass(TimeSpan time)
        {
            for (var t = TimeSpan.Zero; t < time; t += TimeSpan.FromSeconds(1))
            {
                _clock.Advance(TimeSpan.FromSeconds(1));
                _world.Tick();
            }
        }

        [Fact]
        public async Task NewBuilding_ProducesNothingUntilItsConstructionCompletes()
        {
            var (session, connection) = await Connect();
            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 1, 0));
            var oxygen = session.StateForTests.Wallet[ResourceType.Oxygen];

            Pass(TimeSpan.FromSeconds(11));
            Assert.Equal(oxygen, session.StateForTests.Wallet[ResourceType.Oxygen]);

            Pass(TimeSpan.FromSeconds(2));
            var plant = session.StateForTests.Buildings.Single(b => b.DefinitionId == "oxygen_plant");
            Assert.Equal(1, plant.Level);
            Assert.False(plant.IsUnderConstruction);
            Assert.True(session.StateForTests.Wallet[ResourceType.Oxygen] > oxygen, "finished plant does not produce");

            // The completion was pushed to the client on its own.
            Assert.Contains(connection.Messages.Where(m => m.Delta != null).SelectMany(m => m.Delta().UpdatedBuildings()),
                b => b.DefinitionId == "oxygen_plant" && b.Level == 1 && b.ConstructionRemainingMs == 0);
        }

        [Fact]
        public async Task OneBuilder_SecondConstructionIsRefused()
        {
            var (session, connection) = await Connect();
            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 1, 0));
            _world.HandleCommand(session, TestSupport.Place("food_lab", 1, 1));

            var ack = connection.Messages.Last().Delta().Ack()!;
            Assert.Equal(CommandResultCode.Rejected, ack.Result);
            Assert.Equal(nameof(ConstructionRejectReason.BuilderBusy), ack.Reason);
            Assert.Equal(100, session.StateForTests.Wallet[ResourceType.Titanium]); // only the first build was charged
        }

        [Fact]
        public async Task Upgrade_ChargesTheScaledCost_KeepsProducing_ThenLevelsUp()
        {
            var (session, connection) = await Connect();
            var reactor = ReactorId(session);
            var wallet = session.StateForTests.Wallet;
            wallet[ResourceType.Titanium] = 1000;
            wallet[ResourceType.Energy] = 1000;

            _world.HandleCommand(session, TestSupport.Upgrade(reactor));
            Assert.Equal(CommandResultCode.Accepted, connection.Messages.Last().Delta().Ack()!.Result);

            // Level 2 costs buildCost x 1.55: 124 titanium, 93 energy; 40 s x 1.6 = 64 s.
            Assert.Equal(1000 - 124, wallet[ResourceType.Titanium]);
            var updated = Assert.Single(connection.Messages.Last().Delta().UpdatedBuildings());
            Assert.Equal(1, updated.Level);
            Assert.Equal(64_000, updated.ConstructionTotalMs);

            // Still producing at level 1 during the upgrade.
            var energy = wallet[ResourceType.Energy];
            Pass(TimeSpan.FromSeconds(10));
            Assert.True(wallet[ResourceType.Energy] > energy, "reactor stopped producing while upgrading");

            Pass(TimeSpan.FromSeconds(60));
            Assert.Equal(2, session.StateForTests.Buildings.Single(b => b.BuildingId == reactor).Level);
        }

        [Fact]
        public async Task BuildingsCannotOutgrowTheReactor()
        {
            var (session, connection) = await Connect();
            var plant = _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            session.StateForTests.Wallet[ResourceType.Titanium] = 5000;

            _world.HandleCommand(session, TestSupport.Upgrade(plant));

            var ack = connection.Messages.Last().Delta().Ack()!;
            Assert.Equal(CommandResultCode.Rejected, ack.Result);
            Assert.Equal(nameof(ConstructionRejectReason.ReactorLevelTooLow), ack.Reason);
        }

        [Fact]
        public async Task FreeFinish_OnlyInsideTheWindow()
        {
            var (session, connection) = await Connect();
            var reactor = ReactorId(session);
            var state = session.StateForTests;
            state.Wallet[ResourceType.Titanium] = 100_000;
            state.Wallet[ResourceType.Energy] = 100_000;
            state.Buildings.Single(b => b.BuildingId == reactor).Level = 8; // level 9 takes 40 x 1.6^8 ≈ 1718 s

            _world.HandleCommand(session, TestSupport.Upgrade(reactor));
            _world.HandleCommand(session, TestSupport.Finish(reactor));
            var early = connection.Messages.Last().Delta().Ack()!;
            Assert.Equal(nameof(ConstructionRejectReason.NotYetFree), early.Reason);

            _clock.Advance(TimeSpan.FromSeconds(1718 - 299));
            _world.HandleCommand(session, TestSupport.Finish(reactor));
            Assert.Equal(CommandResultCode.Accepted, connection.Messages.Last().Delta().Ack()!.Result);
            Assert.Equal(9, state.Buildings.Single(b => b.BuildingId == reactor).Level);
        }

        [Fact]
        public async Task Upgrade_Rejections()
        {
            var (session, connection) = await Connect();
            var reactor = ReactorId(session);

            _world.HandleCommand(session, TestSupport.Upgrade("b-does-not-exist"));
            Assert.Equal(nameof(ConstructionRejectReason.UnknownBuilding), connection.Messages.Last().Delta().Ack()!.Reason);

            session.StateForTests.Wallet[ResourceType.Titanium] = 10;
            _world.HandleCommand(session, TestSupport.Upgrade(reactor));
            Assert.Equal(nameof(ConstructionRejectReason.InsufficientResources), connection.Messages.Last().Delta().Ack()!.Reason);

            session.StateForTests.Buildings.Single(b => b.BuildingId == reactor).Level = 10;
            _world.HandleCommand(session, TestSupport.Upgrade(reactor));
            Assert.Equal(nameof(ConstructionRejectReason.MaxLevel), connection.Messages.Last().Delta().Ack()!.Reason);

            _world.HandleCommand(session, TestSupport.Finish(reactor));
            Assert.Equal(nameof(ConstructionRejectReason.NotUnderConstruction), connection.Messages.Last().Delta().Ack()!.Reason);
        }

        [Fact]
        public async Task ConstructionFinishedOffline_ProducesFromTheMomentItCompleted()
        {
            var first = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", first, default);
            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 1, 0)); // 12 s build
            var oxygen = session.StateForTests.Wallet[ResourceType.Oxygen];
            await _world.DisconnectAsync(session, first, default);

            _clock.Advance(TimeSpan.FromSeconds(112));

            var second = new RecordingConnection();
            await _world.ConnectAsync("device-a", second, default);
            var snapshot = second.Messages.Single().Snapshot();

            var plant = snapshot.Buildings.Single(b => b.DefinitionId == "oxygen_plant");
            Assert.Equal(1, plant.Level);
            Assert.Equal(0, plant.ConstructionRemainingMs);
            // Produced for the 100 s after completion only (3 oxygen per tick), not the full 112 s.
            Assert.Equal(oxygen + 3 * 100, snapshot.Resources.Amount(ResourceType.Oxygen));
        }

        [Fact]
        public async Task RunningConstruction_SurvivesASave()
        {
            var first = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", first, default);
            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 1, 0));
            _clock.Advance(TimeSpan.FromSeconds(5));
            await _world.DisconnectAsync(session, first, default);

            var second = new RecordingConnection();
            await _world.ConnectAsync("device-a", second, default);
            var plant = second.Messages.Single().Snapshot().Buildings.Single(b => b.DefinitionId == "oxygen_plant");

            Assert.Equal(0, plant.Level);
            Assert.Equal(7_000, plant.ConstructionRemainingMs);
            Assert.Equal(12_000, plant.ConstructionTotalMs);
        }
    }
}
