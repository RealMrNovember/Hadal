using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>VIP (VipRules): sign-in streak, daily chest, VIP items, purchases and the level bonuses.</summary>
    public sealed class VipTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public VipTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static VipStateDto Vip(RecordingConnection connection) =>
            TestSupport.Serializer.Deserialize<VipStateDto>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.Vip).Payload);

        [Fact]
        public void Levels_and_sign_in_points_follow_the_table()
        {
            Assert.Equal(0, VipRules.Level(99));
            Assert.Equal(1, VipRules.Level(100));
            Assert.Equal(10, VipRules.Level(10_000_000));
            Assert.Equal(new long[] { 40, 60, 80 }, new[] { 1, 2, 3 }.Select(VipRules.LoginPoints));
            Assert.Equal(200, VipRules.LoginPoints(30));
            Assert.True(VipRules.TryParseItem("vip_500", out var points) && points == 500);
            Assert.False(VipRules.TryParseItem("vip_x", out _));
        }

        [Fact]
        public async Task The_sign_in_streak_grows_on_consecutive_days_and_breaks_after_a_gap()
        {
            var (session, connection) = await Connect("vip-streak");
            Assert.True(connection.Messages.First().Snapshot().Vip!.LoginReady);

            Send(session, new ClaimVipLoginCommand { CommandId = "a" }, CommandType.ClaimVipLogin);
            Assert.Equal((40L, 1), (Vip(connection).Points, Vip(connection).Streak));
            Send(session, new ClaimVipLoginCommand { CommandId = "b" }, CommandType.ClaimVipLogin);
            Assert.Equal("AlreadyClaimed", LastAck(connection).Reason);

            _clock.Advance(TimeSpan.FromDays(1));
            Send(session, new ClaimVipLoginCommand { CommandId = "c" }, CommandType.ClaimVipLogin);
            Assert.Equal((100L, 2), (Vip(connection).Points, Vip(connection).Streak));

            _clock.Advance(TimeSpan.FromDays(2));
            Send(session, new ClaimVipLoginCommand { CommandId = "d" }, CommandType.ClaimVipLogin);
            Assert.Equal((140L, 1), (Vip(connection).Points, Vip(connection).Streak));
            Assert.Equal(1, Vip(connection).Level);
        }

        [Fact]
        public async Task The_daily_chest_needs_vip_one_and_opens_once_a_day()
        {
            var (session, connection) = await Connect("vip-chest");
            Send(session, new ClaimVipChestCommand { CommandId = "a" }, CommandType.ClaimVipChest);
            Assert.Equal("VipTooLow", LastAck(connection).Reason);

            await _world.GrantAsync("vip-chest", new Dictionary<ResourceType, long>(), default, items: new Dictionary<string, int> { [VipRules.Item(500)] = 2 });
            Send(session, new OpenItemCommand { CommandId = "b", ItemId = VipRules.Item(500), Count = 2 }, CommandType.OpenItem);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal((1000L, 2), (Vip(connection).Points, Vip(connection).Level));
            Assert.True(Vip(connection).ChestReady);

            Send(session, new ClaimVipChestCommand { CommandId = "c" }, CommandType.ClaimVipChest);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.False(Vip(connection).ChestReady);
            Send(session, new ClaimVipChestCommand { CommandId = "d" }, CommandType.ClaimVipChest);
            Assert.Equal("AlreadyClaimed", LastAck(connection).Reason);
        }

        [Fact]
        public async Task Purchases_credit_hadalite_and_vip_points_live_and_offline()
        {
            var (session, _) = await Connect("vip-buy");
            var before = session.StateForTests.Wallet.TryGetValue(ResourceType.Hadalite, out var h) ? h : 0;
            Assert.True(await _world.CreditPurchaseAsync("vip-buy", "hadalite_550", default));
            Assert.Equal(before + 550, session.StateForTests.Wallet[ResourceType.Hadalite]);
            Assert.Equal(500, session.StateForTests.VipPoints);
            Assert.False(await _world.CreditPurchaseAsync("vip-buy", "no_such_pack", default));

            // Offline: written to the stored player.
            var (other, otherConnection) = await Connect("vip-offline");
            await _world.DisconnectAsync(other, otherConnection, default);
            Assert.True(await _world.CreditPurchaseAsync("vip-offline", "hadalite_100", default));
            var stored = await _repository.LoadByIdentityAsync("vip-offline", default);
            Assert.Equal(100, stored!.VipPoints);
        }

        [Fact]
        public void Vip_bonuses_speed_up_building_and_add_a_builder_at_six()
        {
            var modifiers = new PlayerModifiers();
            VipRules.Apply(modifiers, 6);
            Assert.Equal(-12, modifiers.BuildTimePercent);
            Assert.Equal(1, modifiers.ExtraBuilders);
            Assert.Equal(8, modifiers.ProductionPercent[ResourceType.Titanium]);
            Assert.False(modifiers.ProductionPercent.ContainsKey(ResourceType.Hadalite));
        }
    }
}
