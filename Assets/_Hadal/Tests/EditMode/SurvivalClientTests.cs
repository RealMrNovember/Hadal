using System.Collections.Generic;
using HADAL.Shared.DTOs;
using HADAL.Shared.Enums;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.StateSync;
using Hadal.UI;
using NUnit.Framework;
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Tests.EditMode
{
    /// <summary>The survival loop on the client (docs/35): its state replicates, the bundled catalog has it, the alarms read as text.</summary>
    public sealed class SurvivalClientTests
    {
        [Test]
        public void The_survival_state_replicates_with_the_production_summary()
        {
            var view = new ClientStateView();
            view.ResetSession();
            Assert.IsNull(view.Survival);
            var serializer = new HADAL.Shared.Serialization.ProtobufNetNetworkSerializer();
            var production = new ProductionStateDto
            {
                ServerTick = 3,
                Survival = new SurvivalStateDto
                {
                    Energy = 2, EnergyPercent = 50, Oxygen = 1, EmergencyId = "e1", EmergencyKind = EmergencyKinds.Leak,
                    EmergencyBuildingId = "b1", EmergencyDefinitionId = "oxygen_plant", ExpiresRemainingMs = 3_600_000, RepairRemainingMs = 60_000,
                    RepairTotalMs = 600_000, RepairCost = new[] { new ResourceAmountEntry { Key = ResourceType.Titanium, Value = 120 } }
                }
            };
            var version = view.SurvivalVersion;
            ClientStateViewMutator.ApplyDelta(view, new StateDelta
            {
                ServerTick = 3,
                Changes = new List<EntityChangeDto>
                {
                    new() { EntityId = EntityIds.ProductionSummary, Kind = EntityChangeKind.Update, Payload = serializer.Serialize(production) }
                }
            }, new NetworkSerializationLayer(), new List<CommandAckDto>());

            Assert.AreNotEqual(version, view.SurvivalVersion);
            Assert.AreEqual(2, view.Survival.Energy);
            Assert.AreEqual(50, view.Survival.EnergyPercent);
            Assert.AreEqual("e1", view.Survival.EmergencyId);
            Assert.Greater(view.RepairEndsAt, 0f);
            Assert.Greater(view.EmergencyExpiresAt, view.RepairEndsAt);
            Assert.AreEqual(120, view.Survival.RepairCost[0].Value);
        }

        [Test]
        public void The_bundled_catalog_carries_the_survival_switches()
        {
            var survival = ClientCatalog.Catalog.Survival;
            Assert.IsTrue(survival.enabled);
            Assert.IsTrue(survival.emergencies.enabled);
            Assert.GreaterOrEqual(ClientCatalog.Catalog.Data.version, 13);
        }

        [Test]
        public void Alarms_and_hints_read_as_text_in_turkish_and_english()
        {
            var previous = Loc.Current;
            try
            {
                foreach (var language in new[] { "tr", "en" })
                {
                    Loc.Use(language);
                    foreach (var key in new[]
                             {
                                 "survival.title", "survival.hint.title", "survival.hint.oxygen", "survival.hint.energy", "survival.hint.emergency",
                                 "survival.emergency.leak", "survival.emergency.breakdown", "survival.alarm.oxygen.low", "reason.NoEmergency"
                             })
                        Assert.AreNotEqual(key, Loc.T(key), $"{language}: {key} missing");
                    StringAssert.Contains("20", Loc.F("survival.alarm.oxygen.critical", 20));
                    StringAssert.Contains("50", Loc.F("survival.alarm.energy.critical", 50));
                }

                Loc.Use("tr");
                Assert.AreEqual("Ne yapmalıyım?", Loc.T("survival.hint.title"));
            }
            finally
            {
                Loc.Use(previous);
            }
        }
    }
}
