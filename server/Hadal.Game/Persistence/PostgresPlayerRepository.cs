using System.Text.Json;
using HADAL.Shared.Enums;
using Npgsql;

namespace Hadal.Game.Persistence
{
    /// <summary>
    /// PostgreSQL store in schema <c>hadal</c>. The wallet is a JSONB map keyed by resource name
    /// (internal only — never sent to clients as JSON).
    /// </summary>
    public sealed class PostgresPlayerRepository : IPlayerRepository
    {
        private const string Schema = @"
CREATE SCHEMA IF NOT EXISTS hadal;

-- Phase 2: players are keyed by account id (Auth API), not device id.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_schema = 'hadal' AND table_name = 'players' AND column_name = 'device_id') THEN
        ALTER TABLE hadal.players RENAME COLUMN device_id TO identity_key;
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS hadal.players (
    player_id         uuid        PRIMARY KEY,
    identity_key         text        NOT NULL UNIQUE,
    created_at        timestamptz NOT NULL DEFAULT now(),
    server_tick       bigint      NOT NULL DEFAULT 0,
    last_simulated_at timestamptz NOT NULL,
    unlocked_rings    integer     NOT NULL,
    wallet            jsonb       NOT NULL DEFAULT '{}'::jsonb
);

CREATE TABLE IF NOT EXISTS hadal.buildings (
    building_id    text        PRIMARY KEY,
    player_id      uuid        NOT NULL REFERENCES hadal.players (player_id) ON DELETE CASCADE,
    definition_id  text        NOT NULL,
    ring           integer     NOT NULL,
    sector         integer     NOT NULL,
    level          integer     NOT NULL DEFAULT 1,
    rotation_steps integer     NOT NULL DEFAULT 0,
    created_at     timestamptz NOT NULL DEFAULT now(),
    UNIQUE (player_id, ring, sector)
);

-- Running construction (docs/23_Base_Management.md): NULL = idle.
ALTER TABLE hadal.buildings ADD COLUMN IF NOT EXISTS construction_ends_at timestamptz;
ALTER TABLE hadal.buildings ADD COLUMN IF NOT EXISTS construction_seconds integer NOT NULL DEFAULT 0;

-- Research and building functions (docs/24): one JSON document, see PlayerExtras.
ALTER TABLE hadal.players ADD COLUMN IF NOT EXISTS extras jsonb NOT NULL DEFAULT '{}'::jsonb;";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresPlayerRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Task InitializeAsync(CancellationToken cancellationToken) =>
            PostgresSchema.ApplyAsync(_dataSource, Schema, cancellationToken);

        public async Task<Guid?> DeleteAsync(string identityKey, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("DELETE FROM hadal.players WHERE identity_key = @k RETURNING player_id");
            command.Parameters.AddWithValue("k", identityKey);
            var id = await command.ExecuteScalarAsync(cancellationToken);
            return id is Guid g ? g : null;
        }

        public async Task<IReadOnlyList<Guid>> ListPlayerIdsAsync(CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand("SELECT player_id FROM hadal.players");
            var list = new List<Guid>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                list.Add(reader.GetGuid(0));
            return list;
        }

        public async Task<IReadOnlyList<(string IdentityKey, Guid PlayerId, DateTimeOffset CreatedAt)>> ListWithoutAccountAsync(CancellationToken cancellationToken)
        {
            // hadal.accounts belongs to the gateway; a server without it has no accounts to compare with.
            await using var command = _dataSource.CreateCommand(@"
SELECT p.identity_key, p.player_id, p.created_at FROM hadal.players p
WHERE to_regclass('hadal.accounts') IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM hadal.accounts a WHERE replace(a.account_id::text, '-', '') = p.identity_key)
ORDER BY p.created_at");
            var list = new List<(string, Guid, DateTimeOffset)>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                list.Add((reader.GetString(0), reader.GetGuid(1), reader.GetFieldValue<DateTimeOffset>(2)));
            return list;
        }

        public async Task<PlayerState?> LoadByIdentityAsync(string identityKey, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            PlayerState player;
            await using (var command = new NpgsqlCommand(
                "SELECT player_id, server_tick, last_simulated_at, unlocked_rings, wallet::text, extras::text FROM hadal.players WHERE identity_key = @d",
                connection))
            {
                command.Parameters.AddWithValue("d", identityKey);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    return null;

                player = new PlayerState
                {
                    PlayerId = reader.GetGuid(0),
                    IdentityKey = identityKey,
                    ServerTick = (ulong)reader.GetInt64(1),
                    LastSimulatedAt = reader.GetFieldValue<DateTimeOffset>(2),
                    UnlockedRings = reader.GetInt32(3)
                };

                foreach (var pair in ReadWallet(reader.GetString(4)))
                    player.Wallet[pair.Key] = pair.Value;
                player.ExtrasJson = reader.GetString(5);
            }

            await using (var command = new NpgsqlCommand(
                "SELECT building_id, definition_id, ring, sector, level, rotation_steps, construction_ends_at, construction_seconds FROM hadal.buildings WHERE player_id = @p ORDER BY created_at",
                connection))
            {
                command.Parameters.AddWithValue("p", player.PlayerId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    player.Buildings.Add(new PlacedBuilding
                    {
                        BuildingId = reader.GetString(0),
                        DefinitionId = reader.GetString(1),
                        Ring = reader.GetInt32(2),
                        Sector = reader.GetInt32(3),
                        Level = reader.GetInt32(4),
                        RotationSteps = reader.GetInt32(5),
                        ConstructionEndsAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
                        ConstructionSeconds = reader.GetInt32(7)
                    });
                }
            }

            PlayerExtras.Read(player.ExtrasJson, player);
            return player;
        }

        public async Task CreateAsync(PlayerState player, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                @"INSERT INTO hadal.players (player_id, identity_key, server_tick, last_simulated_at, unlocked_rings, wallet)
                  VALUES (@p, @d, @t, @s, @u, @w::jsonb)");
            command.Parameters.AddWithValue("p", player.PlayerId);
            command.Parameters.AddWithValue("d", player.IdentityKey);
            command.Parameters.AddWithValue("t", (long)player.ServerTick);
            command.Parameters.AddWithValue("s", player.LastSimulatedAt);
            command.Parameters.AddWithValue("u", player.UnlockedRings);
            command.Parameters.AddWithValue("w", WriteWallet(player.Wallet));
            await command.ExecuteNonQueryAsync(cancellationToken);

            player.UnsavedBuildings.Clear();
        }

        public async Task SaveAsync(PlayerState player, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await using (var command = new NpgsqlCommand(
                @"UPDATE hadal.players
                  SET server_tick = @t, last_simulated_at = @s, unlocked_rings = @u, wallet = @w::jsonb,
                      extras = CASE WHEN @x = '' THEN extras ELSE @x::jsonb END
                  WHERE player_id = @p", connection, transaction))
            {
                command.Parameters.AddWithValue("x", player.ExtrasJson ?? string.Empty);
                command.Parameters.AddWithValue("p", player.PlayerId);
                command.Parameters.AddWithValue("t", (long)player.ServerTick);
                command.Parameters.AddWithValue("s", player.LastSimulatedAt);
                command.Parameters.AddWithValue("u", player.UnlockedRings);
                command.Parameters.AddWithValue("w", WriteWallet(player.Wallet));
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            // Deletes first: a removed building's slot may already hold its replacement.
            if (player.RemovedBuildingIds.Count > 0)
            {
                await using var delete = new NpgsqlCommand(
                    "DELETE FROM hadal.buildings WHERE player_id = @p AND building_id = ANY(@ids)", connection, transaction);
                delete.Parameters.AddWithValue("p", player.PlayerId);
                delete.Parameters.AddWithValue("ids", player.RemovedBuildingIds.ToArray());
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var building in player.UnsavedBuildings)
            {
                await using var command = new NpgsqlCommand(
                    @"INSERT INTO hadal.buildings (building_id, player_id, definition_id, ring, sector, level, rotation_steps,
                                                   construction_ends_at, construction_seconds)
                      VALUES (@b, @p, @d, @r, @s, @l, @o, @e, @c)
                      ON CONFLICT (building_id) DO UPDATE SET level = EXCLUDED.level,
                          construction_ends_at = EXCLUDED.construction_ends_at,
                          construction_seconds = EXCLUDED.construction_seconds", connection, transaction);
                command.Parameters.AddWithValue("b", building.BuildingId);
                command.Parameters.AddWithValue("p", player.PlayerId);
                command.Parameters.AddWithValue("d", building.DefinitionId);
                command.Parameters.AddWithValue("r", building.Ring);
                command.Parameters.AddWithValue("s", building.Sector);
                command.Parameters.AddWithValue("l", building.Level);
                command.Parameters.AddWithValue("o", building.RotationSteps);
                command.Parameters.Add(new NpgsqlParameter("e", NpgsqlTypes.NpgsqlDbType.TimestampTz)
                {
                    Value = building.ConstructionEndsAt.HasValue ? building.ConstructionEndsAt.Value : DBNull.Value
                });
                command.Parameters.AddWithValue("c", building.ConstructionSeconds);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            player.UnsavedBuildings.Clear();
            player.RemovedBuildingIds.Clear();
        }

        private static string WriteWallet(IReadOnlyDictionary<ResourceType, long> wallet) =>
            JsonSerializer.Serialize(wallet.ToDictionary(p => p.Key.ToString(), p => p.Value));

        private static IEnumerable<KeyValuePair<ResourceType, long>> ReadWallet(string json)
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, long>>(json) ?? new Dictionary<string, long>();
            foreach (var pair in raw)
            {
                if (Enum.TryParse<ResourceType>(pair.Key, out var type) && type != ResourceType.Unknown)
                    yield return new KeyValuePair<ResourceType, long>(type, pair.Value);
            }
        }
    }
}
