#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using HADAL.Shared.Commands;
using HADAL.Shared.Enums;
using Hadal.Core.Network;
using Hadal.Core.StateSync;
using Hadal.Data.Models;
using Hadal.Managers.Bootstrap;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VContainer;

namespace Hadal.Tests.PlayMode
{
    /// <summary>
    /// Phase 1 acceptance against the real gateway (CT 160). Needs HADAL_GATEWAY_URL, e.g.
    /// ws://hadal-gateway:8080/gateway when the editor container joins hadal_isolated_net.
    /// Skipped when it is not set, so the suite still runs offline.
    /// </summary>
    public sealed class LiveServerTests
    {
        private const string ScenePath = "Assets/_Hadal/Scenes/Bootstrap.unity";

        private string _profile;

        [SetUp]
        public void RequireGateway()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("HADAL_GATEWAY_URL")))
                Assert.Ignore("HADAL_GATEWAY_URL not set — live server tests skipped.");

            Environment.SetEnvironmentVariable("HADAL_LOCAL_SIM", "0");
            _profile = Environment.GetEnvironmentVariable("HADAL_AUTH_PROFILE");
        }

        [TearDown]
        public void Restore()
        {
            Environment.SetEnvironmentVariable("HADAL_AUTH_PROFILE", _profile);
            Environment.SetEnvironmentVariable("HADAL_FRESH_ACCOUNT", null);
        }

        /// <summary>A brand-new player places a producer, sees production, drops and resumes.</summary>
        [UnityTest, Category("LiveServer")]
        public IEnumerator NewPlayer_Place_Produce_Reconnect()
        {
            // A brand-new guest account under this profile; the persistence test reuses its stored login.
            if (string.IsNullOrWhiteSpace(_profile))
                Environment.SetEnvironmentVariable("HADAL_AUTH_PROFILE", "playmode-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("HADAL_FRESH_ACCOUNT", "1");

            yield return LoadScene();
            var container = Container();
            var view = container.Resolve<IStateSyncService>().View;
            var session = container.Resolve<IGatewaySessionState>();

            yield return WaitUntil(() => session.IsHandshakeComplete, 10f, "handshake never completed");
            yield return WaitUntil(() => Balance(view, ResourceType.Oxygen) > 0, 5f, "snapshot never arrived");
            Assert.AreEqual(500, Balance(view, ResourceType.Oxygen), "new player starting oxygen");
            Assert.AreEqual(150, Balance(view, ResourceType.Titanium), "new player starting titanium");
            Assert.IsEmpty(view.Data.buildings ?? Array.Empty<BuildingSaveEntry>());

            container.Resolve<ICommandDispatcher>().Dispatch(new PlaceBuildingCommand
            {
                CommandId = Guid.NewGuid().ToString("N"),
                ClientSequence = 1,
                BuildingDefinitionId = "oxygen_plant",
                RingIndex = 1,
                SectorIndex = 0
            });

            yield return WaitUntil(() => HasBuilding(view, "oxygen_plant"), 5f, "server never confirmed the building");
            Assert.AreEqual(100, Balance(view, ResourceType.Titanium), "build cost not charged by the server");

            yield return WaitUntil(() => Balance(view, ResourceType.Oxygen) > 500, 8f, "server production never reached the client");
            var beforeDrop = Balance(view, ResourceType.Oxygen);

            // Drop the socket; the receiver must notice, back off and reconnect on its own.
            container.Resolve<IWebSocketClient>().Disconnect();
            yield return WaitUntil(() => !session.IsHandshakeComplete, 3f, "disconnect not noticed");
            yield return WaitUntil(() => session.IsHandshakeComplete, 15f, "client never reconnected");

            yield return WaitUntil(() => HasBuilding(view, "oxygen_plant"), 5f, "building missing after reconnect");
            Assert.AreEqual(1, view.Data.buildings.Length, "duplicate or lost buildings after reconnect");
            Assert.GreaterOrEqual(Balance(view, ResourceType.Oxygen), beforeDrop, "wallet rewound after reconnect");
        }

        /// <summary>
        /// Run after the gateway restarts with the HADAL_AUTH_PROFILE used by the first test: the
        /// stored refresh token signs back into the same account, whose state comes from PostgreSQL.
        /// </summary>
        [UnityTest, Category("Persistence")]
        public IEnumerator ExistingPlayer_StateSurvivesServerRestart()
        {
            if (string.IsNullOrWhiteSpace(_profile))
                Assert.Ignore("Set HADAL_AUTH_PROFILE to the profile used before the restart.");

            yield return LoadScene();
            var container = Container();
            var view = container.Resolve<IStateSyncService>().View;

            yield return WaitUntil(() => container.Resolve<IGatewaySessionState>().IsHandshakeComplete, 10f, "handshake never completed");
            yield return WaitUntil(() => HasBuilding(view, "oxygen_plant"), 5f, "persisted building missing");
            Assert.Greater(Balance(view, ResourceType.Oxygen), 500, "persisted production missing");
            Assert.AreEqual(100, Balance(view, ResourceType.Titanium), "persisted wallet wrong");
        }

        private static IEnumerator LoadScene()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        }

        private static IObjectResolver Container()
        {
            var scope = UnityEngine.Object.FindFirstObjectByType<ProjectLifetimeScope>();
            Assert.IsNotNull(scope?.Container, "DI container was not built.");
            return scope.Container;
        }

        private static bool HasBuilding(ClientStateView view, string definitionId) =>
            (view.Data.buildings ?? Array.Empty<BuildingSaveEntry>()).Any(b => b.definitionId == definitionId);

        private static long Balance(ClientStateView view, ResourceType type)
        {
            foreach (var entry in view.Data.resources ?? Array.Empty<ResourceSaveEntry>())
            {
                if (ResourceTypeMap.FromStoredValue(entry.resourceType) == type)
                    return entry.amount;
            }

            return 0;
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
    }
}
#endif
