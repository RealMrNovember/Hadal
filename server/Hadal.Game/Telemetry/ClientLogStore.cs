using Hadal.Game.Persistence;
using Npgsql;

namespace Hadal.Game.Telemetry
{
    public sealed record ClientLogRecord(
        string PlayerId, string Level, string Message, string Stack, DateTimeOffset At,
        string ClientVersion, string Platform, string Device);

    /// <summary>Server-side sink for client crash/error reports. Write path is batched; read path feeds the Overseer log page.</summary>
    public interface IClientLogStore
    {
        Task InitializeAsync(CancellationToken cancellationToken);
        Task RecordAsync(IReadOnlyList<ClientLogRecord> entries, CancellationToken cancellationToken);
        /// <summary>Newest first, capped; <paramref name="level"/> filters exactly, <paramref name="query"/> matches message case-insensitively.</summary>
        Task<List<ClientLogRecord>> LoadAsync(DateTimeOffset since, string? level, string? query, int limit, CancellationToken cancellationToken);
    }

    public sealed class InMemoryClientLogStore : IClientLogStore
    {
        private readonly object _gate = new();
        private readonly List<ClientLogRecord> _entries = new();

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RecordAsync(IReadOnlyList<ClientLogRecord> entries, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _entries.AddRange(entries);
            }
            return Task.CompletedTask;
        }

        public Task<List<ClientLogRecord>> LoadAsync(DateTimeOffset since, string? level, string? query, int limit, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult(Query(_entries, since, level, query, limit));
            }
        }

        internal static List<ClientLogRecord> Query(IEnumerable<ClientLogRecord> source, DateTimeOffset since, string? level, string? query, int limit) =>
            source
                .Where(e => e.At >= since
                            && (string.IsNullOrEmpty(level) || string.Equals(e.Level, level, StringComparison.Ordinal))
                            && (string.IsNullOrEmpty(query) || e.Message.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(e => e.At)
                .Take(Math.Clamp(limit, 1, 500))
                .ToList();
    }

    public sealed class PostgresClientLogStore : IClientLogStore
    {
        private const string Schema = @"
CREATE SCHEMA IF NOT EXISTS hadal;
CREATE TABLE IF NOT EXISTS hadal.client_log (
    id             bigserial PRIMARY KEY,
    player_id      text        NOT NULL,
    level          text        NOT NULL,
    message        text        NOT NULL,
    stack          text        NOT NULL DEFAULT '',
    at             timestamptz NOT NULL,
    client_version text        NOT NULL DEFAULT '',
    platform       text        NOT NULL DEFAULT '',
    device         text        NOT NULL DEFAULT ''
);
CREATE INDEX IF NOT EXISTS client_log_at ON hadal.client_log (at DESC);";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresClientLogStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Task InitializeAsync(CancellationToken cancellationToken) => PostgresSchema.ApplyAsync(_dataSource, Schema, cancellationToken);

        public async Task RecordAsync(IReadOnlyList<ClientLogRecord> entries, CancellationToken cancellationToken)
        {
            if (entries.Count == 0)
                return;

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
INSERT INTO hadal.client_log (player_id, level, message, stack, at, client_version, platform, device)
VALUES (@p, @l, @m, @s, @at, @v, @pl, @d)", connection, transaction);

            var playerId = command.Parameters.Add("p", NpgsqlTypes.NpgsqlDbType.Text);
            var level = command.Parameters.Add("l", NpgsqlTypes.NpgsqlDbType.Text);
            var message = command.Parameters.Add("m", NpgsqlTypes.NpgsqlDbType.Text);
            var stack = command.Parameters.Add("s", NpgsqlTypes.NpgsqlDbType.Text);
            var at = command.Parameters.Add("at", NpgsqlTypes.NpgsqlDbType.TimestampTz);
            var version = command.Parameters.Add("v", NpgsqlTypes.NpgsqlDbType.Text);
            var platform = command.Parameters.Add("pl", NpgsqlTypes.NpgsqlDbType.Text);
            var device = command.Parameters.Add("d", NpgsqlTypes.NpgsqlDbType.Text);

            foreach (var entry in entries)
            {
                playerId.Value = entry.PlayerId;
                level.Value = entry.Level;
                message.Value = entry.Message;
                stack.Value = entry.Stack;
                at.Value = entry.At;
                version.Value = entry.ClientVersion;
                platform.Value = entry.Platform;
                device.Value = entry.Device;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        public async Task<List<ClientLogRecord>> LoadAsync(DateTimeOffset since, string? level, string? query, int limit, CancellationToken cancellationToken)
        {
            var list = new List<ClientLogRecord>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
SELECT player_id, level, message, stack, at, client_version, platform, device
FROM hadal.client_log
WHERE at >= @since
  AND (@level = '' OR level = @level)
  AND (@q = '' OR position(lower(@q) IN lower(message)) > 0)
ORDER BY at DESC
LIMIT @limit", connection);

            command.Parameters.AddWithValue("since", since);
            command.Parameters.AddWithValue("level", (object?)level ?? string.Empty);
            command.Parameters.AddWithValue("q", (object?)query ?? string.Empty);
            command.Parameters.AddWithValue("limit", Math.Clamp(limit, 1, 500));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new ClientLogRecord(
                    reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                    reader.GetFieldValue<DateTimeOffset>(4), reader.GetString(5), reader.GetString(6), reader.GetString(7)));
            }

            return list;
        }
    }
}
