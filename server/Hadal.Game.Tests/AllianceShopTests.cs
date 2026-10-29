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
    /// <summary>Alliance coins and the alliance shop (docs/31 step 4): help coins with the daily cap, buying, weekly stock.</summary>
    public sealed class AllianceShopTests
    {
        private readonly FakeClock _clock = new() { UtcNow = new DateTimeOffset(2026, 10, 28, 12, 0, 0, TimeSpan.Zero) }; // a Wednesday
        private readonly InMemorySocialStore _store = new();
        private GameWorld _world;

        public AllianceShopTests()
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

        private void Buy(PlayerSession session, string itemId, int count = 1) =>
            Send(session, new BuyAllianceItemCommand { CommandId = Id(), ItemId = itemId, Count = count }, CommandType.BuyAllianceItem);

        [Fact]
        public void The_shop_week_starts_on_monday_and_help_coins_stop_at_the_daily_cap()
        {
            var monday = new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero);
            Assert.Equal(AllianceShopRules.Week(monday.ToUnixTimeMilliseconds()), AllianceShopRules.Week(monday.AddDays(6.9).ToUnixTimeMilliseconds()));
            Assert.NotEqual(AllianceShopRules.Week(monday.ToUnixTimeMilliseconds()), AllianceShopRules.Week(monday.AddMinutes(-1).ToUnixTimeMilliseconds()));
            Assert.Equal(60_000, AllianceShopRules.MsToReset(monday.AddMinutes(-1).ToUnixTimeMilliseconds()));
            Assert.Equal(60, AllianceShopRules.HelpCoins(3, 0));
            Assert.Equal(10, AllianceShopRules.HelpCoins(3, 990));
            Assert.Equal(0, AllianceShopRules.HelpCoins(1, 1_000));
            Assert.All(AllianceShopRules.Offers, o => Assert.True(o.Price > 0 && o.Stock > 0));
        }

        [Fact]
        public async Task Helping_earns_coins_and_the_player_sees_them()
        {
            var team = await Alliance("shophelp", 3);
            foreach (var asker in team.Skip(1))
            {
                asker.S.StateForTests.Healing = new HealState
                {
                    Troops = { ["armored:1"] = 10 }, StartedAt = _clock.UtcNow, EndsAt = _clock.UtcNow.AddHours(5)
                };
                Send(asker.S, new RequestHelpCommand { CommandId = Id(), Kind = HelpKinds.Heal }, CommandType.RequestHelp);
            }

            Send(team[0].S, new HelpAllCommand { CommandId = Id() }, CommandType.HelpAll);
            Assert.Equal(2 * AllianceShopRules.CoinsPerHelp, _world.Social.CoinsOf(team[0].S.PublicId, _clock.UtcNow));
            var coins = Latest<AllianceCoinsDto>(team[0].C, EntityIds.AllianceCoins);
            Assert.Equal((40L, 40), (coins.Coins, coins.HelpCoinsToday));
            Assert.Equal(AllianceShopRules.Offers.Length, coins.Shop.Length);
        }

        [Fact]
        public async Task Buying_takes_coins_puts_the_item_in_the_bag_and_respects_the_weekly_stock()
        {
            var team = await Alliance("shopbuy", 1);
            var (buyer, connection) = team[0];
            Buy(buyer, PvpRules.Shield8h);
            Assert.Equal(nameof(SocialRejectReason.NotEnoughCoins), LastAck(connection).Reason);

            _world.Social.GrantCoinsForTests(buyer.PublicId, 5_000, _clock.UtcNow);
            Buy(buyer, PvpRules.Shield8h);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(1, buyer.StateForTests.Items[PvpRules.Shield8h]);
            Assert.Equal(3_800, _world.Social.CoinsOf(buyer.PublicId, _clock.UtcNow));
            Assert.Equal(0, Latest<AllianceCoinsDto>(connection, EntityIds.AllianceCoins).Shop.Single(s => s.ItemId == PvpRules.Shield8h).Left);

            Buy(buyer, PvpRules.Shield8h);
            Assert.Equal(nameof(SocialRejectReason.SoldOut), LastAck(connection).Reason);
            Buy(buyer, "no_such_item");
            Assert.Equal(nameof(SocialRejectReason.NotAllowed), LastAck(connection).Reason);

            _clock.Advance(TimeSpan.FromDays(7)); // next Monday has passed: the stock is back
            Buy(buyer, PvpRules.Shield8h);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(2, buyer.StateForTests.Items[PvpRules.Shield8h]);
        }

        [Fact]
        public async Task Only_members_shop_and_coins_survive_a_restart()
        {
            var (loner, connection) = await Connect("shoplone");
            _world.Social.GrantCoinsForTests(loner.PublicId, 1_000, _clock.UtcNow);
            Buy(loner, "speed_any_60");
            Assert.Equal(nameof(SocialRejectReason.NotInAlliance), LastAck(connection).Reason);

            await _world.Social.DrainAsyncForTests();
            _world = NewWorld();
            await _world.Social.InitializeAsync(default);
            Assert.Equal(1_000, _world.Social.CoinsOf(loner.PublicId, _clock.UtcNow));
        }
    }
}
