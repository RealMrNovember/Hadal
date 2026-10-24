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

CREATE TABLE IF NOT EXISTS hadal.players (
    player_id         uuid        PRIMARY KEY,
    device_id         text        NOT NULL UNIQUE,
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
);";

        private readonly NpgsqlDataSource _dataSource;

        public PostgresPlayerRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(Schema);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<PlayerState?> LoadByDeviceAsync(string deviceId, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            PlayerState player;
            await using (var command = new NpgsqlCommand(
                "SELECT player_id, server_tick, last_simulated_at, unlocked_rings, wallet::text FROM hadal.players WHERE device_id = @d",
                connection))
            {
                command.Parameters.AddWithValue("d", deviceId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                    return null;

                player = new PlayerState
                {
                    PlayerId = reader.GetGuid(0),
                    DeviceId = deviceId,
                    ServerTick = (ulong)reader.GetInt64(1),
                    LastSimulatedAt = reader.GetFieldValue<DateTimeOffset>(2),
                    UnlockedRings = reader.GetInt32(3)
                };

                foreach (var pair in ReadWallet(reader.GetString(4)))
                    player.Wallet[pair.Key] = pair.Value;
            }

            await using (var command = new NpgsqlCommand(
                "SELECT building_id, definition_id, ring, sector, level, rotation_steps FROM hadal.buildings WHERE player_id = @p ORDER BY created_at",
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
                        RotationSteps = reader.GetInt32(5)
                    });
                }
            }

            return player;
        }

        public async Task CreateAsync(PlayerState player, CancellationToken cancellationToken)
        {
            await using var command = _dataSource.CreateCommand(
                @"INSERT INTO hadal.players (player_id, device_id, server_tick, last_simulated_at, unlocked_rings, wallet)
                  VALUES (@p, @d, @t, @s, @u, @w::jsonb)");
            command.Parameters.AddWithValue("p", player.PlayerId);
            command.Parameters.AddWithValue("d", player.DeviceId);
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
                  SET server_tick = @t, last_simulated_at = @s, unlocked_rings = @u, wallet = @w::jsonb
                  WHERE player_id = @p", connection, transaction))
            {
                command.Parameters.AddWithValue("p", player.PlayerId);
                command.Parameters.AddWithValue("t", (long)player.ServerTick);
                command.Parameters.AddWithValue("s", player.LastSimulatedAt);
                command.Parameters.AddWithValue("u", player.UnlockedRings);
                command.Parameters.AddWithValue("w", WriteWallet(player.Wallet));
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var building in player.UnsavedBuildings)
            {
                await using var command = new NpgsqlCommand(
                    @"INSERT INTO hadal.buildings (building_id, player_id, definition_id, ring, sector, level, rotation_steps)
                      VALUES (@b, @p, @d, @r, @s, @l, @o)
                      ON CONFLICT (building_id) DO UPDATE SET level = EXCLUDED.level", connection, transaction);
                command.Parameters.AddWithValue("b", building.BuildingId);
                command.Parameters.AddWithValue("p", player.PlayerId);
                command.Parameters.AddWithValue("d", building.DefinitionId);
                command.Parameters.AddWithValue("r", building.Ring);
                command.Parameters.AddWithValue("s", building.Sector);
                command.Parameters.AddWithValue("l", building.Level);
                command.Parameters.AddWithValue("o", building.RotationSteps);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            player.UnsavedBuildings.Clear();
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
