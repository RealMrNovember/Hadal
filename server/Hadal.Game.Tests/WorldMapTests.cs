using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>World map placement and views (docs/28).</summary>
    public sealed class WorldMapTests
    {
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public WorldMapTests()
        {
            _world = TestSupport.World(_clock);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            return (session, connection);
        }

        private WorldViewDto View(PlayerSession session, RecordingConnection connection, int x = -1, int y = -1)
        {
            var command = new ViewWorldCommand { CommandId = Guid.NewGuid().ToString("N"), ClientSequence = 1, X = x, Y = y };
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, CommandType.ViewWorld, TestSupport.Serializer.Serialize(command)));
            var delta = connection.Messages.Last().Delta();
            Assert.Equal(CommandResultCode.Accepted, delta.Ack()!.Result);
            return TestSupport.Serializer.Deserialize<WorldViewDto>(delta.Changes.Single(c => c.EntityId == EntityIds.WorldView).Payload);
        }

        [Fact]
        public void New_bases_spiral_out_from_the_centre_without_overlapping()
        {
            var rules = new WorldCatalogData { size = 200, spacing = 3 };
            var taken = new HashSet<(int, int)>();
            for (var i = 0; i < 50; i++)
            {
                var spot = WorldRules.Place(rules, (x, y) => taken.Contains((x, y)));
                Assert.True(taken.Add(spot));
                Assert.InRange(Math.Abs(spot.X - 100) + Math.Abs(spot.Y - 100), 0, 40);
            }

            Assert.Contains((100, 100), taken);
        }

        [Fact]
        public async Task Players_see_each_others_bases_with_name_and_alliance_tag()
        {
            var (alice, aliceConnection) = await Connect("world-a");
            var (bob, _) = await Connect("world-b");
            bob.StateForTests.Wallet[ResourceType.Titanium] = 5000;
            _world.HandleCommand(bob, EnvelopeBuilder.WrapCommand(
                new CreateAllianceCommand { CommandId = "c", ClientSequence = 1, Name = "Trench Lords", Tag = "TRL" }, CommandType.CreateAlliance,
                TestSupport.Serializer.Serialize(new CreateAllianceCommand { CommandId = "c", ClientSequence = 1, Name = "Trench Lords", Tag = "TRL" })));

            var view = View(alice, aliceConnection);
            Assert.Equal((view.MyX, view.MyY), (view.CenterX, view.CenterY));
            Assert.Equal(2, view.Bases.Length);
            var bobBase = view.Bases.Single(b => b.PlayerId == bob.PublicId);
            Assert.Equal(("world-b", "TRL", true), (bobBase.Name, bobBase.AllianceTag, bobBase.Online));
            Assert.Equal(1, bobBase.Level);
            Assert.NotEqual((view.MyX, view.MyY), (bobBase.X, bobBase.Y));

            // A far corner shows nobody; the base keeps its tile across reconnects.
            Assert.Empty(View(alice, aliceConnection, 5, 5).Bases);
            var spot = _world.Map.PositionOf(alice.PublicId);
            await _world.DisconnectAsync(alice, aliceConnection, default);
            var (again, _) = await Connect("world-a");
            Assert.Equal(spot, _world.Map.PositionOf(again.PublicId));
        }
    }
}
