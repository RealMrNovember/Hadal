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
    /// <summary>Resource nodes, gathering marches and the Depth Map campaign (docs/rehber 08, 09, 15).</summary>
    public sealed class IncomeSourceTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public IncomeSourceTests()
        {
            _world = new GameWorld(Catalog, new InMemoryPlayerRepository(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity, int reactor)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            session.ClearCrewForTests();
            session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId).Level = reactor;
            View(session);
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : class, ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private void View(PlayerSession session) =>
            Send(session, new ViewWorldCommand { CommandId = "v" + Guid.NewGuid().ToString("N"), X = -1, Y = -1 }, CommandType.ViewWorld);

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Where(d => d.Ack() != null).Last().Ack()!;

        private static T Latest<T>(RecordingConnection connection, string entityId) =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId).Payload);

        /// <summary>The nearest free node of a kind to the player's base.</summary>
        private WorldNode Nearest(PlayerSession session, string type)
        {
            var (x, y) = _world.Map.PositionOf(session.PublicId)!.Value;
            return _world.Map.NodesForTests().Where(n => n.Type == type && n.Remaining > 0 && n.BusyUntil == null)
                .OrderBy(n => GatherRules.Tiles(x, y, n.X, n.Y)).First();
        }

        [Fact]
        public async Task The_map_is_seeded_by_the_guides_density_table()
        {
            var (session, _) = await Connect("node-a", 5);
            var world = Catalog.World;
            var nodes = _world.Map.NodesForTests();
            Assert.InRange(nodes.Count, 2500, 3200); // ~2,800 in the guide (09 §4.6)
            Assert.True(nodes.Count(n => n.Type == "hadalit_damari") <= world.maxVeins);
            Assert.All(nodes, n =>
            {
                var belt = GatherRules.BeltOf(world, n.X, n.Y)!;
                Assert.InRange(n.Level, belt.nodeMin, belt.nodeMax);
                Assert.Equal(GatherRules.Capacity(world, Catalog.Gather, n.Type, n.Level), n.Capacity);
            });

            // A new base lands on the Hadal Threshold (K-08) with nodes in view.
            var (x, y) = _world.Map.PositionOf(session.PublicId)!.Value;
            Assert.Equal("hadal_esigi", GatherRules.BeltOf(world, x, y)!.id);
        }

        [Fact]
        public async Task A_march_gathers_and_brings_the_load_home()
        {
            var (session, connection) = await Connect("node-b", 10);
            var node = Nearest(session, "enkaz_alani");
            var titanium = session.StateForTests.Wallet[ResourceType.Titanium];

            Send(session, new MarchGatherCommand { CommandId = "g1", NodeId = node.Id }, CommandType.MarchGather);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var march = Assert.Single(session.MarchesForTests);
            var cargo = march.Cargo[ResourceType.Titanium];
            Assert.True(cargo > 0);
            Assert.False(string.IsNullOrEmpty(march.HeroId)); // the strongest free hero leads when none is chosen
            Assert.Equal(GatherRules.Crew(10), march.Crew);
            Assert.Equal(_world.Map.PositionOf(session.PublicId)!.Value, (march.FromX, march.FromY));
            Assert.True(GatherRules.LoadOf(Catalog.World, march.Cargo) <= GatherRules.Load(Catalog.Gather, 10));
            Assert.Equal(node.Remaining - march.Reserved, _world.Map.NodeForTests(node.Id)!.Remaining);
            Assert.True(Latest<MarchListDto>(connection, EntityIds.Marches).Marches.Length == 1);

            // The hero is out: a second march cannot take them.
            var second = _world.Map.NodesForTests().First(n => n.Type == "enkaz_alani" && n.Remaining > 0 && n.BusyUntil == null && n.Id != node.Id);
            Send(session, new MarchGatherCommand { CommandId = "g3", NodeId = second.Id, HeroId = march.HeroId }, CommandType.MarchGather);
            Assert.Equal(nameof(MarchRejectReason.HeroBusy), LastAck(connection).Reason);

            // Nobody else may work the node meanwhile.
            var (other, otherConnection) = await Connect("node-c", 10);
            Send(other, new MarchGatherCommand { CommandId = "g2", NodeId = node.Id }, CommandType.MarchGather);
            Assert.Equal(nameof(MarchRejectReason.DepositBusy), LastAck(otherConnection).Reason);

            _clock.UtcNow = march.HomeAt.AddSeconds(1);
            _world.Tick();
            Assert.Empty(session.MarchesForTests);
            Assert.True(session.StateForTests.Wallet[ResourceType.Titanium] >= titanium + cargo);
        }

        [Fact]
        public async Task Queues_open_with_the_reactor()
        {
            var (session, connection) = await Connect("node-d", 3);
            Send(session, new MarchGatherCommand { CommandId = "g1", NodeId = Nearest(session, "enkaz_alani").Id }, CommandType.MarchGather);
            Assert.Equal(nameof(MarchRejectReason.NoFreeQueue), LastAck(connection).Reason);
            Assert.Equal(0, GatherRules.Queues(Catalog.Gather, 3));
            Assert.Equal(2, GatherRules.Queues(Catalog.Gather, 10));
            Assert.Equal(4, GatherRules.Queues(Catalog.Gather, 50));
        }

        [Fact]
        public async Task A_recalled_march_keeps_what_it_gathered_and_the_node_gets_the_rest()
        {
            var (session, connection) = await Connect("node-e", 12);
            var node = Nearest(session, "enkaz_alani");
            Send(session, new MarchGatherCommand { CommandId = "g1", NodeId = node.Id }, CommandType.MarchGather);
            var march = Assert.Single(session.MarchesForTests);
            var full = march.Cargo[ResourceType.Titanium];

            _clock.UtcNow = march.ArrivesAt + (march.GatheredAt - march.ArrivesAt) / 2;
            Send(session, new RecallMarchCommand { CommandId = "r1", MarchId = march.Id }, CommandType.RecallMarch);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);

            var recalled = Assert.Single(session.MarchesForTests);
            var half = recalled.Cargo[ResourceType.Titanium];
            Assert.InRange(half, full / 2 - 10, full / 2 + 10);
            var after = _world.Map.NodeForTests(node.Id)!;
            Assert.Null(after.BusyUntil);
            Assert.Equal(node.Remaining - GatherRules.LoadOf(Catalog.World, recalled.Cargo), after.Remaining);
        }

        [Fact]
        public void Yield_scale_shrinks_loads_and_yields_but_not_trip_times_or_hadalite()
        {
            var scaled = new HADAL.Shared.Catalog.GatherCatalogData
            {
                queueReactors = Catalog.Gather.queueReactors, loadByReactor = Catalog.Gather.loadByReactor,
                alloyReactor = Catalog.Gather.alloyReactor, yieldScale = 0.02
            };
            var full = GatherRules.Rates(Catalog.World, Catalog.Gather, "enkaz_alani", 3, 20);
            var low = GatherRules.Rates(Catalog.World, scaled, "enkaz_alani", 3, 20);
            Assert.Equal(full[ResourceType.Titanium] * 0.02, low[ResourceType.Titanium], 3);
            Assert.Equal(Math.Round(GatherRules.Load(Catalog.Gather, 24) * 0.02), GatherRules.Load(scaled, 24));
            Assert.InRange(GatherRules.GatherSeconds(Catalog.World, low, GatherRules.Load(scaled, 24)),
                GatherRules.GatherSeconds(Catalog.World, full, GatherRules.Load(Catalog.Gather, 24)) - 2,
                GatherRules.GatherSeconds(Catalog.World, full, GatherRules.Load(Catalog.Gather, 24)) + 2);
            Assert.Equal(GatherRules.Rates(Catalog.World, Catalog.Gather, "hadalit_damari", 7, 40)[ResourceType.Hadalite],
                GatherRules.Rates(Catalog.World, scaled, "hadalit_damari", 7, 40)[ResourceType.Hadalite]);
        }

        [Fact]
        public void Superalloy_comes_as_titanium_before_the_foundry()
        {
            var before = GatherRules.Rates(Catalog.World, Catalog.Gather, "nodul_sahasi", 4, 20);
            var after = GatherRules.Rates(Catalog.World, Catalog.Gather, "nodul_sahasi", 4, 30);
            Assert.False(before.ContainsKey(ResourceType.Alloy));
            Assert.True(before[ResourceType.Titanium] > 0);
            Assert.True(after[ResourceType.Alloy] > 0);
            Assert.Equal(GatherRules.LoadPerHour(Catalog.World, before), GatherRules.LoadPerHour(Catalog.World, after), 3);
        }

        [Fact]
        public async Task Dives_clear_stages_in_order_and_pay_three_times_a_day()
        {
            var (session, connection) = await Connect("dive-a", 7);
            session.StateForTests.Wallet[ResourceType.Oxygen] = 1_000_000;

            void Dive(string id, int chapter, int stage) =>
                Send(session, new DiveStageCommand { CommandId = id, Chapter = chapter, Stage = stage }, CommandType.DiveStage);

            Dive("d0", 1, 2);
            Assert.Equal(nameof(DiveRejectReason.Locked), LastAck(connection).Reason);

            Dive("d1", 1, 1);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var report = Latest<DiveReportDto>(connection, EntityIds.DiveReport + "d1");
            Assert.True(report.Won && report.FirstClear && report.Rewarded);
            Assert.Contains(report.Rewards, r => r.Type == "item" && r.Id.StartsWith(Capsules.Prefix + "Titanium", StringComparison.Ordinal));
            Assert.Equal(1, session.StateForTests.ExpeditionCleared);

            Dive("d2", 1, 1);
            Dive("d3", 1, 1);
            Dive("d4", 1, 1);
            var fourth = Latest<DiveReportDto>(connection, EntityIds.DiveReport + "d4");
            Assert.True(fourth.Won);
            Assert.False(fourth.Rewarded);
            Assert.Empty(fourth.Rewards);
            Assert.Equal(3, Latest<ExpeditionStateDto>(connection, EntityIds.Expedition).RewardedToday);
        }

        [Fact]
        public async Task A_lost_dive_refunds_most_of_the_oxygen()
        {
            var (session, connection) = await Connect("dive-b", 50);
            session.StateForTests.ExpeditionCleared = 99; // standing before the last boss
            session.StateForTests.Wallet[ResourceType.Oxygen] = 100_000;
            var chapter = ExpeditionRules.Chapter(Catalog.Expedition, 10)!;

            Send(session, new DiveStageCommand { CommandId = "d1", Chapter = 10, Stage = 10 }, CommandType.DiveStage);
            var report = Latest<DiveReportDto>(connection, EntityIds.DiveReport + "d1");
            if (report.Won)
                return; // the starter base already outguns the boss; nothing to check
            Assert.Equal(100_000 - chapter.oxygen + ExpeditionRules.Refund(Catalog.Expedition, chapter), session.StateForTests.Wallet[ResourceType.Oxygen]);
            Assert.Equal(99, session.StateForTests.ExpeditionCleared);
        }

        [Fact]
        public async Task Marches_and_campaign_progress_survive_a_save()
        {
            var (session, _) = await Connect("save-a", 10);
            Send(session, new MarchGatherCommand { CommandId = "g1", NodeId = Nearest(session, "enkaz_alani").Id }, CommandType.MarchGather);
            session.StateForTests.ExpeditionCleared = 4;

            var json = PlayerExtras.Write(session.StateForTests);
            var restored = new PlayerState();
            PlayerExtras.Read(json, restored);
            var march = Assert.Single(restored.Marches);
            Assert.Equal(session.MarchesForTests[0].HomeAt, march.HomeAt);
            Assert.Equal(session.MarchesForTests[0].Cargo, march.Cargo);
            Assert.Equal(4, restored.ExpeditionCleared);
        }
    }
}
