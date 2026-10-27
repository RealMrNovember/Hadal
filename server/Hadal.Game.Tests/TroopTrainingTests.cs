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
    /// <summary>Training yards, the army cap and troop power (docs/rehber 07 §2.3–2.5, 19 §2).</summary>
    public sealed class TroopTrainingTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public TroopTrainingTests()
        {
            _world = new GameWorld(Catalog, new InMemoryPlayerRepository(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity, int yardLevel = 1, int domeLevel = 1)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            var state = session.StateForTests;
            state.Buildings.Add(new PlacedBuilding { BuildingId = "yard", DefinitionId = "armor_yard", Ring = 4, Sector = 0, Level = yardLevel });
            state.Buildings.Add(new PlacedBuilding { BuildingId = "dome", DefinitionId = "military_dome", Ring = 4, Sector = 1, Level = domeLevel });
            foreach (var type in new[] { ResourceType.Food, ResourceType.Biomass, ResourceType.Titanium })
                state.Wallet[type] = 100000;
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : class, ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private void Train(PlayerSession session, int tier, int count, string building = "yard", string type = "armored") =>
            Send(session, new TrainTroopsCommand { CommandId = Guid.NewGuid().ToString("N"), BuildingId = building, TroopType = type, Tier = tier, Count = count },
                CommandType.TrainTroops);

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Where(d => d.Ack() != null).Last().Ack()!;

        private static ArmyDto Army(RecordingConnection connection) =>
            TestSupport.Serializer.Deserialize<ArmyDto>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.Army).Payload);

        [Fact]
        public void The_catalog_has_four_types_ten_tiers_and_a_yard_for_each_type()
        {
            var troops = Catalog.Troops;
            Assert.Equal(new[] { "armored", "harpoon", "arc", "driller" }, troops.types.Select(t => t.id));
            Assert.Equal(Enumerable.Range(1, 10), troops.tiers.Select(t => t.tier));
            foreach (var type in troops.types)
                Assert.True(Catalog.TryGetBuilding(type.building, out _), type.building);
            Assert.Equal(300, TroopRules.Interpolate(troops.capacity, 1));
            Assert.Equal(40, TroopRules.BatchSize(troops, 1));
            Assert.Equal(1, TroopRules.MaxTier(troops, 1));
            Assert.Equal(2, TroopRules.MaxTier(troops, 5));
        }

        [Fact]
        public async Task A_batch_is_paid_up_front_and_joins_the_army_when_its_time_is_up()
        {
            var (session, connection) = await Connect("train-a");
            var foodBefore = session.StateForTests.Wallet[ResourceType.Food];

            Train(session, 1, 40);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var tier = TroopRules.Tier(Catalog.Troops, 1)!;
            Assert.Equal(foodBefore - (long)Math.Ceiling(tier.food * 40), session.StateForTests.Wallet[ResourceType.Food]);
            var training = Assert.Single(Army(connection).Training);
            Assert.Equal(("yard", "armored", 1, 40), (training.BuildingId, training.Type, training.Tier, training.Count));

            // The yard trains one batch at a time.
            Train(session, 1, 10);
            Assert.Equal(nameof(TroopRejectReason.QueueBusy), LastAck(connection).Reason);

            var powerBefore = session.Power;
            _clock.UtcNow = DateTimeOffset.FromUnixTimeMilliseconds(training.EndsAtMs).AddSeconds(1);
            _world.Tick();

            var army = Army(connection);
            Assert.Empty(army.Training);
            var stack = Assert.Single(army.Troops);
            Assert.Equal(("armored", 1, 40L), (stack.Type, stack.Tier, stack.Count));
            Assert.Equal(powerBefore + tier.power * 40, session.Power);
        }

        [Fact]
        public async Task Cancelling_refunds_the_whole_price()
        {
            var (session, connection) = await Connect("train-b");
            var wallet = new Dictionary<ResourceType, long>(session.StateForTests.Wallet);

            Train(session, 1, 25);
            Send(session, new CancelTrainingCommand { CommandId = "c1", BuildingId = "yard" }, CommandType.CancelTraining);

            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Empty(Army(connection).Training);
            foreach (var pair in wallet)
                Assert.Equal(pair.Value, session.StateForTests.Wallet[pair.Key]);

            Send(session, new CancelTrainingCommand { CommandId = "c2", BuildingId = "yard" }, CommandType.CancelTraining);
            Assert.Equal(nameof(TroopRejectReason.NotTraining), LastAck(connection).Reason);
        }

        [Fact]
        public async Task Locked_tiers_wrong_yards_big_batches_and_a_full_army_are_refused()
        {
            var (session, connection) = await Connect("train-c");

            Train(session, 2, 10);
            Assert.Equal(nameof(TroopRejectReason.TierLocked), LastAck(connection).Reason);

            Train(session, 1, 10, type: "harpoon");
            Assert.Equal(nameof(TroopRejectReason.WrongBuilding), LastAck(connection).Reason);

            Train(session, 1, 41);
            Assert.Equal(nameof(TroopRejectReason.BadCount), LastAck(connection).Reason);

            // A level-1 dome holds 300: troops at home count against it.
            session.StateForTests.Troops[TroopRules.Key("armored", 1)] = 290;
            Train(session, 1, 20);
            Assert.Equal(nameof(TroopRejectReason.ArmyFull), LastAck(connection).Reason);
            Train(session, 1, 10);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
        }

        [Fact]
        public async Task Higher_yard_levels_open_tiers_and_train_faster()
        {
            var (session, connection) = await Connect("train-d", yardLevel: 5, domeLevel: 5);

            Train(session, 2, 100);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var training = Army(connection).Training.Single();
            var seconds = (training.EndsAtMs - training.StartedAtMs) / 1000.0;
            var tier = TroopRules.Tier(Catalog.Troops, 2)!;
            Assert.True(seconds < tier.seconds * 100, $"{seconds}s with a 2.5% faster yard");
        }

        [Fact]
        public async Task The_med_bay_heals_wounded_at_a_share_of_the_price_and_the_army_eats()
        {
            var (session, connection) = await Connect("heal-a");
            var state = session.StateForTests;
            var key = TroopRules.Key("armored", 1);

            Send(session, new HealTroopsCommand { CommandId = "h0" }, CommandType.HealTroops);
            Assert.Equal(nameof(TroopRejectReason.NoMedBay), LastAck(connection).Reason);

            state.Buildings.Add(new PlacedBuilding { BuildingId = "bay", DefinitionId = "med_bay", Ring = 4, Sector = 2, Level = 1 });
            Assert.Equal(100, session.WoundForTests(key, 150)); // a level-1 Med Bay holds 100; the rest is lost
            var food = state.Wallet[ResourceType.Food];

            Send(session, new HealTroopsCommand { CommandId = "h1" }, CommandType.HealTroops);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var tier = TroopRules.Tier(Catalog.Troops, 1)!;
            Assert.Equal(food - (long)Math.Ceiling(tier.food * 100 * 0.3), state.Wallet[ResourceType.Food]);
            var army = Army(connection);
            Assert.Empty(army.Wounded);
            Assert.Equal(100, army.Healing!.Troops.Single().Count);

            _clock.UtcNow = DateTimeOffset.FromUnixTimeMilliseconds(army.Healing.EndsAtMs).AddSeconds(1);
            _world.Tick();
            Assert.Null(Army(connection).Healing);
            Assert.Equal(100, state.Troops[key]);

            // Upkeep: the base's food rate drops by the army's appetite.
            var before = session.ProductionForTests(ResourceType.Food);
            state.Troops[key] = 10_100;
            var after = session.ProductionForTests(ResourceType.Food);
            Assert.True(after < before, $"{before} -> {after}");
        }

        [Fact]
        public async Task Speedups_cut_training_and_finish_it_when_the_time_runs_out()
        {
            var (session, connection) = await Connect("speed-a");
            var state = session.StateForTests;
            Train(session, 1, 40);
            var before = Army(connection).Training.Single();

            state.Items[Speedups.Id(SpeedupKind.Build, 5)] = 1;
            Send(session, new UseSpeedupCommand { CommandId = "s0", ItemId = Speedups.Id(SpeedupKind.Build, 5), Target = "train:yard" }, CommandType.UseSpeedup);
            Assert.Equal(nameof(SpeedupRejectReason.WrongTarget), LastAck(connection).Reason);

            // 40 troops take 80 s: a 5-minute universal speed-up finishes them at once and joins them to the army.
            state.Items[Speedups.Id(SpeedupKind.Any, 5)] = 2;
            Send(session, new UseSpeedupCommand { CommandId = "s1", ItemId = Speedups.Id(SpeedupKind.Any, 5), Target = "train:yard" }, CommandType.UseSpeedup);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(1, state.Items[Speedups.Id(SpeedupKind.Any, 5)]);
            Assert.Empty(Army(connection).Training);
            Assert.Equal(40, state.Troops[TroopRules.Key("armored", 1)]);
            Assert.True(before.EndsAtMs > _clock.UtcNow.ToUnixTimeMilliseconds());

            Send(session, new UseSpeedupCommand { CommandId = "s2", ItemId = Speedups.Id(SpeedupKind.Any, 5), Target = "train:yard" }, CommandType.UseSpeedup);
            Assert.Equal(nameof(SpeedupRejectReason.NothingToSpeedUp), LastAck(connection).Reason);
        }

        [Fact]
        public void Speedup_ids_parse_and_percent_cuts_compound()
        {
            Assert.True(Speedups.TryParse("speed_any_60", out var kind, out var value, out var percent));
            Assert.Equal((SpeedupKind.Any, 60, false), (kind, value, percent));
            Assert.True(Speedups.TryParse("speed_march_p50", out kind, out value, out percent));
            Assert.Equal((SpeedupKind.March, 50, true), (kind, value, percent));
            Assert.False(Speedups.TryParse("speed_march_60", out _, out _, out _));
            Assert.False(Speedups.TryParse("speed_any_p50", out _, out _, out _));
            Assert.Equal(75, Speedups.Cut(50, true, 2, 100), 3);
            Assert.Equal(100, Speedups.Cut(5, false, 3, 100), 3);
            Assert.True(Catalog.TryGetItem("chest_silver", out var chest) && chest.table.Any(t => t.reward.id == "speed_march_p50"));
        }

        [Fact]
        public async Task Troops_and_training_survive_a_save()
        {
            var (session, _) = await Connect("train-e");
            Train(session, 1, 30);
            session.StateForTests.Troops[TroopRules.Key("armored", 1)] = 12;

            var copy = new PlayerState();
            Hadal.Game.Persistence.PlayerExtras.Read(Hadal.Game.Persistence.PlayerExtras.Write(session.StateForTests), copy);

            Assert.Equal(12, copy.Troops[TroopRules.Key("armored", 1)]);
            var training = Assert.Single(copy.Training);
            Assert.Equal(("yard", 30), (training.BuildingId, training.Count));
            Assert.True(training.Cost[ResourceType.Food] > 0);
        }
    }
}
