using Hadal.Game.Events;
using Hadal.Game.Persistence;
using HADAL.Shared.Commands;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>Mobilization (docs/29 §4.5) and the Alliance Duel (§4.3): rules, task lifecycle, pairing, days and results.</summary>
    public sealed class AllianceEventTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryEventStore _store = new();
        private readonly GameWorld _world;

        public AllianceEventTests()
        {
            _world = new GameWorld(new CatalogRuntime(TestSupport.Catalog), new InMemoryPlayerRepository(), new Hadal.Game.Social.InMemorySocialStore(),
                new Hadal.Game.World.InMemoryWorldStore(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance, _store);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(string identity)
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
                .SelectMany(d => d.Changes).Last(c => c.EntityId == entityId).Payload);

        private static EventDto Event(RecordingConnection connection, string id) =>
            Latest<EventListDto>(connection, EntityIds.Events).Events.Single(e => e.Id == id);

        private static EventRewardConfig Titanium(long amount) => new() { Type = "resource", Id = "Titanium", Amount = amount };

        private string Publish(EventDefinition definition)
        {
            var (error, saved) = _world.Events.Save(definition, "test", _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.Equal(string.Empty, _world.Events.Publish(saved!.Id, _clock.UtcNow));
            return saved.Id;
        }

        private void Tick()
        {
            _clock.Advance(TimeSpan.FromSeconds(6));
            _world.Tick();
        }

        private async Task<(PlayerSession Leader, RecordingConnection Connection, string AllianceId)> Alliance(string identity, string tag)
        {
            var (leader, connection) = await Connect(identity);
            leader.StateForTests.Wallet[ResourceType.Titanium] = 1_000_000;
            leader.StateForTests.Wallet[ResourceType.Hadalite] = 1_000_000;
            Send(leader, new CreateAllianceCommand { CommandId = Id(), Name = "House " + tag, Tag = tag }, CommandType.CreateAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            return (leader, connection, Latest<AllianceDto>(connection, EntityIds.MyAlliance).Id);
        }

        private async Task<(PlayerSession Session, RecordingConnection Connection)> Join(string identity, string allianceId)
        {
            var (member, connection) = await Connect(identity);
            Send(member, new JoinAllianceCommand { CommandId = Id(), AllianceId = allianceId }, CommandType.JoinAlliance);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            return (member, connection);
        }

        private MobilizationTaskCommand Task(string eventId, string action, int slot = 0) => new() { CommandId = Id(), EventId = eventId, Action = action, Slot = slot };

        // --- Mobilization rules ---------------------------------------------------------------------

        [Fact]
        public void Cards_are_dealt_the_same_everywhere_with_the_colour_shares()
        {
            var a = MobilizationRules.Deal("ev1", "al1", 3, 7, null, 100);
            var b = MobilizationRules.Deal("ev1", "al1", 3, 7, null, 100);
            Assert.Equal((a.Kind, a.Tier, a.Target, a.Points), (b.Kind, b.Tier, b.Target, b.Points));
            Assert.Equal(MobilizationRules.Target(a.Kind, a.Tier, 100), a.Target);
            Assert.Equal(MobilizationRules.Points(a.Tier), a.Points);

            // Over many draws every colour and kind shows up, green most and gold least.
            var cards = Enumerable.Range(0, 4000).Select(i => MobilizationRules.Deal("ev1", "al" + (i % 7), i % 8, i, null, 100)).ToList();
            var tiers = cards.GroupBy(c => c.Tier).ToDictionary(g => g.Key, g => g.Count());
            Assert.Equal(4, tiers.Count);
            Assert.True(tiers[MobilizationRules.Green] > tiers[MobilizationRules.Blue]);
            Assert.True(tiers[MobilizationRules.Blue] > tiers[MobilizationRules.Purple]);
            Assert.True(tiers[MobilizationRules.Purple] > tiers[MobilizationRules.Gold]);
            Assert.Equal(MobilizationRules.Kinds.Length, cards.Select(c => c.Kind).Distinct().Count());

            // Limits and scale.
            Assert.All(Enumerable.Range(0, 50).Select(i => MobilizationRules.Deal("e", "a", 0, i, new[] { "gather" }, 50)), c => Assert.Equal("gather", c.Kind));
            Assert.Equal(150_000 * 5 / 2, MobilizationRules.Target("gather", MobilizationRules.Gold, 50));
            Assert.True(MobilizationRules.Moves("train", EventActions.Train(4)));
            Assert.False(MobilizationRules.Moves("train", EventActions.Gather));
            Assert.Equal(560, MobilizationRules.Points(MobilizationRules.Gold));
        }

        private EventDefinition MobilizationEvent(int minMembers = 2) => new()
        {
            Template = EventTemplates.Mobilization,
            Title = { ["en"] = "Mobilization" },
            Banner = "trench",
            StartsAt = _clock.UtcNow,
            EndsAt = _clock.UtcNow.AddDays(3),
            ClaimDays = 1,
            Mobilization = new MobilizationConfig { MinMembers = minMembers, Slots = 4, RefillMinutes = 5, TasksPerDay = 3, RefreshesPerDay = 1, RefreshCooldownMinutes = 10 },
            Stages = { new EventTierConfig { Points = 100, Rewards = { Titanium(1000) } } },
            RankBands = { new EventTierConfig { From = 1, To = 3, Rewards = { Titanium(7000) } } }
        };

        private static (string Action, long Amount) Finish(MobilizationTaskDto task) => (MobilizationRules.Find(task.Kind)!.Actions[0], task.Target);

        [Fact]
        public async Task Mobilization_tasks_are_taken_finished_dropped_and_refreshed()
        {
            Assert.Equal("InvalidMobilization", EventService.Validate(new EventDefinition
            {
                Template = EventTemplates.Mobilization, Title = { ["en"] = "x" }, StartsAt = _clock.UtcNow, EndsAt = _clock.UtcNow.AddDays(1),
                Mobilization = new MobilizationConfig { Kinds = { "fly" } }
            }, TestSupport.Catalog));
            var id = Publish(MobilizationEvent());
            var (loner, lonerConnection) = await Connect("mob-loner");
            Send(loner, Task(id, MobilizationActions.Take), CommandType.MobilizationTask);
            Assert.Equal("NotInAlliance", LastAck(lonerConnection).Reason);

            var (leader, leaderConnection, allianceId) = await Alliance("mob-lead", "MOBI");
            Send(leader, Task(id, MobilizationActions.Take), CommandType.MobilizationTask);
            Assert.Equal("TooFewForEvent", LastAck(leaderConnection).Reason);
            Assert.Empty(Event(leaderConnection, id).Mobilization!.Pool);

            var (diver, diverConnection) = await Join("mob-diver", allianceId);
            Tick();
            Send(diver, Task(id, MobilizationActions.View), CommandType.MobilizationTask);
            Assert.Equal(CommandResultCode.Accepted, LastAck(diverConnection).Result);
            var view = Event(diverConnection, id).Mobilization!;
            Assert.Equal((2, 2, 4), (view.Members, view.MinMembers, view.Pool.Length));
            Assert.All(view.Pool, c => Assert.True(c.Kind.Length > 0 && c.Target > 0 && c.ReadyAtMs == 0));

            // Take: the card moves to the hand, its slot refills after five minutes; both members see the table change.
            var card = view.Pool[0];
            Send(diver, Task(id, MobilizationActions.Take, 0), CommandType.MobilizationTask);
            Assert.Equal(CommandResultCode.Accepted, LastAck(diverConnection).Result);
            var mine = Event(diverConnection, id).Mobilization!;
            Assert.Equal((card.Kind, card.Tier, card.Target, 0L), (mine.Mine!.Kind, mine.Mine.Tier, mine.Mine.Target, mine.Mine.Progress));
            Assert.True(mine.Pool[0].Kind.Length == 0 && mine.Pool[0].ReadyAtMs > _clock.UtcNow.ToUnixTimeMilliseconds());
            Assert.Equal(2, mine.TasksLeftToday);
            Assert.Equal(0, Event(leaderConnection, id).Mobilization!.Pool[0].Tier); // pushed to the mate as well
            Send(diver, Task(id, MobilizationActions.Take, 1), CommandType.MobilizationTask);
            Assert.Equal("TaskHeld", LastAck(diverConnection).Reason);
            Send(leader, Task(id, MobilizationActions.Take, 0), CommandType.MobilizationTask);
            Assert.Equal("TaskGone", LastAck(leaderConnection).Reason);

            // Playing moves it; something else does not; the last unit finishes it for the player and the alliance.
            var (action, target) = Finish(mine.Mine);
            diver.ScoreForTests(action, target - 1);
            diver.ScoreForTests(action == EventActions.Help ? EventActions.Dive : EventActions.Help, 1_000);
            Tick();
            Assert.Equal(target - 1, Event(diverConnection, id).Mobilization!.Mine!.Progress);
            diver.ScoreForTests(action, 5);
            Tick();
            var done = Event(diverConnection, id);
            Assert.Null(done.Mobilization!.Mine);
            Assert.Equal(1, done.Mobilization.Completed);
            Assert.Equal(card.Points, done.MyPoints);
            Assert.Equal(card.Points, done.AlliancePoints);
            Assert.Equal(1, done.AllianceRank);

            // The slot deals its next card when the refill is over.
            _clock.Advance(TimeSpan.FromMinutes(5));
            Tick();
            Send(diver, Task(id, MobilizationActions.View), CommandType.MobilizationTask);
            Assert.True(Event(diverConnection, id).Mobilization!.Pool[0].Kind.Length > 0);

            // Drop throws the task away; the day's takes run out.
            Send(diver, Task(id, MobilizationActions.Take, 1), CommandType.MobilizationTask);
            Assert.Equal(CommandResultCode.Accepted, LastAck(diverConnection).Result);
            Send(diver, Task(id, MobilizationActions.Drop), CommandType.MobilizationTask);
            Assert.Equal(CommandResultCode.Accepted, LastAck(diverConnection).Result);
            Assert.Null(Event(diverConnection, id).Mobilization!.Mine);
            Send(diver, Task(id, MobilizationActions.Drop), CommandType.MobilizationTask);
            Assert.Equal("NoTask", LastAck(diverConnection).Reason);
            Send(diver, Task(id, MobilizationActions.Take, 2), CommandType.MobilizationTask);
            Assert.Equal(CommandResultCode.Accepted, LastAck(diverConnection).Result);
            Send(diver, Task(id, MobilizationActions.Drop), CommandType.MobilizationTask);
            Send(diver, Task(id, MobilizationActions.Take, 3), CommandType.MobilizationTask);
            Assert.Equal("NoTasksLeft", LastAck(diverConnection).Reason);

            // A new UTC day: takes again; one refresh rerolls the task (progress gone), a second is over the limit.
            _clock.Advance(TimeSpan.FromDays(1));
            Tick();
            Send(diver, Task(id, MobilizationActions.Take, 3), CommandType.MobilizationTask);
            Assert.Equal(CommandResultCode.Accepted, LastAck(diverConnection).Result);
            Assert.NotNull(Event(diverConnection, id).Mobilization!.Mine);
            Send(diver, Task(id, MobilizationActions.Refresh), CommandType.MobilizationTask);
            Assert.Equal(CommandResultCode.Accepted, LastAck(diverConnection).Result);
            var fresh = Event(diverConnection, id).Mobilization!;
            Assert.Equal(0, fresh.Mine!.Progress);
            Assert.Equal(0, fresh.RefreshesLeftToday);
            Assert.True(fresh.RefreshReadyAtMs > _clock.UtcNow.ToUnixTimeMilliseconds());
            Send(diver, Task(id, MobilizationActions.Refresh), CommandType.MobilizationTask);
            Assert.Equal("NoRefreshesLeft", LastAck(diverConnection).Reason);

            // At the end the alliance's band goes by mail to the member who scored.
            _clock.Advance(TimeSpan.FromDays(3));
            Tick();
            var mail = Assert.Single(_world.Mail.MailsOf(diver.PublicId, _clock.UtcNow), m => m.Body.StartsWith("loc:mail.event.allianceRank.body|", StringComparison.Ordinal));
            Assert.Equal(7000, mail.Attachments.Amounts["Titanium"]);
            Assert.DoesNotContain(_world.Mail.MailsOf(leader.PublicId, _clock.UtcNow), m => m.Body.StartsWith("loc:mail.event.allianceRank", StringComparison.Ordinal));
            Send(diver, Task(id, MobilizationActions.Take, 0), CommandType.MobilizationTask);
            Assert.Equal("NotOpen", LastAck(diverConnection).Reason);
        }

        [Fact]
        public async Task Mobilization_pools_and_held_tasks_survive_a_restart()
        {
            var id = Publish(MobilizationEvent(minMembers: 1));
            var (leader, connection, allianceId) = await Alliance("mob-keep", "KEEP");
            Tick();
            Send(leader, Task(id, MobilizationActions.View), CommandType.MobilizationTask);
            var pool = Event(connection, id).Mobilization!.Pool;
            Assert.Equal(4, pool.Length);
            Send(leader, Task(id, MobilizationActions.Take, 2), CommandType.MobilizationTask);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Tick();
            await _world.Events.DrainAsync();

            var restarted = new EventService(_store, new CatalogRuntime(TestSupport.Catalog), TestSupport.Serializer, NullLogger.Instance)
            {
                AllianceIdOf = _ => allianceId, MemberCount = _ => 1
            };
            await restarted.InitializeAsync(_clock.UtcNow, default);
            var view = restarted.MobilizationPools(id, _clock.UtcNow).Single();
            Assert.Equal(1, view.Holding);
            Assert.Equal(pool.Where((_, i) => i != 2).Select(c => c.Kind), view.Cards.Where(c => c.Slot != 2).Select(c => c.Kind));
            Assert.Equal(0, view.Cards[2].Tier); // still refilling
        }

        // --- Alliance Duel rules --------------------------------------------------------------------

        [Fact]
        public void Duel_pairs_by_power_with_the_echo_for_the_odd_one()
        {
            static KeyValuePair<string, long> A(string id, long power) => new(id, power);
            var pairs = AllianceDuelRules.Pair(new[] { A("c", 300), A("a", 900), A("e", 100), A("b", 900), A("d", 200) });
            Assert.Equal(new[] { ("a", "b"), ("c", "d"), ("e", AllianceDuelRules.Echo) }, pairs.Select(p => (p.Key, p.Value)));
            Assert.Equal(new[] { ("x", AllianceDuelRules.Echo) }, AllianceDuelRules.Pair(new[] { A("x", 5) }).Select(p => (p.Key, p.Value)));
            Assert.Empty(AllianceDuelRules.Pair(Array.Empty<KeyValuePair<string, long>>()));
            Assert.Equal(2, AllianceDuelRules.Pair(new[] { A("a", 1), A("b", 1), A("c", 1), A("d", 1) }).Count);

            Assert.Equal(20, AllianceDuelRules.EchoScore(new long[] { 10, 30, 21 }));
            Assert.Equal(0, AllianceDuelRules.EchoScore(Array.Empty<long>()));
        }

        [Fact]
        public void Day_wins_add_up_and_the_first_to_seven_takes_the_week()
        {
            Assert.Equal(new[] { 1, 2, 2, 2, 2, 4 }, Enumerable.Range(0, 6).Select(AllianceDuelRules.DayWorth));
            Assert.Equal(13, Enumerable.Range(0, 6).Sum(AllianceDuelRules.DayWorth));
            Assert.Equal(AllianceDuelRules.Tie, AllianceDuelRules.DayWinner(0, 0));

            // A wins days 1, 3, 4 and 6 (1+2+2+4 = 9), B days 2 and 5 (4).
            var a = new long[] { 30, 0, 50, 50, 1, 90 };
            var b = new long[] { 10, 50, 40, 40, 9, 80 };
            AllianceDuelRules.WeekPoints(a, b, 6, out var aw, out var bw);
            Assert.Equal((9, 4), (aw, bw));
            Assert.Equal(AllianceDuelRules.Won, AllianceDuelRules.WeekResult(a, b));
            Assert.Equal(AllianceDuelRules.Lost, AllianceDuelRules.WeekResult(b, a));
            AllianceDuelRules.WeekPoints(a, b, 2, out aw, out bw);
            Assert.Equal((1, 2), (aw, bw)); // only finished days count

            // Neither reaches 7 (ties): more week points wins, then more duel points, else a draw.
            var c = new long[] { 5, 5, 5, 5, 5, 5 };
            Assert.Equal(AllianceDuelRules.Draw, AllianceDuelRules.WeekResult(c, (long[])c.Clone()));
            Assert.Equal(AllianceDuelRules.Won, AllianceDuelRules.WeekResult(new long[] { 6, 5, 5, 5, 5, 5 }, c));
        }

        private EventDefinition DuelEvent()
        {
            var def = new EventDefinition
            {
                Template = EventTemplates.AllianceDuel, Title = { ["en"] = "Alliance Duel" }, Banner = "leviathan",
                StartsAt = _clock.UtcNow, EndsAt = _clock.UtcNow.AddHours(6), ClaimDays = 1, MinRankPoints = 10,
                Stages = { new EventTierConfig { Points = 20, Rewards = { Titanium(100) } } },
                Duel = new DuelConfig { MinMembers = 1, Win = { Titanium(5000) }, Loss = { Titanium(1000) }, Draw = { Titanium(2500) } }
            };
            for (var d = 0; d < 6; d++)
                def.Phases.Add(new EventPhaseConfig { Title = { ["en"] = "Day " + (d + 1) }, StartHour = d, Hours = 1, Actions = { new EventActionConfig { Action = EventActions.MonsterKill, Points = 10 } } });
            return def;
        }

        [Fact]
        public void Duel_definitions_need_six_back_to_back_days()
        {
            Assert.Equal(string.Empty, EventService.Validate(DuelEvent(), TestSupport.Catalog));
            var five = DuelEvent();
            five.Phases.RemoveAt(5);
            Assert.Equal("InvalidDuelDays", EventService.Validate(five, TestSupport.Catalog));
            var gap = DuelEvent();
            gap.Phases[3].StartHour = 3.5;
            Assert.Equal("InvalidDuelDays", EventService.Validate(gap, TestSupport.Catalog));
            var badReward = DuelEvent();
            badReward.Duel.Win.Add(new EventRewardConfig { Type = "item", Id = "nope", Amount = 1 });
            Assert.Equal("InvalidDuelRewards", EventService.Validate(badReward, TestSupport.Catalog));
        }

        [Fact]
        public void Duels_pair_inside_each_kingdom_only()
        {
            _world.Events.DuelEntrants = () => new[]
            {
                new DuelEntrant("a1", "w1", 900, 10, "AAA", "Alpha"), new DuelEntrant("b1", "w1", 800, 10, "BBB", "Beta"),
                new DuelEntrant("c1", "w1", 100, 10, "CCC", "Gamma"), new DuelEntrant("tiny", "w1", 5_000, 0, "TNY", "Tiny"),
                new DuelEntrant("d2", "w2", 700, 10, "DDD", "Delta"), new DuelEntrant("e2", "w2", 600, 10, "EEE", "Eps")
            };
            var id = Publish(DuelEvent());
            Tick();
            var pairings = _world.Events.DuelPairings(id, _clock.UtcNow);
            Assert.Equal(new[] { ("w1", "a1", "b1"), ("w1", "c1", AllianceDuelRules.Echo), ("w2", "d2", "e2") },
                pairings.Select(p => (p.Kingdom, p.A.AllianceId, p.B.AllianceId)));
            Assert.Equal(("AAA", 900L), (pairings[0].A.Tag, pairings[0].A.Power));

            // Pairing happens once; the Overseer may pair again only before anyone scored.
            Tick();
            Assert.Equal(3, _world.Events.DuelPairings(id, _clock.UtcNow).Count);
            Assert.Equal(string.Empty, _world.Events.RepairDuel(id, _clock.UtcNow));
        }

        [Fact]
        public async Task A_duel_week_scores_days_opens_daily_chests_and_mails_the_result()
        {
            var id = Publish(DuelEvent());
            var (a, aConnection, aId) = await Alliance("duel-a", "AAA");
            var (b, bConnection, bId) = await Alliance("duel-b", "BBB");
            var (loner, lonerConnection) = await Connect("duel-loner");
            Tick();
            var view = Event(aConnection, id).Duel!;
            Assert.True(view.InDuel);
            Assert.Equal(bId, view.Rival!.AllianceId);
            Assert.Equal("BBB", view.Rival.Tag);
            Assert.False(Event(lonerConnection, id).Duel!.InDuel);

            // Day 1: A 30, B 10. The loner's kill counts for nothing.
            a.ScoreForTests(EventActions.MonsterKill, 3);
            b.ScoreForTests(EventActions.MonsterKill, 1);
            loner.ScoreForTests(EventActions.MonsterKill, 9);
            Tick();
            Assert.Equal(0, Event(lonerConnection, id).MyPoints);
            Assert.Equal(30, Event(aConnection, id).Duel!.MyDayPoints[0]);
            Assert.True(EventRules.HasClaim(Event(aConnection, id), _clock.UtcNow.ToUnixTimeMilliseconds()));

            // Day 2: B 50. A takes yesterday's chest now; B never reached it.
            _clock.Advance(TimeSpan.FromHours(1));
            b.ScoreForTests(EventActions.MonsterKill, 5);
            Tick();
            Send(a, new ClaimEventStageCommand { CommandId = Id(), EventId = id, Index = 0, Day = 1 }, CommandType.ClaimEventStage);
            Assert.Equal(CommandResultCode.Accepted, LastAck(aConnection).Result);
            Send(a, new ClaimEventStageCommand { CommandId = Id(), EventId = id, Index = 0, Day = 1 }, CommandType.ClaimEventStage);
            Assert.Equal("AlreadyClaimed", LastAck(aConnection).Reason);
            Send(a, new ClaimEventStageCommand { CommandId = Id(), EventId = id, Index = 0, Day = 3 }, CommandType.ClaimEventStage);
            Assert.Equal("UnknownStage", LastAck(aConnection).Reason); // a day that has not come
            Send(b, new ClaimEventStageCommand { CommandId = Id(), EventId = id, Index = 0, Day = 1 }, CommandType.ClaimEventStage);
            Assert.Equal("NotReached", LastAck(bConnection).Reason);
            var mid = Event(aConnection, id).Duel!;
            Assert.Equal(AllianceDuelRules.First, mid.DayWinners[0]);
            Assert.Equal(AllianceDuelRules.Pending, mid.DayWinners[1]);
            Assert.Equal((1, 0), (mid.Mine!.Wins, mid.Rival!.Wins));

            // Days 3, 4 and 6 to A; day 5 to B: 9–4.
            foreach (var (aKills, bKills) in new[] { (5, 4), (5, 4), (1, 3), (9, 8) })
            {
                _clock.Advance(TimeSpan.FromHours(1));
                a.ScoreForTests(EventActions.MonsterKill, aKills);
                b.ScoreForTests(EventActions.MonsterKill, bKills);
                Tick();
            }

            _clock.Advance(TimeSpan.FromHours(1));
            Tick();
            var end = Event(aConnection, id).Duel!;
            Assert.Equal((9, 4), (end.Mine!.Wins, end.Rival!.Wins));
            Assert.Equal(AllianceDuelRules.Won, end.Result);
            Assert.Equal(AllianceDuelRules.Lost, Event(bConnection, id).Duel!.Result);

            // Result mails with the week's rewards, and every reached daily chest left unclaimed.
            var aMails = _world.Mail.MailsOf(a.PublicId, _clock.UtcNow);
            var won = Assert.Single(aMails, m => m.Title == "loc:mail.duel.won.title");
            Assert.Equal("loc:mail.duel.result.body|[BBB] House BBB|9|4|230", won.Body);
            Assert.Equal(5000, won.Attachments.Amounts["Titanium"]);
            var chests = Assert.Single(aMails, m => m.Body.StartsWith("loc:mail.duel.chests.body|", StringComparison.Ordinal));
            Assert.Equal("loc:mail.duel.chests.body|3", chests.Body); // days 3, 4 and 6 (50, 50, 90); day 5 had 10, day 1 was taken
            Assert.Equal(300, chests.Attachments.Amounts["Titanium"]);
            var lost = Assert.Single(_world.Mail.MailsOf(b.PublicId, _clock.UtcNow), m => m.Title == "loc:mail.duel.lost.title");
            Assert.Equal(1000, lost.Attachments.Amounts["Titanium"]);
            Assert.DoesNotContain(_world.Mail.MailsOf(loner.PublicId, _clock.UtcNow), m => m.Kind == Hadal.Game.Mailbox.MailKinds.Event);

            // The finished week shows in the next duel's history.
            _clock.Advance(TimeSpan.FromHours(2));
            var next = DuelEvent();
            next.StartsAt = _clock.UtcNow;
            next.EndsAt = _clock.UtcNow.AddHours(6);
            var nextId = Publish(next);
            Tick();
            var history = Assert.Single(Event(aConnection, nextId).Duel!.History);
            Assert.Equal(("BBB", 9, 4, AllianceDuelRules.Won), (history.RivalTag, history.MyWins, history.RivalWins, history.Result));
        }
    }
}
