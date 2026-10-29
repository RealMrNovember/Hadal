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
    /// <summary>Alliance gifts (docs/31 step 5): dropping, collecting once, chests by gift level, coins, expiry, storage.</summary>
    public sealed class AllianceGiftTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemorySocialStore _store = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private GameWorld _world;

        public AllianceGiftTests()
        {
            _world = NewWorld();
        }

        private GameWorld NewWorld() => new(new CatalogRuntime(TestSupport.Catalog), _repository, _store, new InMemoryWorldStore(), _clock,
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

        private void Collect(PlayerSession session, string giftId = "") =>
            Send(session, new CollectAllianceGiftCommand { CommandId = Id(), GiftId = giftId }, CommandType.CollectAllianceGift);

        private static int Held(PlayerSession session, string item) => session.StateForTests.Items.TryGetValue(item, out var n) ? n : 0;

        [Fact]
        public void Levels_points_and_chests_follow_the_table()
        {
            Assert.Equal(1, AllianceGiftRules.Level(0));
            Assert.Equal(2, AllianceGiftRules.Level(2_000));
            Assert.Equal(10, AllianceGiftRules.Level(99_000_000));
            Assert.Equal(17, AllianceGiftRules.Points(AllianceGiftRules.Monster, 12));
            Assert.Equal(20, AllianceGiftRules.Points(AllianceGiftRules.Purchase, 500));
            Assert.Equal(200, AllianceGiftRules.Points(AllianceGiftRules.Purchase, 10_000));
            Assert.Equal(("chest_bronze", "chest_silver", "chest_gold"), (AllianceGiftRules.Chest(3), AllianceGiftRules.Chest(4), AllianceGiftRules.Chest(7)));
        }

        [Fact]
        public async Task A_monster_win_drops_a_gift_every_member_collects_once()
        {
            var (team, allianceId) = await Alliance("giftmon", 2);
            _world.Social.GiftsFrom(team[0].S, new List<(string, long)> { (EventActions.MonsterKill, 1), (EventActions.MonsterLevels, 12) }, _clock.UtcNow);
            var later = await Connect("giftmon-late");
            Send(later.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);

            var gift = Assert.Single(Latest<AllianceGiftsDto>(team[1].C, EntityIds.AllianceGifts).Gifts);
            Assert.Equal((AllianceGiftRules.Monster, 12L, false, team[0].S.DisplayName), (gift.Source, gift.Arg, gift.Collected, gift.ByName));

            Collect(team[1].S, gift.Id);
            Assert.Equal(CommandResultCode.Accepted, LastAck(team[1].C).Result);
            Assert.Equal(1, Held(team[1].S, "chest_bronze"));
            Assert.Equal(AllianceGiftRules.CoinsPerGift, _world.Social.CoinsOf(team[1].S.PublicId, _clock.UtcNow));
            Assert.Equal(17, _world.Social.GiftPointsOf(allianceId));
            Collect(team[1].S, gift.Id);
            Assert.Equal(nameof(SocialRejectReason.NothingToCollect), LastAck(team[1].C).Reason);

            Collect(team[0].S); // the one who earned it collects too
            Assert.Equal(1, Held(team[0].S, "chest_bronze"));
            Assert.Equal(34, _world.Social.GiftPointsOf(allianceId));

            Collect(later.S); // joined after the drop: nothing
            Assert.Equal(nameof(SocialRejectReason.NothingToCollect), LastAck(later.C).Reason);
        }

        [Fact]
        public async Task Purchases_and_event_ranks_drop_gifts_and_a_higher_level_gives_better_chests()
        {
            var (team, allianceId) = await Alliance("giftbuy", 2);
            Assert.NotNull(_world.Events.DropGift); // event ranks are wired to the hub
            var pack = StorePacks.All.Last();
            for (var i = 0; i < 3; i++)
                Assert.True(await _world.CreditPurchaseAsync("giftbuy0", pack.Id, default));
            _world.Events.DropGift!(team[0].S.PublicId, AllianceGiftRules.Event, 0, _clock.UtcNow);
            Assert.Equal(4, Latest<AllianceGiftsDto>(team[1].C, EntityIds.AllianceGifts).Gifts.Length);

            // Collect all: each chest is the one of the level reached so far.
            Collect(team[1].S);
            Assert.Equal(CommandResultCode.Accepted, LastAck(team[1].C).Result);
            var points = 3 * AllianceGiftRules.Points(AllianceGiftRules.Purchase, pack.Hadalite) + AllianceGiftRules.Points(AllianceGiftRules.Event, 0);
            Assert.Equal(points, _world.Social.GiftPointsOf(allianceId));
            var chests = Held(team[1].S, "chest_bronze") + Held(team[1].S, "chest_silver") + Held(team[1].S, "chest_gold");
            Assert.Equal(4, chests);
            Assert.Equal(AllianceGiftRules.Level(points), Latest<AllianceGiftsDto>(team[1].C, EntityIds.AllianceGifts).Level);
            Assert.Equal(4 * AllianceGiftRules.CoinsPerGift, _world.Social.CoinsOf(team[1].S.PublicId, _clock.UtcNow));
        }

        [Fact]
        public async Task Gifts_expire_and_the_pool_survives_a_restart()
        {
            var (team, allianceId) = await Alliance("giftold", 2);
            _world.Social.DropGift(team[0].S.PublicId, AllianceGiftRules.Event, 0, _clock.UtcNow);
            Collect(team[0].S);
            _world.Social.DropGift(team[0].S.PublicId, AllianceGiftRules.Event, 0, _clock.UtcNow);
            await _world.Social.DrainAsyncForTests();

            _world = NewWorld();
            await _world.Social.InitializeAsync(default);
            Assert.Equal(50, _world.Social.GiftPointsOf(allianceId));

            _clock.Advance(AllianceGiftRules.Lifetime + TimeSpan.FromMinutes(1));
            await _world.Map.InitializeAsync(default);
            var back = await Connect("giftold1");
            Collect(back.S);
            Assert.Equal(nameof(SocialRejectReason.NothingToCollect), LastAck(back.C).Reason);
        }
    }
}
