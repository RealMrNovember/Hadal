using Hadal.Game.Persistence;
using Hadal.Gateway.Admin;
using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>
    /// The backup restore drill and the balance rollback drill (roadmap §1 "Operasyon", runbook in
    /// docs/HADAL_SERVER_INFRASTRUCTURE.md). Runs only when HADAL_RESTORE_DRILL_PG names a database a backup was
    /// restored into (server/tests/ct-restore-drill.sh does that on CT 160); never against the live "hadal" database.
    /// The real gateway boots on the restored data, every player loads through the repository code, sample players
    /// log in, and an older balance version is rolled back to and survives a restart.
    /// </summary>
    public sealed class RestoreDrillTests
    {
        private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("HADAL_RESTORE_DRILL_PG");
        private readonly ITestOutputHelper _output;

        public RestoreDrillTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private sealed class DrillGateway : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseSetting("Game:CatalogPath", Path.Combine(AppContext.BaseDirectory, "catalog", "catalog.json"));
                builder.UseSetting("ConnectionStrings:Hadal", ConnectionString);
                builder.UseSetting("Auth:JwtSecret", "drill-secret-drill-secret-drill-secret-01");
                builder.UseSetting("Version:MinClientVersion", "0.1.0");
            }
        }

        private static bool Configured()
        {
            if (string.IsNullOrEmpty(ConnectionString))
                return false; // no restored database on this machine
            Assert.False(PostgresTest.IsLiveDatabase(ConnectionString), "The drill must never run against the live database.");
            return true;
        }

        [Fact]
        public async Task The_restored_backup_boots_the_gateway_and_every_player_loads()
        {
            if (!Configured())
                return;

            await using var gateway = new DrillGateway();
            var health = await gateway.CreateClient().GetAsync("/health");
            Assert.True(health.IsSuccessStatusCode);

            var world = gateway.Services.GetRequiredService<GameWorld>();
            var repository = gateway.Services.GetRequiredService<IPlayerRepository>();
            var ids = await repository.ListPlayerIdsAsync(default);
            Assert.NotEmpty(ids);

            var identities = new List<string>();
            long buildings = 0, hadalite = 0;
            foreach (var id in ids)
            {
                var identity = await repository.IdentityOfAsync(id, default);
                Assert.False(string.IsNullOrEmpty(identity), $"player {id:N} has no identity");
                var state = await repository.LoadByIdentityAsync(identity!, default);
                Assert.NotNull(state);
                Assert.Equal(id, state!.PlayerId);
                Assert.All(state.Wallet, p => Assert.True(p.Value >= 0, $"{id:N} {p.Key} = {p.Value}"));
                Assert.All(state.Items, p => Assert.True(p.Value >= 0, $"{id:N} item {p.Key} = {p.Value}"));
                buildings += state.Buildings.Count;
                hadalite += state.Wallet.TryGetValue(ResourceType.Hadalite, out var h) ? h : 0;
                identities.Add(identity!);
            }

            _output.WriteLine($"players {ids.Count}, buildings {buildings}, hadalite total {hadalite}, catalog v{world.Catalog.Version}");

            // A sample of players logs in on the restored data: snapshot, offline accrual, then a clean logout.
            foreach (var identity in identities.Take(25))
            {
                var connection = new RecordingConnection();
                var session = await world.ConnectAsync(identity, connection, default);
                var snapshot = connection.Messages.First(m => m.Snapshot != null).Snapshot();
                Assert.NotEmpty(snapshot.Buildings);
                Assert.All(snapshot.Resources.Amounts, p => Assert.True(p.Value >= 0));
                await world.DisconnectAsync(session, connection, default);
            }

            _output.WriteLine($"logged in {Math.Min(25, identities.Count)} players on the restored data");
        }

        [Fact]
        public async Task A_balance_rollback_takes_effect_at_once_and_survives_a_restart()
        {
            if (!Configured())
                return;

            int rolledBack;
            string rolledBackJson;
            await using (var gateway = new DrillGateway())
            {
                Assert.True((await gateway.CreateClient().GetAsync("/health")).IsSuccessStatusCode);
                var store = gateway.Services.GetRequiredService<IAdminStore>();
                var world = gateway.Services.GetRequiredService<GameWorld>();
                var live = gateway.Services.GetRequiredService<LiveCatalog>();
                var versions = await store.ListCatalogsAsync(50, default);
                Assert.NotEmpty(versions);
                var running = versions.Max(v => v.Version);
                var target = versions.Where(v => v.Version < running).OrderByDescending(v => v.Version).FirstOrDefault() ?? versions.First(v => v.Version == running);
                var old = await store.GetCatalogAsync(target.Version, default);
                Assert.NotNull(old);

                // The older version must still load under today's code, or a rollback would fail when needed.
                var catalog = CatalogLoader.Parse(old!.Json);

                // What the Overseer's "restore" does (AdminEndpoints.PublishCatalogAsync): store it as a new version, swap it in.
                rolledBack = await store.AddCatalogAsync(old.Json, "drill", $"restore of v{target.Version}", default);
                world.ReplaceCatalog(catalog, rolledBack);
                live.Set(rolledBack, old.Json);
                rolledBackJson = old.Json;
                Assert.Equal(rolledBack, world.Catalog.Version);
                var current = await store.GetCatalogAsync(running, default);
                _output.WriteLine($"running v{running} (design {CatalogBootstrap.DesignVersion(current?.Json ?? "{}")}), " +
                                  $"rolled back to v{target.Version} (design {CatalogBootstrap.DesignVersion(old.Json)}) as v{rolledBack}");

                // Players log in under the rolled-back rules.
                var repository = gateway.Services.GetRequiredService<IPlayerRepository>();
                var identity = await repository.IdentityOfAsync((await repository.ListPlayerIdsAsync(default)).First(), default);
                var connection = new RecordingConnection();
                var session = await world.ConnectAsync(identity!, connection, default);
                Assert.NotNull(connection.Messages.First(m => m.Snapshot != null).Snapshot());
                await world.DisconnectAsync(session, connection, default);
            }

            // A restart keeps the rollback, unless the image's catalog file is a newer design (then it supersedes it:
            // the runbook says to pin Game:PublishCatalogUpTo, i.e. HADAL_PUBLISH_CATALOG_UP_TO, in that case).
            await using (var restarted = new DrillGateway())
            {
                Assert.True((await restarted.CreateClient().GetAsync("/health")).IsSuccessStatusCode);
                var live = restarted.Services.GetRequiredService<LiveCatalog>();
                var file = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "catalog", "catalog.json"));
                if (CatalogBootstrap.FileSupersedes(file, rolledBackJson))
                {
                    _output.WriteLine($"the release file (design {CatalogBootstrap.DesignVersion(file)}) superseded the rollback on restart: pin HADAL_PUBLISH_CATALOG_UP_TO");
                    Assert.True(live.Version > rolledBack);
                }
                else
                {
                    Assert.Equal(rolledBack, live.Version);
                    Assert.Equal(rolledBack, restarted.Services.GetRequiredService<GameWorld>().Catalog.Version);
                    _output.WriteLine($"after a restart the game still runs v{live.Version}");
                }
            }
        }
    }
}
