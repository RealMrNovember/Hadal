using Hadal.Game.Events;
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
    /// The sea-creature monster kinds (anglers, lantern maws, armoured crabs) and the event bosses (Chasm Maw, Depth
    /// Kraken): who roams where, power and rewards, and a boss that a live event puts on the map as one shared body.
    /// </summary>
    public sealed class CreatureMonsterTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public CreatureMonsterTests()
        {
            _world = new GameWorld(Catalog, new InMemoryPlayerRepository(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            session.ClearCrewForTests();
            session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId).Level = 10;
            session.StateForTests.Troops.Clear();
            session.StateForTests.Troops[TroopRules.Key("harpoon", 4)] = 20000;
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

        private void Tick()
        {
            _clock.Advance(TimeSpan.FromSeconds(6));
            _world.Tick();
        }

        private EventDefinition BossEvent(string kind, int level, int x, int y, string template = EventTemplates.Alliance) => new()
        {
            Template = template,
            Title = new() { ["tr"] = "Uçurum Ağzı Uyanıyor", ["en"] = "The Chasm Maw Wakes" },
            Banner = "abyss",
            StartsAt = _clock.UtcNow,
            EndsAt = _clock.UtcNow.AddDays(1),
            Actions = { new EventActionConfig { Action = EventActions.BossDamage, Points = 1, Per = 1 } },
            Bosses = { new EventBossConfig { Kind = kind, Level = level, X = x, Y = y } }
        };

        private string Publish(EventDefinition definition)
        {
            var (error, saved) = _world.Events.Save(definition, "test", _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.Equal(string.Empty, _world.Events.Publish(saved!.Id, _clock.UtcNow));
            return saved.Id;
        }

        private WorldMonster Boss(PlayerSession viewer)
        {
            View(viewer); // the map takes the event's bosses in when it is looked at
            return _world.Map.MonstersForTests().Single(m => m.IsBoss);
        }

        [Fact]
        public void Every_belt_has_roaming_kinds_and_event_bosses_never_roam()
        {
            foreach (var belt in Catalog.World.belts)
            {
                var kinds = MonsterRules.KindsInBelt(belt.id);
                Assert.NotEmpty(kinds);
                Assert.DoesNotContain(kinds, k => k.EventOnly);
            }

            Assert.Equal(new[] { "chasm_maw", "depth_kraken" }, MonsterRules.Kinds.Where(k => k.EventOnly).Select(k => k.Id));
            Assert.Equal(5, MonsterRules.Roaming.Length);
            Assert.Equal("abyss_crawler", MonsterRules.Kinds[0].Id); // TerritoryRules' garrison stand-in
        }

        [Fact]
        public async Task The_map_lays_out_the_new_kinds_by_depth()
        {
            await Connect("sea-a");
            var monsters = _world.Map.MonstersForTests();
            Assert.DoesNotContain(monsters, m => MonsterRules.Kind(m.Type)!.EventOnly);
            foreach (var id in new[] { "abyss_angler", "lantern_maw", "armored_crab" })
                Assert.Contains(monsters, m => m.Type == id);
            Assert.All(monsters, m =>
            {
                var kind = MonsterRules.Kind(m.Type)!;
                var (min, max) = MonsterRules.LevelsInBelt(GatherRules.BeltOf(Catalog.World, m.X, m.Y)!.id);
                Assert.InRange(m.Level, Math.Max(min, kind.MinLevel), Math.Min(max, kind.MaxLevel));
            });

            // The beginner grounds stay mostly crawlers; lantern maws only live deep.
            Assert.DoesNotContain(monsters, m => m.Type == "lantern_maw" && GatherRules.BeltOf(Catalog.World, m.X, m.Y)!.id is "abisal_duzluk" or "hadal_esigi");
        }

        [Fact]
        public void Kinds_pick_by_weight_and_differ_in_power_and_rewards()
        {
            var kinds = MonsterRules.KindsInBelt("derin_yamac");
            var counts = kinds.ToDictionary(k => k.Id, _ => 0);
            var total = kinds.Sum(k => k.Weight);
            for (var r = 0; r < total; r++)
                counts[MonsterRules.Pick(kinds, _ => r).Id]++;
            Assert.All(kinds, k => Assert.Equal(k.Weight, counts[k.Id]));

            var troops = Catalog.Troops;
            long Power(string id) => MonsterRules.Power(troops, MonsterRules.Kind(id)!, 20);
            Assert.True(Power("armored_crab") > Power("abyss_crawler"));
            Assert.True(Power("lantern_maw") > Power("abyss_angler"));
            Assert.True(Power("chasm_maw") > 4 * Power("lantern_maw"));
            Assert.True(Power("depth_kraken") > Power("chasm_maw"));

            static long Capsule(Dictionary<string, int> items) =>
                items.Keys.Select(k => Capsules.TryParse(k, out _, out var amount) ? amount : 0).Max();
            var crawler = MonsterRules.Rewards(MonsterRules.Kind("abyss_crawler")!, 20, false, _ => 99);
            var maw = MonsterRules.Rewards(MonsterRules.Kind("lantern_maw")!, 20, false, _ => 99);
            Assert.Equal(4000, Capsule(crawler)); // unchanged for the old kinds
            Assert.Equal(MonsterRules.Rewards(20, false, _ => 99), crawler);
            Assert.Equal(7200, Capsule(maw));

            var boss = MonsterRules.Rewards(MonsterRules.Kind("chasm_maw")!, 20, true, _ => 5);
            Assert.Equal(1, boss["chest_silver"]);
            Assert.Equal(1, boss["chest_gold"]);
            Assert.Single(boss.Keys, k => k.StartsWith(Capsules.Prefix)); // no first-win capsule
        }

        [Fact]
        public void Boss_settings_are_validated()
        {
            var size = Catalog.World.size;
            string Check(EventBossConfig boss)
            {
                var def = BossEvent("chasm_maw", 20, 1, 1);
                def.Bosses = new List<EventBossConfig> { boss };
                return EventService.Validate(def, Catalog);
            }

            Assert.Equal(string.Empty, Check(new EventBossConfig { Kind = "chasm_maw", Level = 20, X = 10, Y = 10 }));
            Assert.Equal(string.Empty, Check(new EventBossConfig { Kind = "depth_kraken", Level = 35, X = size - 1, Y = 0 }));
            Assert.Equal("InvalidBoss", Check(new EventBossConfig { Kind = "abyss_crawler", Level = 5, X = 10, Y = 10 }));
            Assert.Equal("InvalidBoss", Check(new EventBossConfig { Kind = "nope", Level = 5, X = 10, Y = 10 }));
            Assert.Equal("InvalidBoss", Check(new EventBossConfig { Kind = "depth_kraken", Level = 5, X = 10, Y = 10 }));
            Assert.Equal("InvalidBoss", Check(new EventBossConfig { Kind = "chasm_maw", Level = 20, X = size, Y = 10 }));
            var many = BossEvent("chasm_maw", 20, 1, 1);
            for (var i = 0; i < 5; i++)
                many.Bosses.Add(new EventBossConfig { Kind = "chasm_maw", Level = 20, X = i, Y = 1 });
            Assert.Equal("InvalidBoss", EventService.Validate(many, Catalog));
        }

        [Fact]
        public async Task A_live_event_puts_its_boss_on_the_map_until_it_ends()
        {
            var (session, connection) = await Connect("boss-view");
            var (x, y) = _world.Map.PositionOf(session.PublicId)!.Value;
            var draft = _world.Events.Save(BossEvent("chasm_maw", 20, x + 2, y), "test", _clock.UtcNow).Saved!;
            Tick();
            Assert.DoesNotContain(_world.Map.MonstersForTests(), m => m.IsBoss); // a draft shows nothing

            Assert.Equal(string.Empty, _world.Events.Publish(draft.Id, _clock.UtcNow));
            Tick();
            View(session);
            var boss = Boss(session);
            Assert.Equal("chasm_maw", boss.Type);
            Assert.Equal(WorldService.BossId(draft.Id, 0), boss.Id);
            Assert.True(Math.Max(Math.Abs(boss.X - x - 2), Math.Abs(boss.Y - y)) <= 6);
            Assert.Equal(MonsterRules.Count(MonsterRules.Kind("chasm_maw")!, 20), boss.HpMax);
            var shown = Latest<WorldViewDto>(connection, EntityIds.WorldView).Monsters.Single(m => m.Id == boss.Id);
            Assert.Equal(boss.HpMax, shown.HpLeft);
            Assert.Equal(boss.HpMax, shown.HpMax);
            Assert.Equal(draft.EndsAt.ToUnixTimeMilliseconds(), shown.EndsAtMs);
            Assert.Equal(MonsterRules.Power(Catalog.Troops, MonsterRules.Kind("chasm_maw")!, 20), shown.Power);

            Assert.Equal(string.Empty, _world.Events.EndNow(draft.Id, _clock.UtcNow));
            Tick();
            View(session);
            Assert.DoesNotContain(_world.Map.MonstersForTests(), m => m.IsBoss);
            Assert.DoesNotContain(Latest<WorldViewDto>(connection, EntityIds.WorldView).Monsters, m => m.HpMax > 0);
        }

        [Fact]
        public async Task Everyone_hits_the_same_body_and_damage_scores_for_the_event()
        {
            var (a, aConnection) = await Connect("boss-a");
            var (b, bConnection) = await Connect("boss-b");
            var (x, y) = _world.Map.PositionOf(a.PublicId)!.Value;
            var eventId = Publish(BossEvent("chasm_maw", 10, x + 1, y, EventTemplates.Points));
            Tick();
            var boss = Boss(a);

            Send(a, new MarchAttackCommand { CommandId = "ba1", MonsterId = boss.Id }, CommandType.MarchAttack);
            Assert.Equal(CommandResultCode.Accepted, LastAck(aConnection).Result);
            Send(b, new MarchAttackCommand { CommandId = "bb1", MonsterId = boss.Id }, CommandType.MarchAttack);
            Assert.Equal(CommandResultCode.Accepted, LastAck(bConnection).Result); // nobody takes a boss

            var aHit = a.MarchesForTests.Single().Battle!;
            var bHit = b.MarchesForTests.Single().Battle!;
            Assert.False(aHit.Won);
            var aDamage = aHit.EnemyLost.Values.Sum();
            var bDamage = bHit.EnemyLost.Values.Sum();
            Assert.True(aDamage > 0 && bDamage > 0);
            Assert.Equal(boss.HpMax - aDamage - bDamage, boss.HpLeft);
            Assert.Equal(new[] { aDamage, bDamage }.OrderByDescending(d => d), _world.Map.BossDamage(boss.Id).Select(p => p.Value));
            // A hit that does not bring it down still pays for the damage.
            Assert.Contains(aHit.Rewards.Keys, k => Capsules.TryParse(k, out _, out _));
            // The second team fought what the first left.
            Assert.Equal(boss.HpMax - aDamage, bHit.EnemyStart.Values.Sum());

            var aMarch = a.MarchesForTests.Single().Id;
            var arrives = new[] { a.MarchesForTests.Single().ArrivesAt, b.MarchesForTests.Single().ArrivesAt }.Max();
            _clock.UtcNow = arrives.AddSeconds(1);
            _world.Tick();
            Tick();
            var report = Latest<BattleReportDto>(aConnection, EntityIds.BattleReport + aMarch);
            Assert.Equal("chasm_maw", report.MonsterType);
            Assert.DoesNotContain(10, a.StateForTests.MonsterWins); // a boss is no first win
            var points = Latest<EventListDto>(aConnection, EntityIds.Events).Events.Single(e => e.Id == eventId).MyPoints;
            Assert.Equal(aDamage, points);
        }

        [Fact]
        public async Task A_recalled_hit_gives_its_damage_back()
        {
            var (session, connection) = await Connect("boss-recall");
            var (x, y) = _world.Map.PositionOf(session.PublicId)!.Value;
            Publish(BossEvent("depth_kraken", 15, x, y + 1));
            Tick();
            var boss = Boss(session);
            Send(session, new MarchAttackCommand { CommandId = "br1", MonsterId = boss.Id }, CommandType.MarchAttack);
            Assert.True(boss.HpLeft < boss.HpMax);

            Send(session, new RecallMarchCommand { CommandId = "br2", MarchId = session.MarchesForTests.Single().Id }, CommandType.RecallMarch);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(boss.HpMax, boss.HpLeft);
            Assert.Empty(_world.Map.BossDamage(boss.Id));
        }

        [Fact]
        public async Task The_final_blow_brings_the_boss_down_for_the_rest_of_the_event()
        {
            var (session, connection) = await Connect("boss-kill");
            var (other, otherConnection) = await Connect("boss-late");
            var (x, y) = _world.Map.PositionOf(session.PublicId)!.Value;
            Publish(BossEvent("chasm_maw", 10, x + 1, y + 1));
            Tick();
            var boss = Boss(session);
            boss.HpLeft = 3; // everyone else already wore it down

            Send(session, new MarchAttackCommand { CommandId = "bk1", MonsterId = boss.Id }, CommandType.MarchAttack);
            var battle = session.MarchesForTests.Single().Battle!;
            Assert.True(battle.Won);
            Assert.Equal(3, battle.EnemyStart.Values.Sum());
            Assert.Equal(1, battle.Rewards["chest_silver"]);
            Assert.Equal(0, boss.HpLeft);

            // Too late: the body is spent.
            Send(other, new MarchAttackCommand { CommandId = "bk2", MonsterId = boss.Id }, CommandType.MarchAttack);
            Assert.Equal(nameof(AttackRejectReason.MonsterBusy), LastAck(otherConnection).Reason);

            _clock.UtcNow = session.MarchesForTests.Single().ArrivesAt.AddSeconds(1);
            _world.Tick();
            Tick();
            View(session);
            Assert.DoesNotContain(Latest<WorldViewDto>(connection, EntityIds.WorldView).Monsters, m => m.Id == boss.Id);
            _clock.Advance(TimeSpan.FromMinutes(30)); // past an ordinary swarm's respawn
            Tick();
            View(session);
            Assert.DoesNotContain(Latest<WorldViewDto>(connection, EntityIds.WorldView).Monsters, m => m.Id == boss.Id);
        }
    }
}
