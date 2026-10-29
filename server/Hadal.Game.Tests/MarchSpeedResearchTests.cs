using Hadal.Game.Persistence;
using Hadal.Game.World;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// The propulsion research tree (docs/24 §research): Thruster Efficiency and Hydrodynamic Hull speed up every march the
    /// server times (gathering, monster attacks, PvP), stacking with VIP; Quick Return speeds up a gathering team's way home.
    /// </summary>
    public sealed class MarchSpeedResearchTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public MarchSpeedResearchTests()
        {
            _world = new GameWorld(Catalog, new InMemoryPlayerRepository(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity, int thruster = 0, int hull = 0, int quickReturn = 0)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            session.ClearCrewForTests();
            var core = session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);
            core.Level = 10;
            session.StateForTests.UnsavedBuildings.Add(core);
            foreach (var id in Catalog.Heroes.Select(h => h.Id).Where(id => session.StateForTests.Heroes.All(h => h.HeroId != id)).Take(5))
                session.StateForTests.Heroes.Add(new OwnedHeroState { HeroId = id, Level = 60, Stars = 5, Slot = -1 });
            session.StateForTests.Troops.Clear();
            session.StateForTests.Troops[TroopRules.Key("harpoon", 3)] = 5000;
            session.StateForTests.Wallet[ResourceType.Titanium] = 1_000_000;
            var levels = session.StateForTests.ResearchLevels;
            if (thruster > 0) levels["thruster_efficiency"] = thruster;
            if (hull > 0) levels["hydrodynamic_hull"] = hull;
            if (quickReturn > 0) levels["quick_return"] = quickReturn;
            session.RefreshModifiersForTests();
            Send(session, new ViewWorldCommand { CommandId = "v" + Guid.NewGuid().ToString("N"), X = -1, Y = -1 }, CommandType.ViewWorld);
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : class, ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Where(d => d.Ack() != null).Last().Ack()!;

        /// <summary>Seconds at base march speed for a team of <paramref name="speed"/> (100 = base), before any bonus.</summary>
        private static int Raw(int fromX, int fromY, int toX, int toY, int speed) =>
            (int)Math.Ceiling(GatherRules.TravelSeconds(Catalog.World, fromX, fromY, toX, toY) * 100.0 / Math.Max(1, speed));

        private static int Seconds(DateTimeOffset from, DateTimeOffset to) => (int)Math.Round((to - from).TotalSeconds);

        /// <summary>A free node at least a few minutes out, so the bonus is well above rounding.</summary>
        private WorldNode FarNode(PlayerSession session, int minTiles = 10)
        {
            var (x, y) = _world.Map.PositionOf(session.PublicId)!.Value;
            return _world.Map.NodesForTests().Where(n => n.Type == "enkaz_alani" && n.Remaining > 0 && n.BusyUntil == null && GatherRules.Tiles(x, y, n.X, n.Y) >= minTiles)
                .OrderBy(n => GatherRules.Tiles(x, y, n.X, n.Y)).First();
        }

        [Fact]
        public void The_tree_adds_up_to_65_percent_and_turns_a_20_minute_trip_into_about_12()
        {
            Assert.True(Catalog.TryGetResearch("thruster_efficiency", out var thruster));
            Assert.True(Catalog.TryGetResearch("hydrodynamic_hull", out var hull));
            Assert.True(Catalog.TryGetResearch("quick_return", out var quick));
            Assert.All(new[] { thruster, hull, quick }, n => Assert.Equal("propulsion", n.Tree));
            // New players feel it soon: the first levels open at Research Center 1 and 3.
            Assert.Equal(1, ResearchRules.CenterLevelFor(thruster, 1, Catalog.Research));
            Assert.Equal(3, ResearchRules.CenterLevelFor(thruster, 2, Catalog.Research));
            Assert.Equal(3, ResearchRules.CenterLevelFor(quick, 1, Catalog.Research));
            Assert.Equal(8, ResearchRules.CenterLevelFor(hull, 1, Catalog.Research));

            int Percent(int t, int h) => ResearchRules.Modifiers(Catalog, new Dictionary<string, int> { ["thruster_efficiency"] = t, ["hydrodynamic_hull"] = h }).MarchSpeedPercent;
            Assert.Equal(4, Percent(1, 0));
            Assert.Equal(40, Percent(10, 0));
            Assert.Equal(65, Percent(10, 5));

            var full = ResearchRules.Modifiers(Catalog, new Dictionary<string, int> { ["thruster_efficiency"] = 10, ["hydrodynamic_hull"] = 5, ["quick_return"] = 5 });
            Assert.Equal(857, ResearchRules.Modifiers(Catalog, new Dictionary<string, int> { ["thruster_efficiency"] = 10 }).MarchSeconds(1200)); // 14:17
            Assert.Equal(727, full.MarchSeconds(1200)); // 12:07
            Assert.Equal(25, full.ReturnSpeedPercent);
            Assert.Equal(582, full.ReturnSeconds(727)); // the gathering team's way home: 9:42

            // Stacks with VIP (level 4: +5%) and alliance research (March Speed).
            VipRules.Apply(full, 4);
            AllianceResearchRules.Apply(full, new Dictionary<string, int> { ["war_march"] = 3 });
            Assert.Equal(65 + 5 + AllianceResearchRules.Bonus(AllianceResearchRules.Find("war_march")!, 3), full.MarchSpeedPercent);
        }

        [Fact]
        public async Task A_gathering_march_sails_faster_with_n_levels_and_comes_home_faster_with_quick_return()
        {
            var (plain, plainConnection) = await Connect("speed-plain");
            var node = FarNode(plain);
            Send(plain, new MarchGatherCommand { CommandId = "p1", NodeId = node.Id }, CommandType.MarchGather);
            Assert.Equal(CommandResultCode.Accepted, LastAck(plainConnection).Result);
            var slow = Assert.Single(plain.MarchesForTests);
            var speed = TroopRules.MarchSpeed(Catalog.Troops, slow.Troops.Keys);
            var raw = Raw(slow.FromX, slow.FromY, node.X, node.Y, speed);
            Assert.True(raw >= 120, $"trip of {raw}s is too short to measure");
            Assert.Equal(raw, Seconds(slow.StartedAt, slow.ArrivesAt));
            Assert.Equal(raw, Seconds(slow.GatheredAt, slow.HomeAt));

            foreach (var (thruster, hull) in new[] { (3, 0), (10, 0), (10, 5) })
            {
                var (fast, fastConnection) = await Connect($"speed-{thruster}-{hull}", thruster, hull, quickReturn: 5);
                var percent = thruster * 4 + hull * 5;
                Assert.Equal(percent, fast.ModifiersForTests.MarchSpeedPercent);
                var target = FarNode(fast);
                Send(fast, new MarchGatherCommand { CommandId = "f1", NodeId = target.Id }, CommandType.MarchGather);
                Assert.Equal(CommandResultCode.Accepted, LastAck(fastConnection).Result);
                var march = Assert.Single(fast.MarchesForTests);
                var fastRaw = Raw(march.FromX, march.FromY, target.X, target.Y, TroopRules.MarchSpeed(Catalog.Troops, march.Troops.Keys));
                var outbound = Seconds(march.StartedAt, march.ArrivesAt);
                Assert.Equal(PlayerModifiers.ApplyPercent(fastRaw, -percent), outbound);
                Assert.InRange(outbound, fastRaw / (1 + percent / 100.0) - 1, fastRaw / (1 + percent / 100.0) + 1);
                // Quick Return 5: +25% on the way home on top of the march speed.
                Assert.Equal(PlayerModifiers.ApplyPercent(outbound, -25), Seconds(march.GatheredAt, march.HomeAt));

                // Recalled while gathering: home in the (faster) way-home time too.
                _clock.UtcNow = march.ArrivesAt.AddSeconds(5);
                Send(fast, new RecallMarchCommand { CommandId = "r1", MarchId = march.Id }, CommandType.RecallMarch);
                Assert.Equal(CommandResultCode.Accepted, LastAck(fastConnection).Result);
                var back = fast.MarchesForTests.Single();
                Assert.InRange((back.HomeAt - _clock.UtcNow).TotalSeconds, outbound / 1.25 - 1, outbound / 1.25 + 1);
                _clock.UtcNow = back.HomeAt.AddSeconds(1);
                _world.Tick();
                Assert.Empty(fast.MarchesForTests);
            }
        }

        [Fact]
        public async Task Vip_and_research_stack_on_the_server()
        {
            var (session, connection) = await Connect("speed-vip", thruster: 10);
            Assert.True(await _world.SetVipPointsAsync("speed-vip", VipRules.Thresholds[4], default));
            Assert.Equal(45, session.ModifiersForTests.MarchSpeedPercent);

            var node = FarNode(session);
            Send(session, new MarchGatherCommand { CommandId = "v1", NodeId = node.Id }, CommandType.MarchGather);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var march = Assert.Single(session.MarchesForTests);
            var raw = Raw(march.FromX, march.FromY, node.X, node.Y, TroopRules.MarchSpeed(Catalog.Troops, march.Troops.Keys));
            Assert.Equal(PlayerModifiers.ApplyPercent(raw, -45), Seconds(march.StartedAt, march.ArrivesAt));
            // No Quick Return: the way home is as long as the way out.
            Assert.Equal(Seconds(march.StartedAt, march.ArrivesAt), Seconds(march.GatheredAt, march.HomeAt));
        }

        [Fact]
        public async Task A_monster_attack_sails_with_the_research_bonus_and_returns_at_the_same_speed()
        {
            var (session, connection) = await Connect("speed-hunt", thruster: 10, hull: 5, quickReturn: 5);
            var (x, y) = _world.Map.PositionOf(session.PublicId)!.Value;
            var target = _world.Map.MonstersForTests().Where(m => m.Type == "abyss_crawler" && m.Level <= 3 && GatherRules.Tiles(x, y, m.X, m.Y) >= 5)
                .OrderBy(m => GatherRules.Tiles(x, y, m.X, m.Y)).First();

            Send(session, new MarchAttackCommand { CommandId = "h1", MonsterId = target.Id }, CommandType.MarchAttack);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var march = Assert.Single(session.MarchesForTests);
            var raw = Raw(x, y, target.X, target.Y, TroopRules.MarchSpeed(Catalog.Troops, march.Troops.Keys));
            var outbound = Seconds(march.StartedAt, march.ArrivesAt);
            Assert.Equal(PlayerModifiers.ApplyPercent(raw, -65), outbound);
            Assert.True(outbound < raw * 0.62);
            // Quick Return is for gathering teams only.
            Assert.Equal(outbound, Seconds(march.GatheredAt, march.HomeAt));
        }

        [Fact]
        public async Task A_pvp_attack_sails_with_the_attackers_research_bonus()
        {
            var (attacker, attackerConnection) = await Connect("speed-pvp-a", thruster: 10, hull: 5, quickReturn: 5);
            var (defender, _) = await Connect("speed-pvp-b");
            Send(attacker, new MarchPvpCommand
            {
                CommandId = "a1", Mode = PvpRules.Attack, TargetKind = PvpRules.TargetBase, TargetId = defender.PublicId
            }, CommandType.MarchPvp);
            Assert.Equal(CommandResultCode.Accepted, LastAck(attackerConnection).Result);
            var march = attacker.MarchesForTests.Single(m => m.Pvp != null);
            var (x, y) = _world.Map.PositionOf(attacker.PublicId)!.Value;
            var (tx, ty) = _world.Map.PositionOf(defender.PublicId)!.Value;
            var raw = Raw(x, y, tx, ty, march.Pvp!.Speed);
            Assert.True(raw >= 60, $"trip of {raw}s is too short to measure");
            Assert.Equal(PlayerModifiers.ApplyPercent(raw, -65), Seconds(march.StartedAt, march.ArrivesAt));
        }
    }
}
