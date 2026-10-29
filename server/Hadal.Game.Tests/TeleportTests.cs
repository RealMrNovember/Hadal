using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>Base teleports (TeleportRules): random, to a chosen tile, next to an alliance mate.</summary>
    public sealed class TeleportTests
    {
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public TeleportTests()
        {
            _world = TestSupport.World(_clock, new InMemoryPlayerRepository());
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            session.StateForTests.Wallet[ResourceType.Hadalite] = 10_000;
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private (int X, int Y) Where(PlayerSession session) => _world.Map.PositionOf(session.PublicId)!.Value;

        [Fact]
        public async Task A_random_jump_moves_the_base_and_costs_hadalite_without_an_item()
        {
            var (session, connection) = await Connect("tp-random");
            var before = Where(session);
            Send(session, new TeleportCommand { CommandId = "r", Mode = TeleportRules.Random }, CommandType.Teleport);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.NotEqual(before, Where(session));
            Assert.Equal(10_000 - TeleportRules.HadaliteFor(TeleportRules.Random), session.StateForTests.Wallet[ResourceType.Hadalite]);

            // With the item in the bag the item is used instead.
            await _world.GrantAsync("tp-random", new Dictionary<ResourceType, long>(), default, items: new Dictionary<string, int> { [TeleportRules.RandomItem] = 1 });
            Send(session, new TeleportCommand { CommandId = "r2", Mode = TeleportRules.Random }, CommandType.Teleport);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.False(session.StateForTests.Items.ContainsKey(TeleportRules.RandomItem));
            Assert.Equal(10_000 - TeleportRules.HadaliteFor(TeleportRules.Random), session.StateForTests.Wallet[ResourceType.Hadalite]);
        }

        [Fact]
        public async Task A_targeted_jump_needs_a_free_tile()
        {
            var (a, aConnection) = await Connect("tp-a");
            var (b, _) = await Connect("tp-b");
            var other = Where(b);
            Send(a, new TeleportCommand { CommandId = "t1", Mode = TeleportRules.Target, X = other.X, Y = other.Y }, CommandType.Teleport);
            Assert.Equal("TileTaken", LastAck(aConnection).Reason);

            var free = Enumerable.Range(1, 6).Select(d => (X: other.X + d, Y: other.Y)).First(t => _world.Map.PositionOf(b.PublicId) != t);
            Send(a, new TeleportCommand { CommandId = "t2", Mode = TeleportRules.Target, X = free.X, Y = free.Y }, CommandType.Teleport);
            if (LastAck(aConnection).Result == CommandResultCode.Accepted)
                Assert.Equal(free, Where(a));
            else
                Assert.Equal("TileTaken", LastAck(aConnection).Reason); // a node may sit there
        }

        [Fact]
        public async Task An_alliance_jump_lands_next_to_a_mate()
        {
            var (leader, leaderConnection) = await Connect("tp-lead");
            leader.StateForTests.Wallet[ResourceType.Titanium] = 1_000_000;
            Send(leader, new CreateAllianceCommand { CommandId = "c", Name = "Deep Wardens", Tag = "DPW" }, CommandType.CreateAlliance);
            var allianceId = TestSupport.Serializer.Deserialize<AllianceDto>(leaderConnection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.MyAlliance).Payload).Id;
            var (diver, diverConnection) = await Connect("tp-diver");
            var (stranger, strangerConnection) = await Connect("tp-stranger");

            Send(stranger, new TeleportCommand { CommandId = "s", Mode = TeleportRules.Alliance, MemberId = leader.PublicId }, CommandType.Teleport);
            Assert.Equal("NotInAlliance", LastAck(strangerConnection).Reason);

            Send(diver, new JoinAllianceCommand { CommandId = "j", AllianceId = allianceId }, CommandType.JoinAlliance);
            Send(diver, new TeleportCommand { CommandId = "d", Mode = TeleportRules.Alliance, MemberId = leader.PublicId }, CommandType.Teleport);
            Assert.Equal(CommandResultCode.Accepted, LastAck(diverConnection).Result);
            var (lx, ly) = Where(leader);
            var (dx, dy) = Where(diver);
            Assert.True(Math.Max(Math.Abs(lx - dx), Math.Abs(ly - dy)) <= TeleportRules.AllianceRadius);
        }

        [Fact]
        public async Task Nobody_jumps_with_teams_out()
        {
            var (session, connection) = await Connect("tp-busy");
            session.StateForTests.Marches.Add(new MarchState
            {
                Id = "m", NodeId = "n", X = 1, Y = 1, StartedAt = _clock.UtcNow, ArrivesAt = _clock.UtcNow.AddMinutes(5),
                GatheredAt = _clock.UtcNow.AddMinutes(10), HomeAt = _clock.UtcNow.AddMinutes(15)
            });
            Send(session, new TeleportCommand { CommandId = "b", Mode = TeleportRules.Random }, CommandType.Teleport);
            Assert.Equal("TeamsOut", LastAck(connection).Reason);
        }
    }
}
