using Hadal.Game.Persistence;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    public sealed class GameWorldTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public GameWorldTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        [Fact]
        public async Task NewPlayer_GetsSnapshotWithStartingWallet()
        {
            var connection = new RecordingConnection();
            await _world.ConnectAsync("device-a", connection, default);

            var snapshot = connection.Messages.First().Snapshot();
            Assert.Equal(600, snapshot.Resources.Amount(ResourceType.Oxygen));
            Assert.Equal(500, snapshot.Resources.Amount(ResourceType.Titanium));
            var reactor = Assert.Single(snapshot.Buildings);
            Assert.Equal("core_reactor", reactor.DefinitionId);
            Assert.Equal(0, reactor.RingIndex);
        }

        [Fact]
        public async Task ValidPlacement_CreatesBuildingChargesWalletAndAcks()
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", connection, default);

            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 1, 0, "cmd-1"));

            var delta = connection.Messages.Last().Delta();
            var ack = delta.Ack()!;
            Assert.Equal("cmd-1", ack.CommandId);
            Assert.Equal(CommandResultCode.Accepted, ack.Result);
            var created = Assert.Single(delta.CreatedBuildings());
            Assert.Equal("oxygen_plant", created.DefinitionId);
            Assert.Equal(0, created.Level); // under its first construction
            Assert.Equal(10_000, created.ConstructionTotalMs);
            Assert.Equal(440, delta.Wallet()!.Amount(ResourceType.Titanium));
        }

        [Fact]
        public async Task InvalidPlacement_IsRejectedWithReason_AndChangesNothing()
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", connection, default);

            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 1, 0));
            _world.HandleCommand(session, TestSupport.Place("food_lab", 1, 0, "cmd-dup"));

            var delta = connection.Messages.Last().Delta();
            var ack = delta.Ack()!;
            Assert.Equal(CommandResultCode.Rejected, ack.Result);
            Assert.Equal(nameof(PlacementRejectReason.SlotOccupied), ack.Reason);
            Assert.Empty(delta.CreatedBuildings());
            Assert.Null(delta.Wallet());
        }

        [Fact]
        public async Task CannotAfford_IsRejected()
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", connection, default);

            session.StateForTests.Wallet[ResourceType.Titanium] = 49; // oxygen_plant costs 50

            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 1, 0));

            var ack = connection.Messages.Last().Delta().Ack()!;
            Assert.Equal(CommandResultCode.Rejected, ack.Result);
            Assert.Equal(nameof(PlacementRejectReason.InsufficientResources), ack.Reason);
            Assert.Equal(49, session.StateForTests.Wallet[ResourceType.Titanium]);
            Assert.Equal("core_reactor", Assert.Single(session.StateForTests.Buildings).DefinitionId);
        }

        [Fact]
        public async Task Production_ReplicatesOnTheReplicationInterval()
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", connection, default);
            _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            connection.Messages.Clear();

            for (var i = 0; i < SimulationTick.ReplicationIntervalSeconds; i++)
            {
                _clock.Advance(TimeSpan.FromSeconds(SimulationTick.IntervalSeconds));
                _world.Tick();
            }

            var wallet = Assert.Single(connection.Messages).Delta().Wallet()!;
            Assert.Equal(600 + 3 * SimulationTick.ReplicationIntervalSeconds, wallet.Amount(ResourceType.Oxygen));
            // Starter reactor +8, oxygen plant -1.
            Assert.Equal(400 + (8 - 1) * SimulationTick.ReplicationIntervalSeconds, wallet.Amount(ResourceType.Energy));
        }

        [Fact]
        public async Task StarterReactor_ProducesEnergyFromTheStart()
        {
            var connection = new RecordingConnection();
            await _world.ConnectAsync("device-a", connection, default);
            connection.Messages.Clear();

            for (var i = 0; i < SimulationTick.ReplicationIntervalSeconds; i++)
            {
                _clock.Advance(TimeSpan.FromSeconds(SimulationTick.IntervalSeconds));
                _world.Tick();
            }

            var wallet = Assert.Single(connection.Messages).Delta().Wallet()!;
            Assert.Equal(400 + 8 * SimulationTick.ReplicationIntervalSeconds, wallet.Amount(ResourceType.Energy));
        }

        [Fact]
        public async Task StarterReactor_IsPersistedWithTheNewPlayer()
        {
            var first = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", first, default);
            await _world.DisconnectAsync(session, first, default);

            var second = new RecordingConnection();
            await _world.ConnectAsync("device-a", second, default);

            Assert.Equal("core_reactor", Assert.Single(second.Messages.First().Snapshot().Buildings).DefinitionId);
        }

        [Fact]
        public async Task Reconnect_RestoresStateAndAccruesOfflineOnTheServerClock()
        {
            var first = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", first, default);
            _world.PlaceBuilt(session, first, "oxygen_plant", 1, 0);
            await _world.DisconnectAsync(session, first, default);
            Assert.Equal(0, _world.SessionCount);

            _clock.Advance(TimeSpan.FromMinutes(10));

            var second = new RecordingConnection();
            await _world.ConnectAsync("device-a", second, default);
            var snapshot = second.Messages.First().Snapshot();

            Assert.Equal(2, snapshot.Buildings.Count); // starter reactor + oxygen plant
            Assert.Equal(600 + 3 * 600, snapshot.Resources.Amount(ResourceType.Oxygen));
        }

        [Fact]
        public async Task LongAbsence_IsClampedToTwelveHours_AndCapped()
        {
            var first = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", first, default);
            _world.HandleCommand(session, TestSupport.Place("food_lab", 1, 1));
            await _world.DisconnectAsync(session, first, default);

            _clock.Advance(TimeSpan.FromDays(7));

            var second = new RecordingConnection();
            var resumed = await _world.ConnectAsync("device-a", second, default);
            var state = resumed.StateForTests;

            Assert.Equal(5000, state.Wallet[ResourceType.Food]); // capacity
            Assert.Equal((ulong)SimulationTick.MaxOfflineAccrualTicks, state.ServerTick);
            Assert.Equal(_clock.UtcNow, state.LastSimulatedAt); // the remainder is forfeited, not banked
        }

        [Fact]
        public async Task ClockGoingBackwards_GrantsNothing()
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", connection, default);
            _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            var before = session.StateForTests.Wallet[ResourceType.Oxygen];

            // A backwards step re-anchors the clock and awards nothing...
            _clock.Advance(TimeSpan.FromHours(-2));
            _world.Tick();
            Assert.Equal(before, session.StateForTests.Wallet[ResourceType.Oxygen]);

            // ...and production then resumes from the new anchor, without replaying the step.
            _clock.Advance(TimeSpan.FromSeconds(1));
            _world.Tick();
            Assert.Equal(before + 3, session.StateForTests.Wallet[ResourceType.Oxygen]);
        }

        [Fact]
        public async Task SecondConnection_ReplacesTheFirst()
        {
            var first = new RecordingConnection();
            await _world.ConnectAsync("device-a", first, default);
            var second = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", second, default);

            Assert.NotNull(first.ClosedReason);
            Assert.Same(second, session.Connection);

            // The stale connection's disconnect must not unload the live session.
            await _world.DisconnectAsync(session, first, default);
            Assert.Equal(1, _world.SessionCount);
        }

        [Fact]
        public async Task UnknownCommand_IsRejectedNotIgnored()
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", connection, default);

            _world.HandleCommand(session, new HADAL.Shared.Commands.CommandEnvelope
            {
                CommandId = "x", Type = CommandType.StartExpedition, Payload = new byte[] { 1 }
            });

            var ack = connection.Messages.Last().Delta().Ack()!;
            Assert.Equal(CommandResultCode.Rejected, ack.Result);
            Assert.Equal("NotSupported", ack.Reason);
        }

        [Fact]
        public async Task Snapshot_TickIsNotOlderThanAnyDeltaSentBefore()
        {
            // The client drops wallet frames older than the newest it holds; a reconnect snapshot
            // must therefore carry a tick >= the last delta's.
            var first = new RecordingConnection();
            var session = await _world.ConnectAsync("device-a", first, default);
            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 1, 0));
            for (var i = 0; i < 6; i++) { _clock.Advance(TimeSpan.FromSeconds(1)); _world.Tick(); }
            var lastDeltaTick = first.Messages.Where(m => m.Delta != null).Max(m => m.Delta!.ServerTick);
            await _world.DisconnectAsync(session, first, default);

            var second = new RecordingConnection();
            await _world.ConnectAsync("device-a", second, default);
            Assert.True(second.Messages.First().Snapshot!.ServerTick >= lastDeltaTick);
        }
    }
}
