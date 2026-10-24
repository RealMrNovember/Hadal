using System.IO;
using System.Linq;
using HADAL.Shared.Catalog;
using Hadal.Data.Config;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>
    /// The server validates and simulates with shared/catalog/catalog.json; the client reads the
    /// ScriptableObjects generated from it. If someone edits the JSON without re-running
    /// Hadal > Setup (or edits an SO by hand), client previews and server verdicts disagree.
    /// </summary>
    public sealed class CatalogSyncTests
    {
        private const string SettingsPath = "Assets/_Hadal/Settings/ScriptableObjects";

        private static GameCatalog LoadCatalog() =>
            GameCatalog.Create(JsonUtility.FromJson<GameCatalogData>(
                File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "shared/catalog/catalog.json"))));

        [Test]
        public void BuildingDefinitions_MatchCatalog()
        {
            var catalog = LoadCatalog();
            var database = AssetDatabase.LoadAssetAtPath<BuildingDatabaseSO>(SettingsPath + "/BuildingDatabase.asset");
            Assert.IsNotNull(database, "Run Hadal > Setup > Create Full Project Data.");

            Assert.AreEqual(catalog.Buildings.Count(), database.Buildings.Count, "building count");
            foreach (var building in catalog.Buildings)
            {
                var so = database.GetById(building.Id);
                Assert.IsNotNull(so, building.Id);
                Assert.AreEqual(building.SlotType, so.RequiredSlotType, building.Id + " slot type");
                Assert.AreEqual(building.MinRing, so.MinRing, building.Id + " min ring");
                Assert.AreEqual(building.MaxRing, so.MaxRing, building.Id + " max ring");
                CollectionAssert.AreEqual(building.Production.Select(p => (p.Key, p.Value)),
                    so.ProductionPerTick.Select(p => (p.Type, p.Amount)), building.Id + " production");
                CollectionAssert.AreEqual(building.Consumption.Select(p => (p.Key, p.Value)),
                    so.ConsumptionPerTick.Select(p => (p.Type, p.Amount)), building.Id + " consumption");
                CollectionAssert.AreEqual(building.BuildCost.Select(p => (p.Key, p.Value)),
                    so.BuildCosts.Select(p => (p.Type, p.Amount)), building.Id + " cost");
            }
        }

        [Test]
        public void ResourceDefinitions_MatchCatalog()
        {
            var catalog = LoadCatalog();
            var database = AssetDatabase.LoadAssetAtPath<ResourceDatabaseSO>(SettingsPath + "/ResourceDatabase.asset");
            Assert.IsNotNull(database, "Run Hadal > Setup > Create Full Project Data.");

            foreach (var resource in catalog.Resources)
            {
                var so = database.Get(resource.Type);
                Assert.IsNotNull(so, resource.Type.ToString());
                Assert.AreEqual(resource.StartingAmount, so.StartingAmount, resource.Type + " starting amount");
                Assert.AreEqual(resource.MaxCapacity, so.MaxCapacity, resource.Type + " capacity");
            }
        }

        [Test]
        public void GridLayout_MatchesCatalog()
        {
            var catalog = LoadCatalog();
            var grid = AssetDatabase.LoadAssetAtPath<CircularGridConfigSO>(SettingsPath + "/CircularGridConfig.asset");
            Assert.IsNotNull(grid, "Run Hadal > Setup > Create Full Project Data.");

            Assert.AreEqual(catalog.Grid.startingUnlockedRings, grid.StartingUnlockedRings);
            Assert.AreEqual(catalog.Grid.maxRings, grid.MaxRings);

            for (var ring = 0; ring <= catalog.Grid.maxRings; ring++)
            {
                Assert.AreEqual(catalog.GetSectorCount(ring), grid.GetSectorCount(ring), $"ring {ring} sectors");

                var ringDef = grid.GetRing(ring);
                for (var sector = 0; sector < catalog.GetSectorCount(ring); sector++)
                {
                    var clientType = ringDef?.GetSector(sector)?.SlotType ?? ringDef?.DefaultSlotType ?? HADAL.Shared.Enums.GridSlotType.Universal;
                    Assert.AreEqual(catalog.GetSlotType(ring, sector), clientType, $"slot type R{ring}:S{sector}");
                }
            }
        }
    }
}
