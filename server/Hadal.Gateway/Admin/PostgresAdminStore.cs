using System.Text.Json;
using Hadal.Game.Persistence;
using HADAL.Shared.Enums;
using Npgsql;
using NpgsqlTypes;

namespace Hadal.Gateway.Admin
{
    public sealed class PostgresAdminStore : IAdminStore
    {
        private const string Schema = @"
CREATE SCHEMA IF NOT EXISTS hadal;

-- Bans live on hadal.accounts (columns owned by PostgresAccountStore).

CREATE TABLE IF NOT EXISTS hadal.admins (
    admin_id      uuid        PRIMARY KEY,
    username      text        NOT NULL UNIQUE,
    password_hash text        NOT NULL,
    totp_secret   text        NOT NULL,
    role          text        NOT NULL,
    disabled      boolean     NOT NULL DEFAULT false,
    created_at    timestamptz NOT NULL DEFAULT now(),
    last_login_at timestamptz
);

CREATE TABLE IF NOT EXISTS hadal.admin_audit (
    audit_id bigserial   PRIMARY KEY,
    at       timestamptz NOT NULL DEFAULT now(),
    admin    text        NOT NULL,
    action   text        NOT NULL,
    target   text        NOT NULL DEFAULT '',
    details  text        NOT NULL DEFAULT '',
    ip       text        NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS hadal.coupons (
    code            text        PRIMARY KEY,
    rewards         jsonb       NOT NULL,
    max_redemptions integer     NOT NULL DEFAULT 0,
    redeemed        integer     NOT NULL DEFAULT 0,
    expires_at      timestamptz,
    active          boolean     NOT NULL DEFAULT true,
    created_by      text        NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now(),
    note            text        NOT NULL DEFAULT ''
);

-- Optional validity start and single owner (per-player codes).
ALTER TABLE hadal.coupons ADD COLUMN IF NOT EXISTS starts_at timestamptz;
ALTER TABLE hadal.coupons ADD COLUMN IF NOT EXISTS target_account uuid;

CREATE TABLE IF NOT EXISTS hadal.coupon_redemptions (
    code       text        NOT NULL REFERENCES hadal.coupons (code) ON DELETE CASCADE,
    account_id uuid        NOT NULL,
    at         timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (code, account_id)
);

CREATE TABLE IF NOT EXISTS hadal.liveops_settings (
    key   text  PRIMARY KEY,
    value jsonb NOT NULL
);

CREATE TABLE IF NOT EXISTS hadal.catalog_versions (
    version    serial      PRIMARY KEY,
    json       text        NOT NULL,
    created_by text        NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    note       text        NOT NULL DEFAULT ''
);";

        private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        private readonly NpgsqlDataSource _dataSource;

        public PostgresAdminStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Task InitializeAsync(CancellationToken cancellationToken) => PostgresSchema.ApplyAsync(_dataSource, Schema, cancellationToken);

        // --- Admins and audit ------------------------------------------------------------------

        public async Task<int> CountAdminsAsync(CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("SELECT count(*) FROM hadal.admins");
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }

        public async Task<AdminUser?> FindAdminAsync(string username, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "SELECT admin_id, username, password_hash, totp_secret, role, disabled, created_at, last_login_at FROM hadal.admins WHERE lower(username) = lower(@u)");
            command.Parameters.AddWithValue("u", username);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadAdmin(reader) : null;
        }

        public async Task<IReadOnlyList<AdminUser>> ListAdminsAsync(CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "SELECT admin_id, username, password_hash, totp_secret, role, disabled, created_at, last_login_at FROM hadal.admins ORDER BY username");
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var list = new List<AdminUser>();
            while (await reader.ReadAsync(cancellationToken))
                list.Add(ReadAdmin(reader));
            return list;
        }

        private static AdminUser ReadAdmin(NpgsqlDataReader r) => new(
            r.GetGuid(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetBoolean(5),
            r.GetFieldValue<DateTimeOffset>(6), r.IsDBNull(7) ? null : r.GetFieldValue<DateTimeOffset>(7));

        public async Task<bool> CreateAdminAsync(AdminUser admin, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                @"INSERT INTO hadal.admins (admin_id, username, password_hash, totp_secret, role) VALUES (@i, @u, @p, @t, @r)
                  ON CONFLICT (username) DO NOTHING");
            command.Parameters.AddWithValue("i", admin.Id);
            command.Parameters.AddWithValue("u", admin.Username);
            command.Parameters.AddWithValue("p", admin.PasswordHash);
            command.Parameters.AddWithValue("t", admin.TotpSecret);
            command.Parameters.AddWithValue("r", admin.Role);
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }

        public async Task SetAdminDisabledAsync(Guid id, bool disabled, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("UPDATE hadal.admins SET disabled = @d WHERE admin_id = @i");
            command.Parameters.AddWithValue("i", id);
            command.Parameters.AddWithValue("d", disabled);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task TouchLoginAsync(Guid id, DateTimeOffset at, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("UPDATE hadal.admins SET last_login_at = @a WHERE admin_id = @i");
            command.Parameters.AddWithValue("i", id);
            command.Parameters.AddWithValue("a", at);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task AuditAsync(string admin, string action, string target, string details, string ip, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "INSERT INTO hadal.admin_audit (admin, action, target, details, ip) VALUES (@a, @c, @t, @d, @i)");
            command.Parameters.AddWithValue("a", admin);
            command.Parameters.AddWithValue("c", action);
            command.Parameters.AddWithValue("t", target);
            command.Parameters.AddWithValue("d", details);
            command.Parameters.AddWithValue("i", ip);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<AuditEntry>> ListAuditAsync(int limit, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "SELECT audit_id, at, admin, action, target, details, ip FROM hadal.admin_audit ORDER BY audit_id DESC LIMIT @l");
            command.Parameters.AddWithValue("l", limit);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var list = new List<AuditEntry>();
            while (await reader.ReadAsync(cancellationToken))
                list.Add(new AuditEntry(reader.GetInt64(0), reader.GetFieldValue<DateTimeOffset>(1), reader.GetString(2), reader.GetString(3),
                    reader.GetString(4), reader.GetString(5), reader.GetString(6)));
            return list;
        }

        // --- Coupons ---------------------------------------------------------------------------

        public async Task<bool> CreateCouponAsync(Coupon coupon, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                @"INSERT INTO hadal.coupons (code, rewards, max_redemptions, expires_at, active, created_by, note, starts_at, target_account)
                  VALUES (@c, @r::jsonb, @m, @e, true, @b, @n, @s, @t) ON CONFLICT (code) DO NOTHING");
            command.Parameters.AddWithValue("c", coupon.Code);
            command.Parameters.AddWithValue("r", WriteRewards(coupon.Rewards));
            command.Parameters.AddWithValue("m", coupon.MaxRedemptions);
            command.Parameters.Add(new NpgsqlParameter("e", NpgsqlDbType.TimestampTz) { Value = coupon.ExpiresAt.HasValue ? coupon.ExpiresAt.Value : DBNull.Value });
            command.Parameters.AddWithValue("b", coupon.CreatedBy);
            command.Parameters.AddWithValue("n", coupon.Note);
            command.Parameters.Add(new NpgsqlParameter("s", NpgsqlDbType.TimestampTz) { Value = coupon.StartsAt.HasValue ? coupon.StartsAt.Value : DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter("t", NpgsqlDbType.Uuid) { Value = coupon.TargetAccountId.HasValue ? coupon.TargetAccountId.Value : DBNull.Value });
            return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }

        public async Task<IReadOnlyList<Coupon>> ListCouponsAsync(CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                $"SELECT {CouponColumns} FROM hadal.coupons ORDER BY created_at DESC LIMIT 500");
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var list = new List<Coupon>();
            while (await reader.ReadAsync(cancellationToken))
                list.Add(ReadCoupon(reader));
            return list;
        }

        private const string CouponColumns =
            "code, rewards::text, max_redemptions, redeemed, expires_at, active, created_by, created_at, note, starts_at, target_account";

        private static Coupon ReadCoupon(NpgsqlDataReader r) => new(
            r.GetString(0), ReadRewards(r.GetString(1)), r.GetInt32(2), r.GetInt32(3),
            r.IsDBNull(4) ? null : r.GetFieldValue<DateTimeOffset>(4), r.GetBoolean(5), r.GetString(6), r.GetFieldValue<DateTimeOffset>(7), r.GetString(8),
            r.IsDBNull(9) ? null : r.GetFieldValue<DateTimeOffset>(9), r.IsDBNull(10) ? null : r.GetGuid(10));

        public async Task SetCouponActiveAsync(string code, bool active, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("UPDATE hadal.coupons SET active = @a WHERE code = @c");
            command.Parameters.AddWithValue("c", code);
            command.Parameters.AddWithValue("a", active);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<(RedeemResult Result, Coupon? Coupon)> RedeemAsync(string code, Guid accountId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            Coupon? coupon;
            await using (var select = new NpgsqlCommand(
                $"SELECT {CouponColumns} FROM hadal.coupons WHERE code = @c FOR UPDATE",
                connection, transaction))
            {
                select.Parameters.AddWithValue("c", code);
                await using var reader = await select.ExecuteReaderAsync(cancellationToken);
                coupon = await reader.ReadAsync(cancellationToken) ? ReadCoupon(reader) : null;
            }

            if (coupon == null)
                return (RedeemResult.Unknown, null);

            // "You already used it" beats "it ran out" for the player who used the last one.
            await using (var used = new NpgsqlCommand(
                "SELECT 1 FROM hadal.coupon_redemptions WHERE code = @c AND account_id = @a", connection, transaction))
            {
                used.Parameters.AddWithValue("c", code);
                used.Parameters.AddWithValue("a", accountId);
                if (await used.ExecuteScalarAsync(cancellationToken) != null)
                    return (RedeemResult.AlreadyRedeemed, coupon);
            }

            var verdict = InMemoryAdminStore.Check(coupon, accountId, now);
            if (verdict != RedeemResult.Redeemed)
                return (verdict, coupon);

            await using (var insert = new NpgsqlCommand(
                "INSERT INTO hadal.coupon_redemptions (code, account_id) VALUES (@c, @a) ON CONFLICT DO NOTHING", connection, transaction))
            {
                insert.Parameters.AddWithValue("c", code);
                insert.Parameters.AddWithValue("a", accountId);
                if (await insert.ExecuteNonQueryAsync(cancellationToken) == 0)
                    return (RedeemResult.AlreadyRedeemed, coupon);
            }

            await using (var update = new NpgsqlCommand("UPDATE hadal.coupons SET redeemed = redeemed + 1 WHERE code = @c", connection, transaction))
            {
                update.Parameters.AddWithValue("c", code);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (RedeemResult.Redeemed, coupon with { Redeemed = coupon.Redeemed + 1 });
        }

        public async Task UndoRedemptionAsync(string code, Guid accountId, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                @"WITH removed AS (DELETE FROM hadal.coupon_redemptions WHERE code = @c AND account_id = @a RETURNING code)
                  UPDATE hadal.coupons SET redeemed = greatest(0, redeemed - 1) WHERE code IN (SELECT code FROM removed)");
            command.Parameters.AddWithValue("c", code);
            command.Parameters.AddWithValue("a", accountId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static string WriteRewards(Dictionary<ResourceType, long> rewards) =>
            JsonSerializer.Serialize(rewards.ToDictionary(p => p.Key.ToString(), p => p.Value));

        private static Dictionary<ResourceType, long> ReadRewards(string json)
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, long>>(json) ?? new();
            var rewards = new Dictionary<ResourceType, long>();
            foreach (var pair in raw)
            {
                if (Enum.TryParse<ResourceType>(pair.Key, out var type))
                    rewards[type] = pair.Value;
            }

            return rewards;
        }

        // --- Live ops and balance --------------------------------------------------------------

        public async Task<LiveOpsSettings> GetLiveOpsAsync(CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("SELECT value::text FROM hadal.liveops_settings WHERE key = 'liveops'");
            var value = await command.ExecuteScalarAsync(cancellationToken) as string;
            return value == null ? new LiveOpsSettings() : JsonSerializer.Deserialize<LiveOpsSettings>(value, Json) ?? new LiveOpsSettings();
        }

        public async Task SetLiveOpsAsync(LiveOpsSettings settings, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                @"INSERT INTO hadal.liveops_settings (key, value) VALUES ('liveops', @v::jsonb)
                  ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value");
            command.Parameters.AddWithValue("v", JsonSerializer.Serialize(settings, Json));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<CatalogVersion?> GetLatestCatalogAsync(CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "SELECT version, json, created_by, created_at, note FROM hadal.catalog_versions ORDER BY version DESC LIMIT 1");
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadCatalog(reader, true) : null;
        }

        public async Task<CatalogVersion?> GetCatalogAsync(int version, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "SELECT version, json, created_by, created_at, note FROM hadal.catalog_versions WHERE version = @v");
            command.Parameters.AddWithValue("v", version);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadCatalog(reader, true) : null;
        }

        public async Task<int> AddCatalogAsync(string json, string createdBy, string note, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "INSERT INTO hadal.catalog_versions (json, created_by, note) VALUES (@j, @b, @n) RETURNING version");
            command.Parameters.AddWithValue("j", json);
            command.Parameters.AddWithValue("b", createdBy);
            command.Parameters.AddWithValue("n", note);
            return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }

        public async Task<IReadOnlyList<CatalogVersion>> ListCatalogsAsync(int limit, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "SELECT version, '' AS json, created_by, created_at, note FROM hadal.catalog_versions ORDER BY version DESC LIMIT @l");
            command.Parameters.AddWithValue("l", limit);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var list = new List<CatalogVersion>();
            while (await reader.ReadAsync(cancellationToken))
                list.Add(ReadCatalog(reader, false));
            return list;
        }

        private static CatalogVersion ReadCatalog(NpgsqlDataReader r, bool withJson) =>
            new(r.GetInt32(0), withJson ? r.GetString(1) : string.Empty, r.GetString(2), r.GetFieldValue<DateTimeOffset>(3), r.GetString(4));

        // --- Players ---------------------------------------------------------------------------

        private const string PlayerColumns = @"a.account_id, a.display_name, a.created_at, a.last_login_at, a.banned_until, a.ban_reason,
            COALESCE((SELECT array_agg(DISTINCT l.provider ORDER BY l.provider) FROM hadal.account_links l WHERE l.account_id = a.account_id), '{}'),
            a.client_version";

        public async Task<IReadOnlyList<PlayerSummary>> SearchPlayersAsync(string query, int limit, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                $@"SELECT {PlayerColumns} FROM hadal.accounts a
                   WHERE @q = '' OR a.display_name ILIKE '%' || @q || '%' OR replace(a.account_id::text, '-', '') LIKE lower(@q) || '%'
                   ORDER BY a.last_login_at DESC LIMIT @l");
            command.Parameters.AddWithValue("q", query ?? string.Empty);
            command.Parameters.AddWithValue("l", limit);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var list = new List<PlayerSummary>();
            while (await reader.ReadAsync(cancellationToken))
                list.Add(ReadPlayer(reader));
            return list;
        }

        public async Task<PlayerSummary?> GetPlayerAsync(Guid accountId, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand($"SELECT {PlayerColumns} FROM hadal.accounts a WHERE a.account_id = @a");
            command.Parameters.AddWithValue("a", accountId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadPlayer(reader) : null;
        }

        private static PlayerSummary ReadPlayer(NpgsqlDataReader r) => new(
            r.GetGuid(0), r.GetString(1), r.GetFieldValue<DateTimeOffset>(2), r.GetFieldValue<DateTimeOffset>(3),
            r.IsDBNull(4) ? null : r.GetFieldValue<DateTimeOffset>(4), r.GetString(5), r.GetFieldValue<string[]>(6), r.GetString(7));

        public async Task<(int Total, int Last24h)> CountPlayersAsync(DateTimeOffset now, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "SELECT count(*), count(*) FILTER (WHERE created_at > @d) FROM hadal.accounts");
            command.Parameters.AddWithValue("d", now.AddDays(-1));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return ((int)reader.GetInt64(0), (int)reader.GetInt64(1));
        }

        public async Task<bool> DeleteAccountAsync(Guid accountId, CancellationToken cancellationToken)
        {
            // Links and refresh tokens cascade; redemptions keep no foreign key, so they go first.
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using (var redemptions = new NpgsqlCommand("DELETE FROM hadal.coupon_redemptions WHERE account_id = @a", connection, transaction))
            {
                redemptions.Parameters.AddWithValue("a", accountId);
                await redemptions.ExecuteNonQueryAsync(cancellationToken);
            }

            int deleted;
            await using (var account = new NpgsqlCommand("DELETE FROM hadal.accounts WHERE account_id = @a", connection, transaction))
            {
                account.Parameters.AddWithValue("a", accountId);
                deleted = await account.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return deleted > 0;
        }

        public async Task SetBanAsync(Guid accountId, DateTimeOffset? until, string reason, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("UPDATE hadal.accounts SET banned_until = @u, ban_reason = @r WHERE account_id = @a");
            command.Parameters.AddWithValue("a", accountId);
            command.Parameters.Add(new NpgsqlParameter("u", NpgsqlDbType.TimestampTz) { Value = until.HasValue ? until.Value : DBNull.Value });
            command.Parameters.AddWithValue("r", reason ?? string.Empty);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<(DateTimeOffset Until, string Reason)?> GetActiveBanAsync(Guid accountId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                "SELECT banned_until, ban_reason FROM hadal.accounts WHERE account_id = @a AND banned_until > @n");
            command.Parameters.AddWithValue("a", accountId);
            command.Parameters.AddWithValue("n", now);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? (reader.GetFieldValue<DateTimeOffset>(0), reader.GetString(1)) : null;
        }
    }
}
