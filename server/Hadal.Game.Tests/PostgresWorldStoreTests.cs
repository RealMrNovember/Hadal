using Hadal.Game.World;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>Bases and resource nodes against a real PostgreSQL (HADAL_TEST_PG); skipped otherwise.</summary>
    public sealed class PostgresWorldStoreTests
    {
        private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("HADAL_TEST_PG");

        [Fact]
        public async Task Nodes_round_trip_and_update_in_place()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var store = new PostgresWorldStore(dataSource);
            await store.InitializeAsync(default);
            await store.InitializeAsync(default); // idempotent

            var id = "test" + Guid.NewGuid().ToString("N")[..8];
            var now = new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
            try
            {
                var node = new WorldNode { Id = id, Type = "balina_lesi", Level = 3, X = 5, Y = 6, Remaining = 1000, Capacity = 2000, FadesAt = now };
                await store.SaveNodesAsync(new[] { node }, default);
                node.Remaining = 400;
                node.BusyUntil = now.AddHours(1);
                node.BusyBy = "p1";
                await store.SaveNodesAsync(new[] { node }, default);

                var loaded = Assert.Single(await store.LoadNodesAsync(default), n => n.Id == id);
                Assert.Equal((400L, 2000L, "p1", "balina_lesi"), (loaded.Remaining, loaded.Capacity, loaded.BusyBy, loaded.Type));
                Assert.Equal(now.AddHours(1), loaded.BusyUntil);
                Assert.Equal(now, loaded.FadesAt);
                Assert.Null(loaded.DepletedAt);
            }
            finally
            {
                await using var command = dataSource.CreateCommand("DELETE FROM hadal.world_nodes WHERE id = @id");
                command.Parameters.AddWithValue("id", id);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
