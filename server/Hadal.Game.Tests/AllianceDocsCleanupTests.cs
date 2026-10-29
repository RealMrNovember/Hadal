using Hadal.Game.Persistence;
using Hadal.Game.Social;
using Hadal.Game.World;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Roadmap §1 "Operasyon": documents of disbanded alliances ("a:&lt;allianceId&gt;:…" in hadal.alliance_docs) do not
    /// pile up. A disband removes them at once; start-up removes any left from before.
    /// </summary>
    public sealed class AllianceDocsCleanupTests
    {
        private readonly FakeClock _clock = new();

        private GameWorld World(InMemorySocialStore store) => new(new CatalogRuntime(TestSupport.Catalog), new InMemoryPlayerRepository(), store,
            new InMemoryWorldStore(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance);

        private static CommandEnvelope Envelope<T>(T command, CommandType type) where T : ICommand =>
            EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command));

        private static async Task<List<string>> Keys(InMemorySocialStore store) =>
            (await store.LoadDocsAsync(default)).Select(d => d.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();

        [Fact]
        public void Document_keys_name_their_alliance()
        {
            Assert.Equal("a1b2", SocialHub.AllianceOfDoc("a:a1b2:log"));
            Assert.Equal("a1b2", SocialHub.AllianceOfDoc("a:a1b2:research"));
            Assert.Null(SocialHub.AllianceOfDoc("p:a1b2:coins"));
            Assert.Null(SocialHub.AllianceOfDoc("a::log"));
            Assert.Null(SocialHub.AllianceOfDoc("a:nothing"));
        }

        [Fact]
        public async Task Start_up_removes_the_documents_of_alliances_that_no_longer_exist()
        {
            var store = new InMemorySocialStore();
            await store.InitializeAsync(default);
            var now = _clock.UtcNow;
            await store.SaveAllianceAsync(new AllianceRecord("alive", "Alive", "LIVE", "", "p1", now), default);
            await store.SaveMemberAsync(new MemberRecord("p1", "alive", "One", 10, now), default);
            foreach (var key in new[] { "a:alive:log", "a:alive:research", "a:gone:log", "a:gone:battle", "a:gone:gifts", "a:gone:research",
                         "a:gone:kills", "a:goner:log", "p:p1:coins", "p:left-player:coins" })
                await store.SaveDocAsync(key, key.EndsWith(":log") || key.EndsWith(":battle") ? "[]" : "{}", default);

            var world = World(store);
            await world.Social.InitializeAsync(default);

            Assert.Equal(new[] { "a:alive:log", "a:alive:research", "p:left-player:coins", "p:p1:coins" }, await Keys(store));
            Assert.Empty(world.Social.LogOf("gone", "log"));
        }

        [Fact]
        public async Task Disbanding_removes_the_alliances_documents_and_keeps_the_others()
        {
            var store = new InMemorySocialStore();
            var world = World(store);
            await world.Social.InitializeAsync(default);

            async Task<(PlayerSession Session, string AllianceId)> Found(string identity, string tag)
            {
                var connection = new RecordingConnection();
                var session = await world.ConnectAsync(identity, connection, default, identity);
                foreach (var type in Enum.GetValues<ResourceType>().Where(t => t != ResourceType.Unknown))
                    session.StateForTests.Wallet[type] = 1_000_000;
                world.HandleCommand(session, Envelope(new CreateAllianceCommand { CommandId = Guid.NewGuid().ToString("N"), Name = "House " + tag, Tag = tag },
                    CommandType.CreateAlliance));
                var id = TestSupport.Serializer.Deserialize<AllianceDto>(connection.Messages.Where(m => m.Delta != null)
                    .SelectMany(m => m.Delta().Changes).Last(c => c.EntityId == EntityIds.MyAlliance).Payload).Id;
                // A donation writes the research tree and the alliance log; a battle line writes the battle log.
                world.HandleCommand(session, Envelope(new DonateAllianceResearchCommand { CommandId = Guid.NewGuid().ToString("N") },
                    CommandType.DonateAllianceResearch));
                world.Social.LogBattle(id, "hub_level", _clock.UtcNow, "x", "2", "0", "0");
                return (session, id);
            }

            var (leaver, goneId) = await Found("docs-a", "GONE");
            var (_, keptId) = await Found("docs-b", "KEPT");
            await world.Social.DrainAsyncForTests();
            Assert.Contains(await Keys(store), k => k.StartsWith("a:" + goneId + ":", StringComparison.Ordinal));

            world.HandleCommand(leaver, Envelope(new LeaveAllianceCommand { CommandId = Guid.NewGuid().ToString("N") }, CommandType.LeaveAlliance));
            // A late battle line for the gone alliance (a world event still in flight) must not bring a document back.
            world.Social.LogBattle(goneId, "hub_level", _clock.UtcNow, "x", "3", "0", "0");
            await world.Social.DrainAsyncForTests();

            var keys = await Keys(store);
            Assert.DoesNotContain(keys, k => k.StartsWith("a:" + goneId + ":", StringComparison.Ordinal));
            Assert.Contains(keys, k => k.StartsWith("a:" + keptId + ":", StringComparison.Ordinal));
            Assert.Contains("p:" + leaver.PublicId + ":coins", keys); // the player's own purse stays with the player
            Assert.Empty(world.Social.LogOf(goneId, "battle"));
            Assert.Empty(world.Social.ResearchLevelsOf(goneId));
        }
    }
}
