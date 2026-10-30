using Hadal.Game.Events;
using Hadal.Game.Mailbox;
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
    /// <summary>
    /// End-of-event rewards by mail (docs/29 §2.2 "Payout", roadmap §7): rank rewards and unclaimed stages arrive in the
    /// Events tab exactly once — across ticks, restarts, a crash between sending and marking, and a claimed, deleted and
    /// purged mail.
    /// </summary>
    public sealed class EventPayoutTests
    {
        private readonly FakeClock _clock = new();
        private readonly InMemoryEventStore _events = new();
        private readonly InMemoryMailStore _mails = new();
        private readonly InMemoryPlayerRepository _players = new();
        private readonly Hadal.Game.Social.InMemorySocialStore _social = new();

        private GameWorld NewWorld() =>
            new(new CatalogRuntime(TestSupport.Catalog), _players, _social, new Hadal.Game.World.InMemoryWorldStore(), _clock, TestSupport.Serializer,
                NullLogger<GameWorld>.Instance, _events, _mails);

        private async Task<GameWorld> Started()
        {
            var world = NewWorld();
            await world.Mail.InitializeAsync(_clock.UtcNow, default);
            await world.Events.InitializeAsync(_clock.UtcNow, default);
            return world;
        }

        private static async Task<(PlayerSession Session, RecordingConnection Connection)> Connect(GameWorld world, string identity)
        {
            var connection = new RecordingConnection();
            var session = await world.ConnectAsync(identity, connection, default, identity);
            return (session, connection);
        }

        private static void Send<T>(GameWorld world, PlayerSession session, T command, CommandType type) where T : ICommand =>
            world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, type, TestSupport.Serializer.Serialize(command)));

        private static CommandAckDto LastAck(RecordingConnection connection) =>
            connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta()).Last(d => d.Ack() != null).Ack()!;

        private static EventRewardConfig Titanium(long amount) => new() { Type = "resource", Id = "Titanium", Amount = amount };

        private EventDefinition Contest() => new()
        {
            Template = EventTemplates.Points,
            Title = new() { ["tr"] = "Derinlik Yarışı", ["en"] = "Depth Race" },
            Banner = "abyss",
            StartsAt = _clock.UtcNow,
            EndsAt = _clock.UtcNow.AddHours(4),
            ClaimDays = 2,
            Actions = { new EventActionConfig { Action = EventActions.MonsterKill, Points = 50 } },
            Stages = { new EventTierConfig { Points = 10, Rewards = { Titanium(500) } } },
            RankBands = { new EventTierConfig { From = 1, To = 1, Rewards = { Titanium(9000), new EventRewardConfig { Type = "item", Id = "chest_gold", Amount = 1 } } } }
        };

        private static string Publish(GameWorld world, EventDefinition definition, DateTimeOffset now)
        {
            var (error, saved) = world.Events.Save(definition, "test", now);
            Assert.Equal(string.Empty, error);
            Assert.Equal(string.Empty, world.Events.Publish(saved!.Id, now));
            return saved.Id;
        }

        private void Tick(GameWorld world)
        {
            _clock.Advance(TimeSpan.FromSeconds(6));
            world.Tick();
        }

        private List<Mail> Stored(string playerId) =>
            _mails.LoadAsync(playerId, _clock.UtcNow, default).GetAwaiter().GetResult().Where(m => m.Kind == MailKinds.Event).ToList();

        [Fact]
        public async Task A_crash_between_sending_and_marking_never_sends_twice()
        {
            var world = await Started();
            var id = Publish(world, Contest(), _clock.UtcNow);
            var (session, _) = await Connect(world, "pay-crash");
            session.ScoreForTests(EventActions.MonsterKill, 2);
            Tick(world);

            // The event ends; the payout's mails are stored, then the server dies before marking the event paid.
            _clock.Advance(TimeSpan.FromHours(4));
            var payout = Assert.Single(world.Events.TakePayouts(_clock.UtcNow));
            Assert.Equal(new[] { $"ev:{id}:{session.PublicId}:rank", $"ev:{id}:{session.PublicId}:stages" }, payout.Mails.Select(m => m.Key).OrderBy(k => k));
            Assert.Equal(2, (await world.Mail.SendOnceAsync(payout.Mails, _clock.UtcNow, default)).Count);
            Assert.Empty(world.Events.TakePayouts(_clock.UtcNow)); // handed out once, until confirmed or failed
            await world.Events.DrainAsync();

            // After the restart the payout runs again: the keys are taken, so nothing more is stored, and now it is marked.
            var restarted = await Started();
            var (again, connection) = await Connect(restarted, "pay-crash");
            Tick(restarted);
            Assert.Equal(2, Stored(again.PublicId).Count);
            var summary = restarted.Events.Get(id, _clock.UtcNow)!;
            Assert.NotNull(summary.Definition.PaidOutAt);
            Assert.Equal(0, summary.Definition.PaidOutMails);
            Assert.Empty(restarted.Events.TakePayouts(_clock.UtcNow));

            // The player claims the rank mail and deletes it; a restart purges it. It still never comes back.
            var rank = Stored(again.PublicId).Single(m => m.Body.StartsWith("loc:mail.event.rank.body|", StringComparison.Ordinal));
            Send(restarted, again, new ClaimMailCommand { CommandId = "c1", MailId = rank.Id }, CommandType.ClaimMail);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            Assert.Equal(1, again.StateForTests.Items.GetValueOrDefault("chest_gold"));
            Send(restarted, again, new DeleteMailCommand { CommandId = "d1", MailId = rank.Id }, CommandType.DeleteMail);
            await restarted.Mail.DrainAsync();
            await restarted.Events.DrainAsync();

            var third = await Started();
            var (last, _) = await Connect(third, "pay-crash");
            Tick(third);
            Tick(third);
            Assert.Single(Stored(last.PublicId)); // only the stages mail is left
            Assert.Empty(third.Events.TakePayouts(_clock.UtcNow));
        }

        [Fact]
        public async Task A_failed_send_is_retried_and_then_paid_once()
        {
            var world = await Started();
            var id = Publish(world, Contest(), _clock.UtcNow);
            var (session, _) = await Connect(world, "pay-retry");
            session.ScoreForTests(EventActions.MonsterKill, 1);
            Tick(world);
            _clock.Advance(TimeSpan.FromHours(4));

            var payout = Assert.Single(world.Events.TakePayouts(_clock.UtcNow));
            world.Events.PayoutFailed(id, _clock.UtcNow);
            Assert.Empty(world.Events.TakePayouts(_clock.UtcNow.AddSeconds(30))); // waits a minute
            Assert.Equal(payout.Mails.Select(m => m.Key), Assert.Single(world.Events.TakePayouts(_clock.UtcNow.AddMinutes(2))).Mails.Select(m => m.Key));
            world.Events.PayoutFailed(id, _clock.UtcNow);

            _clock.Advance(TimeSpan.FromMinutes(2));
            world.Tick();
            Assert.Equal(2, Stored(session.PublicId).Count);
            Assert.Equal(2, world.Events.Get(id, _clock.UtcNow)!.Definition.PaidOutMails);
            world.Tick();
            Assert.Equal(2, Stored(session.PublicId).Count);
        }

        [Fact]
        public async Task Hero_card_rewards_travel_by_mail_and_land_on_claim()
        {
            var world = await Started();
            var contest = Contest();
            var rarity = TestSupport.Catalog.Heroes.First().Rarity.id;
            contest.RankBands[0].Rewards = new List<EventRewardConfig> { new() { Type = "shards", Rarity = rarity, Amount = 5 } };
            Publish(world, contest, _clock.UtcNow);
            var (session, connection) = await Connect(world, "pay-cards");
            session.ScoreForTests(EventActions.MonsterKill, 1);
            Tick(world);
            _clock.Advance(TimeSpan.FromHours(4));
            Tick(world);

            var mail = Assert.Single(world.Mail.MailsOf(session.PublicId, _clock.UtcNow), m => m.Body.StartsWith("loc:mail.event.rank.body|", StringComparison.Ordinal));
            var (hero, cards) = Assert.Single(mail.Attachments.Shards);
            Assert.Equal(5, cards);
            Assert.Equal(rarity, TestSupport.Catalog.Heroes.Single(h => h.Id == hero).Rarity.id);

            Send(world, session, new ClaimMailCommand { CommandId = "c", MailId = mail.Id }, CommandType.ClaimMail);
            Assert.Equal(CommandResultCode.Accepted, LastAck(connection).Result);
            var owned = session.StateForTests.Heroes.FirstOrDefault(h => h.HeroId == hero);
            Assert.True((owned?.Shards ?? 0) >= 5 || session.StateForTests.HeroCards.GetValueOrDefault(hero) >= 5);
        }

        [Fact]
        public async Task Sign_in_days_left_unclaimed_and_cancelled_or_unpublished_events()
        {
            var world = await Started();
            var login = new EventDefinition
            {
                Template = EventTemplates.Login, Title = { ["en"] = "Dive Log" }, Banner = "wreck", StartsAt = _clock.UtcNow.AddMinutes(-1), EndsAt = _clock.UtcNow.AddDays(2),
                Stages = Enumerable.Range(1, 2).Select(d => new EventTierConfig { Points = d, Rewards = { Titanium(100 * d) } }).ToList()
            };
            var loginId = Publish(world, login, _clock.UtcNow);
            var draft = Contest();
            world.Events.Save(draft, "test", _clock.UtcNow);
            var (session, _) = await Connect(world, "pay-login");
            Tick(world);
            _clock.Advance(TimeSpan.FromDays(1));
            Tick(world); // day 2 counted, nothing claimed
            _clock.Advance(TimeSpan.FromDays(1));
            Tick(world);

            // (The default calendar's own Dive Log ends in this window too and mails under its key name.)
            var mail = Assert.Single(world.Mail.MailsOf(session.PublicId, _clock.UtcNow), m => m.Body.Contains("|Dive Log|", StringComparison.Ordinal));
            Assert.Equal("loc:mail.event.stages.body|Dive Log|2", mail.Body);
            Assert.Equal(300, mail.Attachments.Amounts["Titanium"]);
            Assert.NotNull(world.Events.Get(loginId, _clock.UtcNow)!.Definition.PaidOutAt);
            // The draft never went out, so it pays nothing and stays unpaid.
            Assert.All(world.Events.List(_clock.UtcNow).Where(s => s.Definition.Status == EventStatus.Draft), s => Assert.Null(s.Definition.PaidOutAt));
        }

        [Fact]
        public async Task Default_schedules_name_their_events_by_key_and_the_extension_is_refused_once_paid()
        {
            var world = await Started();
            world.Events.RunCalendarNow(_clock.UtcNow);
            var race = world.Events.List(_clock.UtcNow).Select(s => s.Definition).First(d => d.ScheduleId == DefaultSchedules.DepthRace && d.StartsAt <= _clock.UtcNow);
            var (session, _) = await Connect(world, "pay-name");
            // Whatever theme runs, one of these scores in it.
            session.ScoreForTests(EventActions.MonsterKill, 20);
            session.ScoreForTests(EventActions.Gather, 1_000_000);
            session.ScoreForTests(EventActions.Research, 5);
            session.ScoreForTests(EventActions.BuildPower, 10_000);
            session.ScoreForTests(EventActions.Train(1), 1_000);
            session.ScoreForTests(EventActions.HeroLevel, 20);
            Tick(world);

            _clock.UtcNow = race.EndsAt.AddMinutes(1);
            Tick(world);
            Assert.Contains(world.Mail.MailsOf(session.PublicId, _clock.UtcNow), m => m.Title == "loc:mail.event.rank.title|event.name.depth-race");
            Assert.Equal("AlreadyPaid", world.Events.Extend(race.Id, 2, _clock.UtcNow));
        }
    }
}
