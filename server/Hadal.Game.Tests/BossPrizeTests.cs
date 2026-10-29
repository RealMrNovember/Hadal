using Hadal.Game.Events;
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
    /// Event bosses pay every contributor (EventBossRewards): damage ranked inside each alliance, ranks 1–3 bigger,
    /// the rest a participation gift, mailed once; the damage board; and the attack preview (MonsterBattle).
    /// </summary>
    public sealed class BossPrizeTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public BossPrizeTests()
        {
            _world = new GameWorld(new CatalogRuntime(Catalog), new InMemoryPlayerRepository(), new InMemorySocialStore(), new InMemoryWorldStore(),
                _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance);
        }

        private static string Id() => Guid.NewGuid().ToString("N");

        private async Task<(PlayerSession S, RecordingConnection C)> Connect(string identity, int harpoons = 20000)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            session.ClearCrewForTests();
            session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId).Level = 10;
            foreach (var type in Enum.GetValues<ResourceType>().Where(t => t != ResourceType.Unknown))
                session.StateForTests.Wallet[type] = 10_000_000;
            session.StateForTests.Troops.Clear();
            session.StateForTests.Troops[TroopRules.Key("harpoon", 4)] = harpoons;
            View(session);
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : class, ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private void View(PlayerSession session) =>
            Send(session, new ViewWorldCommand { CommandId = "v" + Id(), X = -1, Y = -1 }, CommandType.ViewWorld);

        private static T Latest<T>(RecordingConnection connection, string entityId) =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId && c.Kind != EntityChangeKind.Delete).Payload);

        private void Tick()
        {
            _clock.Advance(TimeSpan.FromSeconds(6));
            _world.Tick();
        }

        private string Join(List<(PlayerSession S, RecordingConnection C)> members, string name)
        {
            Send(members[0].S, new CreateAllianceCommand { CommandId = Id(), Name = "House " + name, Tag = name[..4].ToUpperInvariant() }, CommandType.CreateAlliance);
            var allianceId = Latest<AllianceDto>(members[0].C, EntityIds.MyAlliance).Id;
            foreach (var m in members.Skip(1))
                Send(m.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
            Assert.All(members, m => Assert.Equal(allianceId, _world.Social.AllianceIdOf(m.S.PublicId)));
            return allianceId;
        }

        private static EventRewardConfig Item(string id, long n) => new() { Type = "item", Id = id, Amount = n };

        private string Publish(int x, int y, string kind = "chasm_maw", int level = 10, bool custom = false)
        {
            var def = new EventDefinition
            {
                Template = EventTemplates.Alliance,
                Title = new() { ["tr"] = "Uçurum Ağzı Uyanıyor", ["en"] = "The Chasm Maw Wakes" },
                Banner = "abyss",
                StartsAt = _clock.UtcNow,
                EndsAt = _clock.UtcNow.AddDays(1),
                Actions = { new EventActionConfig { Action = EventActions.BossDamage, Points = 1, Per = 1 } },
                Bosses = { new EventBossConfig { Kind = kind, Level = level, X = x, Y = y } }
            };
            if (custom)
            {
                def.BossRanks.Add(new EventTierConfig { From = 1, To = 1, Rewards = { Item("chest_gold", 3) } });
                def.BossRanks.Add(new EventTierConfig { From = 2, To = 3, Rewards = { Item("chest_silver", 2) } });
                def.BossParticipation.Add(Item("chest_bronze", 2));
            }

            var (error, saved) = _world.Events.Save(def, "test", _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.Equal(string.Empty, _world.Events.Publish(saved!.Id, _clock.UtcNow));
            return saved.Id;
        }

        private WorldMonster Boss(PlayerSession viewer)
        {
            View(viewer);
            return _world.Map.MonstersForTests().Single(m => m.IsBoss);
        }

        private Dictionary<string, int> MailedItems(PlayerSession session) =>
            _world.Mail.MailsOf(session.PublicId, _clock.UtcNow).Where(m => m.HasAttachments)
                .SelectMany(m => m.Attachments.Items).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Sum(p => p.Value));

        [Fact]
        public void Prizes_rank_inside_each_alliance_and_the_rest_take_part()
        {
            var damage = new Dictionary<string, long> { ["a1"] = 500, ["a2"] = 300, ["a3"] = 200, ["a4"] = 100, ["b1"] = 50, ["solo"] = 900, ["idle"] = 0 };
            var alliances = new Dictionary<string, string> { ["a1"] = "A", ["a2"] = "A", ["a3"] = "A", ["a4"] = "A", ["b1"] = "B", ["solo"] = "", ["idle"] = "A" };
            var outcome = new BossOutcome("ev", 0, "chasm_maw", 20, true, "a1", damage, alliances);
            var ranks = new List<EventTierConfig>
            {
                new() { From = 1, To = 1, Rewards = { Item("chest_gold", 3) } },
                new() { From = 2, To = 3, Rewards = { Item("chest_silver", 2) } }
            };
            var prizes = EventBossRewards.Plan(outcome, ranks, new List<EventRewardConfig> { Item("chest_bronze", 1) }).ToDictionary(p => p.PlayerId);

            Assert.Equal(6, prizes.Count); // "idle" did nothing
            Assert.Equal((1, "chest_gold"), (prizes["a1"].Rank, prizes["a1"].Rewards.Single().Id));
            Assert.Equal((2, "chest_silver"), (prizes["a2"].Rank, prizes["a2"].Rewards.Single().Id));
            Assert.Equal((3, "chest_silver"), (prizes["a3"].Rank, prizes["a3"].Rewards.Single().Id));
            Assert.Equal((4, "chest_bronze"), (prizes["a4"].Rank, prizes["a4"].Rewards.Single().Id));
            Assert.Equal((1, "chest_gold"), (prizes["b1"].Rank, prizes["b1"].Rewards.Single().Id)); // first in its own alliance
            Assert.Equal((0, "chest_bronze"), (prizes["solo"].Rank, prizes["solo"].Rewards.Single().Id)); // no alliance: taking part

            // Nothing configured: the built-in tiers, gold for the top three's best.
            var builtIn = EventBossRewards.Plan(outcome, new List<EventTierConfig>(), new List<EventRewardConfig>()).ToDictionary(p => p.PlayerId);
            Assert.Contains(builtIn["a1"].Rewards, r => r.Id == "chest_gold");
            Assert.Contains(builtIn["a2"].Rewards, r => r.Id == "chest_gold");
            Assert.Equal("chest_bronze", builtIn["solo"].Rewards.Single().Id);
        }

        [Fact]
        public void Boss_prize_settings_are_validated()
        {
            EventDefinition Def(params EventTierConfig[] ranks)
            {
                var def = new EventDefinition
                {
                    Template = EventTemplates.Alliance, Title = new() { ["tr"] = "Boss" }, Banner = "abyss",
                    StartsAt = _clock.UtcNow, EndsAt = _clock.UtcNow.AddDays(1),
                    Actions = { new EventActionConfig { Action = EventActions.BossDamage, Points = 1, Per = 1 } }
                };
                def.BossRanks.AddRange(ranks);
                return def;
            }

            Assert.Equal(string.Empty, EventService.Validate(Def(new EventTierConfig { From = 1, To = 3, Rewards = { Item("chest_gold", 1) } }), Catalog));
            Assert.Equal("InvalidBossRewards", EventService.Validate(Def(new EventTierConfig { From = 0, To = 3, Rewards = { Item("chest_gold", 1) } }), Catalog));
            Assert.Equal("InvalidBossRewards", EventService.Validate(Def(
                new EventTierConfig { From = 1, To = 3, Rewards = { Item("chest_gold", 1) } },
                new EventTierConfig { From = 3, To = 5, Rewards = { Item("chest_silver", 1) } }), Catalog));
            Assert.Equal("InvalidBossRewards", EventService.Validate(Def(new EventTierConfig { From = 1, To = 1, Rewards = { Item("no_such_item", 1) } }), Catalog));
            Assert.Equal("InvalidBossRewards", EventService.Validate(Def(new EventTierConfig
                { From = 1, To = 1, Rewards = { new EventRewardConfig { Type = "shards", Rarity = "rare", Amount = 5 } } }), Catalog)); // not mailable
        }

        [Fact]
        public async Task Everyone_who_hit_the_boss_is_paid_once_when_the_event_ends_and_sees_the_board()
        {
            var members = new List<(PlayerSession S, RecordingConnection C)> { await Connect("bp-a0", 30000), await Connect("bp-a1", 20000), await Connect("bp-a2", 10000) };
            Join(members, "bpal");
            var (solo, _) = await Connect("bp-solo", 10000);
            var (idle, _) = await Connect("bp-idle");
            var (x, y) = _world.Map.PositionOf(members[0].S.PublicId)!.Value;
            var eventId = Publish(x + 1, y, custom: true);
            Tick();
            var boss = Boss(members[0].S);

            foreach (var who in members.Select(m => m.S).Append(solo))
                Send(who, new MarchAttackCommand { CommandId = "h" + Id(), MonsterId = boss.Id }, CommandType.MarchAttack);
            var damage = members.Select(m => m.S).Append(solo).ToDictionary(s => s.PublicId, s => s.MarchesForTests.Single().Battle!.EnemyLost.Values.Sum());
            Assert.All(damage.Values, d => Assert.True(d > 0));
            Assert.True(boss.HpLeft > 0, "the test boss must survive");

            // The board: my alliance's members by damage, with my rank.
            Send(members[1].S, new ViewWorldCommand { CommandId = "v" + Id(), X = boss.X, Y = boss.Y }, CommandType.ViewWorld);
            var shown = Latest<WorldViewDto>(members[1].C, EntityIds.WorldView).Monsters.Single(m => m.Id == boss.Id);
            var expected = members.Select(m => m.S.PublicId).OrderByDescending(id => damage[id]).ThenBy(id => id, StringComparer.Ordinal).ToList();
            Assert.Equal(expected, shown.Board.Select(e => e.PlayerId));
            Assert.Equal(new[] { 1, 2, 3 }, shown.Board.Select(e => e.Rank));
            Assert.Equal(damage[members[1].S.PublicId], shown.MyDamage);
            Assert.Equal(expected.IndexOf(members[1].S.PublicId) + 1, shown.MyRank);
            Assert.DoesNotContain(shown.Board, e => e.PlayerId == solo.PublicId); // another alliance's (or nobody's) hits stay off it

            // The event ends: everyone who hit it is mailed their prize, once.
            _clock.UtcNow = members.Select(m => m.S).Append(solo).Max(s => s.MarchesForTests.Single().ArrivesAt).AddSeconds(1);
            _world.Tick();
            Assert.Equal(string.Empty, _world.Events.EndNow(eventId, _clock.UtcNow));
            Tick();
            Tick();
            Assert.Equal(3, MailedItems(_world.Map.SessionOf(expected[0])!).GetValueOrDefault("chest_gold"));
            Assert.Equal(2, MailedItems(_world.Map.SessionOf(expected[1])!).GetValueOrDefault("chest_silver"));
            Assert.Equal(2, MailedItems(_world.Map.SessionOf(expected[2])!).GetValueOrDefault("chest_silver"));
            Assert.Equal(2, MailedItems(solo).GetValueOrDefault("chest_bronze"));
            Assert.Empty(MailedItems(idle));
            var mail = _world.Mail.MailsOf(expected[0], _clock.UtcNow).Single(m => m.HasAttachments);
            Assert.StartsWith("loc:mail.boss.", mail.Title);
            Assert.Equal("loc:mail.boss.ranked|monster.chasm_maw|1|" + damage[expected[0]], mail.Body);

            // Paid for good: more ticks, an Overseer edit or a second outcome pay nothing more.
            var before = _world.Mail.MailsOf(expected[0], _clock.UtcNow).Count;
            Tick();
            var outcome = new BossOutcome(eventId, 0, "chasm_maw", 10, false, string.Empty, damage, damage.Keys.ToDictionary(k => k, _ => string.Empty));
            Assert.Empty(_world.Events.ClaimBossPrizes(outcome, _clock.UtcNow));
            var def = _world.Events.Get(eventId, _clock.UtcNow)!.Definition;
            Assert.Equal(new[] { 0 }, def.PaidBosses);
            def.Note = "edited";
            Assert.Equal(string.Empty, _world.Events.Save(def, "test", _clock.UtcNow).Error);
            Assert.Equal(new[] { 0 }, _world.Events.Get(eventId, _clock.UtcNow)!.Definition.PaidBosses);
            Tick();
            Assert.Equal(before, _world.Mail.MailsOf(expected[0], _clock.UtcNow).Count);
        }

        [Fact]
        public async Task A_killed_boss_pays_when_the_killer_arrives_and_does_not_come_back()
        {
            var (killer, connection) = await Connect("bp-kill");
            var (helper, _) = await Connect("bp-help", 5000);
            var (x, y) = _world.Map.PositionOf(killer.PublicId)!.Value;
            Publish(x + 1, y + 1);
            Tick();
            var boss = Boss(killer);
            Send(helper, new MarchAttackCommand { CommandId = "k1", MonsterId = boss.Id }, CommandType.MarchAttack);
            boss.HpLeft = 3;
            Send(killer, new MarchAttackCommand { CommandId = "k2", MonsterId = boss.Id }, CommandType.MarchAttack);
            Assert.True(killer.MarchesForTests.Single().Battle!.Won);
            Assert.Equal(killer.PublicId, boss.KillerId);

            _clock.UtcNow = new[] { killer, helper }.Max(s => s.MarchesForTests.Single().ArrivesAt).AddSeconds(1);
            _world.Tick();
            Tick();
            // Built-in tiers, no alliances: both take part; the killer also brought home the final blow's bronze chest.
            Assert.Equal(1, MailedItems(killer).GetValueOrDefault("chest_bronze"));
            Assert.Equal(1, MailedItems(helper).GetValueOrDefault("chest_bronze"));
            Assert.StartsWith("loc:mail.boss.killed|", _world.Mail.MailsOf(killer.PublicId, _clock.UtcNow).Single(m => m.HasAttachments).Title);

            _clock.Advance(TimeSpan.FromMinutes(30));
            Tick();
            View(killer);
            Assert.DoesNotContain(_world.Map.MonstersForTests(), m => m.IsBoss);
            Assert.Single(_world.Mail.MailsOf(killer.PublicId, _clock.UtcNow), m => m.HasAttachments);
        }

        [Fact]
        public async Task The_attack_preview_is_the_battle_the_server_fights()
        {
            var (session, _) = await Connect("bp-preview", 8000);
            var (x, y) = _world.Map.PositionOf(session.PublicId)!.Value;
            Publish(x, y + 1, "depth_kraken", 15);
            Tick();
            var boss = Boss(session);
            var kind = MonsterRules.Kind(boss.Type)!;
            var bodyBefore = boss.HpLeft;
            Send(session, new MarchAttackCommand { CommandId = "p1", MonsterId = boss.Id }, CommandType.MarchAttack);
            var march = session.MarchesForTests.Single();
            var battle = march.Battle!;

            // The setup's preview with the very team the server sent (troops aboard, heroes in order) and the body it met.
            var aboard = new Dictionary<string, long>(march.Troops);
            var heroPowers = march.HeroIds.Select(id => session.StateForTests.Heroes.Single(h => h.HeroId == id))
                .Select(h => Catalog.TryGetHero(h.HeroId, out var d) ? HeroRules.Power(d, h.Level, h.Stars) : 0).ToList();
            var outlook = MonsterBattle.Outlook(Catalog.Troops, aboard, heroPowers, kind, boss.Level, bodyBefore);
            var damage = MonsterBattle.Damage(Catalog.Troops, aboard, heroPowers, kind, boss.Level, bodyBefore);
            Assert.Equal(battle.EnemyLost.Values.Sum(), damage); // the preview is the battle the server fought
            Assert.True(damage > 0);
            Assert.False(battle.Won);
            Assert.NotEqual(CombatOutlook.Win, outlook); // a lost fight is never shown as a win

            // A small swarm against the same team: an easy win.
            Assert.Equal(CombatOutlook.Win, MonsterBattle.Outlook(Catalog.Troops, aboard, heroPowers, MonsterRules.Kind("abyss_crawler")!, 1));
            // One weak trooper against a full boss: no damage at all (the setup warns), a loss.
            var weak = new Dictionary<string, long> { [TroopRules.Key("armored", 1)] = 1 };
            Assert.Equal(0, MonsterBattle.Damage(Catalog.Troops, weak, Array.Empty<int>(), kind, 35));
            Assert.Equal(CombatOutlook.Lose, MonsterBattle.Outlook(Catalog.Troops, weak, Array.Empty<int>(), kind, 35));
        }
    }
}
