using System;
using System.Linq;
using HADAL.Shared.Enums;
using Hadal.Core.StateSync;
using NUnit.Framework;

namespace Hadal.Tests.EditMode
{
    /// <summary>
    /// QA R1 / S1: two ResourceType enums once aliased Oxygen onto Energy. There must be exactly
    /// one enum, its numeric values are wire contract, and unknown ordinals must never alias.
    /// </summary>
    public sealed class ResourceTypeContractTests
    {
        [Test]
        public void WireValues_ArePinned()
        {
            // Changing any of these silently relabels every stored and replicated balance.
            Assert.AreEqual(0, (int)ResourceType.Unknown);
            Assert.AreEqual(1, (int)ResourceType.Oxygen);
            Assert.AreEqual(2, (int)ResourceType.Food);
            Assert.AreEqual(3, (int)ResourceType.Biomass);
            Assert.AreEqual(4, (int)ResourceType.Research);
            Assert.AreEqual(5, (int)ResourceType.Alloy);
            Assert.AreEqual(6, (int)ResourceType.Energy);
            Assert.AreEqual(7, (int)ResourceType.Titanium);
            Assert.AreEqual(8, (int)ResourceType.Hadalite);
        }

        [Test]
        public void EveryKnownResource_RoundTripsThroughStoredValue()
        {
            foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
            {
                var stored = ResourceTypeMap.ToStoredValue(resource);
                Assert.AreEqual(resource, ResourceTypeMap.FromStoredValue(stored), resource.ToString());
            }
        }

        [TestCase(-1)]
        [TestCase(9)]
        [TestCase(99)]
        [TestCase(int.MaxValue)]
        public void UnknownStoredValue_DegradesToUnknown(int stored)
        {
            Assert.AreEqual(ResourceType.Unknown, ResourceTypeMap.FromStoredValue(stored));
        }

        [Test]
        public void NoSecondResourceTypeEnum_ExistsInGameAssemblies()
        {
            var duplicates = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("Hadal.", StringComparison.Ordinal))
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsEnum && t.Name == nameof(ResourceType))
                .Select(t => t.FullName)
                .ToArray();

            CollectionAssert.IsEmpty(duplicates, "ResourceType must only exist in HADAL.Shared.");
        }
    }
}
