using Hadal.Game.Persistence;
using HADAL.Shared.Simulation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hadal.Game.Tests
{
    /// <summary>Copy limits by reactor level (catalog countUnlocks): bases over the limit are trimmed and refunded.</summary>
    public sealed class BuildingLimitTests
    {
        private readonly FakeClock _clock = new();

        [Fact]
        public async Task Copies_over_the_limit_are_removed_and_refunded_into_the_bag()
        {
            var world = new GameWorld(new CatalogRuntime(TestSupport.BalanceCatalog), new InMemoryPlayerRepository(), _clock, TestSupport.Serializer,
                NullLogger<GameWorld>.Instance);
            var session = await world.ConnectAsync("limits", new RecordingConnection(), default, "limits");
            var state = session.StateForTests;
            var reactor = state.Buildings.Single(b => b.DefinitionId == BuildingProgression.ReactorId).Level;
            Assert.True(TestSupport.BalanceCatalog.TryGetBuilding("oxygen_plant", out var plant));
            var allowed = plant.CountAt(reactor);

            // Four oxygen plants at a reactor level that allows fewer.
            state.Buildings.RemoveAll(b => b.DefinitionId == "oxygen_plant");
            for (var i = 0; i < 4; i++)
                state.Buildings.Add(new PlacedBuilding { BuildingId = "ox" + i, DefinitionId = "oxygen_plant", Ring = 1, Sector = i % 3, Level = 4 - i });
            var itemsBefore = state.Items.Count;

            var refunds = session.EnforceBuildingLimitsForTests();

            var kept = state.Buildings.Where(b => b.DefinitionId == "oxygen_plant").ToList();
            Assert.Equal(allowed, kept.Count);
            Assert.Equal(Enumerable.Range(0, allowed).Select(i => 4 - i), kept.Select(b => b.Level).OrderByDescending(l => l)); // the strongest stay
            Assert.NotEmpty(refunds);
            Assert.True(state.Items.Count > itemsBefore);
            Assert.Contains("ox3", state.RemovedBuildingIds);

            // Within the limits nothing happens.
            Assert.Empty(session.EnforceBuildingLimitsForTests());
        }

        [Fact]
        public void The_shipped_catalog_opens_one_ring_and_unlocks_the_rest_in_order()
        {
            var catalog = TestSupport.BalanceCatalog;
            Assert.Equal(1, catalog.Grid.startingUnlockedRings);
            var last = 0;
            for (var ring = 2; ring <= catalog.Grid.maxRings; ring++)
            {
                Assert.True(catalog.TryGetRingUnlock(ring, out var unlock));
                Assert.True(unlock.ReactorLevel >= last);
                last = unlock.ReactorLevel;
            }
        }
    }
}
