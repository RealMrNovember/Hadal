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
    /// The Abyss Siege (docs/29 §4.2, AbyssSiegeRules): officers set it; twenty waves come on the clock; kills are points;
    /// shields do not help, nothing is stolen and nobody dies; a base breached twice drops out; elite waves spare those
    /// who are offline; reinforcing scores half.
    /// </summary>
    public sealed class AbyssSiegeTests
    {
        private static readonly GameCatalog Catalog = TestSupport.BalanceCatalog;
        private readonly FakeClock _clock = new();
        private readonly GameWorld _world;

        public AbyssSiegeTests()
        {
            _world = new GameWorld(new CatalogRuntime(Catalog), new InMemoryPlayerRepository(), new InMemorySocialStore(), new InMemoryWorldStore(),
                _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance, new InMemoryEventStore());
        }

        private static string Id() => Guid.NewGuid().ToString("N");
        private static string Harpoon => TroopRules.Key("harpoon", 4);

        private async Task<(PlayerSession S, RecordingConnection C)> Connect(string identity, int harpoons)
        {
            var connection = new RecordingConnection();
            var session = await _world.ConnectAsync(identity, connection, default, identity);
            session.ClearCrewForTests();
            session.StateForTests.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId).Level = 10;
            foreach (var type in Enum.GetValues<ResourceType>().Where(t => t != ResourceType.Unknown))
                session.StateForTests.Wallet[type] = 10_000_000;
            session.StateForTests.Troops.Clear();
            if (harpoons > 0)
                session.StateForTests.Troops[Harpoon] = harpoons;
            // Five seasoned heroes: a team leads up to their command, the rest stays home.
            foreach (var id in Catalog.Heroes.Select(h => h.Id).Where(id => session.StateForTests.Heroes.All(h => h.HeroId != id)).Take(5))
                session.StateForTests.Heroes.Add(new OwnedHeroState { HeroId = id, Level = 60, Stars = 5, Slot = -1 });
            Send(session, new ViewWorldCommand { CommandId = "v" + Id(), X = -1, Y = -1 }, CommandType.ViewWorld);
            return (session, connection);
        }

        private void Send<T>(PlayerSession session, T command, CommandType type) where T : class, ICommand =>
            _world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static T Latest<T>(RecordingConnection connection, string entityId) =>
            TestSupport.Serializer.Deserialize<T>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId && c.Kind != EntityChangeKind.Delete).Payload);

        private (AbyssSiegeDto View, EventDto Event) View(PlayerSession session, RecordingConnection connection, string eventId)
        {
            Send(session, new AbyssSiegeCommand { CommandId = Id(), EventId = eventId, Action = AbyssSiegeActions.View }, CommandType.AbyssSiege);
            var e = Latest<EventListDto>(connection, EntityIds.Events).Events.Single(x => x.Id == eventId);
            return (e.AbyssSiege!, e);
        }

        private void Join(List<(PlayerSession S, RecordingConnection C)> members, string name)
        {
            Send(members[0].S, new CreateAllianceCommand { CommandId = Id(), Name = "House " + name, Tag = name[..4].ToUpperInvariant() }, CommandType.CreateAlliance);
            var allianceId = Latest<AllianceDto>(members[0].C, EntityIds.MyAlliance).Id;
            foreach (var m in members.Skip(1))
                Send(m.S, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
        }

        private string Publish(int minMembers = 2)
        {
            var def = new EventDefinition
            {
                Template = EventTemplates.AbyssSiege,
                Title = new() { ["tr"] = "Derinlik Kuşatması", ["en"] = "Abyss Siege" },
                Banner = "abyss",
                StartsAt = _clock.UtcNow,
                EndsAt = _clock.UtcNow.AddHours(24),
                ClaimDays = 1,
                AbyssSiege = new AbyssSiegeConfig { MinMembers = minMembers, MinReactor = 10 }
            };
            var (error, saved) = _world.Events.Save(def, "test", _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.Equal(string.Empty, _world.Events.Publish(saved!.Id, _clock.UtcNow));
            return saved.Id;
        }

        private void Schedule(PlayerSession session, string eventId, DateTimeOffset at, int difficulty) =>
            Send(session, new AbyssSiegeCommand
            {
                CommandId = Id(), EventId = eventId, Action = AbyssSiegeActions.Schedule, StartsAtMs = at.ToUnixTimeMilliseconds(), Difficulty = difficulty
            }, CommandType.AbyssSiege);

        /// <summary>Lands on wave <paramref name="wave"/>'s moment and lets the world fight it.</summary>
        private void RunTo(DateTimeOffset start, int wave)
        {
            _clock.UtcNow = start.AddSeconds((wave - 1) * AbyssSiegeRules.WaveSeconds);
            _world.Tick();
            _world.Tick();
        }

        [Fact]
        public void Rules_mark_the_special_waves_and_grow_the_creatures()
        {
            Assert.Equal(new[] { 7, 14, 17 }, Enumerable.Range(1, AbyssSiegeRules.Waves).Where(AbyssSiegeRules.IsEliteWave));
            Assert.Equal(new[] { 10, 20 }, Enumerable.Range(1, AbyssSiegeRules.Waves).Where(AbyssSiegeRules.IsHubWave));
            Assert.True(AbyssSiegeRules.Level(1, 1) < AbyssSiegeRules.Level(1, 20));
            Assert.True(AbyssSiegeRules.Level(1, 20) < AbyssSiegeRules.Level(10, 20));
            Assert.True(AbyssSiegeRules.Level(10, 20) <= 35);
            Assert.True(AbyssSiegeRules.Army(3, 7).Troops.Values.Sum() > AbyssSiegeRules.Army(3, 6).Troops.Values.Sum());
            var now = 1_000_000_000L;
            var end = now + 3_600_000L;
            Assert.False(AbyssSiegeRules.CanStart(now + 60_000, now, end)); // too soon
            Assert.True(AbyssSiegeRules.CanStart(now + 600_000, now, end));
            Assert.False(AbyssSiegeRules.CanStart(now + 600_000, now, now + 600_000 + AbyssSiegeRules.LengthMs)); // the last wave would miss the event
        }

        [Fact]
        public async Task Officers_set_the_siege_and_twenty_waves_score_kills_without_a_soldier_dying_or_a_resource_lost()
        {
            var members = new List<(PlayerSession S, RecordingConnection C)> { await Connect("as-a", 60000), await Connect("as-b", 40000) };
            Join(members, "siege");
            var (leader, link) = members[0];
            var eventId = Publish();

            var start = _clock.UtcNow.AddMinutes(20);
            Schedule(members[1].S, eventId, start, 1);
            Assert.Equal("NoPermission", LastAck(members[1].C).Reason);
            Schedule(leader, eventId, _clock.UtcNow.AddMinutes(2), 1);
            Assert.Equal("BadTime", LastAck(link).Reason);
            Schedule(leader, eventId, start, 11);
            Assert.Equal("UnknownDifficulty", LastAck(link).Reason);
            Schedule(leader, eventId, start, 1);
            Assert.Equal(CommandResultCode.Accepted, LastAck(link).Result);
            Assert.Equal(start.ToUnixTimeMilliseconds(), View(members[1].S, members[1].C, eventId).View.NextWaveAtMs);

            // A shield is no help: the wave comes anyway. Nothing happens before the start.
            leader.GrantShield(_clock.UtcNow.AddDays(1), _clock.UtcNow);
            var wallet = leader.StateForTests.Wallet[ResourceType.Titanium];
            _world.Tick();
            Assert.Equal(0, View(leader, link, eventId).View.WavesDone);

            RunTo(start, 1);
            var (view, listed) = View(leader, link, eventId);
            Assert.Equal(1, view.WavesDone);
            Assert.True(listed.MyPoints > 0, "kills at the own base score");

            // The siege is fixed once it has started.
            Schedule(leader, eventId, _clock.UtcNow.AddMinutes(30), 2);
            Assert.Equal("SiegeStarted", LastAck(link).Reason);

            for (var wave = 2; wave <= AbyssSiegeRules.Waves; wave++)
                RunTo(start, wave);
            view = View(leader, link, eventId).View;
            Assert.Equal(AbyssSiegeRules.Waves, view.WavesDone);
            Assert.Equal(0, view.NextWaveAtMs);
            Assert.Equal(0, view.MyBreaches);

            // No hub: the hub waves pass without a fight. The fallen are wounded, never dead, and nothing was taken.
            Assert.Equal(0, view.HubHeld + view.HubLost);
            var left = leader.StateForTests.Troops.GetValueOrDefault(Harpoon) + leader.StateForTests.Wounded.GetValueOrDefault(Harpoon);
            Assert.Equal(60000, left);
            Assert.True(leader.StateForTests.Wallet[ResourceType.Titanium] >= wallet);

            // More ticks fight nothing more.
            var points = View(leader, link, eventId).Event.MyPoints;
            _clock.Advance(TimeSpan.FromMinutes(10));
            _world.Tick();
            Assert.Equal(points, View(leader, link, eventId).Event.MyPoints);
        }

        [Fact]
        public async Task A_base_breached_twice_drops_out_and_takes_no_more_waves()
        {
            var members = new List<(PlayerSession S, RecordingConnection C)> { await Connect("as-c", 500000), await Connect("as-d", 0) };
            Join(members, "breach");
            var (leader, link) = members[0];
            var (weak, weakLink) = members[1];
            var eventId = Publish();
            var start = _clock.UtcNow.AddMinutes(20);
            Schedule(leader, eventId, start, 1);

            RunTo(start, 1);
            Assert.Equal(1, View(weak, weakLink, eventId).View.MyBreaches);
            RunTo(start, 2);
            var seen = View(weak, weakLink, eventId).View;
            Assert.Equal(AbyssSiegeRules.BreachLimit, seen.MyBreaches);
            Assert.Equal(1, seen.DroppedOut);

            RunTo(start, 3);
            Assert.Equal(AbyssSiegeRules.BreachLimit, View(weak, weakLink, eventId).View.MyBreaches);
            Assert.Equal(3, View(leader, link, eventId).View.WavesDone); // the rest fight on
        }

        [Fact]
        public async Task A_team_reinforcing_a_mate_scores_half()
        {
            var members = new List<(PlayerSession S, RecordingConnection C)> { await Connect("as-e", 30000), await Connect("as-f", 30000) };
            Join(members, "guard");
            var (leader, link) = members[0];
            var (mate, mateLink) = members[1];
            var eventId = Publish();

            Send(mate, new MarchPvpCommand { CommandId = "r", Mode = PvpRules.Reinforce, TargetId = leader.PublicId }, CommandType.MarchPvp);
            Assert.Equal(CommandResultCode.Accepted, LastAck(mateLink).Result);
            var team = mate.MarchesForTests.Single();
            mate.StateForTests.Troops.Clear(); // the mate's score comes only from the post
            var start = team.ArrivesAt.AddMinutes(20);
            Schedule(leader, eventId, start, 5);
            RunTo(start, 1);

            var home = leader.StateForTests.Troops.Values.Sum();
            var mine = View(leader, link, eventId).Event.MyPoints;
            var guard = View(mate, mateLink, eventId).Event.MyPoints;
            Assert.True(mine > 0 && guard > 0, $"{mine} / {guard} aboard {team.Troops.Values.Sum()}");
            var perTroop = (double)mine / home;
            Assert.InRange(guard, perTroop * team.Troops.Values.Sum() * 0.5 - 2, perTroop * team.Troops.Values.Sum() * 0.5 + 2);
        }

        [Fact]
        public async Task Elite_waves_spare_the_offline_and_the_others_do_not()
        {
            var members = new List<(PlayerSession S, RecordingConnection C)> { await Connect("as-g", 30000), await Connect("as-h", 30000) };
            Join(members, "elite");
            var (leader, _) = members[0];
            var (gone, goneLink) = members[1];
            var ids = members.Select(m => m.S.PublicId).ToList();
            var alliance = _world.Social.AllianceIdOf(leader.PublicId);
            await _world.DisconnectAsync(gone, goneLink, default);

            var elite = _world.Map.FightSiegeWave(new SiegeWaveOrder("ev", alliance, 7, 1, 10, ids, "w1"), _clock.UtcNow);
            Assert.True(elite!.Points.Keys.SequenceEqual(new[] { leader.PublicId }), $"elite keys [{string.Join(",", elite.Points)}], leader {leader.PublicId}, gone {gone.PublicId}");

            // An ordinary wave needs the offline base too: it waits while the session loads, then fights it.
            var order = new SiegeWaveOrder("ev", alliance, 6, 1, 1, ids, "w1");
            var plain = _world.Map.FightSiegeWave(order, _clock.UtcNow);
            for (var i = 0; plain == null && i < 50; i++)
            {
                await Task.Delay(20);
                plain = _world.Map.FightSiegeWave(order, _clock.UtcNow);
            }

            Assert.True(plain!.Points.ContainsKey(leader.PublicId));
            Assert.True(plain.Points.ContainsKey(gone.PublicId) || plain.Breached.Contains(gone.PublicId), "the offline base took the wave");
        }

        [Fact]
        public async Task The_hub_holds_with_guards_and_falls_without_them()
        {
            var members = new List<(PlayerSession S, RecordingConnection C)> { await Connect("as-i", 30000), await Connect("as-j", 30000), await Connect("as-k", 30000) };
            Join(members, "hubs");
            var (leader, link) = members[0];
            var (mate, mateLink) = members[1];
            var ids = members.Select(m => m.S.PublicId).ToList();
            var alliance = _world.Social.AllianceIdOf(leader.PublicId);
            var (lx, ly) = _world.Map.PositionOf(leader.PublicId)!.Value;
            for (var r = 1; r < 40 && _world.Map.StructuresForTests().Count == 0; r++)
                for (var dx = -r; dx <= r && _world.Map.StructuresForTests().Count == 0; dx++)
                    for (var dy = -r; dy <= r && _world.Map.StructuresForTests().Count == 0; dy++)
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r)
                            Send(leader, new TerritoryCommand { CommandId = Id(), Action = AllianceLandRules.PlaceHub, X = lx + dx, Y = ly + dy }, CommandType.Territory);
            var hub = _world.Map.StructuresForTests().Single();

            var fell = _world.Map.FightSiegeWave(new SiegeWaveOrder("ev", alliance, 10, 1, 10, ids, "w1"), _clock.UtcNow);
            Assert.False(fell!.HubHeld);

            Send(mate, new MarchPvpCommand { CommandId = "h", Mode = PvpRules.Reinforce, TargetKind = PvpRules.TargetStructure, TargetId = hub.Id }, CommandType.MarchPvp);
            Assert.Equal(CommandResultCode.Accepted, LastAck(mateLink).Result);
            var team = mate.MarchesForTests.Single();
            _clock.UtcNow = team.ArrivesAt.AddSeconds(5);
            _world.Tick();
            var held = _world.Map.FightSiegeWave(new SiegeWaveOrder("ev", alliance, 10, 1, 10, ids, "w1"), _clock.UtcNow);
            Assert.True(held!.HubHeld);
            Assert.True(held.Points[mate.PublicId] > 0);
            Assert.Empty(held.Breached); // the hub wave leaves the bases alone
        }

        [Fact]
        public void Siege_settings_are_validated()
        {
            EventDefinition Def(AbyssSiegeConfig config, double hours = 24) => new()
            {
                Template = EventTemplates.AbyssSiege, Title = new() { ["tr"] = "Kuşatma" }, Banner = "abyss",
                StartsAt = _clock.UtcNow, EndsAt = _clock.UtcNow.AddHours(hours), AbyssSiege = config
            };

            Assert.Equal(string.Empty, EventService.Validate(Def(new AbyssSiegeConfig()), Catalog));
            Assert.Equal("InvalidAbyssSiege", EventService.Validate(Def(new AbyssSiegeConfig { MinMembers = 0 }), Catalog));
            Assert.Equal("InvalidAbyssSiege", EventService.Validate(Def(new AbyssSiegeConfig { MinReactor = 99 }), Catalog));
            Assert.Equal("InvalidAbyssSiege", EventService.Validate(Def(new AbyssSiegeConfig(), hours: 0.5), Catalog));
        }
    }
}
