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
        private static readonly string? ConnectionString = PostgresTest.ConnectionString;

        [Fact]
        public async Task CreateSaveLoad_RoundTrips()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return; // not configured on this machine

            await using var dataSource = NpgsqlDataSource.Create(ConnectionString);
            var repository = new PostgresPlayerRepository(dataSource);
            await repository.InitializeAsync(default);
            await repository.InitializeAsync(default); // idempotent

            var identityKey = "pgtest-" + Guid.NewGuid().ToString("N");
            var now = DateTimeOffset.UtcNow;
            var player = new PlayerState
            {
                PlayerId = Guid.NewGuid(),
                IdentityKey = identityKey,
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

                var loaded = await repository.LoadByIdentityAsync(identityKey, default);
                Assert.NotNull(loaded);
                Assert.Equal(player.PlayerId, loaded!.PlayerId);
                Assert.Equal(50UL, loaded.ServerTick);
                Assert.Equal(626, loaded.Wallet[ResourceType.Oxygen]);
                Assert.Equal(100, loaded.Wallet[ResourceType.Titanium]);
                Assert.Equal(now.ToUnixTimeMilliseconds(), loaded.LastSimulatedAt.ToUnixTimeMilliseconds());
                var stored = Assert.Single(loaded.Buildings);
                Assert.Equal(building.BuildingId, stored.BuildingId);
                Assert.Equal(2, stored.RotationSteps);

                // Removing deletes the row; a new building may take the same slot in the same save.
                var replacement = new PlacedBuilding { BuildingId = "b" + Guid.NewGuid().ToString("N"), DefinitionId = "food_lab", Ring = 1, Sector = 0 };
                player.RemovedBuildingIds.Add(building.BuildingId);
                player.UnsavedBuildings.Add(replacement);
                player.UnlockedRings = 6;
                player.ResearchLevels["construction_mastery"] = 3;
                replacement.BoostEndsAt = now.AddMinutes(30);
                player.Buildings.Add(replacement);
                player.ExtrasJson = PlayerExtras.Write(player);
                await repository.SaveAsync(player, default);
                Assert.Empty(player.RemovedBuildingIds);
                loaded = await repository.LoadByIdentityAsync(identityKey, default);
                Assert.Equal(replacement.BuildingId, Assert.Single(loaded!.Buildings).BuildingId);
                Assert.Equal(6, loaded.UnlockedRings);
                Assert.Equal(3, loaded.ResearchLevels["construction_mastery"]);
                Assert.Equal(now.AddMinutes(30).ToUnixTimeMilliseconds(), loaded.Buildings.Single().BoostEndsAt!.Value.ToUnixTimeMilliseconds());

                Assert.Null(await repository.LoadByIdentityAsync("pgtest-missing-" + Guid.NewGuid(), default));
            }
            finally
            {
                await using var cleanup = dataSource.CreateCommand("DELETE FROM hadal.players WHERE identity_key = @d");
                cleanup.Parameters.AddWithValue("d", identityKey);
                await cleanup.ExecuteNonQueryAsync();
            }
        }
    }
}
