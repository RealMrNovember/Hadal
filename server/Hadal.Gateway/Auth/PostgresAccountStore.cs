using Hadal.Game.Persistence;
using HADAL.Shared.Auth;
using Npgsql;

namespace Hadal.Gateway.Auth
{
    public sealed class PostgresAccountStore : IAccountStore
    {
        private const string Schema = @"
CREATE SCHEMA IF NOT EXISTS hadal;

CREATE TABLE IF NOT EXISTS hadal.accounts (
    account_id     uuid        PRIMARY KEY,
    created_at     timestamptz NOT NULL DEFAULT now(),
    last_login_at  timestamptz NOT NULL DEFAULT now(),
    device_id      text        NOT NULL DEFAULT '',
    client_version text        NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS hadal.refresh_tokens (
    token_hash bytea       PRIMARY KEY,
    account_id uuid        NOT NULL REFERENCES hadal.accounts (account_id) ON DELETE CASCADE,
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS refresh_tokens_account ON hadal.refresh_tokens (account_id);

ALTER TABLE hadal.accounts ADD COLUMN IF NOT EXISTS display_name text NOT NULL DEFAULT '';

-- Overseer bans (Admin/PostgresAdminStore reads and writes them).
ALTER TABLE hadal.accounts ADD COLUMN IF NOT EXISTS banned_until timestamptz;
ALTER TABLE hadal.accounts ADD COLUMN IF NOT EXISTS ban_reason text NOT NULL DEFAULT '';

-- Identities that sign in to an account: transfer codes (secret_hash = PBKDF2 of the password),
-- Google / Apple subjects (secret_hash empty). One subject belongs to exactly one account.
CREATE TABLE IF NOT EXISTS hadal.account_links (
    provider    text        NOT NULL,
    subject     text        NOT NULL,
    account_id  uuid        NOT NULL REFERENCES hadal.accounts (account_id) ON DELETE CASCADE,
    secret_hash text        NOT NULL DEFAULT '',
    created_at  timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (provider, subject)
);

CREATE INDEX IF NOT EXISTS account_links_account ON hadal.account_links (account_id);";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresAccountStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Task InitializeAsync(CancellationToken cancellationToken) =>
            PostgresSchema.ApplyAsync(_dataSource, Schema, cancellationToken);

        public async Task<Guid> CreateGuestAsync(string deviceId, string clientVersion, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            await using var command = _dataSource.CreateCommand(
                "INSERT INTO hadal.accounts (account_id, device_id, client_version, display_name) VALUES (@a, @d, @v, @n)");
            command.Parameters.AddWithValue("a", id);
            command.Parameters.AddWithValue("n", AccountNames.Default(id));
            command.Parameters.AddWithValue("d", Truncate(deviceId, 128));
            command.Parameters.AddWithValue("v", Truncate(clientVersion, 32));
            await command.ExecuteNonQueryAsync(cancellationToken);
            return id;
        }

        public async Task StoreRefreshTokenAsync(byte[] tokenHash, Guid accountId, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                @"INSERT INTO hadal.refresh_tokens (token_hash, account_id, expires_at) VALUES (@h, @a, @e);
                  UPDATE hadal.accounts SET last_login_at = now() WHERE account_id = @a;");
            command.Parameters.AddWithValue("h", tokenHash);
            command.Parameters.AddWithValue("a", accountId);
            command.Parameters.AddWithValue("e", expiresAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<Guid?> ConsumeRefreshTokenAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken)
        {
            // DELETE ... RETURNING makes the token single-use even under concurrent refreshes.
            await using var command = _dataSource.CreateCommand(
                "DELETE FROM hadal.refresh_tokens WHERE token_hash = @h RETURNING account_id, expires_at");
            command.Parameters.AddWithValue("h", tokenHash);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var account = reader.GetGuid(0);
            var expires = reader.GetFieldValue<DateTimeOffset>(1);
            return expires > now ? account : null;
        }

        public async Task<AccountProfile?> GetProfileAsync(Guid accountId, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                @"SELECT a.display_name, a.created_at,
                         COALESCE(array_agg(DISTINCT l.provider ORDER BY l.provider) FILTER (WHERE l.provider IS NOT NULL), '{}')
                  FROM hadal.accounts a LEFT JOIN hadal.account_links l ON l.account_id = a.account_id
                  WHERE a.account_id = @a
                  GROUP BY a.account_id");
            command.Parameters.AddWithValue("a", accountId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var name = reader.GetString(0);
            return new AccountProfile(
                accountId,
                string.IsNullOrEmpty(name) ? AccountNames.Default(accountId) : name, // accounts created before names existed
                reader.GetFieldValue<DateTimeOffset>(1),
                reader.GetFieldValue<string[]>(2));
        }

        public async Task<bool> IsNameTakenAsync(string displayName, Guid exceptAccountId, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "SELECT EXISTS (SELECT 1 FROM hadal.accounts WHERE lower(display_name) = lower(@n) AND account_id <> @a)");
            command.Parameters.AddWithValue("n", displayName);
            command.Parameters.AddWithValue("a", exceptAccountId);
            return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
        }

        public async Task SetDisplayNameAsync(Guid accountId, string displayName, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("UPDATE hadal.accounts SET display_name = @n WHERE account_id = @a");
            command.Parameters.AddWithValue("a", accountId);
            command.Parameters.AddWithValue("n", displayName);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<AccountLink?> FindLinkAsync(string provider, string subject, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "SELECT account_id, secret_hash FROM hadal.account_links WHERE provider = @p AND subject = @s");
            command.Parameters.AddWithValue("p", provider);
            command.Parameters.AddWithValue("s", subject);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? new AccountLink(reader.GetGuid(0), reader.GetString(1)) : null;
        }

        public async Task<bool> LinkAsync(Guid accountId, string provider, string subject, string secretHash, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            if (provider == AccountProviders.Transfer)
            {
                // One transfer code per account: a new code retires the old one.
                await using var retire = new NpgsqlCommand(
                    "DELETE FROM hadal.account_links WHERE provider = @p AND account_id = @a AND subject <> @s", connection, transaction);
                retire.Parameters.AddWithValue("p", provider);
                retire.Parameters.AddWithValue("a", accountId);
                retire.Parameters.AddWithValue("s", subject);
                await retire.ExecuteNonQueryAsync(cancellationToken);
            }

            // The WHERE on the conflict branch keeps a subject that belongs to another account untouched.
            await using var upsert = new NpgsqlCommand(
                @"INSERT INTO hadal.account_links (provider, subject, account_id, secret_hash) VALUES (@p, @s, @a, @h)
                  ON CONFLICT (provider, subject) DO UPDATE SET secret_hash = EXCLUDED.secret_hash
                  WHERE hadal.account_links.account_id = EXCLUDED.account_id", connection, transaction);
            upsert.Parameters.AddWithValue("p", provider);
            upsert.Parameters.AddWithValue("s", subject);
            upsert.Parameters.AddWithValue("a", accountId);
            upsert.Parameters.AddWithValue("h", secretHash);
            var changed = await upsert.ExecuteNonQueryAsync(cancellationToken);

            if (changed == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        private static string Truncate(string? value, int max) =>
            string.IsNullOrEmpty(value) ? string.Empty : value.Length <= max ? value : value[..max];
    }
}
