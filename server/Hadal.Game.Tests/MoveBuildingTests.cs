using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>Moving and swapping buildings: free, level and construction kept, slot rules still apply.</summary>
    public sealed class MoveBuildingTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public MoveBuildingTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect()
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync("device-move", connection, default);
            foreach (var type in new[] { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Energy, ResourceType.Oxygen, ResourceType.Food })
                session.StateForTests.Wallet[type] = 4000;
            return (session, connection);
        }

        private static CommandEnvelope Move(string buildingEntityId, int ring, int sector)
        {
            var command = new MoveBuildingCommand
            {
                CommandId = Guid.NewGuid().ToString("N"), ClientSequence = 1, BuildingEntityId = buildingEntityId,
                RingIndex = ring, SectorIndex = sector
            };
            return EnvelopeBuilder.WrapCommand(command, CommandType.MoveBuilding, TestSupport.Serializer.Serialize(command));
        }

        private static CommandAckDto LastAck(RecordingConnection connection) => connection.Messages.Last().Delta().Ack()!;

        private static PlacedBuilding Building(PlayerSession session, string id) =>
            session.StateForTests.Buildings.Single(b => b.BuildingId == id || EntityIds.Building + b.BuildingId == id);

        [Fact]
        public async Task A_building_moves_to_a_free_slot_for_free_and_keeps_its_level()
        {
            var (session, connection) = await Connect();
            var plant = _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            Building(session, plant).Level = 4;
            var titanium = session.StateForTests.Wallet[ResourceType.Titanium];

            _world.HandleCommand(session, Move(plant, 1, 2));

            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var moved = Building(session, plant);
            Assert.Equal((1, 2, 4), (moved.Ring, moved.Sector, moved.Level));
            Assert.Equal(titanium, session.StateForTests.Wallet[ResourceType.Titanium]);
            Assert.Contains(moved, session.StateForTests.UnsavedBuildings);

            // The old slot is free again.
            _world.HandleCommand(session, TestSupport.Place("food_lab", 1, 0));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
        }

        [Fact]
        public async Task Moving_onto_another_building_swaps_the_two()
        {
            var (session, connection) = await Connect();
            var plant = _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            var lab = _world.PlaceBuilt(session, connection, "food_lab", 1, 1);

            _world.HandleCommand(session, Move(plant, 1, 1));

            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal((1, 1), (Building(session, plant).Ring, Building(session, plant).Sector));
            Assert.Equal((1, 0), (Building(session, lab).Ring, Building(session, lab).Sector));
            var changes = connection.Messages.Last().Delta().Changes;
            foreach (var id in new[] { Building(session, plant).BuildingId, Building(session, lab).BuildingId })
                Assert.Contains(changes, c => c.EntityId == EntityIds.Building + id);
        }

        [Fact]
        public async Task The_reactor_stays_and_locked_rings_stay_closed()
        {
            var (session, connection) = await Connect();
            var reactor = session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId).BuildingId;
            _world.HandleCommand(session, Move(reactor, 1, 0));
            Assert.Equal(PlacementRejectReason.CannotMove.ToString(), LastAck(connection).Reason);

            var plant = _world.PlaceBuilt(session, connection, "oxygen_plant", 1, 0);
            _world.HandleCommand(session, Move(plant, 0, 0));
            Assert.Equal(PlacementRejectReason.CannotMove.ToString(), LastAck(connection).Reason);

            _world.HandleCommand(session, Move(plant, session.StateForTests.UnlockedRings + 1, 0));
            Assert.Equal(PlacementRejectReason.RingLocked.ToString(), LastAck(connection).Reason);
            Assert.Equal((1, 0), (Building(session, plant).Ring, Building(session, plant).Sector));
        }
    }
}
