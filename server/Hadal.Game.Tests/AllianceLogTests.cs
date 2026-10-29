using Hadal.Game.Persistence;
using Hadal.Game.Social;
using Hadal.Game.World;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>The alliance log and the battle log (docs/31 §2): what is logged, the 200-line cap, storage and viewing.</summary>
    public sealed class AllianceLogTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemorySocialStore _store = new();
        private GameWorld _world;

        public AllianceLogTests()
        {
            _world = NewWorld();
        }

        private GameWorld NewWorld() => new(new CatalogRuntime(TestSupport.Catalog), new InMemoryPlayerRepository(), _store, new InMemoryWorldStore(), _clock,
            TestSupport.Serializer, NullLogger<GameWorld>.Instance);

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

        private static T Latest<T>(RecordingConnection connection, string entityId) where T : class =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId && c.Kind != EntityChangeKind.Delete).Payload);

        private async Task<(PlayerSession Leader, RecordingConnection C, string AllianceId)> Found(string name)
        {
            var (leader, connection) = await Connect(name);
            Send(leader, new CreateAllianceCommand { CommandId = Id(), Name = "House " + name, Tag = name[..4].ToUpperInvariant() }, CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            return (leader, connection, Latest<AllianceDto>(connection, EntityIds.MyAlliance).Id);
        }

        [Fact]
        public async Task Joins_leaves_kicks_and_rank_changes_are_logged_and_viewable()
        {
            var (leader, connection, allianceId) = await Found("logjoin");
            var a = await Connect("logjoin-a");
            var b = await Connect("logjoin-b");
            Send(a.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
            Send(b.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
            Send(leader, new SetMemberRankCommand { CommandId = Id(), MemberId = a.S.PublicId, Rank = 3 }, CommandType.SetMemberRank);
            Send(leader, new KickMemberCommand { CommandId = Id(), MemberId = b.S.PublicId }, CommandType.KickMember);
            Send(a.S, new LeaveAllianceCommand { CommandId = Id() }, CommandType.LeaveAlliance);

            var types = _world.Social.LogOf(allianceId, AllianceLogKinds.Log).Select(l => l.Type).ToList();
            Assert.Equal(new[] { "joined", "joined", "rank", "kicked", "left" }, types);
            var rank = _world.Social.LogOf(allianceId, AllianceLogKinds.Log).Single(l => l.Type == "rank");
            Assert.Equal(new[] { "logjoin-a", "3" }, rank.Args);

            Send(leader, new ViewAllianceLogCommand { CommandId = Id(), Kind = AllianceLogKinds.Log }, CommandType.ViewAllianceLog);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var log = Latest<AllianceLogDto>(connection, EntityIds.AllianceLog + AllianceLogKinds.Log);
            Assert.Equal("left", log.Entries[0].Type); // newest first
            Assert.Equal(_clock.UtcNow.ToUnixTimeMilliseconds(), log.Entries[0].AtMs);
        }

        [Fact]
        public async Task Battles_go_to_their_own_log_which_keeps_the_newest_200_lines()
        {
            var (leader, connection, allianceId) = await Found("logwar");
            for (var i = 0; i < 205; i++)
            {
                _world.Social.LogBattle(allianceId, AllianceLogKinds.AttackWon, _clock.UtcNow, "Ana", "Hedef" + i, "10", "20");
                _clock.Advance(TimeSpan.FromSeconds(1));
            }

            var battles = _world.Social.LogOf(allianceId, AllianceLogKinds.Battle);
            Assert.Equal(AllianceLogKinds.Keep, battles.Count);
            Assert.Equal("Hedef5", battles[0].Args[1]);
            Assert.DoesNotContain(_world.Social.LogOf(allianceId, AllianceLogKinds.Log), l => l.Type == AllianceLogKinds.AttackWon);

            Send(leader, new ViewAllianceLogCommand { CommandId = Id(), Kind = AllianceLogKinds.Battle }, CommandType.ViewAllianceLog);
            Assert.Equal("Hedef204", Latest<AllianceLogDto>(connection, EntityIds.AllianceLog + AllianceLogKinds.Battle).Entries[0].Args[1]);

            _world.Social.LogBattle(string.Empty, AllianceLogKinds.Scout, _clock.UtcNow, "x"); // no alliance: ignored
        }

        [Fact]
        public async Task The_logs_survive_a_restart()
        {
            var (_, _, allianceId) = await Found("logsave");
            var mate = await Connect("logsave-m");
            Send(mate.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
            _world.Social.LogBattle(allianceId, AllianceLogKinds.DefenceWon, _clock.UtcNow, "Saldıran", "Savunan", "1", "2");
            await _world.Social.DrainAsyncForTests();

            _world = NewWorld();
            await _world.Social.InitializeAsync(default);
            Assert.Equal(new[] { "joined" }, _world.Social.LogOf(allianceId, AllianceLogKinds.Log).Select(l => l.Type));
            Assert.Equal(new[] { "Saldıran", "Savunan", "1", "2" }, Assert.Single(_world.Social.LogOf(allianceId, AllianceLogKinds.Battle)).Args);
        }

        [Fact]
        public async Task Only_members_see_the_logs()
        {
            var (stranger, connection) = await Connect("lognone");
            Send(stranger, new ViewAllianceLogCommand { CommandId = Id(), Kind = AllianceLogKinds.Log }, CommandType.ViewAllianceLog);
            Assert.Equal(nameof(SocialRejectReason.NotInAlliance), LastAck(connection).Reason);
        }
    }
}
