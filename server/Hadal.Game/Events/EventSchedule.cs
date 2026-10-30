using System.Globalization;
using System.Text.Json;
using HADAL.Shared.Catalog;

namespace Hadal.Game.Events
{
    /// <summary>
    /// A recurring event (docs/29 §2.2 "Scheduler"): an event blueprint and a weekly rule. The calendar turns each
    /// occurrence into an ordinary event for every kingdom (and reactor bracket) ahead of its start, so the engine runs,
    /// scores and pays it like any other. Stored as one JSON document, like an event definition.
    /// </summary>
    public sealed class EventSchedule
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>A disabled schedule creates nothing; occurrences already created stay.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>What every occurrence is: template, texts, banner, actions, phases, stages, rank bands, claim days,
        /// reactor range, kingdoms (empty = every kingdom). Its id, status and window are ignored.</summary>
        public EventDefinition Blueprint { get; set; } = new();

        /// <summary>Start days (System.DayOfWeek: 0 = Sunday … 6 = Saturday), in server time (UTC).</summary>
        public List<int> Weekdays { get; set; } = new();

        /// <summary>Start time of day in minutes after 00:00 UTC.</summary>
        public int StartMinute { get; set; }

        public double DurationHours { get; set; } = 24;

        /// <summary>How long before its start an occurrence is created (players see it as upcoming from then).</summary>
        public double LeadHours { get; set; } = 24;

        /// <summary>Reactor brackets: one event per bracket and kingdom, each with its own ranking. Empty = one event with
        /// the blueprint's own range.</summary>
        public List<EventBracket> Brackets { get; set; } = new();

        /// <summary>Themed phases change places every day, so a player who plays at the same hour meets every theme.</summary>
        public bool RotatePhases { get; set; }

        public string Note { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public TimeSpan Duration => TimeSpan.FromHours(DurationHours);
        public TimeSpan Lead => TimeSpan.FromHours(LeadHours);

        public string ToJson() => JsonSerializer.Serialize(this, EventDefinition.Json);

        public static EventSchedule FromJson(string json) => JsonSerializer.Deserialize<EventSchedule>(json, EventDefinition.Json) ?? new EventSchedule();

        public EventSchedule Clone() => FromJson(ToJson());
    }

    public sealed class EventBracket
    {
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
    }

    /// <summary>The calendar's arithmetic: when a schedule's occurrences start, their ids and the events they become.</summary>
    public static class EventScheduleRules
    {
        public const double MaxDurationHours = 7 * 24;
        public const double MaxLeadHours = 72;
        public const int MaxBrackets = 5;

        /// <summary>The ledger key that marks the default schedules as seeded (so deleting them all does not bring them back).</summary>
        public const string SeedMarker = "seed:defaults";

        /// <summary>
        /// Starts of the occurrences that begin in [<paramref name="from"/>, <paramref name="to"/>), earliest first, in
        /// UTC. A month or year boundary is nothing special: days are walked one by one.
        /// </summary>
        public static List<DateTimeOffset> Starts(EventSchedule schedule, DateTimeOffset from, DateTimeOffset to)
        {
            var list = new List<DateTimeOffset>();
            if (to <= from || schedule.Weekdays.Count == 0)
                return list;
            var day = new DateTimeOffset(from.UtcDateTime.Date, TimeSpan.Zero);
            var minute = Math.Clamp(schedule.StartMinute, 0, 24 * 60 - 1);
            for (; day < to; day = day.AddDays(1))
            {
                if (!schedule.Weekdays.Contains((int)day.DayOfWeek))
                    continue;
                var start = day.AddMinutes(minute);
                if (start >= from && start < to)
                    list.Add(start);
            }

            return list;
        }

        /// <summary>
        /// Occurrences that are running or will start within the lead time at <paramref name="now"/>: the ones the
        /// calendar must have created.
        /// </summary>
        public static List<DateTimeOffset> Due(EventSchedule schedule, DateTimeOffset now) =>
            Starts(schedule, now - schedule.Duration, now + schedule.Lead + TimeSpan.FromTicks(1)).Where(s => s + schedule.Duration > now).ToList();

        /// <summary>
        /// "depth-race:2026-11-02:w1", and ":b2" for the second reactor bracket: the same occurrence always gets the same
        /// id, which is what keeps the calendar from creating it twice (also across restarts).
        /// </summary>
        public static string OccurrenceId(string scheduleId, DateTimeOffset start, string kingdom, int bracket) =>
            $"{scheduleId}:{start.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}:{kingdom}" + (bracket >= 0 ? $":b{bracket + 1}" : string.Empty);

        /// <summary>The bracket indices to create: -1 alone when the schedule has none.</summary>
        public static IEnumerable<int> BracketIndices(EventSchedule schedule) =>
            schedule.Brackets.Count == 0 ? new[] { -1 } : Enumerable.Range(0, schedule.Brackets.Count);

        /// <summary>The kingdoms an occurrence is created for: the blueprint's list, or every kingdom.</summary>
        public static IEnumerable<string> Kingdoms(EventSchedule schedule, IEnumerable<string> all) =>
            schedule.Blueprint.Kingdoms.Count > 0 ? schedule.Blueprint.Kingdoms : all;

        /// <summary>The event one occurrence becomes, for one kingdom and bracket: published, nothing earned yet.</summary>
        public static EventDefinition Materialize(EventSchedule schedule, DateTimeOffset start, string kingdom, int bracket, DateTimeOffset now)
        {
            var def = schedule.Blueprint.Clone();
            def.Id = OccurrenceId(schedule.Id, start, kingdom, bracket);
            def.ScheduleId = schedule.Id;
            def.Status = EventStatus.Published;
            def.StartsAt = start;
            def.EndsAt = start + schedule.Duration;
            def.Kingdoms = new List<string> { kingdom };
            if (bracket >= 0 && bracket < schedule.Brackets.Count)
            {
                def.MinLevel = schedule.Brackets[bracket].MinLevel;
                def.MaxLevel = schedule.Brackets[bracket].MaxLevel;
            }

            def.PaidBosses = new List<int>();
            def.BossProgress = new List<EventBossProgress>();
            def.CreatedBy = EventService.CalendarAuthor;
            def.CreatedAt = now;
            def.UpdatedAt = now;
            if (schedule.RotatePhases && def.Phases.Count > 1)
                Rotate(def.Phases, (int)(start.UtcDateTime.Date - DateTime.UnixEpoch).TotalDays);
            return def;
        }

        /// <summary>Moves each phase's theme (title and actions) <paramref name="shift"/> slots on; the slots keep their hours.</summary>
        internal static void Rotate(List<EventPhaseConfig> phases, int shift)
        {
            var slots = phases.OrderBy(p => p.StartHour).ToList();
            var n = slots.Count;
            var k = ((shift % n) + n) % n;
            if (k == 0)
                return;
            var themes = slots.Select(p => (p.Title, p.Actions)).ToList();
            for (var i = 0; i < n; i++)
            {
                var (title, actions) = themes[(i + k) % n];
                slots[i].Title = title;
                slots[i].Actions = actions;
            }
        }

        /// <summary>Checks a schedule; returns an error code ("" when fine). The blueprint is checked as an event.</summary>
        public static string Validate(EventSchedule schedule, GameCatalog catalog, DateTimeOffset now)
        {
            if (schedule.Weekdays == null || schedule.Weekdays.Count == 0 || schedule.Weekdays.Any(d => d is < 0 or > 6) || schedule.Weekdays.Distinct().Count() != schedule.Weekdays.Count)
                return "InvalidWeekdays";
            if (schedule.StartMinute is < 0 or >= 24 * 60)
                return "InvalidStartTime";
            if (double.IsNaN(schedule.DurationHours) || schedule.DurationHours < 1 || schedule.DurationHours > MaxDurationHours)
                return "InvalidDuration";
            if (double.IsNaN(schedule.LeadHours) || schedule.LeadHours < 0 || schedule.LeadHours > MaxLeadHours)
                return "InvalidLead";
            schedule.Brackets ??= new List<EventBracket>();
            if (schedule.Brackets.Count > MaxBrackets || schedule.Brackets.Any(b => b == null || b.MinLevel < 0 || b.MaxLevel < 0 || (b.MaxLevel > 0 && b.MaxLevel < b.MinLevel)))
                return "InvalidBrackets";
            if (schedule.Blueprint == null)
                return "InvalidBlueprint";
            if (schedule.Note?.Length > 200)
                return "InvalidNote";

            // The blueprint as one of its own occurrences (a login event's days must fit the duration, and so on).
            var sample = schedule.Blueprint.Clone();
            sample.StartsAt = now;
            sample.EndsAt = now + schedule.Duration;
            sample.Kingdoms = sample.Kingdoms.Take(1).ToList();
            return EventService.Validate(sample, catalog);
        }
    }
}
