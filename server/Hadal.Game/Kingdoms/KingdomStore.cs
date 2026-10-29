using System.Collections.Concurrent;
using Hadal.Game.Persistence;
using Npgsql;

namespace Hadal.Game.Kingdoms
{
    /// <summary>The kingdom statuses (docs/39 §1).</summary>
    public static class KingdomStatus
    {
        /// <summary>Takes new players.</summary>
        public const string Open = "open";

        /// <summary>Reached its cap or age limit: no new players.</summary>
        public const string Full = "full";

        /// <summary>Closed by an admin: no new players.</summary>
        public const string Closed = "closed";

        public static bool IsValid(string status) => status is Open or Full or Closed;
    }

    /// <summary>A kingdom: one world map of its own (docs/39). <see cref="OpenDays"/> 0 = no age limit.</summary>
    public sealed record Kingdom(string Id, int Number, string Name, DateTimeOffset OpenedAt, string Status, int PlayerCap, int OpenDays)
    {
        public static string IdOf(int number) => "w" + number;
    }

    /// <summary>A player's kingdom.</summary>
    public sealed record KingdomMember(string PlayerId, string KingdomId, DateTimeOffset JoinedAt);

    public interface IKingdomStore
    {
        /// <summary>Creates the tables, Kingdom #1 and, the first time, every existing player's membership in it.</summary>
        Task InitializeAsync(CancellationToken cancellationToken);

        Task<(List<Kingdom> Kingdoms, List<KingdomMember> Members)> LoadAsync(CancellationToken cancellationToken);

        Task SaveKingdomAsync(Kingdom kingdom, CancellationToken cancellationToken);

        Task SaveMemberAsync(KingdomMember member, CancellationToken cancellationToken);

        Task DeleteMemberAsync(string playerId, CancellationToken cancellationToken);
    }

    /// <summary>The kingdom tables and the migration of the old single world into Kingdom #1 (docs/39 §3.1). Idempotent.</summary>
    public static class KingdomSchema
    {
        public const string Tables = @"
CREATE SCHEMA IF NOT EXISTS hadal;
CREATE TABLE IF NOT EXISTS hadal.kingdoms (
    id          text        PRIMARY KEY,
    number      integer     NOT NULL UNIQUE,
    name        text        NOT NULL DEFAULT '',
    opened_at   timestamptz NOT NULL DEFAULT now(),
    status      text        NOT NULL DEFAULT 'open',
    player_cap  integer     NOT NULL DEFAULT 1000,
    open_days   integer     NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS hadal.kingdom_members (
    player_id   text        PRIMARY KEY,
    kingdom_id  text        NOT NULL,
    joined_at   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS kingdom_members_kingdom ON hadal.kingdom_members (kingdom_id);
INSERT INTO hadal.kingdoms (id, number) VALUES ('w1', 1) ON CONFLICT (id) DO NOTHING;";

        /// <summary>Every player from before kingdoms joins #1, once: only while no membership exists at all.</summary>
        public const string Members = @"
DO $$
BEGIN
    IF to_regclass('hadal.players') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM hadal.kingdom_members) THEN
        INSERT INTO hadal.kingdom_members (player_id, kingdom_id, joined_at)
        SELECT replace(player_id::text, '-', ''), 'w1', created_at FROM hadal.players
        ON CONFLICT (player_id) DO NOTHING;
        UPDATE hadal.kingdoms SET opened_at = coalesce((SELECT min(created_at) FROM hadal.players), opened_at) WHERE id = 'w1';
    END IF;
END $$;";

        /// <summary>The world tables' keys become per kingdom: a tile, a node id and a region are unique within one map.</summary>
        public const string WorldKeys = @"
DO $$
BEGIN
    IF to_regclass('hadal.world_bases') IS NOT NULL THEN
        IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'world_bases_x_y_key') THEN
            ALTER TABLE hadal.world_bases DROP CONSTRAINT world_bases_x_y_key;
        END IF;
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'world_bases_world_x_y_key') THEN
            ALTER TABLE hadal.world_bases ADD CONSTRAINT world_bases_world_x_y_key UNIQUE (world_id, x, y);
        END IF;
    END IF;
    IF to_regclass('hadal.world_nodes') IS NOT NULL
       AND (SELECT array_length(conkey, 1) FROM pg_constraint WHERE conname = 'world_nodes_pkey') = 1 THEN
        ALTER TABLE hadal.world_nodes DROP CONSTRAINT world_nodes_pkey;
        ALTER TABLE hadal.world_nodes ADD CONSTRAINT world_nodes_pkey PRIMARY KEY (world_id, id);
    END IF;
    IF to_regclass('hadal.world_regions') IS NOT NULL
       AND (SELECT array_length(conkey, 1) FROM pg_constraint WHERE conname = 'world_regions_pkey') = 1 THEN
        ALTER TABLE hadal.world_regions DROP CONSTRAINT world_regions_pkey;
        ALTER TABLE hadal.world_regions ADD CONSTRAINT world_regions_pkey PRIMARY KEY (world_id, region);
    END IF;
END $$;";
    }

    public sealed class InMemoryKingdomStore : IKingdomStore
    {
        private readonly ConcurrentDictionary<string, Kingdom> _kingdoms = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, KingdomMember> _members = new(StringComparer.Ordinal);

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            _kingdoms.TryAdd("w1", new Kingdom("w1", 1, string.Empty, DateTimeOffset.UnixEpoch, KingdomStatus.Open, KingdomRules.DefaultCap, 0));
            return Task.CompletedTask;
        }

        public Task<(List<Kingdom> Kingdoms, List<KingdomMember> Members)> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult((_kingdoms.Values.ToList(), _members.Values.ToList()));

        public Task SaveKingdomAsync(Kingdom kingdom, CancellationToken cancellationToken)
        {
            _kingdoms[kingdom.Id] = kingdom;
            return Task.CompletedTask;
        }

        public Task SaveMemberAsync(KingdomMember member, CancellationToken cancellationToken)
        {
            _members[member.PlayerId] = member;
            return Task.CompletedTask;
        }

        public Task DeleteMemberAsync(string playerId, CancellationToken cancellationToken)
        {
            _members.TryRemove(playerId, out _);
            return Task.CompletedTask;
        }
    }

    public sealed class PostgresKingdomStore : IKingdomStore
    {
        private readonly NpgsqlDataSource _dataSource;

        public PostgresKingdomStore(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await PostgresSchema.ApplyAsync(_dataSource, KingdomSchema.Tables, cancellationToken);
            await PostgresSchema.ApplyAsync(_dataSource, KingdomSchema.Members, cancellationToken);
        }

        public async Task<(List<Kingdom> Kingdoms, List<KingdomMember> Members)> LoadAsync(CancellationToken cancellationToken)
        {
            var kingdoms = new List<Kingdom>();
            var members = new List<KingdomMember>();
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using (var command = new NpgsqlCommand("SELECT id, number, name, opened_at, status, player_cap, open_days FROM hadal.kingdoms", connection))
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                while (await reader.ReadAsync(cancellationToken))
                    kingdoms.Add(new Kingdom(reader.GetString(0), reader.GetInt32(1), reader.GetString(2), reader.GetFieldValue<DateTimeOffset>(3),
                        reader.GetString(4), reader.GetInt32(5), reader.GetInt32(6)));
            await using (var command = new NpgsqlCommand("SELECT player_id, kingdom_id, joined_at FROM hadal.kingdom_members", connection))
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                while (await reader.ReadAsync(cancellationToken))
                    members.Add(new KingdomMember(reader.GetString(0), reader.GetString(1), reader.GetFieldValue<DateTimeOffset>(2)));
            return (kingdoms, members);
        }

        public async Task SaveKingdomAsync(Kingdom k, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
INSERT INTO hadal.kingdoms (id, number, name, opened_at, status, player_cap, open_days) VALUES (@i, @n, @name, @o, @s, @c, @d)
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, status = EXCLUDED.status, player_cap = EXCLUDED.player_cap, open_days = EXCLUDED.open_days",
                connection);
            command.Parameters.AddWithValue("i", k.Id);
            command.Parameters.AddWithValue("n", k.Number);
            command.Parameters.AddWithValue("name", k.Name);
            command.Parameters.AddWithValue("o", k.OpenedAt);
            command.Parameters.AddWithValue("s", k.Status);
            command.Parameters.AddWithValue("c", k.PlayerCap);
            command.Parameters.AddWithValue("d", k.OpenDays);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task SaveMemberAsync(KingdomMember m, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand(@"
INSERT INTO hadal.kingdom_members (player_id, kingdom_id, joined_at) VALUES (@p, @k, @j)
ON CONFLICT (player_id) DO UPDATE SET kingdom_id = EXCLUDED.kingdom_id", connection);
            command.Parameters.AddWithValue("p", m.PlayerId);
            command.Parameters.AddWithValue("k", m.KingdomId);
            command.Parameters.AddWithValue("j", m.JoinedAt);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task DeleteMemberAsync(string playerId, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = new NpgsqlCommand("DELETE FROM hadal.kingdom_members WHERE player_id = @p", connection);
            command.Parameters.AddWithValue("p", playerId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
