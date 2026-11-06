using Hadal.Game.Economy;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Runs against a real PostgreSQL when HADAL_TEST_PG holds a connection string (the CT's
    /// hadal-postgres); skipped otherwise so the suite stays runnable anywhere.
    /// </summary>
    public sealed class EconomyFlowPostgresTests
    {
        private static readonly string? ConnectionString = PostgresTest.ConnectionString;

        [Fact]
        public async Task RecordLoad_RoundTrips_AndSchemaIsIdempotent()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var store = new PostgresEconomyFlowStore(dataSource);
            await store.InitializeAsync(default);
            await store.InitializeAsync(default); // idempotent

            var player = Guid.NewGuid().ToString("N");
            var at = DateTimeOffset.UtcNow;
            try
            {
                await store.RecordAsync(new EconomyFlowEvent(player, "reward", "Oxygen", 25, at), default);
                await store.RecordAsync(new EconomyFlowEvent(player, "building", "Oxygen", -10, at), default);
                var rows = await store.LoadAsync(at.AddSeconds(-1), default);

                Assert.Contains(rows, e => e.PlayerId == player && e.Source == "reward" && e.Amount == 25);
                Assert.Contains(rows, e => e.PlayerId == player && e.Source == "building" && e.Amount == -10);
            }
            finally
            {
                await using var cleanup = dataSource.CreateCommand("DELETE FROM hadal.economy_events WHERE player_id = @p");
                cleanup.Parameters.AddWithValue("p", player);
                await cleanup.ExecuteNonQueryAsync();
            }
        }
    }
}
