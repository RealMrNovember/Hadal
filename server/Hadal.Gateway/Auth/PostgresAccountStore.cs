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

CREATE INDEX IF NOT EXISTS refresh_tokens_account ON hadal.refresh_tokens (account_id);";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresAccountStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(Schema);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<Guid> CreateGuestAsync(string deviceId, string clientVersion, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            await using var command = _dataSource.CreateCommand(
                "INSERT INTO hadal.accounts (account_id, device_id, client_version) VALUES (@a, @d, @v)");
            command.Parameters.AddWithValue("a", id);
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

        private static string Truncate(string? value, int max) =>
            string.IsNullOrEmpty(value) ? string.Empty : value.Length <= max ? value : value[..max];
    }
}
