using Hadal.Game.Kingdoms;
using Hadal.Game.World;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// The kingdom migration (docs/39 §3.1) against a real PostgreSQL (HADAL_TEST_PG), on a fresh database of its own:
    /// the old single world becomes Kingdom #1, twice over without change, two kingdoms then share tiles and node ids,
    /// and the down script restores the old keys. Skipped when HADAL_TEST_PG is not set.
    /// </summary>
    public sealed class PostgresKingdomTests
    {
        [Fact]
        public async Task The_old_world_becomes_kingdom_one_idempotently_and_reversibly()
        {
            if (string.IsNullOrEmpty(PostgresTest.ConnectionString))
                return;

            var name = "kingdoms_" + Guid.NewGuid().ToString("N")[..8];
            await using (var admin = NpgsqlDataSource.Create(PostgresTest.ConnectionString))
            await using (var create = admin.CreateCommand($"CREATE DATABASE {name}"))
                await create.ExecuteNonQueryAsync();
            var builder = new NpgsqlConnectionStringBuilder(PostgresTest.ConnectionString) { Database = name, Pooling = false };
            try
            {
                await using var data = NpgsqlDataSource.Create(builder.ConnectionString);
                async Task Run(string sql)
                {
                    await using var command = data.CreateCommand(sql);
                    await command.ExecuteNonQueryAsync();
                }

                async Task<long> Count(string sql)
                {
                    await using var command = data.CreateCommand(sql);
                    return Convert.ToInt64(await command.ExecuteScalarAsync());
                }

                // The world before kingdoms: two players with bases, the old keys.
                var p1 = Guid.NewGuid();
                var p2 = Guid.NewGuid();
                await Run($@"CREATE SCHEMA hadal;
CREATE TABLE hadal.players (player_id uuid PRIMARY KEY, created_at timestamptz NOT NULL);
INSERT INTO hadal.players VALUES ('{p1}', '2026-10-20T10:00:00Z'), ('{p2}', '2026-10-21T10:00:00Z');");
                var world = new PostgresWorldStore(data);
                await Run(@"CREATE TABLE hadal.world_bases (player_id text PRIMARY KEY, x integer NOT NULL, y integer NOT NULL, name text NOT NULL DEFAULT '',
    level integer NOT NULL DEFAULT 1, power bigint NOT NULL DEFAULT 0, UNIQUE (x, y));");
                await Run($"INSERT INTO hadal.world_bases (player_id, x, y) VALUES ('{p1:N}', 5, 5), ('{p2:N}', 6, 6);");

                var kingdoms = new PostgresKingdomStore(data);
                for (var i = 0; i < 2; i++)
                {
                    await world.InitializeAsync(default);
                    await kingdoms.InitializeAsync(default);
                }

                var (all, members) = await kingdoms.LoadAsync(default);
                var one = Assert.Single(all);
                Assert.Equal(("w1", 1, KingdomStatus.Open), (one.Id, one.Number, one.Status));
                Assert.Equal(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero), one.OpenedAt);
                Assert.Equal(new[] { p1.ToString("N"), p2.ToString("N") }.OrderBy(x => x), members.Select(m => m.PlayerId).OrderBy(x => x));
                Assert.All(members, m => Assert.Equal("w1", m.KingdomId));
                Assert.Equal(2, (await world.LoadAsync(default)).Count);

                // Kingdom #2 may use the same tile and the same node id; #1 does not see #2's rows.
                var two = world.ForWorld("w2");
                await two.SaveAsync(new WorldBaseRecord("p-two", 5, 5, "Two", 1, 0), default);
                await two.SaveNodesAsync(new[] { new WorldNode { Id = "n1", Type = "t", Level = 1, X = 1, Y = 1, Remaining = 1, Capacity = 1 } }, default);
                await world.SaveNodesAsync(new[] { new WorldNode { Id = "n1", Type = "t", Level = 1, X = 1, Y = 1, Remaining = 2, Capacity = 2 } }, default);
                Assert.Equal(2, (await world.LoadAsync(default)).Count);
                Assert.Equal("p-two", Assert.Single(await two.LoadAsync(default)).PlayerId);
                Assert.Equal(1, Assert.Single(await two.LoadNodesAsync(default)).Remaining);
                Assert.Equal(2, Assert.Single(await world.LoadNodesAsync(default)).Remaining);
                // A later start does not move #2's players into #1.
                await kingdoms.SaveMemberAsync(new KingdomMember("p-two", "w2", DateTimeOffset.UtcNow), default);
                await kingdoms.InitializeAsync(default);
                Assert.Equal("w2", (await kingdoms.LoadAsync(default)).Members.Single(m => m.PlayerId == "p-two").KingdomId);

                // The down script refuses while #2 has rows, then restores the old keys.
                var down = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "kingdoms_down.sql"));
                await Assert.ThrowsAsync<PostgresException>(() => Run(down));
                await Run("DELETE FROM hadal.world_bases WHERE world_id = 'w2'; DELETE FROM hadal.world_nodes WHERE world_id = 'w2';");
                await Run(down);
                Assert.Equal(1, await Count("SELECT count(*) FROM pg_constraint WHERE conname = 'world_bases_x_y_key'"));
                Assert.Equal(1, await Count("SELECT array_length(conkey, 1) FROM pg_constraint WHERE conname = 'world_nodes_pkey'"));
                Assert.Equal(0, await Count("SELECT count(*) FROM pg_tables WHERE schemaname = 'hadal' AND tablename LIKE 'kingdom%'"));

                // And up again.
                await world.InitializeAsync(default);
                await kingdoms.InitializeAsync(default);
                Assert.Equal(2, (await kingdoms.LoadAsync(default)).Members.Count);
            }
            finally
            {
                NpgsqlConnection.ClearAllPools();
                await using var admin = NpgsqlDataSource.Create(PostgresTest.ConnectionString);
                await using var drop = admin.CreateCommand($"DROP DATABASE IF EXISTS {name} WITH (FORCE)");
                await drop.ExecuteNonQueryAsync();
            }
        }
    }
}
