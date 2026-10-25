using HADAL.Shared.Catalog;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;

namespace Hadal.Game.Tests
{
    public sealed class CatalogAndPlacementTests
    {
        private static readonly GameCatalog Catalog = TestSupport.Catalog;
        private static readonly HashSet<(int, int)> Empty = new();
        private static readonly Dictionary<ResourceType, long> Rich = Enum.GetValues<ResourceType>().ToDictionary(t => t, _ => 10_000L);

        [Fact]
        public void RealCatalog_LoadsAndValidates()
        {
            Assert.True(Catalog.TryGetBuilding("oxygen_plant", out var plant));
            Assert.Equal(GridSlotType.Production, plant.SlotType);
            Assert.Equal(500, Catalog.StartingWallet()[ResourceType.Oxygen]);
            Assert.Equal(4, Catalog.Grid.startingUnlockedRings);
        }

        [Fact]
        public void InvalidCatalog_ReportsEveryProblem()
        {
            var data = new GameCatalogData
            {
                resources = new[] { new ResourceDefinitionData { type = "Gold" } },
                buildings = new[]
                {
                    new BuildingDefinitionData { id = "x", slotType = "Nope", minRing = 3, maxRing = 1,
                        buildCost = new[] { new ResourceAmountData { resource = "Oxygen", amount = -5 } } }
                }
            };

            var error = Assert.Throws<FormatException>(() => GameCatalog.Create(data));
            Assert.Contains("unknown type 'Gold'", error.Message);
            Assert.Contains("unknown slotType 'Nope'", error.Message);
            Assert.Contains("invalid ring range", error.Message);
            Assert.Contains("negative amount", error.Message);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(4, 5)]
        [InlineData(5, 7)]  // 6 + (5-4)*2 = 8 sectors
        [InlineData(12, 15)] // capped at 16
        public void SectorCounts_FollowRingRules(int ring, int lastValidSector)
        {
            Assert.True(Catalog.SlotExists(ring, lastValidSector));
            Assert.False(Catalog.SlotExists(ring, lastValidSector + 1));
        }

        [Fact]
        public void ValidPlacement_IsAccepted() =>
            Assert.Equal(PlacementRejectReason.None, Validate("oxygen_plant", 1, 0));

        [Theory]
        [InlineData("does_not_exist", 1, 0, PlacementRejectReason.UnknownBuilding)]
        [InlineData("oxygen_plant", 1, 3, PlacementRejectReason.SlotDoesNotExist)]
        [InlineData("oxygen_plant", 13, 0, PlacementRejectReason.SlotDoesNotExist)]
        [InlineData("mega_dome", 5, 0, PlacementRejectReason.RingLocked)]
        [InlineData("oxygen_plant", 2, 0, PlacementRejectReason.RingNotAllowed)]
        [InlineData("research_center", 1, 0, PlacementRejectReason.RingNotAllowed)]
        public void InvalidPlacement_IsRejected(string building, int ring, int sector, PlacementRejectReason expected) =>
            Assert.Equal(expected, Validate(building, ring, sector));

        [Fact]
        public void OccupiedSlot_IsRejected()
        {
            var occupied = new HashSet<(int, int)> { (1, 0) };
            Assert.Equal(PlacementRejectReason.SlotOccupied,
                PlacementRules.Validate(Catalog, "food_lab", 1, 0, 4, occupied, Rich));
        }

        [Fact]
        public void UniversalRing_AcceptsAnyAllowedBuilding()
        {
            // Ring 4 defaults to Universal: mega_dome (MegaDome slot type) fits.
            Assert.Equal(PlacementRejectReason.None, Validate("mega_dome", 4, 2));
        }

        [Fact]
        public void InsufficientResources_IsRejected()
        {
            var poor = new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 49 };
            Assert.Equal(PlacementRejectReason.InsufficientResources,
                PlacementRules.Validate(Catalog, "oxygen_plant", 1, 0, 4, Empty, poor));
        }

        [Fact]
        public void Charge_DeductsBuildCost()
        {
            Catalog.TryGetBuilding("oxygen_plant", out var plant);
            var wallet = new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 150 };
            PlacementRules.Charge(plant, wallet);
            Assert.Equal(100, wallet[ResourceType.Titanium]);
        }

        private static PlacementRejectReason Validate(string building, int ring, int sector) =>
            PlacementRules.Validate(Catalog, building, ring, sector, 4, Empty, Rich);
    }
}
