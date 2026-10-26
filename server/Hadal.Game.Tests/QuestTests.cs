using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    /// <summary>Chapter and daily tasks, chests, hero cards and recruiting (docs/27).</summary>
    public sealed class QuestTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly GameWorld _world;

        public QuestTests()
        {
            _world = TestSupport.World(_clock, _repository);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            return (session, connection);
        }

        private static CommandAckDto LastAck(RecordingConnection connection) => connection.Messages.Last().Delta().Ack()!;

        private static CommandEnvelope Command<T>(T command, CommandType type) where T : ICommand =>
            EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command));

        private static string Id() => Guid.NewGuid().ToString("N");

        private static T Latest<T>(RecordingConnection connection, string entityId) where T : class =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId).Payload);

        private void Claim(PlayerSession session, string quest) =>
            _world.HandleCommand(session, Command(new ClaimQuestCommand { CommandId = Id(), ClientSequence = 1, QuestId = quest }, CommandType.ClaimQuest));

        [Fact]
        public async Task Chapter_tasks_go_in_order_and_their_hero_cards_recruit_a_hero()
        {
            var (session, connection) = await Connect("quest-a");
            var quests = connection.Messages.First().Snapshot().Quests!;
            Assert.Equal("q_oxygen2", quests.Chapter!.Id);
            Assert.Equal(1, quests.ChapterNumber);

            Claim(session, "q_oxygen2");
            Assert.Equal(nameof(QuestRejectReason.NotDone), LastAck(connection).Reason);
            Claim(session, "q_food2");
            Assert.Equal(nameof(QuestRejectReason.NotCurrent), LastAck(connection).Reason);

            // Reach the target: an Oxygen Plant at level 2.
            var plant = session.StateForTests.Buildings.FirstOrDefault(b => b.DefinitionId == "oxygen_plant");
            if (plant == null)
            {
                plant = new PlacedBuilding { BuildingId = "bplant", DefinitionId = "oxygen_plant", Ring = 1, Sector = 0, Level = 2 };
                session.StateForTests.Buildings.Add(plant);
            }
            plant.Level = 2;

            Claim(session, "q_oxygen2");
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var report = connection.Messages.Last().Delta().Changes.Single(c => c.EntityId.StartsWith(EntityIds.RewardReport, StringComparison.Ordinal));
            Assert.Contains(TestSupport.Serializer.Deserialize<RewardReportDto>(report.Payload).Rewards, r => r.Type == "shards" && r.Id == "lyra");
            Assert.Equal("q_food2", Latest<QuestStateDto>(connection, EntityIds.Quests).Chapter!.Id);

            // Ten Lyra cards are her recruit price (common).
            Assert.Equal(10, session.StateForTests.HeroCards["lyra"]);
            _world.HandleCommand(session, Command(new RecruitHeroCommand { CommandId = Id(), ClientSequence = 1, HeroId = "lyra" }, CommandType.RecruitHero));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Contains(session.StateForTests.Heroes, h => h.HeroId == "lyra");
            Assert.False(session.StateForTests.HeroCards.ContainsKey("lyra"));

            _world.HandleCommand(session, Command(new RecruitHeroCommand { CommandId = Id(), ClientSequence = 1, HeroId = "lyra" }, CommandType.RecruitHero));
            Assert.Equal(nameof(QuestRejectReason.AlreadyOwned), LastAck(connection).Reason);
        }

        [Fact]
        public async Task Daily_tasks_count_events_give_points_and_open_chests()
        {
            var (session, connection) = await Connect("quest-b");
            foreach (var type in session.StateForTests.Wallet.Keys.ToList())
                session.StateForTests.Wallet[type] = 20000;

            // Two upgrades finish "d_upgrade" (20 points).
            var reactor = session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId);
            _world.HandleCommand(session, TestSupport.Upgrade(reactor.BuildingId));
            reactor.ConstructionEndsAt = null;
            _world.HandleCommand(session, TestSupport.Upgrade(reactor.BuildingId));
            var daily = Latest<QuestStateDto>(connection, EntityIds.Quests).Daily.Single(d => d.Id == "d_upgrade");
            Assert.Equal((2L, 2L), (daily.Progress, daily.Target));

            _world.HandleCommand(session, Command(new ClaimDailyChestCommand { CommandId = Id(), ClientSequence = 1, Points = 20 }, CommandType.ClaimDailyChest));
            Assert.Equal(nameof(QuestRejectReason.NotEnoughPoints), LastAck(connection).Reason);

            Claim(session, "d_upgrade");
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Claim(session, "d_upgrade");
            Assert.Equal(nameof(QuestRejectReason.AlreadyClaimed), LastAck(connection).Reason);
            Assert.Equal(20, session.StateForTests.DailyPoints);

            _world.HandleCommand(session, Command(new ClaimDailyChestCommand { CommandId = Id(), ClientSequence = 1, Points = 20 }, CommandType.ClaimDailyChest));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);

            // A new day starts over.
            _clock.Advance(TimeSpan.FromDays(1));
            _world.Tick();
            Assert.Equal(0, session.StateForTests.DailyPoints);
            Assert.Empty(session.StateForTests.ClaimedDaily);
        }

        [Fact]
        public async Task Chests_and_cards_open_from_the_bag()
        {
            var (session, connection) = await Connect("quest-c");
            session.StateForTests.Items["chest_gold"] = 2;
            session.StateForTests.Items["card_epic"] = 1;

            _world.HandleCommand(session, Command(new OpenItemCommand { CommandId = Id(), ClientSequence = 1, ItemId = "chest_gold", Count = 3 }, CommandType.OpenItem));
            Assert.Equal(nameof(QuestRejectReason.NotEnoughItems), LastAck(connection).Reason);

            var hadalite = session.StateForTests.Wallet.TryGetValue(ResourceType.Hadalite, out var h) ? h : 0;
            _world.HandleCommand(session, Command(new OpenItemCommand { CommandId = Id(), ClientSequence = 1, ItemId = "chest_gold", Count = 2 }, CommandType.OpenItem));
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.False(session.StateForTests.Items.ContainsKey("chest_gold"));
            Assert.True(session.StateForTests.Wallet[ResourceType.Hadalite] >= Math.Min(1000, hadalite + 60)); // 30 guaranteed each

            _world.HandleCommand(session, Command(new OpenItemCommand { CommandId = Id(), ClientSequence = 1, ItemId = "card_epic", Count = 1 }, CommandType.OpenItem));
            var report = TestSupport.Serializer.Deserialize<RewardReportDto>(connection.Messages.Last().Delta().Changes
                .Single(c => c.EntityId.StartsWith(EntityIds.RewardReport, StringComparison.Ordinal)).Payload);
            var card = Assert.Single(report.Rewards);
            Assert.Equal(("shards", 10L), (card.Type, card.Amount));
            Assert.True(TestSupport.Catalog.TryGetHero(card.Id, out var hero));
            Assert.Equal("epic", hero.Rarity.id);
        }

        [Fact]
        public async Task Quest_progress_survives_a_reconnect()
        {
            var (session, connection) = await Connect("quest-d");
            session.StateForTests.Items["chest_bronze"] = 3;
            session.StateForTests.HeroCards["juno"] = 12;
            session.StateForTests.ChapterIndex = 4;
            session.StateForTests.IsDirty = true;
            await _world.DisconnectAsync(session, connection, default);

            var (_, again) = await Connect("quest-d");
            var snapshot = again.Messages.First().Snapshot();
            Assert.Equal(5, snapshot.Quests!.ChapterNumber);
            Assert.Contains(snapshot.Inventory!.Items, i => i.Id == "chest_bronze" && i.Count == 3);
            Assert.Contains(snapshot.Inventory.HeroCards, i => i.Id == "juno" && i.Count == 12);
        }
    }
}
