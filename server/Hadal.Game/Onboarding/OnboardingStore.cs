using Hadal.Game.Persistence;
using Npgsql;

namespace Hadal.Game.Onboarding
{
    /// <summary>
    /// One step of a player's first-session guide (docs/34): <see cref="Step"/> 0 = the guide started, n = step n
    /// claimed, -1 = an existing account skipped it. <see cref="Seconds"/> counts from the start. Cohort is set on the
    /// start row: "new" (a fresh account) or "existing" (an early account that was enrolled).
    /// </summary>
    public sealed record OnboardingEvent(string PlayerId, int Step, string StepId, string Cohort, DateTimeOffset At, int Seconds);

    /// <summary>The guide's funnel log. Server-side only; a player's step is written once (replays change nothing).</summary>
    public interface IOnboardingStore
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        Task RecordAsync(OnboardingEvent entry, CancellationToken cancellationToken);

        /// <summary>Every event of the players whose guide started at or after <paramref name="since"/> (optionally one cohort), plus skips.</summary>
        Task<List<OnboardingEvent>> LoadAsync(DateTimeOffset since, string? cohort, CancellationToken cancellationToken);
    }

    public sealed class InMemoryOnboardingStore : IOnboardingStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<(string, int), OnboardingEvent> _events = new();

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RecordAsync(OnboardingEvent entry, CancellationToken cancellationToken)
        {
            lock (_gate)
                _events.TryAdd((entry.PlayerId, entry.Step), entry);
            return Task.CompletedTask;
        }

        public Task<List<OnboardingEvent>> LoadAsync(DateTimeOffset since, string? cohort, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var players = _events.Values
                    .Where(e => (e.Step == 0 && e.At >= since && (string.IsNullOrEmpty(cohort) || e.Cohort == cohort)) || (e.Step < 0 && e.At >= since))
                    .Select(e => e.PlayerId).ToHashSet(StringComparer.Ordinal);
                return Task.FromResult(_events.Values.Where(e => players.Contains(e.PlayerId)).OrderBy(e => e.At).ToList());
            }
        }

        public IReadOnlyCollection<OnboardingEvent> All
        {
            get
            {
                lock (_gate)
                    return _events.Values.ToList();
            }
        }
    }

    public sealed class PostgresOnboardingStore : IOnboardingStore
    {
        private const string Schema = @"
CREATE SCHEMA IF NOT EXISTS hadal;

CREATE TABLE IF NOT EXISTS hadal.onboarding_events (
    player_id  text        NOT NULL,
    step       integer     NOT NULL,
    step_id    text        NOT NULL,
    cohort     text        NOT NULL DEFAULT '',
    at         timestamptz NOT NULL,
    seconds    integer     NOT NULL DEFAULT 0,
    PRIMARY KEY (player_id, step)
);
CREATE INDEX IF NOT EXISTS onboarding_events_start ON hadal.onboarding_events (at) WHERE step <= 0;";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresOnboardingStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Task InitializeAsync(CancellationToken cancellationToken) => PostgresSchema.ApplyAsync(_dataSource, Schema, cancellationToken);

        public async Task RecordAsync(OnboardingEvent entry, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
INSERT INTO hadal.onboarding_events (player_id, step, step_id, cohort, at, seconds)
VALUES (@p, @s, @id, @c, @at, @sec) ON CONFLICT (player_id, step) DO NOTHING", connection);
            command.Parameters.AddWithValue("p", entry.PlayerId);
            command.Parameters.AddWithValue("s", entry.Step);
            command.Parameters.AddWithValue("id", entry.StepId);
            command.Parameters.AddWithValue("c", entry.Cohort);
            command.Parameters.AddWithValue("at", entry.At);
            command.Parameters.AddWithValue("sec", entry.Seconds);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<List<OnboardingEvent>> LoadAsync(DateTimeOffset since, string? cohort, CancellationToken cancellationToken)
        {
            var list = new List<OnboardingEvent>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
SELECT e.player_id, e.step, e.step_id, e.cohort, e.at, e.seconds
FROM hadal.onboarding_events e
WHERE e.player_id IN (
    SELECT s.player_id FROM hadal.onboarding_events s
    WHERE s.at >= @since AND ((s.step = 0 AND (@c = '' OR s.cohort = @c)) OR s.step < 0))
ORDER BY e.at
LIMIT 500000", connection);
            command.Parameters.AddWithValue("since", since);
            command.Parameters.AddWithValue("c", cohort ?? string.Empty);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new OnboardingEvent(reader.GetString(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3),
                    reader.GetFieldValue<DateTimeOffset>(4), reader.GetInt32(5)));
            }

            return list;
        }
    }
}
