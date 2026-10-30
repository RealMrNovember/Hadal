using Hadal.Game.Persistence;
using Npgsql;

namespace Hadal.Game.Events
{
    /// <summary>Event definitions, player scores and alliance totals (docs/29).</summary>
    public interface IEventStore
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        Task<List<EventDefinition>> LoadEventsAsync(CancellationToken cancellationToken);

        /// <summary>Scores and alliance totals of the given events.</summary>
        Task<(List<EventScore> Scores, List<(string EventId, string AllianceId, long Points)> Alliances)> LoadProgressAsync(
            IReadOnlyCollection<string> eventIds, CancellationToken cancellationToken);

        Task SaveEventAsync(EventDefinition definition, CancellationToken cancellationToken);
        Task DeleteEventAsync(string eventId, CancellationToken cancellationToken);
        Task SaveScoresAsync(IReadOnlyList<EventScore> scores, CancellationToken cancellationToken);
        Task SaveAlliancePointsAsync(IReadOnlyList<(string EventId, string AllianceId, long Points)> totals, CancellationToken cancellationToken);

        /// <summary>Recurring schedules (EventSchedule).</summary>
        Task<List<EventSchedule>> LoadSchedulesAsync(CancellationToken cancellationToken);
        Task SaveScheduleAsync(EventSchedule schedule, CancellationToken cancellationToken);
        Task DeleteScheduleAsync(string scheduleId, CancellationToken cancellationToken);

        /// <summary>
        /// The calendar's ledger: every occurrence id it ever created (and the seed marker). Insert-only, so an occurrence
        /// the Overseer deleted is not created again. Returns the entries starting at or after <paramref name="since"/>
        /// and the markers.
        /// </summary>
        Task<HashSet<string>> LoadOccurrencesAsync(DateTimeOffset since, CancellationToken cancellationToken);
        Task SaveOccurrencesAsync(IReadOnlyList<(string OccurrenceId, string ScheduleId, DateTimeOffset StartsAt)> occurrences, CancellationToken cancellationToken);

        /// <summary>Per event and alliance: the Mobilization pool and the duel's day points (EventAllianceState as JSON).</summary>
        Task<List<(string EventId, string AllianceId, string Json)>> LoadAllianceStatesAsync(IReadOnlyCollection<string> eventIds, CancellationToken cancellationToken);
        Task SaveAllianceStatesAsync(IReadOnlyList<(string EventId, string AllianceId, string Json)> states, CancellationToken cancellationToken);
    }

    public sealed class InMemoryEventStore : IEventStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, string> _events = new(StringComparer.Ordinal);
        private readonly Dictionary<(string, string), (string AllianceId, long Points, string State, DateTimeOffset At)> _scores = new();
        private readonly Dictionary<(string, string), long> _alliances = new();
        private readonly Dictionary<string, string> _schedules = new(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTimeOffset> _occurrences = new(StringComparer.Ordinal);

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<List<EventDefinition>> LoadEventsAsync(CancellationToken cancellationToken)
        {
            lock (_gate)
                return Task.FromResult(_events.Values.Select(EventDefinition.FromJson).ToList());
        }

        public Task<(List<EventScore> Scores, List<(string EventId, string AllianceId, long Points)> Alliances)> LoadProgressAsync(
            IReadOnlyCollection<string> eventIds, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var ids = new HashSet<string>(eventIds, StringComparer.Ordinal);
                var scores = _scores.Where(p => ids.Contains(p.Key.Item1)).Select(p =>
                {
                    var score = new EventScore { EventId = p.Key.Item1, PlayerId = p.Key.Item2, AllianceId = p.Value.AllianceId, Points = p.Value.Points, UpdatedAt = p.Value.At };
                    score.ApplyState(p.Value.State);
                    return score;
                }).ToList();
                var alliances = _alliances.Where(p => ids.Contains(p.Key.Item1)).Select(p => (p.Key.Item1, p.Key.Item2, p.Value)).ToList();
                return Task.FromResult((scores, alliances));
            }
        }

        public Task SaveEventAsync(EventDefinition definition, CancellationToken cancellationToken)
        {
            lock (_gate)
                _events[definition.Id] = definition.ToJson();
            return Task.CompletedTask;
        }

        public Task DeleteEventAsync(string eventId, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _events.Remove(eventId);
                foreach (var key in _scores.Keys.Where(k => k.Item1 == eventId).ToList())
                    _scores.Remove(key);
                foreach (var key in _alliances.Keys.Where(k => k.Item1 == eventId).ToList())
                    _alliances.Remove(key);
            }

            return Task.CompletedTask;
        }

        public Task SaveScoresAsync(IReadOnlyList<EventScore> scores, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                foreach (var s in scores)
                    _scores[(s.EventId, s.PlayerId)] = (s.AllianceId, s.Points, s.StateJson(), s.UpdatedAt);
            }

            return Task.CompletedTask;
        }

        public Task SaveAlliancePointsAsync(IReadOnlyList<(string EventId, string AllianceId, long Points)> totals, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                foreach (var t in totals)
                    _alliances[(t.EventId, t.AllianceId)] = t.Points;
            }

            return Task.CompletedTask;
        }

        public Task<List<EventSchedule>> LoadSchedulesAsync(CancellationToken cancellationToken)
        {
            lock (_gate)
                return Task.FromResult(_schedules.Values.Select(EventSchedule.FromJson).ToList());
        }

        public Task SaveScheduleAsync(EventSchedule schedule, CancellationToken cancellationToken)
        {
            lock (_gate)
                _schedules[schedule.Id] = schedule.ToJson();
            return Task.CompletedTask;
        }

        public Task DeleteScheduleAsync(string scheduleId, CancellationToken cancellationToken)
        {
            lock (_gate)
                _schedules.Remove(scheduleId);
            return Task.CompletedTask;
        }

        public Task<HashSet<string>> LoadOccurrencesAsync(DateTimeOffset since, CancellationToken cancellationToken)
        {
            lock (_gate)
                return Task.FromResult(_occurrences.Where(p => p.Value >= since || p.Key.StartsWith("seed:", StringComparison.Ordinal))
                    .Select(p => p.Key).ToHashSet(StringComparer.Ordinal));
        }

        public Task SaveOccurrencesAsync(IReadOnlyList<(string OccurrenceId, string ScheduleId, DateTimeOffset StartsAt)> occurrences, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                foreach (var o in occurrences)
                    _occurrences.TryAdd(o.OccurrenceId, o.StartsAt);
            }

            return Task.CompletedTask;
        }

        private readonly Dictionary<(string, string), string> _allianceStates = new();

        public Task<List<(string EventId, string AllianceId, string Json)>> LoadAllianceStatesAsync(IReadOnlyCollection<string> eventIds, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var ids = new HashSet<string>(eventIds, StringComparer.Ordinal);
                return Task.FromResult(_allianceStates.Where(p => ids.Contains(p.Key.Item1) && _events.ContainsKey(p.Key.Item1))
                    .Select(p => (p.Key.Item1, p.Key.Item2, p.Value)).ToList());
            }
        }

        public Task SaveAllianceStatesAsync(IReadOnlyList<(string EventId, string AllianceId, string Json)> states, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                foreach (var s in states)
                    _allianceStates[(s.EventId, s.AllianceId)] = s.Json;
            }

            return Task.CompletedTask;
        }
    }

    public sealed class PostgresEventStore : IEventStore
    {
        private const string Schema = @"
CREATE SCHEMA IF NOT EXISTS hadal;

CREATE TABLE IF NOT EXISTS hadal.events (
    event_id    text        PRIMARY KEY,
    definition  jsonb       NOT NULL,
    updated_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS hadal.event_scores (
    event_id     text        NOT NULL REFERENCES hadal.events (event_id) ON DELETE CASCADE,
    player_id    text        NOT NULL,
    alliance_id  text        NOT NULL DEFAULT '',
    points       bigint      NOT NULL DEFAULT 0,
    state        jsonb       NOT NULL DEFAULT '{}',
    updated_at   timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (event_id, player_id)
);
CREATE INDEX IF NOT EXISTS event_scores_rank ON hadal.event_scores (event_id, points DESC);

CREATE TABLE IF NOT EXISTS hadal.event_alliance_points (
    event_id     text   NOT NULL REFERENCES hadal.events (event_id) ON DELETE CASCADE,
    alliance_id  text   NOT NULL,
    points       bigint NOT NULL DEFAULT 0,
    PRIMARY KEY (event_id, alliance_id)
);

-- The weekly calendar (docs/29 §2.2): recurring schedules, and the ledger of every occurrence the calendar created
-- (insert-only; the seed marker 'seed:defaults' lives there too).
CREATE TABLE IF NOT EXISTS hadal.event_schedules (
    schedule_id  text        PRIMARY KEY,
    definition   jsonb       NOT NULL,
    updated_at   timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS hadal.event_occurrences (
    occurrence_id  text        PRIMARY KEY,
    schedule_id    text        NOT NULL,
    starts_at      timestamptz NOT NULL,
    created_at     timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS event_occurrences_start ON hadal.event_occurrences (starts_at);

-- Per event and alliance: the Mobilization task pool and the Alliance Duel's points per day (docs/29 §4.3, §4.5).
CREATE TABLE IF NOT EXISTS hadal.event_alliance_state (
    event_id     text        NOT NULL REFERENCES hadal.events (event_id) ON DELETE CASCADE,
    alliance_id  text        NOT NULL,
    state        jsonb       NOT NULL DEFAULT '{}',
    updated_at   timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (event_id, alliance_id)
);";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresEventStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Task InitializeAsync(CancellationToken cancellationToken) => PostgresSchema.ApplyAsync(_dataSource, Schema, cancellationToken);

        public async Task<List<EventDefinition>> LoadEventsAsync(CancellationToken cancellationToken)
        {
            var list = new List<EventDefinition>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT definition::text FROM hadal.events", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                list.Add(EventDefinition.FromJson(reader.GetString(0)));
            return list;
        }

        public async Task<(List<EventScore> Scores, List<(string EventId, string AllianceId, long Points)> Alliances)> LoadProgressAsync(
            IReadOnlyCollection<string> eventIds, CancellationToken cancellationToken)
        {
            var scores = new List<EventScore>();
            var alliances = new List<(string, string, long)>();
            if (eventIds.Count == 0)
                return (scores, alliances);

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using (var command = new NpgsqlCommand(
                "SELECT event_id, player_id, alliance_id, points, state::text, updated_at FROM hadal.event_scores WHERE event_id = ANY(@ids)", connection))
            {
                command.Parameters.AddWithValue("ids", eventIds.ToArray());
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var score = new EventScore
                    {
                        EventId = reader.GetString(0), PlayerId = reader.GetString(1), AllianceId = reader.GetString(2), Points = reader.GetInt64(3),
                        UpdatedAt = reader.GetFieldValue<DateTimeOffset>(5)
                    };
                    score.ApplyState(reader.GetString(4));
                    scores.Add(score);
                }
            }

            await using (var command = new NpgsqlCommand("SELECT event_id, alliance_id, points FROM hadal.event_alliance_points WHERE event_id = ANY(@ids)", connection))
            {
                command.Parameters.AddWithValue("ids", eventIds.ToArray());
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    alliances.Add((reader.GetString(0), reader.GetString(1), reader.GetInt64(2)));
            }

            return (scores, alliances);
        }

        public async Task SaveEventAsync(EventDefinition definition, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
INSERT INTO hadal.events (event_id, definition, updated_at) VALUES (@id, @json::jsonb, now())
ON CONFLICT (event_id) DO UPDATE SET definition = EXCLUDED.definition, updated_at = now()", connection);
            command.Parameters.AddWithValue("id", definition.Id);
            command.Parameters.AddWithValue("json", definition.ToJson());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task DeleteEventAsync(string eventId, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM hadal.events WHERE event_id = @id", connection);
            command.Parameters.AddWithValue("id", eventId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task SaveScoresAsync(IReadOnlyList<EventScore> scores, CancellationToken cancellationToken)
        {
            if (scores.Count == 0)
                return;
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(connection);
            foreach (var s in scores)
            {
                var command = new NpgsqlBatchCommand(@"
INSERT INTO hadal.event_scores (event_id, player_id, alliance_id, points, state, updated_at) VALUES (@e, @p, @a, @n, @s::jsonb, @t)
ON CONFLICT (event_id, player_id) DO UPDATE SET alliance_id = EXCLUDED.alliance_id, points = EXCLUDED.points, state = EXCLUDED.state, updated_at = EXCLUDED.updated_at");
                command.Parameters.AddWithValue("e", s.EventId);
                command.Parameters.AddWithValue("p", s.PlayerId);
                command.Parameters.AddWithValue("a", s.AllianceId);
                command.Parameters.AddWithValue("n", s.Points);
                command.Parameters.AddWithValue("s", s.StateJson());
                command.Parameters.AddWithValue("t", s.UpdatedAt);
                batch.BatchCommands.Add(command);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task SaveAlliancePointsAsync(IReadOnlyList<(string EventId, string AllianceId, long Points)> totals, CancellationToken cancellationToken)
        {
            if (totals.Count == 0)
                return;
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(connection);
            foreach (var t in totals)
            {
                var command = new NpgsqlBatchCommand(@"
INSERT INTO hadal.event_alliance_points (event_id, alliance_id, points) VALUES (@e, @a, @n)
ON CONFLICT (event_id, alliance_id) DO UPDATE SET points = EXCLUDED.points");
                command.Parameters.AddWithValue("e", t.EventId);
                command.Parameters.AddWithValue("a", t.AllianceId);
                command.Parameters.AddWithValue("n", t.Points);
                batch.BatchCommands.Add(command);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<List<EventSchedule>> LoadSchedulesAsync(CancellationToken cancellationToken)
        {
            var list = new List<EventSchedule>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT definition::text FROM hadal.event_schedules", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                list.Add(EventSchedule.FromJson(reader.GetString(0)));
            return list;
        }

        public async Task SaveScheduleAsync(EventSchedule schedule, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
INSERT INTO hadal.event_schedules (schedule_id, definition, updated_at) VALUES (@id, @json::jsonb, now())
ON CONFLICT (schedule_id) DO UPDATE SET definition = EXCLUDED.definition, updated_at = now()", connection);
            command.Parameters.AddWithValue("id", schedule.Id);
            command.Parameters.AddWithValue("json", schedule.ToJson());
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task DeleteScheduleAsync(string scheduleId, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM hadal.event_schedules WHERE schedule_id = @id", connection);
            command.Parameters.AddWithValue("id", scheduleId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<HashSet<string>> LoadOccurrencesAsync(DateTimeOffset since, CancellationToken cancellationToken)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT occurrence_id FROM hadal.event_occurrences WHERE starts_at >= @since OR occurrence_id LIKE 'seed:%'", connection);
            command.Parameters.AddWithValue("since", since);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                set.Add(reader.GetString(0));
            return set;
        }

        public async Task SaveOccurrencesAsync(IReadOnlyList<(string OccurrenceId, string ScheduleId, DateTimeOffset StartsAt)> occurrences, CancellationToken cancellationToken)
        {
            if (occurrences.Count == 0)
                return;
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(connection);
            foreach (var o in occurrences)
            {
                var command = new NpgsqlBatchCommand(@"
INSERT INTO hadal.event_occurrences (occurrence_id, schedule_id, starts_at) VALUES (@o, @s, @t) ON CONFLICT (occurrence_id) DO NOTHING");
                command.Parameters.AddWithValue("o", o.OccurrenceId);
                command.Parameters.AddWithValue("s", o.ScheduleId);
                command.Parameters.AddWithValue("t", o.StartsAt);
                batch.BatchCommands.Add(command);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<List<(string EventId, string AllianceId, string Json)>> LoadAllianceStatesAsync(IReadOnlyCollection<string> eventIds, CancellationToken cancellationToken)
        {
            var list = new List<(string, string, string)>();
            if (eventIds.Count == 0)
                return list;
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT event_id, alliance_id, state::text FROM hadal.event_alliance_state WHERE event_id = ANY(@ids)", connection);
            command.Parameters.AddWithValue("ids", eventIds.ToArray());
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                list.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            return list;
        }

        public async Task SaveAllianceStatesAsync(IReadOnlyList<(string EventId, string AllianceId, string Json)> states, CancellationToken cancellationToken)
        {
            if (states.Count == 0)
                return;
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(connection);
            foreach (var s in states)
            {
                var command = new NpgsqlBatchCommand(@"
INSERT INTO hadal.event_alliance_state (event_id, alliance_id, state, updated_at) VALUES (@e, @a, @s::jsonb, now())
ON CONFLICT (event_id, alliance_id) DO UPDATE SET state = EXCLUDED.state, updated_at = now()");
                command.Parameters.AddWithValue("e", s.EventId);
                command.Parameters.AddWithValue("a", s.AllianceId);
                command.Parameters.AddWithValue("s", s.Json);
                batch.BatchCommands.Add(command);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
