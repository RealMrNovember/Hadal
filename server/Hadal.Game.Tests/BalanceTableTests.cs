using Hadal.Game.Persistence;
using HADAL.Shared.Catalog;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>Catalog v9 balance tables from the design guide (docs/rehber, Paket A).</summary>
    public sealed class BalanceTableTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public BalanceTableTests()
        {
            _world = new GameWorld(Catalog, new InMemoryPlayerRepository(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            session.ClearCrewForTests();
            return (session, connection);
        }

        private static CommandAckDto LastAck(RecordingConnection connection) => connection.Messages.Last().Delta().Ack()!;

        private static void Rich(PlayerSession session)
        {
            foreach (var type in new[] { ResourceType.Titanium, ResourceType.Biomass, ResourceType.Food, ResourceType.Oxygen, ResourceType.Energy, ResourceType.Alloy })
                session.StateForTests.Wallet[type] = 100_000_000;
        }

        private static PlacedBuilding Reactor(PlayerSession session) =>
            session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);

        [Fact]
        public void Every_shipped_building_has_a_fifty_level_table()
        {
            Assert.All(Catalog.Buildings, b => Assert.Equal(b.MaxLevel, b.Levels.Count));
            Assert.True(Catalog.TryGetBuilding(BuildingProgression.ReactorId, out var reactor));
            Assert.Equal(reactor.Levels[1].Seconds, BuildingProgression.Seconds(reactor, 2, Catalog.Progression));
            Assert.Equal(reactor.Levels[29].Cost, BuildingProgression.Cost(reactor, 30, Catalog.Progression));
            Assert.True(reactor.Levels[49].Cost.Sum(c => c.Value) > reactor.Levels[9].Cost.Sum(c => c.Value) * 100);
        }

        [Fact]
        public async Task Upgrades_charge_and_take_the_table_values()
        {
            var (session, connection) = await Connect("table-a");
            Rich(session);
            var reactor = Reactor(session);
            Assert.True(Catalog.TryGetBuilding(reactor.DefinitionId, out var definition));
            var before = session.StateForTests.Wallet[ResourceType.Titanium];

            _world.HandleCommand(session, TestSupport.Upgrade(reactor.BuildingId));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var cost = definition.Levels[1].Cost.Single(c => c.Key == ResourceType.Titanium).Value;
            Assert.Equal(before - cost, session.StateForTests.Wallet[ResourceType.Titanium]);
            Assert.Equal(definition.Levels[1].Seconds, reactor.ConstructionSeconds);
        }

        [Fact]
        public async Task The_reactor_needs_its_prerequisite_buildings()
        {
            var (session, connection) = await Connect("table-b");
            Rich(session);
            var reactor = Reactor(session);
            var level = Enumerable.Range(2, 48).First(l => Catalog.ReactorRequirements(l + 1).Count > 0);
            reactor.Level = level;
            var need = Catalog.ReactorRequirements(level + 1)[0];

            _world.HandleCommand(session, TestSupport.Upgrade(reactor.BuildingId));
            Assert.Equal(nameof(ConstructionRejectReason.ReactorPrerequisite), LastAck(connection).Reason);

            // Meet every prerequisite of the next level (a building of each kind at the level).
            foreach (var (id, needLevel) in Catalog.ReactorRequirements(level + 1).Select(p => (p.Key, p.Value)))
            {
                var existing = session.StateForTests.Buildings.FirstOrDefault(b => b.DefinitionId == id);
                if (existing != null)
                    existing.Level = needLevel;
                else
                    session.StateForTests.Buildings.Add(new PlacedBuilding { BuildingId = "b" + id, DefinitionId = id, Ring = 11, Sector = session.StateForTests.Buildings.Count, Level = needLevel });
            }

            _world.HandleCommand(session, TestSupport.Upgrade(reactor.BuildingId));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.NotEmpty(need.Key);
        }

        [Fact]
        public void Copies_open_with_the_reactor()
        {
            Assert.True(Catalog.TryGetBuilding("oxygen_plant", out var plant));
            Assert.True(plant.CountUnlocks.Count > 1);
            var first = plant.CountUnlocks[0];
            var later = plant.CountUnlocks.First(u => u > first);
            var owned = new Dictionary<string, int> { ["oxygen_plant"] = plant.CountAt(first) };
            var occupied = new HashSet<(int, int)>();
            var wallet = new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 1_000_000, [ResourceType.Biomass] = 1_000_000 };

            // Find a free slot the plant may use.
            var (ring, sector) = Enumerable.Range(0, 13).SelectMany(r => Enumerable.Range(0, Catalog.GetSectorCount(r)).Select(s => (r, s)))
                .First(x => PlacementRules.Validate(Catalog, "oxygen_plant", x.r, x.s, 12, occupied, wallet, new Dictionary<string, int>(), 50) == PlacementRejectReason.None);

            Assert.Equal(PlacementRejectReason.ReactorLevelTooLow,
                PlacementRules.Validate(Catalog, "oxygen_plant", ring, sector, 12, occupied, wallet, owned, first));
            Assert.Equal(PlacementRejectReason.None,
                PlacementRules.Validate(Catalog, "oxygen_plant", ring, sector, 12, occupied, wallet, owned, later));
            Assert.Equal(PlacementRejectReason.LimitReached,
                PlacementRules.Validate(Catalog, "oxygen_plant", ring, sector, 12, occupied, wallet,
                    new Dictionary<string, int> { ["oxygen_plant"] = plant.CountUnlocks.Count }, 50));
        }

        [Fact]
        public void Production_is_defined_per_hour()
        {
            Assert.True(Catalog.TryGetBuilding(BuildingProgression.ReactorId, out var reactor));
            var production = Catalog.ToProductionCatalog();
            var perTick = ProductionSimulator.ComputeNetMilliPerTick(new[] { new BuildingProductionInput(reactor.Id, 1) }, production);
            var expectedPerHour = reactor.Levels[0].ProductionMilliPerTick[ResourceType.Energy] * SimulationTick.TicksPerHour / ProductionFormulas.Milli;
            Assert.Equal(expectedPerHour, perTick[ResourceType.Energy] * SimulationTick.TicksPerHour / ProductionFormulas.Milli);
            Assert.InRange(expectedPerHour, 690, 710); // 700 per hour in the guide; milli rounding stays under 1%
        }

        [Fact]
        public void Speed_bonuses_divide_and_never_reach_zero()
        {
            Assert.Equal(50, PlayerModifiers.ApplyPercent(100, -100));
            Assert.Equal(25, PlayerModifiers.ApplyPercent(100, -300));
            Assert.Equal(1, PlayerModifiers.ApplyPercent(100, -100_000));
            Assert.Equal(110, PlayerModifiers.ApplyPercent(100, 10));
        }

        [Fact]
        public async Task Power_adds_each_levels_table_value()
        {
            var (session, _) = await Connect("table-c");
            var expected = session.StateForTests.Buildings.Sum(b =>
                Catalog.TryGetBuilding(b.DefinitionId, out var d) ? BuildingProgression.Power(d, b.Level) : 0)
                + session.StateForTests.Heroes.Sum(h => Catalog.TryGetHero(h.HeroId, out var hero) ? HeroRules.Power(hero, h.Level, h.Stars) : 0);
            Assert.Equal(expected, session.Power);
            Assert.True(Catalog.TryGetBuilding(BuildingProgression.ReactorId, out var reactor));
            Assert.Equal(reactor.Levels[0].Power + reactor.Levels[1].Power, BuildingProgression.Power(reactor, 2));
        }

        [Fact]
        public void Caps_grow_with_the_reactor()
        {
            Assert.True(Catalog.Capacities(30)[ResourceType.Titanium] > Catalog.Capacities(1)[ResourceType.Titanium]);
            Assert.True(Catalog.TryGetResource(ResourceType.Alloy, out _));
            Assert.True(Catalog.IncomeHour(30) > Catalog.IncomeHour(1));
        }

        [Fact]
        public async Task Quest_resources_arrive_as_capsules_that_open_past_the_cap()
        {
            var (session, connection) = await Connect("table-d");
            var plant = session.StateForTests.Buildings.FirstOrDefault(b => b.DefinitionId == "oxygen_plant")
                        ?? new PlacedBuilding { BuildingId = "bplant", DefinitionId = "oxygen_plant", Ring = 1, Sector = 0 };
            if (!session.StateForTests.Buildings.Contains(plant))
                session.StateForTests.Buildings.Add(plant);
            plant.Level = 2;

            var claim = new ClaimQuestCommand { CommandId = "q1", ClientSequence = 1, QuestId = "q_oxygen2" };
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(claim, CommandType.ClaimQuest, TestSupport.Serializer.Serialize(claim)));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var capsule = session.StateForTests.Items.Keys.Single(k => k.StartsWith(Capsules.Prefix, StringComparison.Ordinal));
            Assert.True(Capsules.TryParse(capsule, out var resource, out var amount));
            Assert.Equal(ResourceType.Titanium, resource);

            var cap = Catalog.Capacities(Reactor(session).Level)[ResourceType.Titanium];
            session.StateForTests.Wallet[ResourceType.Titanium] = cap;
            var open = new OpenItemCommand { CommandId = "o1", ClientSequence = 1, ItemId = capsule, Count = 1 };
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(open, CommandType.OpenItem, TestSupport.Serializer.Serialize(open)));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(cap + amount, session.StateForTests.Wallet[ResourceType.Titanium]);
            Assert.False(session.StateForTests.Items.ContainsKey(capsule));
        }
    }
}
