using Hadal.Game.Persistence;
using Hadal.Game.Social;
using Hadal.Game.World;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>The members page's data (docs/31 §2): each member's level and when an offline member was last seen.</summary>
    public sealed class AllianceMembersTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemorySocialStore _store = new();
        private readonly GameWorld _world;

        public AllianceMembersTests()
        {
            _world = new GameWorld(new CatalogRuntime(TestSupport.Catalog), new InMemoryPlayerRepository(), _store, new InMemoryWorldStore(), _clock,
                TestSupport.Serializer, NullLogger<GameWorld>.Instance);
        }

        private async Task<(PlayerSession S, RecordingConnection C)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            foreach (var type in new[] { ResourceType.Titanium, ResourceType.Biomass })
                session.StateForTests.Wallet[type] = 1_000_000;
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static string Id() => Guid.NewGuid().ToString("N");

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static AllianceDto Mine(RecordingConnection connection) =>
            TestSupport.Serializer.Deserialize<AllianceDto>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.MyAlliance && c.Kind != EntityChangeKind.Delete).Payload);

        [Fact]
        public async Task Members_show_their_level_and_when_they_were_last_online()
        {
            var leader = await Connect("mem-l");
            Send(leader.S, new CreateAllianceCommand { CommandId = Id(), Name = "House members", Tag = "MEMB" }, CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leader.C).Result);
            var diver = await Connect("mem-d");
            Send(diver.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = Mine(leader.C).Id }, CommandType.JoinAlliance);

            var online = Mine(leader.C).Members.Single(m => m.PlayerId == diver.S.PublicId);
            Assert.True(online.Online);
            Assert.Equal(diver.S.BaseLevel, online.Level);
            Assert.Equal(0, online.LastSeenMs);

            _clock.Advance(TimeSpan.FromMinutes(5));
            var leftAt = _clock.UtcNow;
            await _world.DisconnectAsync(diver.S, diver.C, default);
            var away = Mine(leader.C).Members.Single(m => m.PlayerId == diver.S.PublicId);
            Assert.False(away.Online);
            Assert.Equal(leftAt.ToUnixTimeMilliseconds(), away.LastSeenMs);
            Assert.True(away.Level >= 1);

            await _world.Social.DrainAsyncForTests();
            var (_, members) = await _store.LoadAlliancesAsync(default);
            var stored = members.Single(m => m.PlayerId == diver.S.PublicId);
            Assert.Equal((leftAt, away.Level), (stored.LastSeen, stored.Level));
        }

        [Fact]
        public async Task Coming_back_online_keeps_the_last_seen_time_for_next_time()
        {
            var leader = await Connect("mem2-l");
            Send(leader.S, new CreateAllianceCommand { CommandId = Id(), Name = "House back", Tag = "BACK" }, CommandType.CreateAlliance);
            var diver = await Connect("mem2-d");
            Send(diver.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = Mine(leader.C).Id }, CommandType.JoinAlliance);
            await _world.DisconnectAsync(diver.S, diver.C, default);
            var seen = Mine(leader.C).Members.Single(m => m.PlayerId == diver.S.PublicId).LastSeenMs;

            _clock.Advance(TimeSpan.FromHours(2));
            var back = await Connect("mem2-d");
            Assert.True(Mine(leader.C).Members.Single(m => m.PlayerId == back.S.PublicId).Online);
            Assert.Equal(0, Mine(leader.C).Members.Single(m => m.PlayerId == back.S.PublicId).LastSeenMs);
            await _world.Social.DrainAsyncForTests();
            var (_, members) = await _store.LoadAlliancesAsync(default);
            Assert.Equal(seen, members.Single(m => m.PlayerId == back.S.PublicId).LastSeen!.Value.ToUnixTimeMilliseconds());
        }
    }
}
