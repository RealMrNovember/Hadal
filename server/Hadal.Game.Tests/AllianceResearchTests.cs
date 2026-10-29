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
    /// <summary>Alliance research (docs/31 step 6): donations, levels, focus, the daily limit and the members' bonuses.</summary>
    public sealed class AllianceResearchTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemorySocialStore _store = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private GameWorld _world;

        public AllianceResearchTests()
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

        private async Task<(List<(PlayerSession S, RecordingConnection C)> Team, string AllianceId)> Alliance(string name, int count)
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

            return (members, allianceId);
        }

        private void Donate(PlayerSession session) =>
            Send(session, new DonateAllianceResearchCommand { CommandId = Id() }, CommandType.DonateAllianceResearch);

        private static long Wallet(PlayerSession s, ResourceType type) => s.StateForTests.Wallet.TryGetValue(type, out var n) ? n : 0;

        [Fact]
        public void The_tree_applies_its_bonuses_to_the_modifiers()
        {
            Assert.Equal(1_000, AllianceResearchRules.PointsToNext(0));
            Assert.Equal(100_000, AllianceResearchRules.PointsToNext(9));
            Assert.Equal(0, AllianceResearchRules.PointsToNext(AllianceResearchRules.MaxLevel));
            Assert.Equal(15_000, AllianceResearchRules.DonationCost(2));
            var modifiers = new PlayerModifiers();
            AllianceResearchRules.Apply(modifiers, new Dictionary<string, int> { ["dev_build"] = 3, ["dev_gather"] = 2, ["war_march"] = 1, ["war_load"] = 4 });
            Assert.Equal((-3, 4, 2, 12), (modifiers.BuildTimePercent, modifiers.GatherPercent, modifiers.MarchSpeedPercent, modifiers.MarchLoadPercent));
        }

        [Fact]
        public async Task Donations_cost_the_focus_resource_and_level_the_node_for_every_member()
        {
            var (team, allianceId) = await Alliance("resdon", 2);
            var (donor, connection) = team[0];
            var titanium = Wallet(donor, ResourceType.Titanium);
            Donate(donor);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(titanium - AllianceResearchRules.DonationCost(0), Wallet(donor, ResourceType.Titanium));
            var research = Latest<AllianceResearchDto>(connection, EntityIds.AllianceResearch);
            Assert.Equal(("dev_production", 100L, AllianceResearchRules.DonationsPerDay - 1, 100L),
                (research.Focus, research.Nodes.Single(n => n.Id == "dev_production").Progress, research.DonationsLeft, research.MyDonated));

            for (var i = 0; i < 9; i++)
                Donate(donor);
            Assert.Equal(1, _world.Social.ResearchLevelsOf(allianceId)["dev_production"]);
            Assert.Equal(1, team[1].S.ModifiersForTests.ProductionPercent[ResourceType.Titanium]); // the mate gets it too
            var line = _world.Social.LogOf(allianceId, AllianceLogKinds.Log).Last();
            Assert.Equal(("research", "dev_production", "1"), (line.Type, line.Args[1], line.Args[2]));
        }

        [Fact]
        public async Task Twenty_donations_a_day_then_tomorrow()
        {
            var (team, _) = await Alliance("reslim", 1);
            var (donor, connection) = team[0];
            for (var i = 0; i < AllianceResearchRules.DonationsPerDay; i++)
                Donate(donor);
            Donate(donor);
            Assert.Equal(nameof(SocialRejectReason.DonationLimit), LastAck(connection).Reason);
            _clock.Advance(TimeSpan.FromDays(1));
            Donate(donor);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
        }

        [Fact]
        public async Task Officers_pick_the_focus_and_donations_follow_it()
        {
            var (team, _) = await Alliance("resfoc", 2);
            Send(team[1].S, new SetResearchFocusCommand { CommandId = Id(), NodeId = "war_march" }, CommandType.SetResearchFocus);
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), LastAck(team[1].C).Reason);
            Send(team[0].S, new SetResearchFocusCommand { CommandId = Id(), NodeId = "war_march" }, CommandType.SetResearchFocus);
            Assert.Equal(CommandResultCode.Accepted, LastAck(team[0].C).Result);

            var food = Wallet(team[1].S, ResourceType.Food);
            Donate(team[1].S);
            Assert.Equal(food - AllianceResearchRules.DonationCost(0), Wallet(team[1].S, ResourceType.Food));
            Assert.Equal("war_march", Latest<AllianceResearchDto>(team[1].C, EntityIds.AllianceResearch).Focus);
        }

        [Fact]
        public async Task Leaving_ends_the_bonuses_and_the_tree_survives_a_restart()
        {
            var (team, allianceId) = await Alliance("resleave", 2);
            for (var i = 0; i < 10; i++)
                Donate(team[0].S);
            Assert.Equal(1, team[1].S.ModifiersForTests.ProductionPercent[ResourceType.Titanium]);
            Send(team[1].S, new LeaveAllianceCommand { CommandId = Id() }, CommandType.LeaveAlliance);
            Assert.False(team[1].S.ModifiersForTests.ProductionPercent.ContainsKey(ResourceType.Titanium));

            await _world.DisconnectAsync(team[0].S, team[0].C, default);
            await _world.Social.DrainAsyncForTests();
            _world = NewWorld();
            await _world.Social.InitializeAsync(default);
            Assert.Equal(1, _world.Social.ResearchLevelsOf(allianceId)["dev_production"]);
            var (back, _) = await Connect("resleave0");
            Assert.Equal(1, back.ModifiersForTests.ProductionPercent[ResourceType.Titanium]); // applied before the snapshot
        }
    }
}
