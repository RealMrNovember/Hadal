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
    /// <summary>The weekly event calendar (docs/29 §2.2, roadmap §7): schedules, occurrences, default seed, player calendar.</summary>
    public sealed class EventCalendarTests
    {
        private readonly FakeClock _clock = new() { UtcNow = new DateTimeOffset(2026, 10, 28, 9, 30, 0, TimeSpan.Zero) }; // a Wednesday
        private readonly InMemoryEventStore _store = new();
        private readonly Hadal.Game.Kingdoms.InMemoryKingdomStore _kingdoms = new();

        private GameWorld NewWorld() =>
            new(new CatalogRuntime(TestSupport.Catalog), new InMemoryPlayerRepository(), new Hadal.Game.Social.InMemorySocialStore(),
                new Hadal.Game.World.InMemoryWorldStore(), _clock, TestSupport.Serializer, NullLogger<GameWorld>.Instance, _store, kingdomStore: _kingdoms);

        private async Task<GameWorld> StartedWorld()
        {
            var world = NewWorld();
            await world.Kingdoms.InitializeAsync(default);
            await world.Events.InitializeAsync(_clock.UtcNow, default);
            return world;
        }

        private static DateTimeOffset Utc(int y, int m, int d, int h = 0, int min = 0) => new(y, m, d, h, min, 0, TimeSpan.Zero);

        private static EventSchedule Daily(string id, params int[] weekdays) => new()
        {
            Id = id,
            Weekdays = weekdays.ToList(),
            StartMinute = 0,
            DurationHours = 24,
            LeadHours = 24,
            Blueprint = new EventDefinition
            {
                Template = EventTemplates.Points, Banner = "abyss", Title = { ["en"] = "Trial", ["tr"] = "Deneme" },
                Actions = { new EventActionConfig { Action = EventActions.MonsterKill, Points = 10 } },
                Stages = { new EventTierConfig { Points = 10, Rewards = { new EventRewardConfig { Type = "resource", Id = "Titanium", Amount = 100 } } } }
            }
        };

        private static IEnumerable<string> CalendarEvents(GameWorld world) =>
            world.Events.List(DateTimeOffset.MaxValue).Select(s => s.Definition).Where(d => d.CreatedBy == EventService.CalendarAuthor).Select(d => d.Id);

        // --- Weekday and time arithmetic --------------------------------------------------------------

        [Fact]
        public void Starts_follow_the_weekdays_across_week_month_and_year_boundaries()
        {
            var schedule = Daily("x", (int)DayOfWeek.Monday, (int)DayOfWeek.Friday);
            schedule.StartMinute = 22 * 60 + 30;
            var starts = EventScheduleRules.Starts(schedule, Utc(2026, 12, 24), Utc(2027, 1, 6));
            Assert.Equal(new[] { Utc(2026, 12, 25, 22, 30), Utc(2026, 12, 28, 22, 30), Utc(2027, 1, 1, 22, 30), Utc(2027, 1, 4, 22, 30) }, starts);

            // Every day across a month end, the window's own edges included at the start and excluded at the end.
            var daily = Daily("d", 0, 1, 2, 3, 4, 5, 6);
            Assert.Equal(new[] { Utc(2026, 10, 30), Utc(2026, 10, 31), Utc(2026, 11, 1), Utc(2026, 11, 2) },
                EventScheduleRules.Starts(daily, Utc(2026, 10, 30), Utc(2026, 11, 3)));
            Assert.Empty(EventScheduleRules.Starts(daily, Utc(2026, 10, 30, 0, 1), Utc(2026, 10, 30, 23, 59)));

            // February in a leap year.
            Assert.Equal(new[] { Utc(2028, 2, 28), Utc(2028, 2, 29), Utc(2028, 3, 1) }, EventScheduleRules.Starts(daily, Utc(2028, 2, 28), Utc(2028, 3, 2)));
        }

        [Fact]
        public void Due_occurrences_are_the_running_ones_and_those_inside_the_lead_time()
        {
            var weekly = Daily("w", (int)DayOfWeek.Monday);
            weekly.DurationHours = 7 * 24;
            // Wednesday: Monday's week is running; next Monday is more than a day away.
            Assert.Equal(new[] { Utc(2026, 10, 26) }, EventScheduleRules.Due(weekly, Utc(2026, 10, 28, 9, 30)));
            // Sunday 01:00: next Monday is inside the 24-hour lead, both show.
            Assert.Equal(new[] { Utc(2026, 10, 26), Utc(2026, 11, 2) }, EventScheduleRules.Due(weekly, Utc(2026, 11, 1, 1)));
            // Monday 00:00 sharp: the old week is over.
            Assert.Equal(new[] { Utc(2026, 11, 2) }, EventScheduleRules.Due(weekly, Utc(2026, 11, 2)));
        }

        [Fact]
        public void Occurrence_ids_are_fixed_by_schedule_day_kingdom_and_bracket()
        {
            Assert.Equal("depth-race:2026-11-02:w1", EventScheduleRules.OccurrenceId("depth-race", Utc(2026, 11, 2, 23, 59), "w1", -1));
            Assert.Equal("depth-race:2027-01-01:w12:b2", EventScheduleRules.OccurrenceId("depth-race", Utc(2027, 1, 1), "w12", 1));
        }

        [Fact]
        public void Themes_rotate_one_slot_a_day_and_keep_their_hours()
        {
            var depth = DefaultSchedules.All(_clock.UtcNow).Single(s => s.Id == DefaultSchedules.DepthRace);
            var monday = EventScheduleRules.Materialize(depth, Utc(2026, 11, 2), "w1", -1, _clock.UtcNow);
            var tuesday = EventScheduleRules.Materialize(depth, Utc(2026, 11, 3), "w1", -1, _clock.UtcNow);
            Assert.Equal(monday.Phases.Select(p => p.StartHour), tuesday.Phases.Select(p => p.StartHour));
            Assert.Equal(monday.Phases[1].Title["en"], tuesday.Phases[0].Title["en"]);
            Assert.Equal(monday.Phases[0].Title["en"], tuesday.Phases[5].Title["en"]);
            Assert.Equal(6, monday.Phases.Select(p => p.Title["en"]).Distinct().Count());
        }

        [Fact]
        public void Schedules_are_validated()
        {
            var catalog = TestSupport.Catalog;
            var now = _clock.UtcNow;
            Assert.Equal(string.Empty, EventScheduleRules.Validate(Daily("ok", 1), catalog, now));
            Assert.Equal("InvalidWeekdays", EventScheduleRules.Validate(Daily("none"), catalog, now));
            Assert.Equal("InvalidWeekdays", EventScheduleRules.Validate(Daily("seven", 7), catalog, now));
            var late = Daily("late", 1);
            late.StartMinute = 24 * 60;
            Assert.Equal("InvalidStartTime", EventScheduleRules.Validate(late, catalog, now));
            var longer = Daily("long", 1);
            longer.DurationHours = 7 * 24 + 1;
            Assert.Equal("InvalidDuration", EventScheduleRules.Validate(longer, catalog, now));
            var brackets = Daily("b", 1);
            brackets.Brackets.Add(new EventBracket { MinLevel = 10, MaxLevel = 5 });
            Assert.Equal("InvalidBrackets", EventScheduleRules.Validate(brackets, catalog, now));
            var badBlueprint = Daily("bp", 1);
            badBlueprint.Blueprint.Actions.Clear();
            Assert.Equal("NoActions", EventScheduleRules.Validate(badBlueprint, catalog, now));
        }

        // --- Default seed -------------------------------------------------------------------------------

        [Fact]
        public async Task Defaults_are_seeded_once_into_an_empty_calendar_and_are_valid()
        {
            foreach (var schedule in DefaultSchedules.All(_clock.UtcNow))
            {
                Assert.Equal(string.Empty, EventScheduleRules.Validate(schedule, TestSupport.Catalog, _clock.UtcNow));
                // Every event text in all 18 languages.
                Assert.Equal(18, schedule.Blueprint.Title.Count);
                Assert.Equal(18, schedule.Blueprint.Description.Count);
                Assert.All(schedule.Blueprint.Phases, p => Assert.Equal(18, p.Title.Count));
            }

            var world = await StartedWorld();
            Assert.Equal(new[] { DefaultSchedules.AbyssSiege, DefaultSchedules.AllianceDuel, DefaultSchedules.AllianceWeek, DefaultSchedules.DepthRace, DefaultSchedules.LoginWeek, DefaultSchedules.LureTrap, DefaultSchedules.Mobilization,
                    DefaultSchedules.TrenchConquest },
                world.Events.Schedules().Select(s => s.Id).OrderBy(i => i, StringComparer.Ordinal));
            var depth = world.Events.Schedule(DefaultSchedules.DepthRace)!;
            Assert.Equal(7, depth.Weekdays.Count);
            Assert.Equal(6, depth.Blueprint.Phases.Count);
            Assert.Equal(24, depth.Blueprint.Phases.Sum(p => p.Hours));
            Assert.Equal(EventTemplates.Alliance, world.Events.Schedule(DefaultSchedules.AllianceWeek)!.Blueprint.Template);
            Assert.Equal(7, world.Events.Schedule(DefaultSchedules.LoginWeek)!.Blueprint.Stages.Count);
            await world.Events.DrainAsync();

            // A restart does not seed again, and an Overseer who deleted every schedule does not get them back.
            var again = await StartedWorld();
            Assert.Equal(8, again.Events.Schedules().Count);
            foreach (var s in again.Events.Schedules())
                Assert.Equal(string.Empty, again.Events.DeleteSchedule(s.Id));
            await again.Events.DrainAsync();
            Assert.Empty((await StartedWorld()).Events.Schedules());
        }

        [Fact]
        public async Task New_defaults_are_added_once_to_a_calendar_seeded_before_them()
        {
            // A live calendar from before Mobilization and the duel: seeded (marker), one default edited, one deleted,
            // and a schedule of the Overseer's own.
            var old = DefaultSchedules.All(_clock.UtcNow).Where(s => s.Id is DefaultSchedules.DepthRace or DefaultSchedules.AllianceWeek).ToList();
            old[0].StartMinute = 7 * 60; // the Overseer moved the race to 07:00
            foreach (var s in old)
                await _store.SaveScheduleAsync(s, default);
            await _store.SaveScheduleAsync(Daily("own-hunt", 6), default);
            await _store.SaveOccurrencesAsync(new[] { (EventScheduleRules.SeedMarker, string.Empty, _clock.UtcNow.AddDays(-30)) }, default);

            var world = await StartedWorld();
            Assert.Equal(new[] { DefaultSchedules.AbyssSiege, DefaultSchedules.AllianceDuel, DefaultSchedules.AllianceWeek, DefaultSchedules.DepthRace, DefaultSchedules.LureTrap, DefaultSchedules.Mobilization, "own-hunt",
                    DefaultSchedules.TrenchConquest },
                world.Events.Schedules().Select(s => s.Id).OrderBy(i => i, StringComparer.Ordinal));
            Assert.Equal(7 * 60, world.Events.Schedule(DefaultSchedules.DepthRace)!.StartMinute); // untouched
            Assert.Null(world.Events.Schedule(DefaultSchedules.LoginWeek)); // deleted before: stays deleted
            var mobilization = world.Events.Schedule(DefaultSchedules.Mobilization)!;
            Assert.Equal(new[] { (int)DayOfWeek.Thursday }, mobilization.Weekdays);
            Assert.Equal(72d, mobilization.DurationHours);
            Assert.Equal(EventTemplates.Mobilization, mobilization.Blueprint.Template);
            var duel = world.Events.Schedule(DefaultSchedules.AllianceDuel)!;
            Assert.Equal(new[] { (int)DayOfWeek.Monday }, duel.Weekdays);
            Assert.Equal(144d, duel.DurationHours);
            Assert.Equal(AllianceDuelRules.Days, duel.Blueprint.Phases.Count);
            var trench = world.Events.Schedule(DefaultSchedules.TrenchConquest)!;
            Assert.Equal(EventTemplates.TrenchConquest, trench.Blueprint.Template);
            Assert.Equal(168d, trench.DurationHours);
            var lure = world.Events.Schedule(DefaultSchedules.LureTrap)!;
            Assert.Equal(EventTemplates.LureTrap, lure.Blueprint.Template);
            Assert.Equal(new[] { (int)DayOfWeek.Tuesday, (int)DayOfWeek.Saturday }, lure.Weekdays);
            await world.Events.DrainAsync();

            // Once added they are ordinary schedules: deleting one keeps it gone after a restart.
            Assert.Equal(string.Empty, world.Events.DeleteSchedule(DefaultSchedules.Mobilization));
            await world.Events.DrainAsync();
            var again = await StartedWorld();
            Assert.Null(again.Events.Schedule(DefaultSchedules.Mobilization));
            Assert.NotNull(again.Events.Schedule(DefaultSchedules.AllianceDuel));
            Assert.Equal(7, again.Events.Schedules().Count);
        }

        [Fact]
        public async Task The_defaults_give_a_timed_event_today_and_an_alliance_goal_this_week()
        {
            var world = await StartedWorld();
            world.Events.RunCalendarNow(_clock.UtcNow);
            var ids = CalendarEvents(world).ToList();
            Assert.Contains("depth-race:2026-10-28:w1", ids);
            Assert.Contains("depth-race:2026-10-29:w1", ids); // tomorrow, inside the lead time
            Assert.Contains("alliance-week:2026-10-26:w1", ids); // this week's, created mid-week
            Assert.Contains("login-week:2026-10-26:w1", ids);
            Assert.DoesNotContain(ids, i => i.StartsWith("depth-race:2026-10-30", StringComparison.Ordinal));

            var race = world.Events.Get("depth-race:2026-10-28:w1", _clock.UtcNow)!;
            Assert.Equal("live", race.Phase);
            Assert.Equal(Utc(2026, 10, 29), race.Definition.EndsAt);
            Assert.Equal(new[] { "w1" }, race.Definition.Kingdoms);
            Assert.Equal(DefaultSchedules.DepthRace, race.Definition.ScheduleId);
        }

        // --- The scheduler ------------------------------------------------------------------------------

        [Fact]
        public async Task Each_occurrence_is_created_once_also_across_restarts()
        {
            var world = await StartedWorld();
            var first = world.Events.RunCalendarNow(_clock.UtcNow);
            Assert.True(first >= 4);
            Assert.Equal(0, world.Events.RunCalendarNow(_clock.UtcNow));
            _clock.Advance(TimeSpan.FromSeconds(31));
            world.Tick();
            var before = CalendarEvents(world).OrderBy(i => i).ToList();
            await world.Events.DrainAsync();

            var restarted = await StartedWorld();
            Assert.Equal(before, CalendarEvents(restarted).OrderBy(i => i));
            Assert.Equal(0, restarted.Events.RunCalendarNow(_clock.UtcNow));

            // A day later only the next day's race is new.
            _clock.Advance(TimeSpan.FromDays(1));
            Assert.Equal(1, restarted.Events.RunCalendarNow(_clock.UtcNow));
            Assert.Contains("depth-race:2026-10-30:w1", CalendarEvents(restarted));
        }

        [Fact]
        public async Task An_occurrence_the_Overseer_deleted_is_not_created_again()
        {
            var world = await StartedWorld();
            world.Events.RunCalendarNow(_clock.UtcNow);
            const string id = "depth-race:2026-10-29:w1";
            Assert.Equal(string.Empty, world.Events.Cancel(id, _clock.UtcNow));
            Assert.Equal(string.Empty, world.Events.Delete(id));
            Assert.Equal(0, world.Events.RunCalendarNow(_clock.UtcNow));
            Assert.DoesNotContain(id, CalendarEvents(world));
            await world.Events.DrainAsync();

            var restarted = await StartedWorld();
            Assert.Equal(0, restarted.Events.RunCalendarNow(_clock.UtcNow));
            Assert.DoesNotContain(id, CalendarEvents(restarted));

            // "Create the next occurrence now" brings it back on purpose.
            var (error, created) = restarted.Events.CreateNextOccurrence(DefaultSchedules.DepthRace, _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.Equal(new[] { id }, created);
        }

        [Fact]
        public async Task Occurrences_are_created_for_every_kingdom()
        {
            var world = await StartedWorld();
            await world.Kingdoms.OpenNextAsync(_clock.UtcNow, default);
            world.Events.RunCalendarNow(_clock.UtcNow);
            var ids = CalendarEvents(world).ToList();
            Assert.Contains("depth-race:2026-10-28:w1", ids);
            Assert.Contains("depth-race:2026-10-28:w2", ids);
            Assert.Equal(new[] { "w2" }, world.Events.Get("depth-race:2026-10-28:w2", _clock.UtcNow)!.Definition.Kingdoms);

            // A schedule for one kingdom only.
            var only = Daily("only-two", 0, 1, 2, 3, 4, 5, 6);
            only.Blueprint.Kingdoms.Add("w2");
            Assert.Equal(string.Empty, world.Events.SaveSchedule(only, "test", _clock.UtcNow).Error);
            world.Events.RunCalendarNow(_clock.UtcNow);
            Assert.Contains("only-two:2026-10-28:w2", CalendarEvents(world));
            Assert.DoesNotContain("only-two:2026-10-28:w1", CalendarEvents(world));
        }

        [Fact]
        public async Task Disabled_schedules_create_nothing_until_switched_on_or_run_by_hand()
        {
            var world = await StartedWorld();
            foreach (var s in world.Events.Schedules())
                Assert.Equal(string.Empty, world.Events.SetScheduleEnabled(s.Id, false, _clock.UtcNow));
            Assert.Equal(0, world.Events.RunCalendarNow(_clock.UtcNow));
            Assert.Empty(CalendarEvents(world));

            var (error, created) = world.Events.CreateNextOccurrence(DefaultSchedules.DepthRace, _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.Equal(new[] { "depth-race:2026-10-28:w1" }, created);
            Assert.Equal(new[] { "depth-race:2026-10-29:w1" }, world.Events.CreateNextOccurrence(DefaultSchedules.DepthRace, _clock.UtcNow).Created);

            Assert.Equal(string.Empty, world.Events.SetScheduleEnabled(DefaultSchedules.LoginWeek, true, _clock.UtcNow));
            Assert.Equal(1, world.Events.RunCalendarNow(_clock.UtcNow));
            Assert.Equal("UnknownSchedule", world.Events.SetScheduleEnabled("nope", true, _clock.UtcNow));
        }

        [Fact]
        public async Task Brackets_make_one_event_per_reactor_range_and_players_see_their_own()
        {
            var world = await StartedWorld();
            foreach (var s in world.Events.Schedules())
                world.Events.SetScheduleEnabled(s.Id, false, _clock.UtcNow);
            var bracketed = Daily("bracketed", 0, 1, 2, 3, 4, 5, 6);
            bracketed.Brackets.Add(new EventBracket { MinLevel = 1, MaxLevel = 9 });
            bracketed.Brackets.Add(new EventBracket { MinLevel = 10, MaxLevel = 0 });
            Assert.Equal(string.Empty, world.Events.SaveSchedule(bracketed, "test", _clock.UtcNow).Error);
            world.Events.RunCalendarNow(_clock.UtcNow);

            var low = world.Events.Get("bracketed:2026-10-28:w1:b1", _clock.UtcNow)!.Definition;
            var high = world.Events.Get("bracketed:2026-10-28:w1:b2", _clock.UtcNow)!.Definition;
            Assert.Equal((1, 9), (low.MinLevel, low.MaxLevel));
            Assert.Equal((10, 0), (high.MinLevel, high.MaxLevel));

            var connection = new RecordingConnection();
            var session = await world.ConnectAsync("cal-bracket", connection, default, "cal-bracket");
            var shown = TestSupport.Serializer.Deserialize<EventListDto>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.Events).Payload).Events.Select(e => e.Id).ToList();
            Assert.Contains("bracketed:2026-10-28:w1:b1", shown);
            Assert.DoesNotContain("bracketed:2026-10-28:w1:b2", shown);

            // The calendar shows only the player's bracket for the days to come as well.
            var calendar = Calendar(world, session, connection, "en");
            Assert.Contains(calendar.Entries, e => e.Id == "bracketed:2026-11-01:w1:b1");
            Assert.DoesNotContain(calendar.Entries, e => e.Id.EndsWith(":b2", StringComparison.Ordinal));
        }

        // --- The player's calendar ----------------------------------------------------------------------

        private EventCalendarDto Calendar(GameWorld world, PlayerSession session, RecordingConnection connection, string lang)
        {
            var command = new ViewEventCalendarCommand { CommandId = Guid.NewGuid().ToString("N"), Lang = lang };
            world.HandleCommand(session, EnvelopeBuilder.WrapCommand(command, CommandType.ViewEventCalendar, TestSupport.Serializer.Serialize(command)));
            return TestSupport.Serializer.Deserialize<EventCalendarDto>(connection.Messages.Where(m => m.Delta != null).Select(m => m.Delta())
                .SelectMany(d => d.Changes).Last(c => c.EntityId == EntityIds.EventCalendar).Payload);
        }

        [Fact]
        public async Task The_player_calendar_shows_the_week_ahead_in_one_language()
        {
            var world = await StartedWorld();
            world.Events.RunCalendarNow(_clock.UtcNow);
            var connection = new RecordingConnection();
            var session = await world.ConnectAsync("cal-view", connection, default, "cal-view");

            var calendar = Calendar(world, session, connection, "tr");
            Assert.Equal(Utc(2026, 10, 27).ToUnixTimeMilliseconds(), calendar.FromMs);
            Assert.True(calendar.ToMs >= Utc(2026, 11, 4).ToUnixTimeMilliseconds());

            // A Depth Race every day of the coming week: created ones and the ones still to be created, with the ids they will get.
            var now = _clock.UtcNow.ToUnixTimeMilliseconds();
            foreach (var day in EventCalendar.Week(now, 0))
                Assert.Contains(EventCalendar.OnDay(calendar.Entries, day), e => e.ScheduleId == DefaultSchedules.DepthRace && EventCalendar.StartsOn(e, day));
            Assert.Contains(calendar.Entries, e => e.Id == "depth-race:2026-11-03:w1" && e.Phases.Length == 6 && e.Stages.Length == 3);
            Assert.Contains(calendar.Entries, e => e.Id == "alliance-week:2026-11-02:w1" && e.Template == EventTemplates.Alliance);

            // One language only (Turkish here), phase titles included.
            Assert.All(calendar.Entries, e => Assert.Equal(new[] { "tr" }, e.Title.Select(t => t.Lang)));
            var race = calendar.Entries.First(e => e.ScheduleId == DefaultSchedules.DepthRace);
            Assert.Equal("Derinlik Yarışı", Assert.Single(race.Title).Text);
            Assert.All(race.Phases, p => Assert.Single(p.Title));

            // A language without texts falls back to English.
            var xx = Calendar(world, session, connection, "xx");
            Assert.Equal("Depth Race", xx.Entries.First(e => e.ScheduleId == DefaultSchedules.DepthRace).Title[0].Text);
        }

        [Fact]
        public async Task The_event_list_comes_in_the_players_language_once_it_is_known()
        {
            var world = await StartedWorld();
            world.Events.RunCalendarNow(_clock.UtcNow);
            var connection = new RecordingConnection();
            var session = await world.ConnectAsync("cal-lang", connection, default, "cal-lang");
            var race = world.Events.ListFor(session, _clock.UtcNow).Events.First(e => e.ScheduleId == DefaultSchedules.DepthRace);
            Assert.Equal(18, race.Title.Length); // not known yet: every language

            var prefs = new SetNotificationPrefsCommand { CommandId = Guid.NewGuid().ToString("N"), Prefs = new NotificationPrefsDto { Language = "de" } };
            world.HandleCommand(session, EnvelopeBuilder.WrapCommand(prefs, CommandType.SetNotificationPrefs, TestSupport.Serializer.Serialize(prefs)));
            Assert.Equal("de", session.Language);
            var list = world.Events.ListFor(session, _clock.UtcNow);
            race = list.Events.First(e => e.ScheduleId == DefaultSchedules.DepthRace);
            Assert.Equal("Tiefenrennen", Assert.Single(race.Title).Text);
            Assert.All(list.Events, e => Assert.Single(e.Title));
            Assert.All(race.Phases, p => Assert.Single(p.Title));

            // The calendar falls back to it when the request names no language.
            Assert.Equal("Tiefenrennen", Calendar(world, session, connection, string.Empty).Entries.First(e => e.ScheduleId == DefaultSchedules.DepthRace).Title[0].Text);
        }

        [Fact]
        public void Calendar_days_split_entries_by_local_day()
        {
            var now = Utc(2026, 10, 28, 22, 30).ToUnixTimeMilliseconds();
            var plus3 = 3 * 3_600_000L;
            var week = EventCalendar.Week(now, plus3);
            // 22:30 UTC is already Thursday 01:30 at UTC+3.
            Assert.Equal(Utc(2026, 10, 28, 21).ToUnixTimeMilliseconds(), week[0]);
            Assert.Equal((int)DayOfWeek.Thursday, EventCalendar.Weekday(week[0], plus3));
            Assert.Equal((int)DayOfWeek.Wednesday, EventCalendar.Weekday(week[6], plus3));

            var daily = new EventDto { Id = "a", StartsAtMs = Utc(2026, 10, 29).ToUnixTimeMilliseconds(), EndsAtMs = Utc(2026, 10, 30).ToUnixTimeMilliseconds() };
            // At UTC+3 a UTC day spans two local days, starting on the first.
            Assert.Single(EventCalendar.OnDay(new[] { daily }, week[0]));
            Assert.Single(EventCalendar.OnDay(new[] { daily }, week[1]));
            Assert.True(EventCalendar.StartsOn(daily, week[0]));
            Assert.False(EventCalendar.StartsOn(daily, week[1]));
            Assert.Empty(EventCalendar.OnDay(new[] { daily }, week[2]));

            // Negative offsets and times before 1970 floor correctly.
            Assert.Equal(Utc(1969, 12, 31, 5).ToUnixTimeMilliseconds(), EventCalendar.DayStart(Utc(1969, 12, 31, 12).ToUnixTimeMilliseconds(), -5 * 3_600_000L));
            Assert.Equal((int)DayOfWeek.Wednesday, EventCalendar.Weekday(Utc(1969, 12, 31, 12).ToUnixTimeMilliseconds(), 0));
        }

        [Fact]
        public async Task A_schedule_can_be_made_from_an_event_and_edited()
        {
            var world = await StartedWorld();
            world.Events.RunCalendarNow(_clock.UtcNow);
            var (error, schedule) = world.Events.ScheduleFromEvent("alliance-week:2026-10-26:w1", "test", _clock.UtcNow);
            Assert.Equal(string.Empty, error);
            Assert.False(schedule!.Enabled);
            Assert.Equal(new[] { (int)DayOfWeek.Monday }, schedule.Weekdays);
            Assert.Equal(0, schedule.StartMinute);
            Assert.Equal(168, schedule.DurationHours);
            Assert.Empty(schedule.Blueprint.Kingdoms);
            Assert.Equal(EventTemplates.Alliance, schedule.Blueprint.Template);

            schedule.Weekdays = new List<int> { (int)DayOfWeek.Saturday };
            schedule.StartMinute = 18 * 60;
            schedule.DurationHours = 30;
            schedule.Enabled = true;
            var (saveError, saved) = world.Events.SaveSchedule(schedule, "test", _clock.UtcNow);
            Assert.Equal(string.Empty, saveError);
            Assert.Equal(schedule.Id, saved!.Id);
            world.Events.RunCalendarNow(_clock.UtcNow.AddDays(2)); // Friday 09:30: Saturday 18:00 is not yet in the lead
            Assert.DoesNotContain(CalendarEvents(world), i => i.StartsWith(schedule.Id, StringComparison.Ordinal));
            world.Events.RunCalendarNow(Utc(2026, 10, 30, 18, 0));
            var made = world.Events.Get($"{schedule.Id}:2026-10-31:w1", _clock.UtcNow)!.Definition;
            Assert.Equal((Utc(2026, 10, 31, 18), Utc(2026, 11, 2, 0)), (made.StartsAt, made.EndsAt));

            Assert.Equal("UnknownEvent", world.Events.ScheduleFromEvent("nope", "test", _clock.UtcNow).Error);
            Assert.Equal("InvalidWeekdays", world.Events.SaveSchedule(new EventSchedule { Blueprint = schedule.Blueprint }, "test", _clock.UtcNow).Error);
        }
    }
}
