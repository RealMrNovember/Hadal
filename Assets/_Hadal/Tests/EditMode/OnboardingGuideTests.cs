using System.Linq;
using HADAL.Shared.DTOs;
using HADAL.Shared.Protocol;
using HADAL.Shared.Simulation;
using Hadal.Core.Catalog;
using Hadal.Core.StateSync;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;
using NetworkSerializationLayer = Hadal.Core.Network.NetworkSerializationLayer;

namespace Hadal.Tests.EditMode
{
    /// <summary>The first-session guide on the client (docs/34): its steps read well, point somewhere and replicate.</summary>
    public sealed class OnboardingGuideTests
    {
        [Test]
        public void The_bundled_catalog_has_the_guide_and_every_step_reads_as_text()
        {
            var steps = ClientCatalog.Catalog.Quests.onboarding;
            Assert.GreaterOrEqual(steps.Length, 10);

            var previous = Loc.Current;
            try
            {
                foreach (var language in new[] { "tr", "en" })
                {
                    Loc.Use(language);
                    foreach (var step in steps)
                    {
                        var task = QuestText.Task(step);
                        Assert.AreNotEqual(step.id, task, $"{language}: {step.id} has no task text");
                        StringAssert.DoesNotContain("quest.", task, $"{language}: {step.id} shows a key");
                        StringAssert.DoesNotContain("guide.", Loc.T("guide." + step.id), $"{language}: guide.{step.id} missing");
                        Assert.IsNotEmpty(QuestText.Rewards(step.rewards));
                    }

                    foreach (var mapStep in steps.Where(s => s.type == "counter" && s.target is "monster" or "gather"))
                        StringAssert.DoesNotContain("guide.", Loc.T("guide." + mapStep.id + ".map"));
                }
            }
            finally
            {
                Loc.Use(previous);
            }
        }

        [Test]
        public void Each_step_points_at_the_button_it_is_done_from()
        {
            var steps = ClientCatalog.Catalog.Quests.onboarding.ToDictionary(s => s.id);
            Assert.AreEqual("GuideGo", OnboardingGuide.TargetOf(steps["ob_reactor2"]));
            Assert.AreEqual("Heroes", OnboardingGuide.TargetOf(steps["ob_hero"]));
            Assert.AreEqual("Territory", OnboardingGuide.TargetOf(steps["ob_monster"]));
            Assert.AreEqual("Territory", OnboardingGuide.TargetOf(steps["ob_gather"]));
            Assert.AreEqual("Bag", OnboardingGuide.TargetOf(steps["ob_open"]));
            Assert.AreEqual("Alliance", OnboardingGuide.TargetOf(steps["ob_alliance"]));
            Assert.AreEqual("Help", OnboardingGuide.TargetOf(steps["ob_helped"]));
        }

        [Test]
        public void Coming_up_lists_what_the_next_reactor_levels_open()
        {
            var catalog = ClientCatalog.Catalog;
            var upcoming = OnboardingRules.Upcoming(catalog, 1, 10);
            Assert.IsNotEmpty(upcoming);
            Assert.IsTrue(upcoming.All(f => f.ReactorLevel > 1));
            Assert.IsTrue(upcoming.Any(f => f.Key == "gather"));
            var previous = Loc.Current;
            try
            {
                Loc.Use("tr");
                StringAssert.Contains("4", Loc.F("unlock.at", 4));
                Assert.AreNotEqual("unlock.pvp", Loc.T("unlock.pvp"));
            }
            finally
            {
                Loc.Use(previous);
            }
        }

        [Test]
        public void The_guide_step_replicates_with_the_quests()
        {
            var view = new ClientStateView();
            view.ResetSession();
            var serializer = new HADAL.Shared.Serialization.ProtobufNetNetworkSerializer();
            var quests = new QuestStateDto
            {
                Guide = new QuestProgressDto { Id = "ob_oxygen", Progress = 0, Target = 1 }, GuideNumber = 2, GuideTotal = 15
            };
            ClientStateViewMutator.ApplyDelta(view, new StateDelta
            {
                ServerTick = 2,
                Changes = new System.Collections.Generic.List<EntityChangeDto>
                {
                    new() { EntityId = EntityIds.Quests, Kind = EntityChangeKind.Update, Payload = serializer.Serialize(quests) }
                }
            }, new NetworkSerializationLayer(), new System.Collections.Generic.List<CommandAckDto>());

            Assert.AreEqual("ob_oxygen", view.Quests.Guide.Id);
            Assert.AreEqual((2, 15), (view.Quests.GuideNumber, view.Quests.GuideTotal));
        }
    }
}
