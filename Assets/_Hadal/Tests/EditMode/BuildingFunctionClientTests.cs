using System.Collections.Generic;
using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Serialization;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.StateSync;
using NUnit.Framework;
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Tests.EditMode
{
    /// <summary>Research, building functions and exact rates on the client (docs/24).</summary>
    public sealed class BuildingFunctionClientTests
    {
        private readonly NetworkSerializationLayer _serialization = new();
        private readonly ProtobufNetNetworkSerializer _serializer = new();
        private ClientStateView _view;

        [SetUp]
        public void SetUp()
        {
            _view = new ClientStateView();
            _view.ResetSession();
        }

        private void Apply(EntityChangeDto change) =>
            ClientStateViewMutator.ApplyDelta(_view, new StateDelta { ServerTick = 2, Changes = new List<EntityChangeDto> { change } },
                _serialization, new List<CommandAckDto>());

        [Test]
        public void Research_and_salvage_reports_replicate()
        {
            ClientStateViewMutator.ApplySnapshot(_view, new StateSnapshot
            {
                ServerTick = 1,
                Research = new ResearchStateDto
                {
                    Levels = new[] { new ResearchLevelEntry { NodeId = "oxygen_yield", Level = 2 } },
                    Active = new[] { new ActiveResearchEntry { NodeId = "food_yield", TargetLevel = 1, RemainingMs = 30_000, TotalMs = 90_000 } }
                }
            }, _serialization);

            Assert.AreEqual(2, _view.ResearchLevels["oxygen_yield"]);
            Assert.AreEqual("food_yield", _view.ActiveResearch.Single().NodeId);
            Assert.AreEqual(90f, _view.ActiveResearch.Single().TotalSeconds);

            Apply(new EntityChangeDto
            {
                EntityId = EntityIds.SalvageReport + "b1",
                Kind = EntityChangeKind.Create,
                Payload = _serializer.Serialize(new SalvageReportDto
                {
                    BuildingEntityId = "b1",
                    MissionId = "quick",
                    Rewards = ResourceAmountEntry.FromMap(new Dictionary<ResourceType, long> { [ResourceType.Titanium] = 1500 })
                })
            });
            Assert.IsTrue(_view.TryTakeSalvageReport(out var report));
            Assert.AreEqual("quick", report.MissionId);
            Assert.IsFalse(_view.TryTakeSalvageReport(out _));
        }

        [Test]
        public void Exact_rates_and_research_capacities_replicate()
        {
            _view.ApplyProduction(new ProductionStateDto
            {
                NetPerTick = new Dictionary<ResourceType, long> { [ResourceType.Food] = 2 },
                NetMilliPerTick = new Dictionary<ResourceType, long> { [ResourceType.Food] = 2500 },
                Capacities = new Dictionary<ResourceType, long> { [ResourceType.Food] = 5250 }
            });

            Assert.AreEqual(2500, _view.ProductionMilliPerTick[ResourceType.Food]);
            Assert.AreEqual(5250, _view.Capacities[ResourceType.Food]);
        }

        [Test]
        public void The_shared_catalog_has_the_research_tree_and_its_effects_add_up()
        {
            var catalog = ClientCatalog.Catalog;
            Assert.GreaterOrEqual(catalog.ResearchNodes.Count, 10);
            Assert.IsTrue(catalog.TryGetSalvageMission("quick", out _));

            var modifiers = ResearchRules.Modifiers(catalog, new Dictionary<string, int>
            {
                ["construction_mastery"] = 3, ["oxygen_yield"] = 2, ["extra_oxygen_line"] = 1, ["second_builder"] = 1
            });
            Assert.AreEqual(-9, modifiers.BuildTimePercent);
            Assert.AreEqual(8, modifiers.ProductionPercent[ResourceType.Oxygen]);
            Assert.AreEqual(1, modifiers.ExtraBuilders);
            Assert.IsTrue(catalog.TryGetBuilding("oxygen_plant", out var plant));
            Assert.AreEqual(plant.MaxCount + 1, modifiers.MaxCount(plant));
        }

        [Test]
        public void The_propulsion_research_cuts_a_20_minute_trip_to_about_12_minutes()
        {
            var catalog = ClientCatalog.Catalog;
            Assert.IsTrue(catalog.TryGetResearch("thruster_efficiency", out var thruster));
            Assert.AreEqual("propulsion", thruster.Tree);
            Assert.AreEqual(1, thruster.RequiredCenterLevel);

            var full = ResearchRules.Modifiers(catalog, new Dictionary<string, int>
            {
                ["thruster_efficiency"] = 10, ["hydrodynamic_hull"] = 5, ["quick_return"] = 5
            });
            Assert.AreEqual(65, full.MarchSpeedPercent);
            Assert.AreEqual(25, full.ReturnSpeedPercent);
            Assert.AreEqual(727, full.MarchSeconds(1200)); // 20:00 → 12:07
            Assert.AreEqual(582, full.ReturnSeconds(727));

            // VIP stacks on top.
            VipRules.Apply(full, 4);
            Assert.AreEqual(70, full.MarchSpeedPercent);
        }

        [Test]
        public void Small_producers_show_every_level()
        {
            // 2 per tick: level 1 = 120/min, level 2 = 150/min (was 120 when rates were whole numbers).
            Assert.AreEqual(2000, ProductionFormulas.ScaleByLevelMilli(2, 1));
            Assert.AreEqual(2500, ProductionFormulas.ScaleByLevelMilli(2, 2));
            Assert.AreEqual(3250, ProductionFormulas.ScaleByLevelMilli(2, 2, 30)); // boosted
        }
    }
}
