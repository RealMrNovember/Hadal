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
    /// The Lure Trap (docs/29 §4.1, LureTrapRules): only the leader or a war-duty officer schedules; a Gulper rises at the
    /// alliance's rally point at its time, for the alliance alone; it never fights back; damage is the members' points,
    /// monster kills charge the bait, and the tier mail pays every hitter at the end (the top hitter of a trap more).
    /// </summary>
    public sealed class LureTrapTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public LureTrapTests()
        {
            _world = new GameWorld(new CatalogRuntime(Catalog), new InMemoryPlayerRepository(), new InMemorySocialStore(), new InMemoryWorldStore(),
                _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance, new InMemoryEventStore());
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

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static T Latest<T>(RecordingConnection connection, string entityId) =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId && c.Kind != EntityChangeKind.Delete).Payload);

        private LureTrapDto Trap(PlayerSession session, RecordingConnection connection, string eventId)
        {
            Send(session, new LureTrapCommand { CommandId = Id(), EventId = eventId, Action = LureTrapActions.View }, CommandType.LureTrap);
            return Latest<EventListDto>(connection, EntityIds.Events).Events.Single(e => e.Id == eventId).LureTrap!;
        }

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

        private string Publish(int minMembers = 3, params EventTierConfig[] tiers)
        {
            var def = new EventDefinition
            {
                Template = EventTemplates.LureTrap,
                Title = new() { ["tr"] = "Yem Kapanı", ["en"] = "Lure Trap" },
                Banner = "abyss",
                StartsAt = _clock.UtcNow,
                EndsAt = _clock.UtcNow.AddHours(48),
                ClaimDays = 1,
                LureTrap = new LureTrapConfig { MinMembers = minMembers, Tiers = tiers.ToList() }
            };
            var (error, saved) = _world.Events.Save(def, "test", _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.Equal(string.Empty, _world.Events.Publish(saved!.Id, _clock.UtcNow));
            return saved.Id;
        }

        private void Schedule(PlayerSession session, string eventId, int trap, DateTimeOffset at, int difficulty = 1) =>
            Send(session, new LureTrapCommand
            {
                CommandId = Id(), EventId = eventId, Action = LureTrapActions.Schedule, Trap = trap, StartsAtMs = at.ToUnixTimeMilliseconds(), Difficulty = difficulty
            }, CommandType.LureTrap);

        private Dictionary<string, int> MailedItems(PlayerSession session) =>
            _world.Mail.MailsOf(session.PublicId, _clock.UtcNow).Where(m => m.HasAttachments)
                .SelectMany(m => m.Attachments.Items).GroupBy(p => p.Key).ToDictionary(g => g.Key, g => g.Sum(p => p.Value));

        [Fact]
        public void Rules_level_the_trap_by_bait_and_count_tiers()
        {
            Assert.Equal(0, LureTrapRules.Level(49));
            Assert.Equal(1, LureTrapRules.Level(50));
            Assert.Equal(LureTrapRules.MaxLevel, LureTrapRules.Level(10_000));
            Assert.Equal(100, LureTrapRules.BaitToNext(50));
            Assert.Equal(0, LureTrapRules.BaitToNext(10_000));
            Assert.Equal(0.25, LureTrapRules.AttackBonus(5), 6);
            Assert.Equal(5, LureTrapRules.GulperLevel(1));
            Assert.Equal(32, LureTrapRules.GulperLevel(10));
            Assert.Equal(0, LureTrapRules.Tier(99, new long[] { 100, 1000 }));
            Assert.Equal(2, LureTrapRules.Tier(1000, new long[] { 100, 1000 }));
            var now = 1_000_000_000L;
            Assert.False(LureTrapRules.CanStart(now + 60_000, now, now + 86_400_000, 30)); // too soon
            Assert.True(LureTrapRules.CanStart(now + 600_000, now, now + 86_400_000, 30));
            Assert.False(LureTrapRules.CanStart(now + 600_000, now, now + 600_000 + 29 * 60_000, 30)); // the fight would outlast the event
        }

        [Fact]
        public async Task Officers_schedule_the_Gulper_rises_for_the_alliance_and_never_fights_back_and_hitters_are_paid_by_tier()
        {
            var members = new List<(PlayerSession S, RecordingConnection C)> { await Connect("lt-a0", 30000), await Connect("lt-a1", 20000), await Connect("lt-a2", 10000) };
            Join(members, "lure");
            var (outsider, outsiderLink) = await Connect("lt-out", 10000);
            var eventId = Publish(3, new EventTierConfig { Points = 1, Rewards = { Item("chest_silver", 1) } },
                new EventTierConfig { Points = 10_000_000_000, Rewards = { Item("chest_gold", 1) } });
            var (leader, leaderLink) = members[0];

            // Only the leader (or a war-duty officer) schedules; times must leave room and traps may not overlap.
            var start = _clock.UtcNow.AddMinutes(20);
            Schedule(members[1].S, eventId, 0, start);
            Assert.Equal("NoPermission", LastAck(members[1].C).Reason);
            Schedule(leader, eventId, 0, _clock.UtcNow.AddMinutes(2));
            Assert.Equal("BadTime", LastAck(leaderLink).Reason);
            Schedule(leader, eventId, 0, start, difficulty: 11);
            Assert.Equal("UnknownDifficulty", LastAck(leaderLink).Reason);
            Schedule(leader, eventId, 0, start, difficulty: 5);
            Assert.Equal(CommandResultCode.Accepted, LastAck(leaderLink).Result);
            Schedule(leader, eventId, 1, start.AddMinutes(10));
            Assert.Equal("TrapOverlap", LastAck(leaderLink).Reason);
            var view = Trap(members[1].S, members[1].C, eventId);
            Assert.False(view.CanSchedule);
            Assert.Equal(start.ToUnixTimeMilliseconds(), view.Traps[0].StartsAtMs);
            Assert.Equal(string.Empty, view.Traps[0].MonsterId);

            // Bait: members' monster kills while the event is live.
            _world.Events.Record(members[2].S, new List<(string, long)> { (EventActions.MonsterKill, 60) }, _clock.UtcNow);
            view = Trap(leader, leaderLink, eventId);
            Assert.Equal(60, view.Bait);
            Assert.Equal(1, view.Level);

            // Nothing on the map before the start; at the start the Gulper is up at the leader's base (no hub).
            Tick();
            Assert.DoesNotContain(_world.Map.MonstersForTests(), m => m.IsBoss);
            _clock.UtcNow = start;
            Tick();
            var gulper = _world.Map.MonstersForTests().Single(m => m.IsBoss);
            Assert.Equal(LureTrapRules.GulperKind, gulper.Type);
            var (lx, ly) = _world.Map.PositionOf(leader.PublicId)!.Value;
            Assert.True(Math.Max(Math.Abs(gulper.X - lx), Math.Abs(gulper.Y - ly)) <= 6);
            Assert.Equal(gulper.Id, Trap(leader, leaderLink, eventId).Traps[0].MonsterId);

            // Only the alliance sees and hits it.
            Send(outsider, new ViewWorldCommand { CommandId = "v" + Id(), X = gulper.X, Y = gulper.Y }, CommandType.ViewWorld);
            Assert.DoesNotContain(Latest<WorldViewDto>(outsiderLink, EntityIds.WorldView).Monsters, m => m.Id == gulper.Id);
            Send(outsider, new MarchAttackCommand { CommandId = "h" + Id(), MonsterId = gulper.Id }, CommandType.MarchAttack);
            Assert.Equal(AttackRejectReason.NotYourTrap.ToString(), LastAck(outsiderLink).Reason);

            foreach (var (s, _) in members)
                Send(s, new MarchAttackCommand { CommandId = "h" + Id(), MonsterId = gulper.Id }, CommandType.MarchAttack);
            var damage = members.ToDictionary(m => m.S.PublicId, m => m.S.MarchesForTests.Single().Battle!.EnemyLost.Values.Sum());
            var b0 = members[0].S.MarchesForTests.Single().Battle!;
            Assert.True(damage.Values.All(d => d > 0), $"start {string.Join(',', b0.EnemyStart)} lost {string.Join(',', b0.EnemyLost)} rounds {b0.Rounds} won {b0.Won} hp {gulper.HpLeft}/{gulper.HpMax} lvl {gulper.Level}");
            Assert.All(members, m => Assert.Empty(m.S.MarchesForTests.Single().Battle!.Lost)); // it never fights back
            Assert.True(gulper.HpLeft > 0, "the test Gulper must survive");

            // The hits are the members' points and the trap's damage.
            Tick();
            view = Trap(leader, leaderLink, eventId);
            Assert.Equal(damage.Values.Sum(), view.Traps[0].Damage);
            Assert.Equal(damage[leader.PublicId], view.Traps[0].MyDamage);
            Assert.Equal(damage.Values.Sum(), view.AllianceDamage);
            Assert.Equal(1, view.Tier);
            var top = damage.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).First().Key;
            Assert.Equal(top, view.Traps[0].Board[0].PlayerId);
            var listed = Latest<EventListDto>(leaderLink, EntityIds.Events).Events.Single(e => e.Id == eventId);
            Assert.Equal(damage[leader.PublicId], listed.MyPoints);

            // Its half hour ends: the trap is done and fixed.
            _clock.UtcNow = start.AddMinutes(31);
            Tick();
            Tick();
            Assert.DoesNotContain(_world.Map.MonstersForTests(), m => m.IsBoss);
            view = Trap(leader, leaderLink, eventId);
            Assert.True(view.Traps[0].Done);
            Schedule(leader, eventId, 0, _clock.UtcNow.AddMinutes(30));
            Assert.Equal("TrapStarted", LastAck(leaderLink).Reason);

            // The event ends: every hitter gets tier 1, the top hitter twice; the outsider nothing.
            Assert.Equal(string.Empty, _world.Events.EndNow(eventId, _clock.UtcNow));
            Tick();
            Tick();
            foreach (var (s, _) in members)
                Assert.Equal(s.PublicId == top ? 2 : 1, MailedItems(s).GetValueOrDefault("chest_silver"));
            Assert.Empty(MailedItems(outsider));
        }

        [Fact]
        public async Task A_small_alliance_cannot_schedule_and_a_scheduled_trap_can_be_called_off()
        {
            var members = new List<(PlayerSession S, RecordingConnection C)> { await Connect("lt-s0"), await Connect("lt-s1") };
            Join(members, "smal");
            var eventId = Publish(3);
            var (leader, link) = members[0];
            Schedule(leader, eventId, 0, _clock.UtcNow.AddMinutes(20));
            Assert.Equal("TooFewMembers", LastAck(link).Reason);

            var bigger = Publish(2);
            Schedule(leader, bigger, 1, _clock.UtcNow.AddMinutes(20), difficulty: 4);
            Assert.Equal(CommandResultCode.Accepted, LastAck(link).Result);
            Send(leader, new LureTrapCommand { CommandId = Id(), EventId = bigger, Action = LureTrapActions.Cancel, Trap = 1 }, CommandType.LureTrap);
            Assert.Equal(CommandResultCode.Accepted, LastAck(link).Result);
            Assert.Equal(0, Trap(leader, link, bigger).Traps[1].StartsAtMs);
            _clock.UtcNow = _clock.UtcNow.AddMinutes(21);
            Tick();
            Assert.DoesNotContain(_world.Map.MonstersForTests(), m => m.IsBoss);
        }

        [Fact]
        public void Lure_settings_are_validated()
        {
            EventDefinition Def(LureTrapConfig config) => new()
            {
                Template = EventTemplates.LureTrap, Title = new() { ["tr"] = "Kapan" }, Banner = "abyss",
                StartsAt = _clock.UtcNow, EndsAt = _clock.UtcNow.AddHours(48), LureTrap = config
            };

            Assert.Equal(string.Empty, EventService.Validate(Def(new LureTrapConfig()), Catalog));
            Assert.Equal("InvalidLureTrap", EventService.Validate(Def(new LureTrapConfig { MinMembers = 0 }), Catalog));
            Assert.Equal("InvalidLureTrap", EventService.Validate(Def(new LureTrapConfig { FightMinutes = 2 }), Catalog));
            Assert.Equal("InvalidLureTrap", EventService.Validate(Def(new LureTrapConfig
            {
                Tiers = { new EventTierConfig { Points = 5, Rewards = { Item("chest_silver", 1) } }, new EventTierConfig { Points = 5 } }
            }), Catalog));
        }
    }
}
