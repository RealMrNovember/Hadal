#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using HADAL.Shared.Enums;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using Hadal.Managers.Bootstrap;
using Hadal.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VContainer;

namespace Hadal.Tests.PlayMode
{
    /// <summary>
    /// Offline dev loop (HADAL_LOCAL_SIM=1): the generated Bootstrap scene boots without errors, the stub server
    /// seeds the wallet through a snapshot, an empty base produces nothing (S2), and a producer
    /// building's output reaches the HUD through tick -> delta -> StateSync -> view.
    /// Any error logged during the run fails the test (DI misconfiguration included).
    /// </summary>
    public sealed class EconomyLoopSmokeTests
    {
        private const string ScenePath = "Assets/_Hadal/Scenes/Bootstrap.unity";

        [SetUp]
        public void UseLocalSimulation() => Environment.SetEnvironmentVariable("HADAL_LOCAL_SIM", "1");

        [TearDown]
        public void ClearLocalSimulation() => Environment.SetEnvironmentVariable("HADAL_LOCAL_SIM", null);

        [UnityTest]
        public IEnumerator BootstrapScene_RunsTheEconomyLoopIntoTheHud()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

            var scope = UnityEngine.Object.FindFirstObjectByType<ProjectLifetimeScope>();
            Assert.IsNotNull(scope, "ProjectLifetimeScope missing from Bootstrap scene.");
            Assert.IsNotNull(scope.Container, "DI container was not built.");

            var view = scope.Container.Resolve<IStateSyncService>().View;

            // Seed snapshot arrives on the simulator's first tick.
            yield return WaitUntil(() => Balance(view, ResourceType.Oxygen) > 0, 5f, "starting wallet never arrived");
            Assert.AreEqual(500, Balance(view, ResourceType.Oxygen), "Oxygen starting amount");
            Assert.AreEqual(150, Balance(view, ResourceType.Titanium), "Titanium starting amount");

            // S2: no buildings, so a full replication interval must not move any balance.
            var before = Snapshot(view);
            yield return new WaitForSecondsRealtime(6f);
            CollectionAssert.AreEquivalent(before, Snapshot(view), "balances moved with zero buildings");

            // A replicated producer building starts the economy.
            view.Data.buildings = new[]
            {
                new BuildingSaveEntry { instanceKey = "smoke-oxygen-1", definitionId = "oxygen_plant", level = 1 }
            };

            yield return WaitUntil(() => Balance(view, ResourceType.Oxygen) > 500, 8f, "oxygen never increased");
            Assert.Less(Balance(view, ResourceType.Energy), 200, "oxygen_plant should consume energy");

            var hud = UnityEngine.Object.FindFirstObjectByType<ResourceHudView>();
            Assert.IsNotNull(hud, "ResourceHudView missing from Bootstrap scene.");
            hud.Refresh();

            var oxygenLabel = hud.transform.GetComponentsInChildren<Text>()
                .First(t => t.name == "Amount" && t.transform.parent.name == nameof(ResourceType.Oxygen));
            StringAssert.StartsWith(Balance(view, ResourceType.Oxygen).ToString(), oxygenLabel.text,
                "HUD does not show the replicated oxygen balance");
        }

        private static IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string failure)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail(failure);
                yield return null;
            }
        }

        private static long Balance(ClientStateView view, ResourceType type)
        {
            var resources = view.Data.resources ?? Array.Empty<ResourceSaveEntry>();
            foreach (var entry in resources)
            {
                if (ResourceTypeMap.FromStoredValue(entry.resourceType) == type)
                    return entry.amount;
            }

            return 0;
        }

        private static System.Collections.Generic.Dictionary<int, long> Snapshot(ClientStateView view) =>
            (view.Data.resources ?? Array.Empty<ResourceSaveEntry>()).ToDictionary(r => r.resourceType, r => r.amount);
    }
}
#endif
