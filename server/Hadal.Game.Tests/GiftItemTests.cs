using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>Overseer gifts in the bag: heroes at a chosen level and stars, troop packs.</summary>
    public sealed class GiftItemTests
    {
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public GiftItemTests()
        {
            _world = TestSupport.World(_clock, new InMemoryPlayerRepository());
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            return (session, connection);
        }

        private void Open(PlayerSession session, string id, int count = 1) =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(new OpenItemCommand { CommandId = Guid.NewGuid().ToString("N"), ItemId = id, Count = count },
                CommandType.OpenItem, TestSupport.Serializer.Serialize(new OpenItemCommand { CommandId = Guid.NewGuid().ToString("N"), ItemId = id, Count = count })));

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        [Fact]
        public void Gift_ids_are_checked_against_the_catalog()
        {
            var catalog = TestSupport.Catalog;
            var hero = catalog.Heroes[0].Id;
            Assert.True(GiftItems.IsValid(catalog, GiftItems.Hero(hero, 30, 3)));
            Assert.False(GiftItems.IsValid(catalog, GiftItems.Hero(hero, 31, 3))); // above the 3-star cap
            Assert.False(GiftItems.IsValid(catalog, GiftItems.Hero("nobody", 1, 1)));
            Assert.False(GiftItems.IsValid(catalog, GiftItems.Hero(hero, 1, 9)));
            Assert.True(GiftItems.IsValid(catalog, GiftItems.Troops("armored", 3, 500)));
            Assert.False(GiftItems.IsValid(catalog, GiftItems.Troops("armored", 99, 500)));
            Assert.False(GiftItems.IsValid(catalog, GiftItems.Troops("dragons", 1, 5)));
            Assert.True(_world.IsGrantableItem(GiftItems.Troops("arc", 1, 10)));
        }

        [Fact]
        public async Task A_hero_gift_recruits_the_hero_and_raises_an_owned_one()
        {
            var (session, connection) = await Connect("gift-hero");
            var fresh = TestSupport.Catalog.Heroes.First(h => session.StateForTests.Heroes.All(o => o.HeroId != h.Id)).Id;
            var gift = GiftItems.Hero(fresh, 25, 3);
            Assert.True(await _world.GrantAsync("gift-hero", new Dictionary<ResourceType, long>(), default, items: new Dictionary<string, int> { [gift] = 2 }));
            Assert.Equal(2, session.StateForTests.Items[gift]);

            Open(session, gift);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var hero = session.StateForTests.Heroes.Single(h => h.HeroId == fresh);
            Assert.Equal((25, 3), (hero.Level, hero.Stars));

            // A weaker gift never lowers the hero; a stronger one raises it.
            var weaker = GiftItems.Hero(fresh, 5, 1);
            var stronger = GiftItems.Hero(fresh, 40, 4);
            await _world.GrantAsync("gift-hero", new Dictionary<ResourceType, long>(), default, items: new Dictionary<string, int> { [weaker] = 1, [stronger] = 1 });
            Open(session, weaker);
            Assert.Equal((25, 3), (hero.Level, hero.Stars));
            Open(session, stronger);
            Assert.Equal((40, 4), (hero.Level, hero.Stars));
        }

        [Fact]
        public async Task A_troop_pack_joins_the_army()
        {
            var (session, connection) = await Connect("gift-troops");
            var pack = GiftItems.Troops("harpoon", 2, 300);
            await _world.GrantAsync("gift-troops", new Dictionary<ResourceType, long>(), default, items: new Dictionary<string, int> { [pack] = 3 });

            Open(session, pack, 2);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(600, session.StateForTests.Troops[TroopRules.Key("harpoon", 2)]);
            Assert.Equal(1, session.StateForTests.Items[pack]);

            Open(session, pack, 5);
            Assert.Equal(nameof(QuestRejectReason.NotEnoughItems), LastAck(connection).Reason);
        }
    }
}
