using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Simulation;
using NUnit.Framework;

namespace Hadal.Tests.EditMode
{
    /// <summary>
    /// QA S3 / S4 / S7 and the §4 boundary list: caps, negatives, overflow, offline clamp,
    /// determinism, and that the shared economy has no engine dependency.
    /// </summary>
    public sealed class ProductionFormulaTests
    {
        [TestCase(10L, 1, 10L)]
        [TestCase(10L, 2, 12L)] // 125%, integer division
        [TestCase(10L, 3, 15L)]
        [TestCase(10L, 0, 10L)] // level < 1 treated as 1
        [TestCase(0L, 5, 0L)]
        public void ScaleByLevel(long basePerTick, int level, long expected)
        {
            Assert.AreEqual(expected, ProductionFormulas.ScaleByLevel(basePerTick, level));
        }

        [Test]
        public void Cap_StopsAtCapacity()
        {
            // S3: 95 + 10/tick for 5 ticks against a cap of 100.
            Assert.AreEqual(100, ProductionFormulas.ApplyToBalance(95, ProductionFormulas.Accrue(10, 5), 100));
        }

        [Test]
        public void NegativeNet_ClampsAtZero()
        {
            // S4: consumption outruns the balance.
            Assert.AreEqual(0, ProductionFormulas.ApplyToBalance(30, ProductionFormulas.Accrue(-10, 5), 0));
        }

        [Test]
        public void NonPositiveCapacity_MeansUncapped()
        {
            Assert.AreEqual(1_000_000, ProductionFormulas.ApplyToBalance(0, 1_000_000, 0));
            Assert.AreEqual(1_000_000, ProductionFormulas.ApplyToBalance(0, 1_000_000, -1));
        }

        [Test]
        public void DefaultCapacity_LongMax_DoesNotOverflow()
        {
            Assert.AreEqual(long.MaxValue, ProductionFormulas.ApplyToBalance(long.MaxValue - 5, 100, long.MaxValue));
        }

        [Test]
        public void ThirtyDaysOfAccrual_DoesNotOverflow()
        {
            var thirtyDays = (ulong)(30 * 24 * SimulationTick.TicksPerHour);
            Assert.AreEqual(1_000L * (long)thirtyDays, ProductionFormulas.Accrue(1_000, thirtyDays));
        }

        [Test]
        public void HugeAccrual_Saturates()
        {
            Assert.AreEqual(long.MaxValue, ProductionFormulas.Accrue(long.MaxValue / 2, 3));
            Assert.AreEqual(long.MinValue, ProductionFormulas.Accrue(long.MinValue / 2 + 1, 3));
            Assert.AreEqual(long.MaxValue, ProductionFormulas.Accrue(1, ulong.MaxValue));
        }

        [Test]
        public void OfflineGap_IsClampedToTwelveHours()
        {
            // S7 / S11: a week away (or a clock pushed forward a week) grants at most 12 h.
            var week = SimulationTick.TicksFromSeconds(7 * 24 * 3600);
            Assert.AreEqual((ulong)(12 * SimulationTick.TicksPerHour), SimulationTick.ClampOfflineTicks(week));
            Assert.AreEqual(10UL, SimulationTick.ClampOfflineTicks(10));
        }

        [Test]
        public void NegativeElapsedTime_GrantsNothing()
        {
            // S11: clock moved backwards.
            Assert.AreEqual(0UL, SimulationTick.TicksFromSeconds(-3600));
        }

        [Test]
        public void ReplicationCadence_DoesNotChangeEconomy()
        {
            // One 5-tick advance must equal five 1-tick advances (CTO decision #5).
            var catalog = Catalog(("oxygen_plant", ResourceType.Oxygen, 3, 0), ("oxygen_plant", ResourceType.Energy, 0, 1));
            var buildings = new[] { new BuildingProductionInput("oxygen_plant", 2) };
            var start = new Dictionary<ResourceType, long> { [ResourceType.Oxygen] = 100, [ResourceType.Energy] = 50 };

            var once = ProductionSimulator.Advance(start, buildings, catalog, null, 5).Totals;

            IReadOnlyDictionary<ResourceType, long> stepped = start;
            for (var i = 0; i < 5; i++)
                stepped = ProductionSimulator.Advance(stepped, buildings, catalog, null, 1).Totals;

            CollectionAssert.AreEquivalent(once, stepped);
        }

        [Test]
        public void SameInput_SameOutput()
        {
            var catalog = Catalog(("food_lab", ResourceType.Food, 2, 0), ("food_lab", ResourceType.Energy, 0, 1));
            var buildings = new[] { new BuildingProductionInput("food_lab", 3), new BuildingProductionInput("food_lab", 1) };
            var start = new Dictionary<ResourceType, long> { [ResourceType.Food] = 7, [ResourceType.Energy] = 9 };

            var a = ProductionSimulator.Advance(start, buildings, catalog, null, 1234);
            var b = ProductionSimulator.Advance(start, buildings, catalog, null, 1234);

            CollectionAssert.AreEquivalent(a.Totals, b.Totals);
            CollectionAssert.AreEquivalent(a.NetPerTick, b.NetPerTick);
        }

        [Test]
        public void ConsumerWithNoBalance_StaysAtZero()
        {
            var catalog = Catalog(("oxygen_plant", ResourceType.Energy, 0, 1));
            var result = ProductionSimulator.Advance(new Dictionary<ResourceType, long>(),
                new[] { new BuildingProductionInput("oxygen_plant", 1) }, catalog, null, 60);

            Assert.IsFalse(result.Totals.TryGetValue(ResourceType.Energy, out var energy) && energy != 0);
        }

        [Test]
        public void UnknownResourceRule_IsIgnored()
        {
            var catalog = Catalog(("x", ResourceType.Unknown, 5, 0));
            var net = ProductionSimulator.ComputeNetPerTick(new[] { new BuildingProductionInput("x", 1) }, catalog);
            Assert.IsEmpty(net);
        }

        [Test]
        public void SharedAssembly_HasNoEngineDependency()
        {
            var references = typeof(ProductionSimulator).Assembly.GetReferencedAssemblies().Select(a => a.Name);
            CollectionAssert.DoesNotContain(references, "UnityEngine");
            CollectionAssert.DoesNotContain(references, "UnityEngine.CoreModule");
        }

        private static ProductionCatalog Catalog(params (string building, ResourceType resource, long produce, long consume)[] rules) =>
            new(rules.Select(r => new ProductionRuleDto
            {
                BuildingDefinitionId = r.building,
                Resource = r.resource,
                ProductionPerTick = r.produce,
                ConsumptionPerTick = r.consume
            }));
    }
}
