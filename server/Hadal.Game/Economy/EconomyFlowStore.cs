using Microsoft.Extensions.Logging;
using Npgsql;
using Hadal.Game.Persistence;

namespace Hadal.Game.Economy
{
    /// <summary>
    /// One signed wallet movement of a player (docs/30 economy budget): positive banks, negative spends.
    /// <see cref="Source"/> names where/why ("reward", "capsule_open", "production", ...). Amount is the exact
    /// clamped delta, so faucet/sink totals in the Overseer reflect what players actually got.
    /// </summary>
    public sealed record EconomyFlowEvent(string PlayerId, string Source, string Resource, long Amount, DateTimeOffset At);

    /// <summary>The economy flow log. Server-side only; a failed write is logged, never retried into the session.</summary>
    public interface IEconomyFlowStore
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        Task RecordAsync(EconomyFlowEvent entry, CancellationToken cancellationToken);

        /// <summary>Every event at or after <paramref name="since"/>.</summary>
        Task<List<EconomyFlowEvent>> LoadAsync(DateTimeOffset since, CancellationToken cancellationToken);
    }

    public sealed class InMemoryEconomyFlowStore : IEconomyFlowStore
    {
        private readonly object _gate = new();
        private readonly List<EconomyFlowEvent> _events = new();

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RecordAsync(EconomyFlowEvent entry, CancellationToken cancellationToken)
        {
            lock (_gate)
                _events.Add(entry);
            return Task.CompletedTask;
        }

        public Task<List<EconomyFlowEvent>> LoadAsync(DateTimeOffset since, CancellationToken cancellationToken)
        {
            lock (_gate)
                return Task.FromResult(_events.Where(e => e.At >= since).OrderBy(e => e.At).ToList());
        }

        public IReadOnlyCollection<EconomyFlowEvent> All
        {
            get
            {
                lock (_gate)
                    return _events.ToList();
            }
        }
    }

    public sealed class PostgresEconomyFlowStore : IEconomyFlowStore
    {
        private const string Schema = @"
CREATE SCHEMA IF NOT EXISTS hadal;

CREATE TABLE IF NOT EXISTS hadal.economy_events (
    player_id  text        NOT NULL,
    source     text        NOT NULL,
    resource   text        NOT NULL,
    amount     bigint      NOT NULL,
    at         timestamptz NOT NULL
);
CREATE INDEX IF NOT EXISTS economy_events_at ON hadal.economy_events (at);";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresEconomyFlowStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Task InitializeAsync(CancellationToken cancellationToken) => PostgresSchema.ApplyAsync(_dataSource, Schema, cancellationToken);

        public async Task RecordAsync(EconomyFlowEvent entry, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
INSERT INTO hadal.economy_events (player_id, source, resource, amount, at)
VALUES (@p, @s, @r, @a, @at)", connection);
            command.Parameters.AddWithValue("p", entry.PlayerId);
            command.Parameters.AddWithValue("s", entry.Source);
            command.Parameters.AddWithValue("r", entry.Resource);
            command.Parameters.AddWithValue("a", entry.Amount);
            command.Parameters.AddWithValue("at", entry.At);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<List<EconomyFlowEvent>> LoadAsync(DateTimeOffset since, CancellationToken cancellationToken)
        {
            var list = new List<EconomyFlowEvent>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
SELECT player_id, source, resource, amount, at
FROM hadal.economy_events
WHERE at >= @since
ORDER BY at
LIMIT 500000", connection);
            command.Parameters.AddWithValue("since", since);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                list.Add(new EconomyFlowEvent(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                    reader.GetInt64(3), reader.GetFieldValue<DateTimeOffset>(4)));
            return list;
        }
    }

    /// <summary>Writes economy events without holding up the game: a failed write is logged, never retried into the session.</summary>
    public sealed class EconomyFlowLog
    {
        private readonly ILogger _logger;

        public EconomyFlowLog(IEconomyFlowStore store, ILogger logger)
        {
            Store = store;
            _logger = logger;
        }

        public IEconomyFlowStore Store { get; }

        public Task InitializeAsync(CancellationToken cancellationToken) => Store.InitializeAsync(cancellationToken);

        public void Record(EconomyFlowEvent entry)
        {
            if (entry.Amount == 0)
                return;

            Task write;
            try
            {
                write = Store.RecordAsync(entry, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Economy event {Source}/{Resource} of {PlayerId} not written.", entry.Source, entry.Resource, entry.PlayerId);
                return;
            }

            if (!write.IsCompleted)
                _ = write.ContinueWith(t => _logger.LogWarning(t.Exception, "Economy event {Source}/{Resource} of {PlayerId} not written.", entry.Source, entry.Resource, entry.PlayerId),
                    TaskContinuationOptions.OnlyOnFaulted);
            else if (write.IsFaulted)
                _logger.LogWarning(write.Exception, "Economy event {Source}/{Resource} of {PlayerId} not written.", entry.Source, entry.Resource, entry.PlayerId);
        }
    }
}
