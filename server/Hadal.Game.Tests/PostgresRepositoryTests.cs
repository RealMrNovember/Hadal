using Hadal.Game.Persistence;
using HADAL.Shared.Enums;
using Npgsql;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// Runs against a real PostgreSQL when HADAL_TEST_PG holds a connection string (the CT's
    /// hadal-postgres); skipped otherwise so the suite stays runnable anywhere.
    /// </summary>
    public sealed class PostgresRepositoryTests
    {
        private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("HADAL_TEST_PG");

        [Fact]
        public async Task CreateSaveLoad_RoundTrips()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var repository = new PostgresPlayerRepository(dataSource);
            await repository.InitializeAsync(default);
            await repository.InitializeAsync(default); // idempotent

            var deviceId = "pgtest-" + Guid.NewGuid().ToString("N");
            var now = DateTimeOffset.UtcNow;
            var player = new PlayerState
            {
                PlayerId = Guid.NewGuid(),
                DeviceId = deviceId,
                LastSimulatedAt = now,
                UnlockedRings = 4
            };
            player.Wallet[ResourceType.Oxygen] = 500;

            try
            {
                await repository.CreateAsync(player, default);

                var building = new PlacedBuilding { BuildingId = "b" + Guid.NewGuid().ToString("N"), DefinitionId = "oxygen_plant", Ring = 1, Sector = 0, RotationSteps = 2 };
                player.ServerTick = 42;
                player.Wallet[ResourceType.Oxygen] = 626;
                player.Wallet[ResourceType.Titanium] = 100;
                player.UnsavedBuildings.Add(building);
                await repository.SaveAsync(player, default);
                Assert.Empty(player.UnsavedBuildings);

                // A later save without buildings must not drop the stored ones.
                player.ServerTick = 50;
                await repository.SaveAsync(player, default);

                var loaded = await repository.LoadByDeviceAsync(deviceId, default);
                Assert.NotNull(loaded);
                Assert.Equal(player.PlayerId, loaded!.PlayerId);
                Assert.Equal(50UL, loaded.ServerTick);
                Assert.Equal(626, loaded.Wallet[ResourceType.Oxygen]);
                Assert.Equal(100, loaded.Wallet[ResourceType.Titanium]);
                Assert.Equal(now.ToUnixTimeMilliseconds(), loaded.LastSimulatedAt.ToUnixTimeMilliseconds());
                var stored = Assert.Single(loaded.Buildings);
                Assert.Equal(building.BuildingId, stored.BuildingId);
                Assert.Equal(2, stored.RotationSteps);

                Assert.Null(await repository.LoadByDeviceAsync("pgtest-missing-" + Guid.NewGuid(), default));
            }
            finally
            {
                await using var cleanup = dataSource.CreateCommand("DELETE FROM hadal.players WHERE device_id = @d");
                cleanup.Parameters.AddWithValue("d", deviceId);
                await cleanup.ExecuteNonQueryAsync();
            }
        }
    }
}
