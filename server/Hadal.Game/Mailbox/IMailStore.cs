using Hadal.Game.Persistence;
using HADAL.Shared.Simulation;
using Npgsql;

namespace Hadal.Game.Mailbox
{
    /// <summary>Player mails with their attachments (docs/29 §2.1).</summary>
    public interface IMailStore
    {
        Task InitializeAsync(CancellationToken cancellationToken);

        Task InsertAsync(IReadOnlyList<Mail> mails, CancellationToken cancellationToken);

        /// <summary>A player's mails that are neither deleted nor expired.</summary>
        Task<List<Mail>> LoadAsync(string playerId, DateTimeOffset now, CancellationToken cancellationToken);

        /// <summary>Writes read, claimed and deleted marks. A claim once stored is never undone.</summary>
        Task UpdateAsync(IReadOnlyList<Mail> mails, CancellationToken cancellationToken);

        /// <summary>Drops expired and deleted mails; returns how many.</summary>
        Task<int> PurgeAsync(DateTimeOffset now, CancellationToken cancellationToken);
    }

    public sealed class InMemoryMailStore : IMailStore
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, Mail> _mails = new(StringComparer.Ordinal);

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task InsertAsync(IReadOnlyList<Mail> mails, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                foreach (var mail in mails)
                    _mails[mail.Id] = mail.Copy();
            }

            return Task.CompletedTask;
        }

        public Task<List<Mail>> LoadAsync(string playerId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            lock (_gate)
                return Task.FromResult(_mails.Values.Where(m => m.PlayerId == playerId && !m.Deleted && !m.Expired(now)).Select(m => m.Copy()).ToList());
        }

        public Task UpdateAsync(IReadOnlyList<Mail> mails, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                foreach (var mail in mails)
                {
                    if (!_mails.TryGetValue(mail.Id, out var stored))
                        continue;
                    stored.ReadAt ??= mail.ReadAt;
                    stored.ClaimedAt ??= mail.ClaimedAt;
                    stored.Deleted |= mail.Deleted;
                }
            }

            return Task.CompletedTask;
        }

        public Task<int> PurgeAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var gone = _mails.Values.Where(m => m.Deleted || m.Expired(now)).Select(m => m.Id).ToList();
                foreach (var id in gone)
                    _mails.Remove(id);
                return Task.FromResult(gone.Count);
            }
        }
    }

    public sealed class PostgresMailStore : IMailStore
    {
        private const string Schema = @"
CREATE SCHEMA IF NOT EXISTS hadal;

CREATE TABLE IF NOT EXISTS hadal.mails (
    mail_id      text        PRIMARY KEY,
    player_id    text        NOT NULL,
    kind         text        NOT NULL,
    sender       text        NOT NULL DEFAULT '',
    title        text        NOT NULL,
    body         text        NOT NULL DEFAULT '',
    attachments  jsonb       NOT NULL DEFAULT '{}',
    created_at   timestamptz NOT NULL,
    expires_at   timestamptz NOT NULL,
    read_at      timestamptz,
    claimed_at   timestamptz,
    deleted      boolean     NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS mails_player ON hadal.mails (player_id, created_at DESC);
CREATE INDEX IF NOT EXISTS mails_expiry ON hadal.mails (expires_at);
ALTER TABLE hadal.mails ADD COLUMN IF NOT EXISTS sender_id text NOT NULL DEFAULT '';";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresMailStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Task InitializeAsync(CancellationToken cancellationToken) => PostgresSchema.ApplyAsync(_dataSource, Schema, cancellationToken);

        public async Task InsertAsync(IReadOnlyList<Mail> mails, CancellationToken cancellationToken)
        {
            if (mails.Count == 0)
                return;
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            // A broadcast reaches every player: written in batches so one statement never grows without bound.
            foreach (var chunk in mails.Chunk(500))
            {
                await using var batch = new NpgsqlBatch(connection);
                foreach (var m in chunk)
                {
                    var command = new NpgsqlBatchCommand(@"
INSERT INTO hadal.mails (mail_id, player_id, kind, sender, sender_id, title, body, attachments, created_at, expires_at)
VALUES (@id, @p, @k, @s, @si, @t, @b, @a::jsonb, @c, @e) ON CONFLICT (mail_id) DO NOTHING");
                    command.Parameters.AddWithValue("id", m.Id);
                    command.Parameters.AddWithValue("p", m.PlayerId);
                    command.Parameters.AddWithValue("k", m.Kind);
                    command.Parameters.AddWithValue("s", m.Sender);
                    command.Parameters.AddWithValue("si", m.SenderId);
                    command.Parameters.AddWithValue("t", m.Title);
                    command.Parameters.AddWithValue("b", m.Body);
                    command.Parameters.AddWithValue("a", m.Attachments.ToJson());
                    command.Parameters.AddWithValue("c", m.CreatedAt);
                    command.Parameters.AddWithValue("e", m.ExpiresAt);
                    batch.BatchCommands.Add(command);
                }

                await batch.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        public async Task<List<Mail>> LoadAsync(string playerId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            var list = new List<Mail>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
SELECT mail_id, kind, sender, title, body, attachments::text, created_at, expires_at, read_at, claimed_at, sender_id FROM hadal.mails
WHERE player_id = @p AND NOT deleted AND expires_at > @now ORDER BY created_at DESC", connection);
            command.Parameters.AddWithValue("p", playerId);
            command.Parameters.AddWithValue("now", now);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                list.Add(new Mail
                {
                    Id = reader.GetString(0), PlayerId = playerId, Kind = reader.GetString(1), Sender = reader.GetString(2),
                    Title = reader.GetString(3), Body = reader.GetString(4), Attachments = MailAttachments.FromJson(reader.GetString(5)),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(6), ExpiresAt = reader.GetFieldValue<DateTimeOffset>(7),
                    ReadAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
                    ClaimedAt = reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9),
                    SenderId = reader.GetString(10)
                });
            }

            return list;
        }

        public async Task UpdateAsync(IReadOnlyList<Mail> mails, CancellationToken cancellationToken)
        {
            if (mails.Count == 0)
                return;
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(connection);
            foreach (var m in mails)
            {
                // COALESCE keeps the first mark: a claim stored once can never be cleared by a later write.
                var command = new NpgsqlBatchCommand(@"
UPDATE hadal.mails SET read_at = COALESCE(read_at, @r), claimed_at = COALESCE(claimed_at, @c), deleted = deleted OR @d WHERE mail_id = @id");
                command.Parameters.AddWithValue("id", m.Id);
                command.Parameters.AddWithValue("r", NpgsqlTypes.NpgsqlDbType.TimestampTz, (object?)m.ReadAt ?? DBNull.Value);
                command.Parameters.AddWithValue("c", NpgsqlTypes.NpgsqlDbType.TimestampTz, (object?)m.ClaimedAt ?? DBNull.Value);
                command.Parameters.AddWithValue("d", m.Deleted);
                batch.BatchCommands.Add(command);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<int> PurgeAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM hadal.mails WHERE deleted OR expires_at <= @now", connection);
            command.Parameters.AddWithValue("now", now);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
