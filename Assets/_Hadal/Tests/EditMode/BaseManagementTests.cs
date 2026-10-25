using System.IO;
using HADAL.Shared.Catalog;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Gameplay.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hadal.Tests.EditMode
{
    /// <summary>Client side of docs/23_Base_Management.md.</summary>
    public sealed class BaseManagementTests
    {
        [Test]
        public void Bundled_catalog_matches_the_shared_catalog()
        {
            var shared = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "shared/catalog/catalog.json"));
            var bundled = Resources.Load<TextAsset>(ClientCatalog.ResourcePath);
            Assert.IsNotNull(bundled, "Resources/catalog.json missing — run the project setup");
            Assert.AreEqual(shared.Replace("\r\n", "\n"), bundled.text.Replace("\r\n", "\n"),
                "Resources/catalog.json is stale: the client would show other costs than the server charges");
        }

        [Test]
        public void Client_progression_numbers_match_the_rules()
        {
            var catalog = ClientCatalog.Catalog;
            Assert.IsTrue(catalog.TryGetBuilding(BuildingProgression.ReactorId, out var reactor));
            var level2 = BuildingProgression.Cost(reactor, 2, catalog.Progression);
            Assert.AreEqual(124, level2.Find(c => c.Key == HADAL.Shared.Enums.ResourceType.Titanium).Value);
            Assert.AreEqual(64, BuildingProgression.Seconds(reactor, 2, catalog.Progression));
        }

        [Test]
        public void Live_catalog_from_the_overseer_replaces_the_bundled_one()
        {
            try
            {
                var json = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "shared/catalog/catalog.json"))
                    .Replace("\"buildSeconds\": 40", "\"buildSeconds\": 99");
                var changed = 0;
                ClientCatalog.Changed += Count;
                Assert.IsTrue(ClientCatalog.Apply(json, 7));
                ClientCatalog.Changed -= Count;

                Assert.AreEqual(1, changed);
                Assert.AreEqual(7, ClientCatalog.Version);
                Assert.IsTrue(ClientCatalog.Catalog.TryGetBuilding(BuildingProgression.ReactorId, out var reactor));
                Assert.AreEqual(99, BuildingProgression.Seconds(reactor, 1, ClientCatalog.Catalog.Progression));

                // A broken download never replaces a working catalog.
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("rejected"));
                Assert.IsFalse(ClientCatalog.Apply("{ not json", 8));
                Assert.AreEqual(7, ClientCatalog.Version);

                void Count() => changed++;
            }
            finally
            {
                ClientCatalog.Reset();
            }
        }

        [Test]
        public void Taps_hit_the_seabed_plane()
        {
            var ray = new Ray(new Vector3(0f, 50f, -50f), new Vector3(0f, -1f, 1f).normalized);
            Assert.IsTrue(BaseInteraction.TrySeabedPoint(ray, out var point));
            Assert.AreEqual(0f, point.y, 1e-4f);
            Assert.AreEqual(0f, point.z, 1e-3f);

            Assert.IsFalse(BaseInteraction.TrySeabedPoint(new Ray(Vector3.up, Vector3.up), out _), "looking up never hits the seabed");
        }
    }
}
