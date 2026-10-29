using System.Text.Json.Nodes;
using Hadal.Game.Persistence;
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
    /// The survival loop (roadmap 6, docs/35): the energy chain, oxygen thresholds and penalties, recovery, emergencies
    /// (rate limit, the guide exclusion, online only, repair, alliance help, auto-resolve, idempotent repair), offline
    /// catch-up and the catalog switches. Numbers come from the mechanics catalog: Core Reactor 8 Energy per tick,
    /// Oxygen Plant 3 Oxygen for 1 Energy, Food Lab 2 Food for 1 Energy, +25 % per level.
    /// </summary>
    public sealed class SurvivalTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();

        // --- Catalogs and players --------------------------------------------------------------------------------------

        private static GameCatalog Catalog(Action<JsonObject>? survival = null, Action<JsonObject>? emergencies = null)
        {
            return TestSupport.CatalogWith(root =>
            {
                var e = new JsonObject
                {
                    ["enabled"] = true, ["minReactorLevel"] = 1, ["cooldownHours"] = 6, ["chancePercent"] = 0, ["autoResolveHours"] = 4,
                    ["repairSeconds"] = 600, ["repairCostIncomePercent"] = 10, ["repairCostMin"] = 20, ["leakPercent"] = 50, ["leakMinPerHour"] = 0
                };
                emergencies?.Invoke(e);
                var s = new JsonObject
                {
                    ["enabled"] = true, ["energyFloorPercent"] = 25, ["warnPercent"] = 20, ["outputPenaltyPercent"] = 20,
                    ["timePenaltyPercent"] = 20, ["crewOxygenPerReactorLevel"] = 0, ["emergencies"] = e
                };
                survival?.Invoke(s);
                root["survival"] = s;
            });
        }

        private GameWorld World(GameCatalog catalog) =>
            new(catalog, _repository, _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance);

        private static async Task<(PlayerSession S, RecordingConnection C)> Connect(GameWorld world, string identity, bool pastGuide = true)
        {
            var connection = new RecordingConnection();
            var session = await world.ConnectAsync(identity, connection, default, identity);
            var state = session.StateForTests;
            if (pastGuide)
                state.OnboardingStatus = OnboardingStatus.Skipped;
            state.Troops.Clear();
            return (session, connection);
        }

        /// <summary>Replaces the base with exactly these buildings (the reactor first).</summary>
        private static Dictionary<string, PlacedBuilding> Base(PlayerSession session, params (string Id, string Definition, int Level)[] buildings)
        {
            var state = session.StateForTests;
            state.RemovedBuildingIds.AddRange(state.Buildings.Select(b => b.BuildingId));
            state.Buildings.Clear();
            var placed = new Dictionary<string, PlacedBuilding>();
            var sector = 0;
            foreach (var (id, definition, level) in buildings)
            {
                var b = new PlacedBuilding { BuildingId = id, DefinitionId = definition, Ring = definition == BuildingProgression.ReactorId ? 0 : 4, Sector = sector++, Level = level };
                state.Buildings.Add(b);
                state.UnsavedBuildings.Add(b);
                placed[id] = b;
            }

            state.IsDirty = true;
            return placed;
        }

        private static long Has(PlayerSession session, ResourceType type) =>
            session.StateForTests.Wallet.TryGetValue(type, out var amount) ? amount : 0;

        private static void Set(PlayerSession session, ResourceType type, long amount) => session.StateForTests.Wallet[type] = amount;

        private void Run(GameWorld world, int seconds)
        {
            _clock.Advance(TimeSpan.FromSeconds(seconds));
            world.Tick();
        }

        private static string Id() => Guid.NewGuid().ToString("N");

        private static void Send<T>(GameWorld world, PlayerSession session, T command, CommandType type) where T : ICommand =>
            world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static long Net(PlayerSession session, DateTimeOffset now, ResourceType type) =>
            session.SurvivalForTests(now).Net.TryGetValue(type, out var milli) ? milli : 0;

        // Four Oxygen Plants at level 13 need 16 Energy per tick; the level-1 reactor gives 8.
        private static (string, string, int)[] StarvedBase() => new[]
        {
            ("reactor", BuildingProgression.ReactorId, 1), ("o1", "oxygen_plant", 13), ("o2", "oxygen_plant", 13),
            ("o3", "oxygen_plant", 13), ("o4", "oxygen_plant", 13)
        };

        // --- Shared math -----------------------------------------------------------------------------------------------

        [Fact]
        public void Energy_ratio_is_supply_over_demand_rounded_up_with_a_floor_and_only_on_an_empty_store()
        {
            var rules = new SurvivalCatalogData { enabled = true, energyFloorPercent = 25 };
            Assert.Equal(500, SurvivalRules.EnergyPermille(8000, 16000, 0, rules));
            Assert.Equal(334, SurvivalRules.EnergyPermille(1000, 3000, 0, rules)); // rounded up: the store stays at 0
            Assert.Equal(250, SurvivalRules.EnergyPermille(1000, 16000, 0, rules)); // never under the floor
            Assert.Equal(250, SurvivalRules.EnergyPermille(0, 16000, 0, rules)); // no reactor at all: still the floor, never a stop
            Assert.Equal(1000, SurvivalRules.EnergyPermille(8000, 16000, 1, rules)); // energy left in the store
            Assert.Equal(1000, SurvivalRules.EnergyPermille(16000, 16000, 0, rules)); // supply covers demand
            Assert.Equal(1000, SurvivalRules.EnergyPermille(8000, 16000, 0, new SurvivalCatalogData { enabled = false }));

            Assert.Equal(SurvivalStatus.Critical, SurvivalRules.EnergyStatus(8000, 16000, 0, 10000, rules));
            Assert.Equal(SurvivalStatus.Low, SurvivalRules.EnergyStatus(8000, 16000, 1999, 10000, rules));
            Assert.Equal(SurvivalStatus.Normal, SurvivalRules.EnergyStatus(8000, 16000, 2000, 10000, rules));
            Assert.Equal(SurvivalStatus.Normal, SurvivalRules.EnergyStatus(16000, 8000, 0, 10000, rules));
        }

        [Fact]
        public void Energy_users_run_at_the_ratio_and_their_energy_use_rounds_up()
        {
            var production = TestSupport.Catalog.ToProductionCatalog();
            var buildings = new[]
            {
                new BuildingProductionInput(BuildingProgression.ReactorId, 1), new BuildingProductionInput("oxygen_plant", 13),
                new BuildingProductionInput("food_lab", 1)
            };
            var full = ProductionSimulator.ComputeNetMilliPerTick(buildings, production, null, SurvivalEffects.None);
            Assert.Equal(12000, full[ResourceType.Oxygen]);
            Assert.Equal(2000, full[ResourceType.Food]);
            Assert.Equal(8000 - 4000 - 1000, full[ResourceType.Energy]);

            var slowed = ProductionSimulator.ComputeNetMilliPerTick(buildings, production, null, new SurvivalEffects(333, 0));
            Assert.Equal(3996, slowed[ResourceType.Oxygen]); // 12000 x 0.333
            Assert.Equal(666, slowed[ResourceType.Food]);
            Assert.Equal(8000 - 1332 - 333, slowed[ResourceType.Energy]); // 4000 x 0.333 = 1332, 1000 x 0.333 = 333 (both rounded up)
            Assert.Equal(8000, slowed[ResourceType.Energy] + 1332 + 333); // the reactor itself is not an energy user

            // The oxygen penalty: everything but oxygen and energy loses its share.
            var penalised = ProductionSimulator.ComputeNetMilliPerTick(buildings, production, null, new SurvivalEffects(1000, 20));
            Assert.Equal(12000, penalised[ResourceType.Oxygen]);
            Assert.Equal(1600, penalised[ResourceType.Food]);
            Assert.Equal(full[ResourceType.Energy], penalised[ResourceType.Energy]);

            // A broken building neither produces nor consumes.
            var broken = ProductionSimulator.ComputeNetMilliPerTick(new[]
            {
                new BuildingProductionInput(BuildingProgression.ReactorId, 1), new BuildingProductionInput("food_lab", 1, 0, stopped: true)
            }, production, null, SurvivalEffects.None);
            Assert.DoesNotContain(ResourceType.Food, broken.Keys);
            Assert.Equal(8000, broken[ResourceType.Energy]);
        }

        [Fact]
        public void Oxygen_thresholds_warn_then_penalise_only_while_falling()
        {
            var rules = new SurvivalCatalogData { enabled = true, warnPercent = 20, outputPenaltyPercent = 20, timePenaltyPercent = 20 };
            Assert.Equal(SurvivalStatus.Normal, SurvivalRules.OxygenStatus(-1, 1200, 6000, rules));
            Assert.Equal(SurvivalStatus.Low, SurvivalRules.OxygenStatus(-1, 1199, 6000, rules));
            Assert.Equal(SurvivalStatus.Critical, SurvivalRules.OxygenStatus(-1, 0, 6000, rules));
            Assert.Equal(SurvivalStatus.Normal, SurvivalRules.OxygenStatus(0, 0, 6000, rules)); // empty but not falling
            Assert.Equal(SurvivalStatus.Normal, SurvivalRules.OxygenStatus(5, 100, 6000, rules)); // low but rising
            Assert.Equal(SurvivalStatus.Normal, SurvivalRules.OxygenStatus(-1, 0, 6000, new SurvivalCatalogData()));

            Assert.Equal(0, SurvivalRules.OutputPenalty(SurvivalStatus.Low, rules));
            Assert.Equal(20, SurvivalRules.OutputPenalty(SurvivalStatus.Critical, rules));
            Assert.Equal(20, SurvivalRules.TimePenalty(SurvivalStatus.Critical, rules));
            Assert.Equal(120, SurvivalRules.SlowSeconds(100, 20));
            Assert.Equal(0, SurvivalRules.SlowSeconds(0, 20));
            Assert.Equal(358UL, SurvivalRules.TicksUntilEmpty(10, 0, -28)); // 10 000 thousandths at 28 a tick
            Assert.Equal(0UL, SurvivalRules.TicksUntilEmpty(10, 0, 5));
            Assert.Equal(10000, SurvivalRules.PerHourToMilliPerTick(36000));
        }

        [Fact]
        public void The_emergency_roll_is_seeded_per_player_and_hour()
        {
            Assert.Equal(SurvivalRules.Seed("p1", 500), SurvivalRules.Seed("p1", 500));
            Assert.NotEqual(SurvivalRules.Seed("p1", 500), SurvivalRules.Seed("p1", 501));
            Assert.NotEqual(SurvivalRules.Seed("p1", 500), SurvivalRules.Seed("p2", 500));
            Assert.DoesNotContain(Enumerable.Range(0, 200), h => SurvivalRules.Rolls("p1", h, 0));
            Assert.True(Enumerable.Range(0, 200).All(h => SurvivalRules.Rolls("p1", h, 100)));
            var hits = Enumerable.Range(0, 5000).Count(h => SurvivalRules.Rolls("player", h, 10));
            Assert.InRange(hits, 350, 650);
            var kinds = Enumerable.Range(0, 400).Select(h => SurvivalRules.Kind("player", h, true)).ToList();
            Assert.Contains(EmergencyKinds.Leak, kinds);
            Assert.Contains(EmergencyKinds.Breakdown, kinds);
            Assert.All(Enumerable.Range(0, 100), h => Assert.Equal(EmergencyKinds.Leak, SurvivalRules.Kind("player", h, false)));
            Assert.All(Enumerable.Range(0, 100), h => Assert.InRange(SurvivalRules.Pick("player", h, 3), 0, 2));
        }

        // --- Energy chain in the session -------------------------------------------------------------------------------

        [Fact]
        public async Task An_empty_energy_store_slows_energy_users_to_supply_over_demand()
        {
            var world = World(Catalog());
            var (session, _) = await Connect(world, "starved");
            Base(session, StarvedBase());
            Set(session, ResourceType.Energy, 0);
            Set(session, ResourceType.Oxygen, 600);

            Run(world, 100);

            // 4 x 12 Oxygen at half speed for 100 ticks; the energy store stays at 0 instead of flickering.
            Assert.Equal(600 + 2400, Has(session, ResourceType.Oxygen));
            Assert.Equal(0, Has(session, ResourceType.Energy));
            var (dto, _) = session.SurvivalForTests(_clock.UtcNow);
            Assert.Equal((int)SurvivalStatus.Critical, dto!.Energy);
            Assert.Equal(50, dto.EnergyPercent);

            // A bigger reactor ends it at once: 8 x 2.25 = 18 Energy covers the 16.
            session.StateForTests.Buildings.Single(b => b.BuildingId == "reactor").Level = 6;
            Run(world, 50);
            Assert.Equal(3000 + 2400, Has(session, ResourceType.Oxygen));
            Assert.Equal((int)SurvivalStatus.Normal, session.SurvivalForTests(_clock.UtcNow).Dto!.Energy);
            Assert.Equal(100, session.SurvivalForTests(_clock.UtcNow).Dto!.EnergyPercent);
        }

        [Fact]
        public async Task The_energy_floor_keeps_producers_running()
        {
            var world = World(Catalog(s => s["energyFloorPercent"] = 75));
            var (session, _) = await Connect(world, "floor");
            Base(session, StarvedBase());
            Set(session, ResourceType.Energy, 0);
            Set(session, ResourceType.Oxygen, 600);

            Run(world, 100);
            Assert.Equal(600 + 3600, Has(session, ResourceType.Oxygen)); // 75 % instead of the 50 % supply allows
            Assert.Equal(0, Has(session, ResourceType.Energy));
        }

        [Fact]
        public async Task The_store_runs_full_speed_until_it_is_empty_online_and_offline_alike()
        {
            var world = World(Catalog());
            var (online, _) = await Connect(world, "online");
            Base(online, StarvedBase());
            Set(online, ResourceType.Energy, 400); // 8 short a tick: empty after exactly 50 ticks
            Set(online, ResourceType.Oxygen, 600);

            var (offline, offlineConnection) = await Connect(world, "offline");
            Base(offline, StarvedBase());
            Set(offline, ResourceType.Energy, 400);
            Set(offline, ResourceType.Oxygen, 600);
            await world.DisconnectAsync(offline, offlineConnection, default);

            for (var i = 0; i < 100; i++)
                Run(world, 1);

            // 50 ticks at 48 Oxygen, then 50 at 24.
            Assert.Equal(600 + 2400 + 1200, Has(online, ResourceType.Oxygen));
            Assert.Equal(0, Has(online, ResourceType.Energy));

            var (back, _) = await Connect(world, "offline");
            Assert.Equal(600 + 2400 + 1200, Has(back, ResourceType.Oxygen));
            Assert.Equal(0, Has(back, ResourceType.Energy));
        }

        [Fact]
        public async Task With_the_master_switch_off_nothing_slows()
        {
            var world = World(Catalog(s => s["enabled"] = false));
            var (session, _) = await Connect(world, "off");
            Base(session, StarvedBase());
            Set(session, ResourceType.Energy, 0);
            Set(session, ResourceType.Oxygen, 600);

            Run(world, 100);
            Assert.Equal(600 + 4800, Has(session, ResourceType.Oxygen)); // the old economy: the deficit only drains the store
            Assert.Null(session.SurvivalForTests(_clock.UtcNow).Dto);
        }

        [Fact]
        public async Task Switching_the_loop_off_live_restores_full_rates_at_once()
        {
            var world = World(Catalog());
            var (session, _) = await Connect(world, "live");
            Base(session, StarvedBase());
            Set(session, ResourceType.Energy, 0);
            Set(session, ResourceType.Oxygen, 600);
            Run(world, 100);
            Assert.Equal(3000, Has(session, ResourceType.Oxygen));

            world.ReplaceCatalog(Catalog(s => s["enabled"] = false), 99);
            Run(world, 50);
            Assert.Equal(3000 + 2400, Has(session, ResourceType.Oxygen));
        }

        // --- Oxygen -----------------------------------------------------------------------------------------------------

        // The crew breathes 36 000 an hour per reactor level: 10 Oxygen a tick at reactor 1.
        private static GameCatalog Breathless() => Catalog(s => s["crewOxygenPerReactorLevel"] = 36000);

        [Fact]
        public async Task Low_oxygen_only_warns()
        {
            var world = World(Breathless());
            var (session, _) = await Connect(world, "low");
            Base(session, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            Set(session, ResourceType.Oxygen, 1000); // under 20 % of 6000 and falling
            var food = Has(session, ResourceType.Food);

            Run(world, 50);
            Assert.Equal(500, Has(session, ResourceType.Oxygen));
            Assert.Equal(food + 100, Has(session, ResourceType.Food));
            var dto = session.SurvivalForTests(_clock.UtcNow).Dto!;
            Assert.Equal((int)SurvivalStatus.Low, dto.Oxygen);
            Assert.Equal(0, dto.OutputPenaltyPercent);
        }

        [Fact]
        public async Task Empty_oxygen_slows_production_from_the_exact_tick_and_new_timers()
        {
            var world = World(Breathless());
            var (session, connection) = await Connect(world, "empty");
            Base(session, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            Set(session, ResourceType.Oxygen, 500); // empty after 50 ticks
            Set(session, ResourceType.Titanium, 100_000);
            Set(session, ResourceType.Biomass, 100_000);
            var food = Has(session, ResourceType.Food);

            Run(world, 100);
            Assert.Equal(0, Has(session, ResourceType.Oxygen));
            Assert.Equal(food + 100 + 80, Has(session, ResourceType.Food)); // 50 ticks at 2, then 50 at 1.6
            var dto = session.SurvivalForTests(_clock.UtcNow).Dto!;
            Assert.Equal((int)SurvivalStatus.Critical, dto.Oxygen);
            Assert.Equal(20, dto.OutputPenaltyPercent);
            Assert.Equal(20, dto.TimePenaltyPercent);

            // The same upgrade, once breathing and once not: 20 % longer while critical.
            var (calm, calmConnection) = await Connect(world, "calm");
            Base(calm, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            Set(calm, ResourceType.Oxygen, 5000);
            Set(calm, ResourceType.Titanium, 100_000);
            Set(calm, ResourceType.Biomass, 100_000);
            world.HandleCommand(calm, TestSupport.Upgrade("reactor"));
            Assert.Equal(CommandResultCode.Accepted, LastAck(calmConnection).Result);
            world.HandleCommand(session, TestSupport.Upgrade("reactor"));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var normal = calm.StateForTests.Buildings.Single(b => b.BuildingId == "reactor").ConstructionSeconds;
            var slowed = session.StateForTests.Buildings.Single(b => b.BuildingId == "reactor").ConstructionSeconds;
            Assert.True(normal > 0);
            Assert.Equal(SurvivalRules.SlowSeconds(normal, 20), slowed);
        }

        [Fact]
        public async Task Restored_oxygen_recovers_at_once_and_nothing_is_ever_lost()
        {
            var world = World(Breathless());
            var (session, _) = await Connect(world, "recover");
            Base(session, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            Set(session, ResourceType.Oxygen, 0);
            session.StateForTests.Troops["armored:1"] = 500;

            // Twelve hours without air: the base works slower but keeps every building and every troop.
            Run(world, 12 * 3600);
            Assert.Equal(2, session.StateForTests.Buildings.Count);
            Assert.Equal(500, session.StateForTests.Troops["armored:1"]);
            Assert.Equal(0, Has(session, ResourceType.Oxygen));
            Assert.Equal((int)SurvivalStatus.Critical, session.SurvivalForTests(_clock.UtcNow).Dto!.Oxygen);

            // An Oxygen Plant (12 a tick against 10 + troops) brings the base straight back.
            session.StateForTests.Troops.Clear();
            var plant = new PlacedBuilding { BuildingId = "plant", DefinitionId = "oxygen_plant", Ring = 4, Sector = 5, Level = 13 };
            session.StateForTests.Buildings.Add(plant);
            var dto = session.SurvivalForTests(_clock.UtcNow).Dto!;
            Assert.Equal((int)SurvivalStatus.Normal, dto.Oxygen);
            Assert.Equal(0, dto.OutputPenaltyPercent);
            var food = Has(session, ResourceType.Food);
            Run(world, 100);
            Assert.Equal(Math.Min(food + 200, 5000), Has(session, ResourceType.Food));
            Assert.Equal(200, Has(session, ResourceType.Oxygen));
        }

        [Fact]
        public async Task The_crew_breathes_per_reactor_level()
        {
            var world = World(Catalog(s => s["crewOxygenPerReactorLevel"] = 3600));
            var (session, _) = await Connect(world, "crew");
            Base(session, ("reactor", BuildingProgression.ReactorId, 3));
            Set(session, ResourceType.Oxygen, 1000);
            Run(world, 100);
            Assert.Equal(1000 - 300, Has(session, ResourceType.Oxygen)); // 1 a tick per level
        }

        // --- Emergencies ------------------------------------------------------------------------------------------------

        [Fact]
        public async Task A_breakdown_stops_one_producer_until_repaired_and_the_repair_runs_once_per_command_id()
        {
            var world = World(Catalog());
            var (session, connection) = await Connect(world, "breakdown");
            Base(session, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            Set(session, ResourceType.Titanium, 500);

            Assert.True(session.ForceEmergencyForTests(_clock.UtcNow, EmergencyKinds.Breakdown));
            Assert.False(session.ForceEmergencyForTests(_clock.UtcNow, EmergencyKinds.Leak)); // never two at once
            var dto = session.SurvivalForTests(_clock.UtcNow).Dto!;
            Assert.Equal(EmergencyKinds.Breakdown, dto.EmergencyKind);
            Assert.Equal("food", dto.EmergencyBuildingId); // the reactor never breaks
            Assert.Equal("food_lab", dto.EmergencyDefinitionId);
            Assert.Equal(0, Net(session, _clock.UtcNow, ResourceType.Food));
            var cost = SurvivalRules.RepairCost(world.Catalog.Catalog, 1).Single();
            Assert.Equal(ResourceType.Titanium, cost.Key);
            Assert.Equal(120, cost.Value); // 10 % of the reactor-1 income hour (1 200)
            Assert.Equal(120, dto.RepairCost.Single().Value);

            var food = Has(session, ResourceType.Food);
            Run(world, 100);
            Assert.Equal(food, Has(session, ResourceType.Food));

            var commandId = Id();
            var repair = new RepairEmergencyCommand { CommandId = commandId, ClientSequence = 1, EmergencyId = dto.EmergencyId };
            Send(world, session, repair, CommandType.RepairEmergency);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(380, Has(session, ResourceType.Titanium));
            var endsAt = session.StateForTests.Emergency!.RepairEndsAt;
            Assert.Equal(_clock.UtcNow.AddSeconds(600), endsAt);

            // The same command again (a re-send): the first answer, no second charge, the timer untouched.
            Send(world, session, repair, CommandType.RepairEmergency);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(380, Has(session, ResourceType.Titanium));
            Assert.Equal(endsAt, session.StateForTests.Emergency!.RepairEndsAt);

            // A new command for the same repair is refused.
            Send(world, session, new RepairEmergencyCommand { CommandId = Id(), EmergencyId = dto.EmergencyId }, CommandType.RepairEmergency);
            Assert.Equal(nameof(EmergencyRejectReason.AlreadyRepairing), LastAck(connection).Reason);
            Assert.Equal(380, Has(session, ResourceType.Titanium));

            Run(world, 600);
            Assert.Null(session.StateForTests.Emergency);
            Assert.Equal(2000, Net(session, _clock.UtcNow, ResourceType.Food));
            Send(world, session, new RepairEmergencyCommand { CommandId = Id(), EmergencyId = dto.EmergencyId }, CommandType.RepairEmergency);
            Assert.Equal(nameof(EmergencyRejectReason.NoEmergency), LastAck(connection).Reason);
        }

        [Fact]
        public async Task A_repair_needs_its_cost()
        {
            var world = World(Catalog());
            var (session, connection) = await Connect(world, "poor");
            Base(session, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            Set(session, ResourceType.Titanium, 50);
            Assert.True(session.ForceEmergencyForTests(_clock.UtcNow, EmergencyKinds.Breakdown));
            Send(world, session, new RepairEmergencyCommand { CommandId = Id() }, CommandType.RepairEmergency);
            Assert.Equal(nameof(EmergencyRejectReason.InsufficientResources), LastAck(connection).Reason);
            Assert.Equal(50, Has(session, ResourceType.Titanium));
            Assert.Null(session.StateForTests.Emergency!.RepairEndsAt);
        }

        [Fact]
        public async Task Emergencies_are_rate_limited_from_the_last_start()
        {
            var world = World(Catalog());
            var (session, connection) = await Connect(world, "cooldown");
            Base(session, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            Set(session, ResourceType.Titanium, 10_000);
            var start = _clock.UtcNow;
            Assert.True(session.ForceEmergencyForTests(start, EmergencyKinds.Breakdown));
            Send(world, session, new RepairEmergencyCommand { CommandId = Id() }, CommandType.RepairEmergency);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Run(world, 600);
            Assert.Null(session.StateForTests.Emergency);

            _clock.UtcNow = start.AddHours(5).AddMinutes(59);
            Assert.False(session.ForceEmergencyForTests(_clock.UtcNow));
            _clock.UtcNow = start.AddHours(6);
            Assert.True(session.ForceEmergencyForTests(_clock.UtcNow));
        }

        [Fact]
        public async Task An_ignored_leak_fixes_itself_and_offline_it_drains_only_until_then()
        {
            var world = World(Catalog(emergencies: e =>
            {
                e["autoResolveHours"] = 1;
                e["leakMinPerHour"] = 1800;
            }));
            var (session, connection) = await Connect(world, "leak");
            Base(session, ("reactor", BuildingProgression.ReactorId, 1));
            Set(session, ResourceType.Oxygen, 5000);
            Assert.True(session.ForceEmergencyForTests(_clock.UtcNow, EmergencyKinds.Leak));
            Assert.Equal(EmergencyKinds.Leak, session.StateForTests.Emergency!.Kind);
            Assert.Equal("reactor", session.StateForTests.Emergency.BuildingId); // no Oxygen Plant: it shows on the reactor
            Assert.Equal(-500, Net(session, _clock.UtcNow, ResourceType.Oxygen)); // 1 800 an hour

            await world.DisconnectAsync(session, connection, default);
            _clock.Advance(TimeSpan.FromHours(3));
            var (back, _) = await Connect(world, "leak");
            Assert.Null(back.StateForTests.Emergency);
            Assert.Equal(5000 - 1800, Has(back, ResourceType.Oxygen));
        }

        [Fact]
        public async Task A_leak_loses_a_share_of_the_oxygen_production()
        {
            var world = World(Catalog());
            var (session, _) = await Connect(world, "share");
            Base(session, ("reactor", BuildingProgression.ReactorId, 1), ("plant", "oxygen_plant", 13));
            Assert.Equal(12000, Net(session, _clock.UtcNow, ResourceType.Oxygen));
            Assert.True(session.ForceEmergencyForTests(_clock.UtcNow, EmergencyKinds.Leak));
            Assert.Equal("plant", session.StateForTests.Emergency!.BuildingId);
            Assert.Equal(6000, Net(session, _clock.UtcNow, ResourceType.Oxygen)); // half of it leaks
            Assert.Equal(6000, session.SurvivalForTests(_clock.UtcNow).Dto!.LeakMilliPerTick);
        }

        [Fact]
        public async Task Emergencies_start_only_online_and_never_during_the_guide()
        {
            var world = World(Catalog(emergencies: e => e["chancePercent"] = 100));
            var (newcomer, _) = await Connect(world, "newcomer", pastGuide: false);
            Base(newcomer, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            Assert.Equal(OnboardingStatus.Active, newcomer.StateForTests.OnboardingStatus);
            for (var hour = 0; hour < 8; hour++)
                Run(world, 3600);
            Assert.Null(newcomer.StateForTests.Emergency);

            // Past the guide, the next online hour rolls (and with a 100 % chance, hits).
            newcomer.StateForTests.OnboardingStatus = OnboardingStatus.Done;
            Run(world, 3600);
            Assert.NotNull(newcomer.StateForTests.Emergency);

            // Offline, nothing starts: a player who comes back after a day finds none waiting.
            var (away, awayConnection) = await Connect(world, "away");
            Base(away, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            await world.DisconnectAsync(away, awayConnection, default);
            for (var hour = 0; hour < 8; hour++)
                Run(world, 3600);
            var (back, _) = await Connect(world, "away");
            Assert.Null(back.StateForTests.Emergency);
            world.Tick();
            Assert.NotNull(back.StateForTests.Emergency);
        }

        [Fact]
        public async Task Turning_emergencies_off_ends_the_running_one()
        {
            var world = World(Catalog());
            var (session, _) = await Connect(world, "switch");
            Base(session, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            Assert.True(session.ForceEmergencyForTests(_clock.UtcNow, EmergencyKinds.Breakdown));
            Assert.Equal(0, Net(session, _clock.UtcNow, ResourceType.Food));

            world.ReplaceCatalog(Catalog(emergencies: e => e["enabled"] = false), 98);
            Assert.Equal(2000, Net(session, _clock.UtcNow, ResourceType.Food)); // its effect stops with the switch
            Run(world, 1);
            Assert.Null(session.StateForTests.Emergency);
            Assert.False(session.ForceEmergencyForTests(_clock.UtcNow));
        }

        [Fact]
        public async Task Alliance_mates_can_help_a_repair()
        {
            var world = World(Catalog());
            var (owner, ownerConnection) = await Connect(world, "repairowner");
            var (mate, mateConnection) = await Connect(world, "repairmate");
            foreach (var s in new[] { owner, mate })
                Set(s, ResourceType.Titanium, 1_000_000);
            Send(world, owner, new CreateAllianceCommand { CommandId = Id(), Name = "House Repair", Tag = "REPR" }, CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(ownerConnection).Result);
            var allianceId = TestSupport.Serializer.Deserialize<AllianceDto>(ownerConnection.Messages.Where(m => m.Delta != null)
                .Select(m => m.Delta()).SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.MyAlliance && c.Kind != EntityChangeKind.Delete).Payload).Id;
            Send(world, mate, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(mateConnection).Result);

            Base(owner, ("reactor", BuildingProgression.ReactorId, 1), ("food", "food_lab", 1));
            Assert.True(owner.ForceEmergencyForTests(_clock.UtcNow, EmergencyKinds.Breakdown));
            var emergencyId = owner.StateForTests.Emergency!.Id;

            // No repair running yet: nothing to help.
            Send(world, owner, new RequestHelpCommand { CommandId = Id(), Kind = HelpKinds.Repair, Target = emergencyId }, CommandType.RequestHelp);
            Assert.Equal(CommandResultCode.Rejected, LastAck(ownerConnection).Result);

            Send(world, owner, new RepairEmergencyCommand { CommandId = Id(), EmergencyId = emergencyId }, CommandType.RepairEmergency);
            var before = owner.StateForTests.Emergency!.RepairEndsAt!.Value;
            Send(world, owner, new RequestHelpCommand { CommandId = Id(), Kind = HelpKinds.Repair, Target = emergencyId }, CommandType.RequestHelp);
            Assert.Equal(CommandResultCode.Accepted, LastAck(ownerConnection).Result);

            Send(world, mate, new HelpAllCommand { CommandId = Id() }, CommandType.HelpAll);
            Assert.Equal(CommandResultCode.Accepted, LastAck(mateConnection).Result);
            var cut = SocialRules.HelpSeconds(world.Catalog.Catalog.Alliance, 600);
            Assert.Equal(before.AddSeconds(-cut), owner.StateForTests.Emergency!.RepairEndsAt);
        }

        // --- Persistence and wire ---------------------------------------------------------------------------------------

        [Fact]
        public void The_emergency_and_its_schedule_survive_a_save()
        {
            var at = new DateTimeOffset(2026, 10, 30, 9, 0, 0, TimeSpan.Zero);
            var state = new PlayerState
            {
                Emergency = new EmergencyState
                {
                    Id = "e1", Kind = EmergencyKinds.Breakdown, BuildingId = "b1", StartedAt = at, ExpiresAt = at.AddHours(4),
                    RepairEndsAt = at.AddMinutes(10), RepairSeconds = 600
                },
                LastEmergencyAt = at,
                EmergencyRollHour = 123
            };
            var loaded = new PlayerState();
            PlayerExtras.Read(PlayerExtras.Write(state), loaded);
            Assert.Equal("e1", loaded.Emergency!.Id);
            Assert.Equal(EmergencyKinds.Breakdown, loaded.Emergency.Kind);
            Assert.Equal("b1", loaded.Emergency.BuildingId);
            Assert.Equal(at.AddHours(4), loaded.Emergency.ExpiresAt);
            Assert.Equal(at.AddMinutes(10), loaded.Emergency.RepairEndsAt);
            Assert.Equal(600, loaded.Emergency.RepairSeconds);
            Assert.Equal(at, loaded.LastEmergencyAt);
            Assert.Equal(123, loaded.EmergencyRollHour);

            var empty = new PlayerState();
            PlayerExtras.Read(PlayerExtras.Write(new PlayerState()), empty);
            Assert.Null(empty.Emergency);
            Assert.Null(empty.LastEmergencyAt);
        }

        [Fact]
        public void The_survival_state_travels_inside_the_production_summary()
        {
            var dto = new ProductionStateDto
            {
                ServerTick = 5,
                Survival = new SurvivalStateDto
                {
                    Energy = 2, EnergyPercent = 50, Oxygen = 1, EmergencyId = "e9", EmergencyKind = EmergencyKinds.Leak, RepairRemainingMs = 1000,
                    RepairCost = new[] { new ResourceAmountEntry { Key = ResourceType.Titanium, Value = 120 } }
                }
            };
            var back = TestSupport.Serializer.Deserialize<ProductionStateDto>(TestSupport.Serializer.Serialize(dto));
            Assert.Equal(2, back.Survival!.Energy);
            Assert.Equal(50, back.Survival.EnergyPercent);
            Assert.Equal(1, back.Survival.Oxygen);
            Assert.Equal("e9", back.Survival.EmergencyId);
            Assert.Equal(120, back.Survival.RepairCost.Single().Value);
            Assert.Null(TestSupport.Serializer.Deserialize<ProductionStateDto>(TestSupport.Serializer.Serialize(new ProductionStateDto())).Survival);
        }

        [Fact]
        public void The_shipped_catalog_turns_the_loop_on_with_conservative_values()
        {
            var survival = TestSupport.BalanceCatalog.Survival;
            Assert.True(survival.enabled);
            Assert.True(survival.emergencies.enabled);
            Assert.InRange(survival.energyFloorPercent, 25, 100);
            Assert.InRange(survival.outputPenaltyPercent, 0, 25);
            Assert.InRange(survival.timePenaltyPercent, 0, 25);
            Assert.True(survival.emergencies.cooldownHours >= 6);
            Assert.True(survival.emergencies.autoResolveHours <= 6);
            Assert.True(TestSupport.BalanceCatalog.Data.version >= 13);
            Assert.False(TestSupport.Catalog.Survival.enabled); // the mechanics catalog runs the old economy
        }
    }
}
