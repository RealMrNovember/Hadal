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
    }

    public sealed class InMemoryEventStore : IEventStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, string> _events = new(StringComparer.Ordinal);
        private readonly Dictionary<(string, string), (string AllianceId, long Points, string State, DateTimeOffset At)> _scores = new();
        private readonly Dictionary<(string, string), long> _alliances = new();

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
    }
}
