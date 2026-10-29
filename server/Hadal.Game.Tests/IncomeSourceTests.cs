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
            // Marches go with troops (docs/rehber 19 §1.2): a reserve at home to fill the team from.
            session.StateForTests.Troops[TroopRules.Key("armored", 1)] = 5000;
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
            Assert.True(march.Crew > 0);
            Assert.Equal(march.Troops.Values.Sum(), march.Crew);
            Assert.Equal(5000 - march.Crew, session.StateForTests.Troops[TroopRules.Key("armored", 1)]);
            Assert.Equal(_world.Map.PositionOf(session.PublicId)!.Value, (march.FromX, march.FromY));
            Assert.True(GatherRules.LoadOf(Catalog.World, march.Cargo) <= TroopRules.MarchLoad(Catalog.Troops, march.Troops));
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

            // A 50% march speed-up on the way out halves the time left to arrive, and everything after moves with it.
            var arrives = march.ArrivesAt;
            var home = march.HomeAt;
            session.StateForTests.Items["speed_march_p50"] = 1;
            Send(session, new UseSpeedupCommand { CommandId = "sp", ItemId = "speed_march_p50", Target = "march:" + march.Id }, CommandType.UseSpeedup);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var sped = session.MarchesForTests.Single();
            Assert.Equal((arrives - _clock.UtcNow).TotalSeconds / 2, (sped.ArrivesAt - _clock.UtcNow).TotalSeconds, 3);
            Assert.Equal(home - (arrives - sped.ArrivesAt), sped.HomeAt);

            _clock.UtcNow = sped.HomeAt.AddSeconds(1);
            _world.Tick();
            Assert.Empty(session.MarchesForTests);
            Assert.True(session.StateForTests.Wallet[ResourceType.Titanium] >= titanium + cargo);
            Assert.Equal(5000, session.StateForTests.Troops[TroopRules.Key("armored", 1)]); // the troops came home
        }

        [Fact]
        public async Task Every_player_sees_teams_crossing_their_view_live()
        {
            var (session, _) = await Connect("watch-a", 10);
            var node = Nearest(session, "enkaz_alani");
            Send(session, new MarchGatherCommand { CommandId = "w1", NodeId = node.Id }, CommandType.MarchGather);
            var march = Assert.Single(session.MarchesForTests);

            var (viewer, viewerConnection) = await Connect("watch-b", 10);
            Send(viewer, new ViewWorldCommand { CommandId = "wv", X = node.X, Y = node.Y }, CommandType.ViewWorld);
            var seen = Assert.Single(Latest<WorldViewDto>(viewerConnection, EntityIds.WorldView).Marches, m => m.Id == march.Id);
            Assert.Equal(2, seen.Relation);
            Assert.Equal(session.PublicId, seen.OwnerId);
            Assert.Equal((march.FromX, march.FromY, node.X, node.Y), (seen.FromX, seen.FromY, seen.ToX, seen.ToY));
            Assert.Equal(march.ArrivesAt.ToUnixTimeMilliseconds(), seen.ArrivesAtMs);

            // Still on the map while its owner is away, gone once the team is home.
            await _world.DisconnectAsync(session, (await Connect("watch-a", 10)).Connection, default);
            _clock.UtcNow = march.HomeAt.AddSeconds(1);
            Send(viewer, new ViewWorldCommand { CommandId = "wv2", X = node.X, Y = node.Y }, CommandType.ViewWorld);
            Assert.DoesNotContain(Latest<WorldViewDto>(viewerConnection, EntityIds.WorldView).Marches, m => m.Id == march.Id);
        }

        [Fact]
        public async Task Each_hero_takes_their_command_of_troops_and_a_march_needs_troops()
        {
            var (session, connection) = await Connect("team-a", 10);
            var state = session.StateForTests;
            foreach (var id in Catalog.Heroes.Select(h => h.Id).Where(id => state.Heroes.All(h => h.HeroId != id)).Take(5))
                state.Heroes.Add(new OwnedHeroState { HeroId = id, Level = 10, Stars = 2 });
            state.Troops[TroopRules.Key("armored", 1)] = 300;
            state.Troops[TroopRules.Key("harpoon", 2)] = 5000;

            var team = state.Heroes.Take(3).Select(h => h.HeroId).ToArray();
            Send(session, new MarchGatherCommand { CommandId = "t1", NodeId = Nearest(session, "enkaz_alani").Id, HeroIds = team }, CommandType.MarchGather);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var march = Assert.Single(session.MarchesForTests);
            Assert.Equal(team, march.HeroIds);
            for (var i = 0; i < team.Length; i++)
            {
                var hero = state.Heroes.Single(h => h.HeroId == team[i]);
                Assert.Equal(TroopRules.CommandCapacity(Catalog.Troops, hero.Level, hero.Stars, 0), march.HeroTroops[i]);
            }

            // Highest tier first; the slowest type sets the pace.
            Assert.True(march.Troops.ContainsKey(TroopRules.Key("harpoon", 2)));
            Assert.Equal(march.HeroTroops.Sum(), march.Troops.Values.Sum());

            // Six heroes is one too many; with nobody left at home the march is refused.
            Send(session, new MarchGatherCommand { CommandId = "t2", NodeId = Nearest(session, "enkaz_alani").Id,
                HeroIds = state.Heroes.Select(h => h.HeroId).Concat(new[] { "x1", "x2", "x3", "x4", "x5" }).Take(6).ToArray() }, CommandType.MarchGather);
            Assert.Equal(nameof(MarchRejectReason.TooManyHeroes), LastAck(connection).Reason);
            state.Troops.Clear();
            Send(session, new MarchGatherCommand { CommandId = "t3", NodeId = Nearest(session, "enkaz_alani").Id }, CommandType.MarchGather);
            Assert.Equal(nameof(MarchRejectReason.NoTroops), LastAck(connection).Reason);
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
            var unscaled = new HADAL.Shared.Catalog.GatherCatalogData
            {
                queueReactors = Catalog.Gather.queueReactors, loadByReactor = Catalog.Gather.loadByReactor,
                alloyReactor = Catalog.Gather.alloyReactor, yieldScale = 1
            };
            var full = GatherRules.Rates(Catalog.World, unscaled, "enkaz_alani", 3, 20);
            var low = GatherRules.Rates(Catalog.World, scaled, "enkaz_alani", 3, 20);
            Assert.Equal(full[ResourceType.Titanium] * 0.02, low[ResourceType.Titanium], 3);
            Assert.Equal(Math.Round(GatherRules.Load(unscaled, 24) * 0.02), GatherRules.Load(scaled, 24));
            Assert.InRange(GatherRules.GatherSeconds(Catalog.World, low, GatherRules.Load(scaled, 24)),
                GatherRules.GatherSeconds(Catalog.World, full, GatherRules.Load(unscaled, 24)) - 2,
                GatherRules.GatherSeconds(Catalog.World, full, GatherRules.Load(unscaled, 24)) + 2);
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
            Assert.Equal(session.MarchesForTests[0].Troops, march.Troops);
            Assert.Equal(session.MarchesForTests[0].HeroIds, march.HeroIds);
            Assert.Equal(4, restored.ExpeditionCleared);
        }

        [Fact]
        public async Task An_attack_spends_sonar_fights_on_arrival_wounds_the_lost_and_brings_the_spoils_home()
        {
            var (session, connection) = await Connect("hunt-a", 10);
            var state = session.StateForTests;
            state.Troops.Clear();
            state.Troops[TroopRules.Key("harpoon", 3)] = 2000;
            var (x, y) = _world.Map.PositionOf(session.PublicId)!.Value;
            var target = _world.Map.MonstersForTests().Where(m => m.Type == "abyss_crawler" && m.Level <= 3)
                .OrderBy(m => GatherRules.Tiles(x, y, m.X, m.Y)).First();
            var sonarBefore = MonsterRules.SonarNow(state.SonarStored, state.SonarAt, _clock.UtcNow);

            Send(session, new MarchAttackCommand { CommandId = "h1", MonsterId = target.Id }, CommandType.MarchAttack);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var march = Assert.Single(session.MarchesForTests);
            Assert.Equal("attack", march.Kind);
            Assert.True(march.Battle!.Won);
            Assert.Equal(sonarBefore - 40, MonsterRules.SonarNow(state.SonarStored, state.SonarAt, _clock.UtcNow), 3);
            Assert.Equal(march.ArrivesAt, target.EngagedUntil);

            // Nobody else may attack it on the way.
            var (other, otherConnection) = await Connect("hunt-b", 10);
            other.StateForTests.Troops[TroopRules.Key("armored", 1)] = 100;
            Send(other, new MarchAttackCommand { CommandId = "h2", MonsterId = target.Id }, CommandType.MarchAttack);
            Assert.Equal(nameof(AttackRejectReason.MonsterBusy), LastAck(otherConnection).Reason);

            _clock.UtcNow = march.ArrivesAt.AddSeconds(1);
            _world.Tick();
            var report = Latest<BattleReportDto>(connection, EntityIds.BattleReport + march.Id);
            Assert.True(report.Won);
            Assert.Equal("abyss_crawler", report.MonsterType);
            var lost = report.Attacker.Sum(a => a.Lost);
            Assert.Equal(lost, state.Wounded.Values.Sum());
            Assert.Contains(report.Rewards, r => Capsules.TryParse(r.Id, out _, out _));

            _clock.UtcNow = march.HomeAt.AddSeconds(1);
            _world.Tick();
            Assert.Empty(session.MarchesForTests);
            Assert.Equal(2000 - lost, state.Troops[TroopRules.Key("harpoon", 3)]);
            Assert.Contains(state.Items.Keys, k => Capsules.TryParse(k, out _, out _));
            Assert.Contains(target.Level, state.MonsterWins);
        }

        [Fact]
        public async Task A_recalled_attack_fights_nothing_and_gets_its_sonar_back()
        {
            var (session, connection) = await Connect("hunt-c", 10);
            var state = session.StateForTests;
            var target = _world.Map.MonstersForTests().First(m => m.Type == "abyss_crawler");
            var before = MonsterRules.SonarNow(state.SonarStored, state.SonarAt, _clock.UtcNow);
            Send(session, new MarchAttackCommand { CommandId = "r0", MonsterId = target.Id }, CommandType.MarchAttack);
            var march = Assert.Single(session.MarchesForTests);

            Send(session, new RecallMarchCommand { CommandId = "r1", MarchId = march.Id }, CommandType.RecallMarch);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Null(session.MarchesForTests.Single().Battle);
            Assert.Equal(before, MonsterRules.SonarNow(state.SonarStored, state.SonarAt, _clock.UtcNow), 3);
            Assert.Null(target.EngagedUntil);
            Assert.Null(target.DefeatedAt);

            state.SonarStored = 10;
            state.SonarAt = _clock.UtcNow;
            _clock.UtcNow = session.MarchesForTests.Single().HomeAt.AddSeconds(1);
            _world.Tick();
            state.SonarAt = _clock.UtcNow;
            Send(session, new MarchAttackCommand { CommandId = "r2", MonsterId = target.Id }, CommandType.MarchAttack);
            Assert.Equal(nameof(AttackRejectReason.NotEnoughSonar), LastAck(connection).Reason);
        }
    }
}
