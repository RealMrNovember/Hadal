using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>Removal, building limits, the reactor gate and ring unlocks (docs/23, rules 9–14).</summary>
    public sealed class BaseExpansionTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public BaseExpansionTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity = "device-a")
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default);
            return (session, connection);
        }

        private static void Rich(PlayerSession session)
        {
            foreach (var type in new[] { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Energy, ResourceType.Oxygen, ResourceType.Food })
                session.StateForTests.Wallet[type] = 4000;
        }

        private static void SetReactorLevel(PlayerSession session, int level) =>
            session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId).Level = level;

        private CommandAckDto LastAck(RecordingConnection connection) => connection.Messages.Last().Delta().Ack()!;

        private static CommandEnvelope Remove(string buildingEntityId)
        {
            var command = new RemoveBuildingCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = 1, BuildingEntityId = buildingEntityId };
            return EnvelopeBuilder.WrapCommand(command, CommandType.RemoveBuilding, TestSupport.Serializer.Serialize(command));
        }

        private static CommandEnvelope Unlock(int ring)
        {
            var command = new UnlockRingCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = 1, Ring = ring };
            return EnvelopeBuilder.WrapCommand(command, CommandType.UnlockRing, TestSupport.Serializer.Serialize(command));
        }

        [Fact]
        public async Task Removing_AFinishedBuilding_RefundsHalfOfWhatItCost()
        {
            var (session, connection) = await Connect();
            Rich(session);
            var plant = _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            var before = session.StateForTests.Wallet[ResourceType.Titanium];

            _world.HandleCommand(session, Remove(plant));

            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(before + 30, session.StateForTests.Wallet[ResourceType.Titanium]); // 50% of 60
            Assert.DoesNotContain(session.StateForTests.Buildings, b => b.BuildingId == plant);
            Assert.Contains(connection.Messages.Last().Delta().Changes,
                c => c.EntityId == EntityIds.Building + plant && c.Kind == EntityChangeKind.Delete);

            // The freed slot takes a new building.
            _world.HandleCommand(session, TestSupport.Place("food_lab", 1, 0));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
        }

        [Fact]
        public async Task Removing_AnUpgradedBuilding_RefundsHalfOfEveryLevel()
        {
            var (session, connection) = await Connect();
            Rich(session);
            SetReactorLevel(session, 3);
            var plant = _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            _world.HandleCommand(session, TestSupport.Upgrade(plant));
            _world.HandleCommand(session, TestSupport.Finish(plant));
            var before = session.StateForTests.Wallet[ResourceType.Titanium];

            _world.HandleCommand(session, Remove(plant));

            // Level 1 cost 60, level 2 cost 65 (60 × 1.09): half of 125.
            Assert.Equal(before + 62, session.StateForTests.Wallet[ResourceType.Titanium]);
        }

        [Fact]
        public async Task Cancelling_AFirstConstruction_RefundsEverything()
        {
            var (session, connection) = await Connect();
            Rich(session);
            var before = session.StateForTests.Wallet[ResourceType.Titanium];
            _world.HandleCommand(session, TestSupport.Place("drone_factory", 2, 1));
            Assert.Equal(CommandResultCode.Rejected, LastAck(connection).Result); // reactor 1 < 2
            SetReactorLevel(session, 2);
            _world.HandleCommand(session, TestSupport.Place("drone_factory", 2, 1));
            var site = session.StateForTests.Buildings.Single(b => b.DefinitionId == "drone_factory");
            Assert.True(site.IsUnderConstruction);

            _world.HandleCommand(session, Remove(site.BuildingId));

            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(before, session.StateForTests.Wallet[ResourceType.Titanium]);
            Assert.Equal(4000, session.StateForTests.Wallet[ResourceType.Biomass]);
        }

        [Fact]
        public async Task Reactor_And_RunningUpgrades_CannotBeRemoved()
        {
            var (session, connection) = await Connect();
            Rich(session);
            var reactor = session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId).BuildingId;
            _world.HandleCommand(session, Remove(reactor));
            Assert.Equal(RemoveRejectReason.CannotRemoveReactor.ToString(), LastAck(connection).Reason);

            SetReactorLevel(session, 5);
            var plant = _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            session.StateForTests.Wallet[ResourceType.Titanium] = 4000;
            _world.HandleCommand(session, TestSupport.Upgrade(plant));
            _world.HandleCommand(session, Remove(plant));
            Assert.Equal(RemoveRejectReason.UpgradeInProgress.ToString(), LastAck(connection).Reason);
        }

        [Fact]
        public async Task Removal_IsSaved()
        {
            var (session, connection) = await Connect();
            Rich(session);
            var plant = _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            await _world.SaveAllAsync(default);
            _world.HandleCommand(session, Remove(plant));
            await _world.SaveAllAsync(default);
            await _world.DisconnectAsync(session, connection, default);

            var again = new RecordingConnection();
            await _world.ConnectAsync("device-a", again, default);
            Assert.DoesNotContain(again.Messages.Single().Snapshot().Buildings, b => b.EntityId == plant);
        }

        [Fact]
        public async Task Buildings_HaveALimit_And_AReactorLevel()
        {
            var (session, connection) = await Connect();
            Rich(session);

            _world.HandleCommand(session, TestSupport.Place("bio_lab", 1, 0));
            Assert.Equal(PlacementRejectReason.ReactorLevelTooLow.ToString(), LastAck(connection).Reason);

            // Four oxygen plants: three inner production slots and one outer universal slot.
            foreach (var (ring, sector) in new[] { (1, 0), (1, 1), (1, 2), (4, 0) })
                _world.PlaceBuilt(session, connection, "oxygen_plant", ring, sector);
            Rich(session);

            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 4, 1));
            Assert.Equal(PlacementRejectReason.LimitReached.ToString(), LastAck(connection).Reason);
        }

        [Fact]
        public async Task Rings_OpenInOrder_WithReactorLevel_Cost_And_MegaDome()
        {
            var (session, connection) = await Connect();
            Assert.Equal(4, connection.Messages.Single().Snapshot().UnlockedRings);
            Rich(session);

            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 5, 0));
            Assert.Equal(PlacementRejectReason.RingLocked.ToString(), LastAck(connection).Reason);

            _world.HandleCommand(session, Unlock(6));
            Assert.Equal(RingRejectReason.NotNextRing.ToString(), LastAck(connection).Reason);
            _world.HandleCommand(session, Unlock(5));
            Assert.Equal(RingRejectReason.ReactorLevelTooLow.ToString(), LastAck(connection).Reason);

            SetReactorLevel(session, 6);
            _world.HandleCommand(session, Unlock(5));
            var delta = connection.Messages.Last().Delta();
            Assert.Equal(CommandResultCode.Accepted, delta.Ack()!.Result);
            var layout = delta.Changes.Single(c => c.EntityId == EntityIds.BaseLayout);
            Assert.Equal(5, TestSupport.Serializer.Deserialize<BaseLayoutDto>(layout.Payload).UnlockedRings);
            Assert.Equal(4000 - 400, session.StateForTests.Wallet[ResourceType.Titanium]);

            _world.HandleCommand(session, TestSupport.Place("oxygen_plant", 5, 0));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            _world.HandleCommand(session, TestSupport.Finish(session.StateForTests.Buildings.Single(b => b.Ring == 5).BuildingId));

            Rich(session);
            _world.HandleCommand(session, Unlock(6));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);

            // Ring 7 needs the base to have grown into a Mega Dome.
            _world.HandleCommand(session, Unlock(7));
            Assert.Equal(RingRejectReason.MegaDomeRequired.ToString(), LastAck(connection).Reason);
            _world.HandleCommand(session, TestSupport.Place("mega_dome", 4, 2));
            var dome = session.StateForTests.Buildings.Single(b => b.DefinitionId == "mega_dome");
            _clock.Advance(TimeSpan.FromSeconds(301));
            _world.Tick();
            Assert.Equal(1, dome.Level);

            Rich(session);
            _world.HandleCommand(session, Unlock(7));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(7, session.StateForTests.UnlockedRings);
        }
    }
}
