using HADAL.Shared.DTOs;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging;

namespace Hadal.Game.Events
{
    /// <summary>
    /// The weekly calendar (docs/29 §2.2 "Scheduler", roadmap §7): recurring schedules create their occurrences as
    /// ordinary published events, one per kingdom (and reactor bracket), up to <see cref="EventSchedule.LeadHours"/>
    /// before they start. An occurrence's id is fixed by its schedule, day and kingdom, and every id created is written
    /// to an insert-only ledger, so a restart, a second pass or a deleted event never creates one twice.
    /// </summary>
    public sealed partial class EventService
    {
        /// <summary>The author of calendar-made events (EventDefinition.CreatedBy).</summary>
        public const string CalendarAuthor = "calendar";

        private static readonly TimeSpan CalendarEvery = TimeSpan.FromSeconds(30);

        /// <summary>Ledger entries older than this are not loaded: no occurrence that old can still be due.</summary>
        private static readonly TimeSpan LedgerMemory = TimeSpan.FromDays(14);

        private readonly Dictionary<string, EventSchedule> _schedules = new(StringComparer.Ordinal);
        private readonly HashSet<string> _ledger = new(StringComparer.Ordinal);
        private readonly HashSet<string> _invalidWarned = new(StringComparer.Ordinal);
        private DateTimeOffset _nextCalendarRun;

        /// <summary>Every kingdom's id (set by the world): occurrences are created for each one.</summary>
        public Func<IReadOnlyList<string>>? KingdomIds { get; set; }

        private IReadOnlyList<string> AllKingdoms()
        {
            var ids = KingdomIds?.Invoke();
            return ids is { Count: > 0 } ? ids : new[] { AllianceLandRules.DefaultWorld };
        }

        /// <summary>Loads the schedules and the ledger; seeds the default schedules once, on an empty calendar.</summary>
        private async Task InitializeCalendarAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            var schedules = await _store.LoadSchedulesAsync(cancellationToken);
            var ledger = await _store.LoadOccurrencesAsync(now - LedgerMemory, cancellationToken);
            if (schedules.Count == 0 && !ledger.Contains(EventScheduleRules.SeedMarker))
            {
                schedules = DefaultSchedules.All(now);
                foreach (var schedule in schedules)
                    await _store.SaveScheduleAsync(schedule, cancellationToken);
                var markers = new List<(string, string, DateTimeOffset)> { (EventScheduleRules.SeedMarker, string.Empty, now) };
                markers.AddRange(DefaultSchedules.Added(now).Select(s => (EventScheduleRules.SeedMarkerOf(s.Id), s.Id, now)));
                await _store.SaveOccurrencesAsync(markers, cancellationToken);
                ledger.UnionWith(markers.Select(m => m.Item1));
                _logger.LogInformation("Event calendar: {Count} default schedules seeded.", schedules.Count);
            }
            else
            {
                // Defaults that came after the first seed are added once each, next to whatever the calendar holds: an
                // existing schedule is never touched, and one the Overseer deleted (its marker is there) never returns.
                foreach (var added in DefaultSchedules.Added(now))
                {
                    var marker = EventScheduleRules.SeedMarkerOf(added.Id);
                    if (ledger.Contains(marker))
                        continue;
                    if (schedules.All(s => s.Id != added.Id))
                    {
                        await _store.SaveScheduleAsync(added, cancellationToken);
                        schedules.Add(added);
                        _logger.LogInformation("Event calendar: default schedule {Id} added.", added.Id);
                    }

                    await _store.SaveOccurrencesAsync(new[] { (marker, added.Id, now) }, cancellationToken);
                    ledger.Add(marker);
                }
            }

            lock (_gate)
            {
                foreach (var schedule in schedules)
                    _schedules[schedule.Id] = schedule;
                _ledger.UnionWith(ledger);
            }

            _logger.LogInformation("Event calendar: {Schedules} schedules ({Enabled} enabled).", schedules.Count, schedules.Count(s => s.Enabled));
        }

        /// <summary>Creates every due occurrence now (the tick does this twice a minute). Returns how many events were made.</summary>
        public int RunCalendarNow(DateTimeOffset now)
        {
            lock (_gate)
            {
                var created = RunCalendar(now);
                if (created > 0)
                {
                    foreach (var session in _online.Values)
                        PushTo(session, now);
                }

                return created;
            }
        }

        /// <summary>Caller holds the lock.</summary>
        private int RunCalendar(DateTimeOffset now)
        {
            var created = 0;
            var kingdoms = AllKingdoms();
            foreach (var schedule in _schedules.Values.Where(s => s.Enabled).OrderBy(s => s.Id, StringComparer.Ordinal))
            {
                foreach (var start in EventScheduleRules.Due(schedule, now))
                    created += CreateOccurrence(schedule, start, now, respectLedger: true, kingdoms).Count;
            }

            return created;
        }

        /// <summary>
        /// Creates one occurrence for every kingdom and bracket that has no event yet. With <paramref name="respectLedger"/>
        /// an id created before (and since deleted by the Overseer) is skipped. Caller holds the lock.
        /// </summary>
        private List<string> CreateOccurrence(EventSchedule schedule, DateTimeOffset start, DateTimeOffset now, bool respectLedger, IReadOnlyList<string> kingdoms)
        {
            var created = new List<string>();
            var ledger = new List<(string, string, DateTimeOffset)>();
            foreach (var kingdom in EventScheduleRules.Kingdoms(schedule, kingdoms))
            {
                foreach (var bracket in EventScheduleRules.BracketIndices(schedule))
                {
                    var id = EventScheduleRules.OccurrenceId(schedule.Id, start, kingdom, bracket);
                    if (_events.ContainsKey(id) || (respectLedger && _ledger.Contains(id)))
                        continue;
                    var def = EventScheduleRules.Materialize(schedule, start, kingdom, bracket, now);
                    var error = Validate(def, Catalog);
                    if (error.Length > 0)
                    {
                        if (_invalidWarned.Add(id))
                            _logger.LogWarning("Event calendar: {Id} not created ({Error}).", id, error);
                        continue;
                    }

                    Normalise(def);
                    _events[id] = new Live { Def = def, Shape = Shape(def), Phase = PhaseOf(def, now) };
                    _ledger.Add(id);
                    ledger.Add((id, schedule.Id, start));
                    created.Add(id);
                    var copy = def.Clone();
                    Write(ct => _store.SaveEventAsync(copy, ct));
                }
            }

            if (ledger.Count > 0)
            {
                Write(ct => _store.SaveOccurrencesAsync(ledger, ct));
                RefreshBosses(now);
                _logger.LogInformation("Event calendar: {Schedule} on {Start:u} created for {Count} kingdom(s).", schedule.Id, start, ledger.Count);
            }

            return created;
        }

        // --- Player calendar ----------------------------------------------------------------------

        /// <summary>
        /// What one player's calendar shows: every event they can see that overlaps yesterday … a week from today (UTC), and
        /// the coming occurrences of enabled schedules that are not created yet, as the event each will become. Texts only in
        /// <paramref name="lang"/> (with the usual fallback). Caller holds the lock.
        /// </summary>
        internal EventCalendarDto CalendarFor(PlayerSession session, string? lang, DateTimeOffset now)
        {
            lang = !string.IsNullOrWhiteSpace(lang) ? lang.Trim() : session.Language is { Length: > 0 } known ? known : "en";
            var from = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero).AddDays(-1);
            var to = from.AddDays(EventCalendar.Days + 2);
            var entries = new List<EventDto>();
            var mine = _schedules.Values.Where(s => s.Enabled && EventScheduleRules.Kingdoms(s, AllKingdoms()).Contains(session.KingdomId)).ToList();

            foreach (var live in _events.Values)
            {
                if (live.Def.StartsAt >= to || live.Def.EndsAt <= from || !Sees(live.Def, session))
                    continue;
                entries.Add(InLanguage(live.Shape, lang));
            }

            foreach (var schedule in mine)
            {
                foreach (var start in EventScheduleRules.Starts(schedule, from - schedule.Duration, to))
                {
                    if (start + schedule.Duration <= from)
                        continue;
                    foreach (var bracket in EventScheduleRules.BracketIndices(schedule))
                    {
                        var id = EventScheduleRules.OccurrenceId(schedule.Id, start, session.KingdomId, bracket);
                        if (_events.ContainsKey(id) || _ledger.Contains(id))
                            continue;
                        var def = EventScheduleRules.Materialize(schedule, start, session.KingdomId, bracket, now);
                        if (Sees(def, session))
                            entries.Add(InLanguage(Shape(def), lang));
                    }
                }
            }

            return new EventCalendarDto
            {
                Entries = entries.OrderBy(e => e.StartsAtMs).ThenBy(e => e.Id, StringComparer.Ordinal).Take(80).ToArray(),
                ServerNowMs = now.ToUnixTimeMilliseconds(),
                FromMs = from.ToUnixTimeMilliseconds(),
                ToMs = to.ToUnixTimeMilliseconds()
            };
        }

        /// <summary>A copy of an event's shape with each text in one language only.</summary>
        private static EventDto InLanguage(EventDto shape, string lang)
        {
            LocalTextDto[] One(LocalTextDto[] texts)
            {
                var text = EventRules.Text(texts, lang);
                return text.Length == 0 ? Array.Empty<LocalTextDto>() : new[] { new LocalTextDto { Lang = lang, Text = text } };
            }

            var dto = Copy(shape);
            dto.Title = One(shape.Title);
            dto.Description = One(shape.Description);
            dto.Phases = shape.Phases.Select(p => new EventPhaseDto { Title = One(p.Title), StartMs = p.StartMs, EndMs = p.EndMs, Actions = p.Actions }).ToArray();
            return dto;
        }

        // --- Overseer -----------------------------------------------------------------------------

        public List<EventSchedule> Schedules()
        {
            lock (_gate)
                return _schedules.Values.OrderBy(s => s.CreatedAt).ThenBy(s => s.Id, StringComparer.Ordinal).Select(s => s.Clone()).ToList();
        }

        public EventSchedule? Schedule(string id)
        {
            lock (_gate)
                return _schedules.TryGetValue(id ?? string.Empty, out var s) ? s.Clone() : null;
        }

        /// <summary>
        /// Creates or replaces a schedule. Occurrences already created keep what they were made with; the change applies to
        /// the ones created from now on. Returns an error code, or the saved schedule.
        /// </summary>
        public (string Error, EventSchedule? Saved) SaveSchedule(EventSchedule input, string by, DateTimeOffset now)
        {
            if (input == null)
                return ("InvalidSchedule", null);
            var schedule = input.Clone();
            schedule.Blueprint ??= new EventDefinition();
            schedule.Blueprint.Kingdoms = (schedule.Blueprint.Kingdoms ?? new List<string>()).Select(k => k.Trim()).Where(k => k.Length > 0)
                .Distinct(StringComparer.Ordinal).ToList();
            if (schedule.Blueprint.Kingdoms.Any(k => k.Length > 12))
                return ("InvalidKingdoms", null);
            schedule.Weekdays = (schedule.Weekdays ?? new List<int>()).OrderBy(d => d).ToList();
            var error = EventScheduleRules.Validate(schedule, Catalog, now);
            if (error.Length > 0)
                return (error, null);

            // The blueprint is a template, not an event: no id, window, status, scores or boss progress of its own.
            var blueprint = schedule.Blueprint;
            Normalise(blueprint);
            blueprint.Id = string.Empty;
            blueprint.ScheduleId = string.Empty;
            blueprint.Status = EventStatus.Draft;
            blueprint.StartsAt = blueprint.EndsAt = default;
            ResetRuntime(blueprint);

            lock (_gate)
            {
                if (string.IsNullOrWhiteSpace(schedule.Id) || !_schedules.TryGetValue(schedule.Id, out var existing))
                {
                    // A new schedule may bring a readable id ("weekend-hunt"); it becomes part of every occurrence id.
                    if (string.IsNullOrWhiteSpace(schedule.Id))
                        schedule.Id = "sc" + Guid.NewGuid().ToString("N")[..10];
                    else if (!ValidScheduleId(schedule.Id))
                        return ("InvalidId", null);
                    schedule.CreatedBy = by;
                    schedule.CreatedAt = now;
                }
                else
                {
                    schedule.CreatedBy = existing.CreatedBy;
                    schedule.CreatedAt = existing.CreatedAt;
                }

                schedule.UpdatedAt = now;
                _schedules[schedule.Id] = schedule;
                var copy = schedule.Clone();
                Write(ct => _store.SaveScheduleAsync(copy, ct));
                _nextCalendarRun = default; // the next tick creates what is now due
                return (string.Empty, schedule.Clone());
            }
        }

        private static bool ValidScheduleId(string id) =>
            id.Length is >= 3 and <= 32 && id.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

        public string SetScheduleEnabled(string id, bool enabled, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (!_schedules.TryGetValue(id ?? string.Empty, out var schedule))
                    return "UnknownSchedule";
                schedule.Enabled = enabled;
                schedule.UpdatedAt = now;
                var copy = schedule.Clone();
                Write(ct => _store.SaveScheduleAsync(copy, ct));
                _nextCalendarRun = default;
                return string.Empty;
            }
        }

        /// <summary>Removes a schedule; the events it created stay.</summary>
        public string DeleteSchedule(string id)
        {
            lock (_gate)
            {
                if (!_schedules.Remove(id ?? string.Empty))
                    return "UnknownSchedule";
                Write(ct => _store.DeleteScheduleAsync(id!, ct));
                return string.Empty;
            }
        }

        /// <summary>
        /// A new, disabled schedule with an existing event as its blueprint: on the event's weekday and start time (UTC),
        /// for the event's length (at most a week).
        /// </summary>
        public (string Error, EventSchedule? Saved) ScheduleFromEvent(string eventId, string by, DateTimeOffset now)
        {
            EventDefinition source;
            lock (_gate)
            {
                if (!_events.TryGetValue(eventId ?? string.Empty, out var live))
                    return ("UnknownEvent", null);
                source = live.Def.Clone();
            }

            var start = source.StartsAt.ToUniversalTime();
            var hours = Math.Clamp(Math.Round((source.EndsAt - source.StartsAt).TotalHours, 2), 1, EventScheduleRules.MaxDurationHours);
            var schedule = new EventSchedule
            {
                Enabled = false,
                Blueprint = source,
                Weekdays = new List<int> { (int)start.DayOfWeek },
                StartMinute = start.Hour * 60 + start.Minute,
                DurationHours = hours,
                LeadHours = 24,
                Note = "from " + source.Id
            };
            // A calendar-made event's kingdom was set by the calendar, not by the Overseer.
            if (!string.IsNullOrEmpty(source.ScheduleId))
                schedule.Blueprint.Kingdoms = new List<string>();
            schedule.Blueprint.TestAccounts = new List<string>();
            return SaveSchedule(schedule, by, now);
        }

        /// <summary>
        /// Creates the schedule's next occurrence now, whatever its lead time (and even when the schedule is disabled):
        /// the earliest one not over yet that some kingdom still lacks. Never makes an event that exists.
        /// </summary>
        public (string Error, List<string> Created) CreateNextOccurrence(string id, DateTimeOffset now)
        {
            lock (_gate)
            {
                if (!_schedules.TryGetValue(id ?? string.Empty, out var schedule))
                    return ("UnknownSchedule", new List<string>());
                var kingdoms = AllKingdoms();
                foreach (var start in EventScheduleRules.Starts(schedule, now - schedule.Duration, now.AddDays(8)))
                {
                    if (start + schedule.Duration <= now)
                        continue;
                    var missing = EventScheduleRules.Kingdoms(schedule, kingdoms)
                        .SelectMany(k => EventScheduleRules.BracketIndices(schedule).Select(b => EventScheduleRules.OccurrenceId(schedule.Id, start, k, b)))
                        .Any(o => !_events.ContainsKey(o));
                    if (!missing)
                        continue;
                    var created = CreateOccurrence(schedule, start, now, respectLedger: false, kingdoms);
                    if (created.Count == 0)
                        return ("InvalidBlueprint", created);
                    foreach (var session in _online.Values)
                        PushTo(session, now);
                    return (string.Empty, created);
                }

                return ("NoOccurrence", new List<string>());
            }
        }

        /// <summary>One line of the Overseer's week grid.</summary>
        public sealed record CalendarEntry(
            string Id, string ScheduleId, string Template, Dictionary<string, string> Title, string Banner, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
            string Kingdom, int MinLevel, int MaxLevel, string State);

        /// <summary>
        /// Every event and every occurrence to come in [<paramref name="from"/>, <paramref name="to"/>), per kingdom. State:
        /// an event's phase or status; "planned" (not created yet), "disabled" (its schedule is off) or "skipped" (created
        /// once and deleted since).
        /// </summary>
        public List<CalendarEntry> Calendar(DateTimeOffset from, DateTimeOffset to, DateTimeOffset now)
        {
            lock (_gate)
            {
                var list = new List<CalendarEntry>();
                var all = AllKingdoms();
                foreach (var live in _events.Values)
                {
                    var def = live.Def;
                    if (def.StartsAt >= to || def.EndsAt <= from)
                        continue;
                    var state = def.Status != EventStatus.Published ? def.Status : PhaseOf(def, now).ToString().ToLowerInvariant();
                    var kingdoms = def.Kingdoms.Count > 0 ? def.Kingdoms : new List<string> { string.Empty };
                    foreach (var kingdom in kingdoms)
                        list.Add(new CalendarEntry(def.Id, def.ScheduleId ?? string.Empty, def.Template, def.Title, def.Banner, def.StartsAt, def.EndsAt, kingdom, def.MinLevel, def.MaxLevel, state));
                }

                foreach (var schedule in _schedules.Values)
                {
                    foreach (var start in EventScheduleRules.Starts(schedule, from - schedule.Duration, to))
                    {
                        if (start + schedule.Duration <= from)
                            continue;
                        foreach (var kingdom in EventScheduleRules.Kingdoms(schedule, all))
                        {
                            foreach (var bracket in EventScheduleRules.BracketIndices(schedule))
                            {
                                var id = EventScheduleRules.OccurrenceId(schedule.Id, start, kingdom, bracket);
                                if (_events.ContainsKey(id))
                                    continue;
                                var state = _ledger.Contains(id) ? "skipped" : schedule.Enabled ? "planned" : "disabled";
                                var b = bracket >= 0 ? schedule.Brackets[bracket] : new EventBracket { MinLevel = schedule.Blueprint.MinLevel, MaxLevel = schedule.Blueprint.MaxLevel };
                                list.Add(new CalendarEntry(id, schedule.Id, schedule.Blueprint.Template, schedule.Blueprint.Title, schedule.Blueprint.Banner, start,
                                    start + schedule.Duration, kingdom, b.MinLevel, b.MaxLevel, state));
                            }
                        }
                    }
                }

                return list.OrderBy(e => e.StartsAt).ThenBy(e => e.Kingdom, StringComparer.Ordinal).ThenBy(e => e.Id, StringComparer.Ordinal).ToList();
            }
        }
    }
}
