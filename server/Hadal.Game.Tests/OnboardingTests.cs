using Hadal.Game.Onboarding;
using Hadal.Game.Persistence;
using Hadal.Game.Social;
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
    /// The first-session guide (docs/34): a fresh account walks it with real commands on the live balance, steps go in
    /// order and only when the server sees them done, a re-sent claim pays once, accounts from before the guide skip or
    /// join it, and every step lands once in the funnel log.
    /// </summary>
    public sealed class OnboardingTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly InMemoryPlayerRepository _repository = new();
        private readonly InMemoryOnboardingStore _store = new();
        private readonly GameWorld _world;

        public OnboardingTests()
        {
            _world = new GameWorld(new CatalogRuntime(Catalog), _repository, new InMemorySocialStore(), new InMemoryWorldStore(), _clock,
                TestSupport.Serializer, NullLogger<GameWorld>.Instance, onboardingStore: _store);
        }

        private async Task<(PlayerSession S, RecordingConnection C)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
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

        private static QuestStateDto Quests(RecordingConnection connection) =>
            connection.Messages.Any(m => m.Delta != null && m.Delta().Changes.Any(c => c.EntityId == EntityIds.Quests))
                ? Latest<QuestStateDto>(connection, EntityIds.Quests)
                : connection.Messages.First().Snapshot().Quests!;

        private static string[] Steps => Catalog.Quests.onboarding.Select(q => q.id).ToArray();

        private void Claim(PlayerSession session, string step, string? commandId = null) =>
            Send(session, new ClaimQuestCommand { CommandId = commandId ?? Id(), ClientSequence = 1, QuestId = step }, CommandType.ClaimQuest);

        /// <summary>Claims the current step and checks it was accepted and the guide moved on.</summary>
        private void ClaimOk(PlayerSession session, RecordingConnection connection, string step)
        {
            Assert.Equal(step, Quests(connection).Guide?.Id);
            Claim(session, step);
            var ack = LastAck(connection);
            Assert.True(ack.Result == CommandResultCode.Accepted, $"{step}: {ack.Reason}");
            var next = Array.IndexOf(Steps, step) + 1;
            Assert.Equal(next < Steps.Length ? Steps[next] : null, Quests(connection).Guide?.Id);
        }

        private PlacedBuilding Building(PlayerSession session, string id) =>
            session.StateForTests.Buildings.Where(b => b.DefinitionId == id).OrderByDescending(b => b.Level).First();

        /// <summary>Upgrades and finishes (free while short) like a player does.</summary>
        private void Upgrade(PlayerSession session, RecordingConnection connection, string id)
        {
            var building = Building(session, id);
            _world.HandleCommand(session, TestSupport.Upgrade(building.BuildingId));
            Assert.True(LastAck(connection).Result == CommandResultCode.Accepted, $"upgrade {id}: {LastAck(connection).Reason}");
            _world.HandleCommand(session, TestSupport.Finish(building.BuildingId));
            Assert.True(LastAck(connection).Result == CommandResultCode.Accepted, $"finish {id}: {LastAck(connection).Reason}");
        }

        /// <summary>Builds on the first slot the rules allow, as the guide's build button does.</summary>
        private void Build(PlayerSession session, RecordingConnection connection, string id)
        {
            var state = session.StateForTests;
            var occupied = new HashSet<(int, int)>(state.Buildings.Select(b => (b.Ring, b.Sector)));
            var owned = state.Buildings.GroupBy(b => b.DefinitionId).ToDictionary(g => g.Key, g => g.Count());
            var reactor = Building(session, BuildingProgression.ReactorId).Level;
            for (var ring = 1; ring <= state.UnlockedRings; ring++)
            {
                for (var sector = 0; sector < Catalog.GetSectorCount(ring); sector++)
                {
                    if (PlacementRules.Validate(Catalog, id, ring, sector, state.UnlockedRings, occupied, state.Wallet, owned, reactor) != PlacementRejectReason.None)
                        continue;
                    _world.PlaceBuilt(session, connection, id, ring, sector);
                    return;
                }
            }

            Assert.Fail($"no slot for {id}");
        }

        private TimeSpan Wait(Func<bool> until, int maxSeconds = 1800)
        {
            var start = _clock.UtcNow;
            while (!until())
            {
                Assert.True(_clock.UtcNow - start < TimeSpan.FromSeconds(maxSeconds), "waited too long");
                _clock.Advance(TimeSpan.FromSeconds(2));
                _world.Tick();
            }

            return _clock.UtcNow - start;
        }

        [Fact]
        public async Task A_new_account_starts_the_guide_and_logs_its_start()
        {
            var (session, connection) = await Connect("guide-new");
            var quests = connection.Messages.First().Snapshot().Quests!;
            Assert.Equal(("ob_reactor2", 1, Steps.Length), (quests.Guide!.Id, quests.GuideNumber, quests.GuideTotal));
            Assert.Equal("q_oxygen2", quests.Chapter!.Id); // the chapter carries on underneath

            var start = Assert.Single(_store.All);
            Assert.Equal((0, "start", "new", session.PublicId), (start.Step, start.StepId, start.Cohort, start.PlayerId));
        }

        [Fact]
        public async Task A_fresh_account_walks_the_whole_guide_with_real_commands_in_under_twenty_minutes()
        {
            var (session, connection) = await Connect("guide-walk");
            var began = _clock.UtcNow;

            // Out of order and not yet done are refused; nothing moves.
            Claim(session, "ob_oxygen");
            Assert.Equal(nameof(QuestRejectReason.NotCurrent), LastAck(connection).Reason);
            Claim(session, "ob_reactor2");
            Assert.Equal(nameof(QuestRejectReason.NotDone), LastAck(connection).Reason);

            Upgrade(session, connection, BuildingProgression.ReactorId);
            ClaimOk(session, connection, "ob_reactor2");

            Build(session, connection, "oxygen_plant");
            ClaimOk(session, connection, "ob_oxygen");

            // The step's ten Lyra cards recruit her: the first hero of one's own.
            Send(session, new RecruitHeroCommand { CommandId = Id(), ClientSequence = 1, HeroId = "lyra" }, CommandType.RecruitHero);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            ClaimOk(session, connection, "ob_hero");

            Upgrade(session, connection, "oxygen_plant");
            ClaimOk(session, connection, "ob_oxygen2");
            Upgrade(session, connection, BuildingProgression.ReactorId);
            ClaimOk(session, connection, "ob_reactor3");

            Send(session, new UnlockRingCommand { CommandId = Id(), ClientSequence = 1, Ring = 2 }, CommandType.UnlockRing);
            Assert.True(LastAck(connection).Result == CommandResultCode.Accepted, LastAck(connection).Reason);
            ClaimOk(session, connection, "ob_ring2");

            Build(session, connection, "drone_factory");
            ClaimOk(session, connection, "ob_drones");
            Upgrade(session, connection, "drone_factory");
            Upgrade(session, connection, "drone_factory");
            ClaimOk(session, connection, "ob_drones3");

            Upgrade(session, connection, BuildingProgression.ReactorId);
            var troopsBefore = session.StateForTests.Troops.Values.Sum();
            ClaimOk(session, connection, "ob_reactor4");
            Assert.Equal(troopsBefore + 300, session.StateForTests.Troops.Values.Sum()); // the first army, straight home

            // The first monster: the nearest level 1 swarm.
            Send(session, new ViewWorldCommand { CommandId = Id(), X = -1, Y = -1 }, CommandType.ViewWorld);
            var view = Latest<WorldViewDto>(connection, EntityIds.WorldView);
            var monster = _world.Map.MonstersForTests().Where(m => m.Level == 1 && !m.IsBoss)
                .OrderBy(m => GatherRules.Km(m.X, m.Y, view.MyX, view.MyY)).First();
            Send(session, new MarchAttackCommand { CommandId = Id(), ClientSequence = 1, MonsterId = monster.Id }, CommandType.MarchAttack);
            Assert.True(LastAck(connection).Result == CommandResultCode.Accepted, LastAck(connection).Reason);
            Wait(() => session.StateForTests.QuestCounters.TryGetValue("monster", out var won) && won > 0);
            ClaimOk(session, connection, "ob_monster");

            // The first reward opened from the bag.
            Send(session, new OpenItemCommand { CommandId = Id(), ClientSequence = 1, ItemId = "chest_bronze", Count = 1 }, CommandType.OpenItem);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            ClaimOk(session, connection, "ob_open");

            // The first gathering march, once the hunting team is home (one queue at reactor 4).
            Wait(() => session.StateForTests.Marches.Count == 0);
            Send(session, new ViewWorldCommand { CommandId = Id(), X = -1, Y = -1 }, CommandType.ViewWorld);
            view = Latest<WorldViewDto>(connection, EntityIds.WorldView);
            var node = view.Nodes.Where(n => !n.Busy && n.Remaining > 0).OrderBy(n => GatherRules.Km(n.X, n.Y, view.MyX, view.MyY)).First();
            Send(session, new MarchGatherCommand { CommandId = Id(), ClientSequence = 1, NodeId = node.Id }, CommandType.MarchGather);
            Assert.True(LastAck(connection).Result == CommandResultCode.Accepted, LastAck(connection).Reason);
            ClaimOk(session, connection, "ob_gather");

            // The first-session milestone well inside the 15-20 minutes (here only the marches take time).
            Assert.True(_clock.UtcNow - began < TimeSpan.FromMinutes(20), $"took {_clock.UtcNow - began}");

            // Day one: join an alliance, ask for help and get it.
            Claim(session, "ob_alliance");
            Assert.Equal(nameof(QuestRejectReason.NotDone), LastAck(connection).Reason);
            var (founder, founderConnection) = await Connect("guide-founder");
            founder.StateForTests.Wallet[ResourceType.Titanium] = 10_000;
            Send(founder, new CreateAllianceCommand { CommandId = Id(), Name = "Guide House", Tag = "GDE" }, CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(founderConnection).Result);
            Send(session, new JoinAllianceCommand { CommandId = Id(), AllianceId = Latest<AllianceDto>(founderConnection, EntityIds.MyAlliance).Id }, CommandType.JoinAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            ClaimOk(session, connection, "ob_alliance");

            // Help is asked on a running construction (the clock stands still here, so even a short one is running).
            var plant = Building(session, "oxygen_plant");
            _world.HandleCommand(session, TestSupport.Upgrade(plant.BuildingId));
            Assert.True(LastAck(connection).Result == CommandResultCode.Accepted, LastAck(connection).Reason);
            Send(session, new RequestHelpCommand { CommandId = Id(), Kind = HelpKinds.Build, BuildingEntityId = plant.BuildingId }, CommandType.RequestHelp);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            ClaimOk(session, connection, "ob_help_ask");

            Send(founder, new HelpAllCommand { CommandId = Id() }, CommandType.HelpAll);
            Assert.Equal(CommandResultCode.Accepted, LastAck(founderConnection).Result);
            ClaimOk(session, connection, "ob_helped");

            Assert.Null(Quests(connection).Guide);
            Assert.Equal(OnboardingStatus.Done, session.StateForTests.OnboardingStatus);

            // The funnel: the start and every step once, in order, timed from the start.
            var log = _store.All.Where(e => e.PlayerId == session.PublicId).OrderBy(e => e.Step).ToList();
            Assert.Equal(Enumerable.Range(0, Steps.Length + 1), log.Select(e => e.Step));
            Assert.Equal(Steps, log.Skip(1).Select(e => e.StepId));
            Assert.True(log.Zip(log.Skip(1)).All(p => p.First.Seconds <= p.Second.Seconds));
        }

        [Fact]
        public async Task A_resent_claim_pays_once_and_a_fresh_claim_of_a_done_step_is_refused()
        {
            var (session, connection) = await Connect("guide-twice");
            Upgrade(session, connection, BuildingProgression.ReactorId);
            var titanium = session.StateForTests.Wallet[ResourceType.Titanium];

            var claim = new ClaimQuestCommand { CommandId = "claim-once", ClientSequence = 1, QuestId = "ob_reactor2" };
            var envelope = EnvelopeBuilder.WrapCommand(claim, CommandType.ClaimQuest, TestSupport.Serializer.Serialize(claim));
            _world.HandleCommand(session, envelope);
            _world.HandleCommand(session, envelope); // the network re-sends it
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(titanium + 200, session.StateForTests.Wallet[ResourceType.Titanium]); // paid once, straight to the wallet
            Assert.Equal(1, session.StateForTests.OnboardingIndex);

            Claim(session, "ob_reactor2");
            Assert.Equal(nameof(QuestRejectReason.NotCurrent), LastAck(connection).Reason);
            Assert.Single(_store.All, e => e.PlayerId == session.PublicId && e.Step == 1);
        }

        [Fact]
        public async Task Guide_progress_survives_a_reconnect()
        {
            var (session, connection) = await Connect("guide-again");
            Upgrade(session, connection, BuildingProgression.ReactorId);
            ClaimOk(session, connection, "ob_reactor2");
            session.StateForTests.IsDirty = true;
            await _world.DisconnectAsync(session, connection, default);

            var (_, again) = await Connect("guide-again");
            var quests = again.Messages.First().Snapshot().Quests!;
            Assert.Equal(("ob_oxygen", 2), (quests.Guide!.Id, quests.GuideNumber));
            Assert.Single(_store.All, e => e.Step == 0); // the start is not logged again
        }

        /// <summary>An account saved before the guide existed: its extras have no guide document.</summary>
        private async Task<(PlayerSession S, RecordingConnection C)> Legacy(string identity, int reactorLevel)
        {
            var (session, connection) = await Connect(identity);
            var reactor = Building(session, BuildingProgression.ReactorId);
            reactor.Level = reactorLevel;
            session.StateForTests.UnsavedBuildings.Add(reactor);
            session.StateForTests.OnboardingStatus = OnboardingStatus.Unknown;
            session.StateForTests.IsDirty = true;
            await _world.DisconnectAsync(session, connection, default);
            return await Connect(identity);
        }

        [Fact]
        public async Task Existing_accounts_past_the_milestone_skip_the_guide_and_early_ones_join_it()
        {
            var (veteran, veteranConnection) = await Legacy("guide-veteran", OnboardingRules.SkipReactorLevel);
            Assert.Null(veteranConnection.Messages.First().Snapshot().Quests!.Guide);
            Assert.Equal(OnboardingStatus.Skipped, veteran.StateForTests.OnboardingStatus);
            Assert.Contains(_store.All, e => e.PlayerId == veteran.PublicId && e.Step == -1);
            Claim(veteran, "ob_reactor2");
            Assert.Equal(nameof(QuestRejectReason.NotCurrent), LastAck(veteranConnection).Reason);

            var (early, earlyConnection) = await Legacy("guide-early", 1);
            Assert.Equal("ob_reactor2", earlyConnection.Messages.First().Snapshot().Quests!.Guide!.Id);
            Assert.Equal(OnboardingStatus.Active, early.StateForTests.OnboardingStatus);
            Assert.Contains(_store.All, e => e.PlayerId == early.PublicId && e.Step == 0); // (here its first login already logged a start)

            // The decision is saved: the veteran stays out on the next login.
            await _world.DisconnectAsync(veteran, veteranConnection, default);
            var (_, back) = await Connect("guide-veteran");
            Assert.Null(back.Messages.First().Snapshot().Quests!.Guide);
        }

        [Fact]
        public void The_funnel_counts_players_per_step_with_the_median_time_and_the_ten_minute_stall()
        {
            var now = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
            var day = now.AddDays(-2);
            var events = new List<OnboardingEvent>
            {
                new("a", 0, "start", "new", day, 0), new("a", 1, "s1", "", day.AddSeconds(60), 60), new("a", 2, "s2", "", day.AddSeconds(900), 900),
                new("b", 0, "start", "new", day, 0), new("b", 1, "s1", "", day.AddSeconds(120), 120),
                new("c", 0, "start", "new", day, 0),
                new("d", 0, "start", "new", now.AddMinutes(-5), 0), new("d", 1, "s1", "", now.AddMinutes(-4), 60),
                new("x", -1, "skipped", "existing", day, 0)
            };

            var report = OnboardingFunnel.Compute(events, new[] { "s1", "s2" }, now);
            Assert.Equal((4, 1, 1), (report.Started, report.Skipped, report.Finished));
            Assert.Equal((3, 75.0, 60, 3), (report.Steps[0].Players, report.Steps[0].Percent, report.Steps[0].MedianSeconds, report.Steps[0].Within10Minutes));
            Assert.Equal((1, 900, 0), (report.Steps[1].Players, report.Steps[1].MedianSeconds, report.Steps[1].Within10Minutes));
            // b and c (a day old, nothing after ten minutes, not finished); d is too new to judge, a finished.
            Assert.Equal((2, 3), (report.StalledIn10Minutes, report.OldEnough));
        }

        [Fact]
        public void The_catalog_guide_is_valid_and_upcoming_unlocks_follow_the_reactor()
        {
            Assert.InRange(Catalog.Quests.onboarding.Length, 10, 16);
            Assert.All(Catalog.Quests.onboarding, q => Assert.True(QuestRules.IsKnownType(q.type), q.id));
            Assert.Contains(Catalog.Quests.onboarding, q => q.rewards.Any(r => r.type == "troops"));

            var atOne = OnboardingRules.Upcoming(Catalog, 1);
            Assert.NotEmpty(atOne);
            Assert.All(atOne, f => Assert.True(f.ReactorLevel > 1));
            Assert.Equal(atOne.OrderBy(f => f.ReactorLevel).Select(f => f.ReactorLevel), atOne.Select(f => f.ReactorLevel));
            Assert.Contains(OnboardingRules.Upcoming(Catalog, 3, 10), f => f.Key == "gather" && f.ReactorLevel == OnboardingRules.MarchUnlock(Catalog));
            Assert.Empty(OnboardingRules.Upcoming(Catalog, 99));

            // Troops are a guide-only reward: anywhere else the catalog is refused.
            var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "catalog", "catalog.json")))!;
            json["quests"]!["chapter"]![0]!["rewards"]!.AsArray().Add(new System.Text.Json.Nodes.JsonObject
            {
                ["type"] = "troops", ["id"] = "armored:1", ["amount"] = 10
            });
            Assert.Throws<FormatException>(() => CatalogLoader.Parse(json.ToJsonString()));
        }
    }

    /// <summary>The funnel log against a real PostgreSQL (HADAL_TEST_PG); skipped otherwise.</summary>
    public sealed class PostgresOnboardingStoreTests
    {
        [Fact]
        public async Task Events_are_written_once_and_load_by_start_date_and_cohort()
        {
            var connectionString = PostgresTest.ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
                return;

            await using var dataSource = Npgsql.NpgsqlDataSource.Create(connectionString);
            var store = new PostgresOnboardingStore(dataSource);
            await store.InitializeAsync(default);
            await store.InitializeAsync(default); // idempotent

            var now = DateTimeOffset.UtcNow;
            var player = "guide" + Guid.NewGuid().ToString("N")[..10];
            var old = "guide" + Guid.NewGuid().ToString("N")[..10];
            await store.RecordAsync(new OnboardingEvent(player, 0, "start", "new", now, 0), default);
            await store.RecordAsync(new OnboardingEvent(player, 1, "ob_reactor2", "", now.AddSeconds(30), 30), default);
            await store.RecordAsync(new OnboardingEvent(player, 1, "ob_reactor2", "", now.AddSeconds(99), 99), default); // replay: ignored
            await store.RecordAsync(new OnboardingEvent(old, 0, "start", "new", now.AddDays(-40), 0), default);

            var loaded = (await store.LoadAsync(now.AddMinutes(-1), "new", default)).Where(e => e.PlayerId == player || e.PlayerId == old).ToList();
            Assert.Equal(new[] { 0, 1 }, loaded.Select(e => e.Step));
            Assert.Equal(30, loaded[1].Seconds);
            Assert.DoesNotContain(await store.LoadAsync(now.AddMinutes(-1), "existing", default), e => e.PlayerId == player);
        }
    }
}
