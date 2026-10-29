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
    /// <summary>The alliance's rankings (docs/31 step 7): power, PvP kills and research donations.</summary>
    public sealed class AllianceRankingTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemorySocialStore _store = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private GameWorld _world;

        public AllianceRankingTests()
        {
            _world = NewWorld();
        }

        private GameWorld NewWorld() => new(new CatalogRuntime(TestSupport.Catalog), _repository, _store, new InMemoryWorldStore(), _clock,
            TestSupport.Serializer, NullLogger<GameWorld>.Instance);

        private async Task<(PlayerSession S, RecordingConnection C)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            foreach (var type in new[] { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Food })
                session.StateForTests.Wallet[type] = 10_000_000;
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

        private async Task<List<(PlayerSession S, RecordingConnection C)>> Alliance(string name, int count)
        {
            var members = new List<(PlayerSession S, RecordingConnection C)> { await Connect(name + "0") };
            Send(members[0].S, new CreateAllianceCommand { CommandId = Id(), Name = "House " + name, Tag = name[..4].ToUpperInvariant() }, CommandType.CreateAlliance);
            var allianceId = Latest<AllianceDto>(members[0].C, EntityIds.MyAlliance).Id;
            for (var i = 1; i < count; i++)
            {
                var m = await Connect(name + i);
                Send(m.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
                members.Add(m);
            }

            return members;
        }

        private AllianceRankingDto View(PlayerSession session, RecordingConnection connection, string kind)
        {
            Send(session, new ViewAllianceRankingCommand { CommandId = Id(), Kind = kind }, CommandType.ViewAllianceRanking);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            return Latest<AllianceRankingDto>(connection, EntityIds.AllianceRanking + kind);
        }

        [Fact]
        public async Task Every_member_is_ranked_by_power_kills_and_donations()
        {
            var team = await Alliance("rankall", 3);
            var power = View(team[0].S, team[0].C, AllianceRankingKinds.Power);
            Assert.Equal(3, power.Entries.Length);
            Assert.Equal(new[] { 1, 2, 3 }, power.Entries.Select(e => e.Rank));
            Assert.True(power.Entries[0].Value >= power.Entries[1].Value && power.Entries[1].Value >= power.Entries[2].Value);

            _world.Social.AddKills(team[2].S.PublicId, 500, _clock.UtcNow);
            _world.Social.AddKills(team[1].S.PublicId, 200, _clock.UtcNow);
            _world.Social.AddKills(team[2].S.PublicId, 50, _clock.UtcNow);
            var kills = View(team[0].S, team[0].C, AllianceRankingKinds.Kills);
            Assert.Equal((team[2].S.PublicId, 550L), (kills.Entries[0].PlayerId, kills.Entries[0].Value));
            Assert.Equal((team[1].S.PublicId, 200L), (kills.Entries[1].PlayerId, kills.Entries[1].Value));
            Assert.Equal(0, kills.Entries[2].Value);

            for (var i = 0; i < 3; i++)
                Send(team[1].S, new DonateAllianceResearchCommand { CommandId = Id() }, CommandType.DonateAllianceResearch);
            Send(team[0].S, new DonateAllianceResearchCommand { CommandId = Id() }, CommandType.DonateAllianceResearch);
            var donations = View(team[2].S, team[2].C, AllianceRankingKinds.Donations);
            Assert.Equal((team[1].S.PublicId, 300L), (donations.Entries[0].PlayerId, donations.Entries[0].Value));
            Assert.Equal((team[0].S.PublicId, 100L), (donations.Entries[1].PlayerId, donations.Entries[1].Value));
        }

        [Fact]
        public async Task Kills_outside_an_alliance_are_not_counted_and_kills_survive_a_restart()
        {
            var (loner, connection) = await Connect("rankloner");
            _world.Social.AddKills(loner.PublicId, 100, _clock.UtcNow);
            Send(loner, new ViewAllianceRankingCommand { CommandId = Id(), Kind = AllianceRankingKinds.Kills }, CommandType.ViewAllianceRanking);
            Assert.Equal(nameof(SocialRejectReason.NotInAlliance), LastAck(connection).Reason);

            var team = await Alliance("ranksave", 1);
            _world.Social.AddKills(team[0].S.PublicId, 77, _clock.UtcNow);
            await _world.DisconnectAsync(team[0].S, team[0].C, default);
            await _world.Social.DrainAsyncForTests();

            _world = NewWorld();
            await _world.Social.InitializeAsync(default);
            var back = await Connect("ranksave0");
            Assert.Equal(77, View(back.S, back.C, AllianceRankingKinds.Kills).Entries.Single().Value);
        }
    }
}
