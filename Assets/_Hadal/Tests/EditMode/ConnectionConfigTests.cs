using System;
using Hadal.Core.Network;
using Hadal.Data.Config;
using NUnit.Framework;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>Patch-check version policy and the gateway → Auth API URL derivation.</summary>
    public sealed class ConnectionConfigTests
    {
        [TestCase("1.0", "0.0.0", false)]
        [TestCase("1.0", "1.0.0", false)]
        [TestCase("1.0", "1.0.1", true)]
        [TestCase("0.9.5", "1.0", true)]
        [TestCase("2.0.0", "1.9.9", false)]
        [TestCase("garbage", "1.0", false)] // never lock a client out on an unparsable version
        public void IsOlder(string current, string minimum, bool expected)
        {
            Assert.AreEqual(expected, PatchService.IsOlder(current, minimum));
        }

        [TestCase("wss://hadal-api.cicibyte.com/gateway", "https://hadal-api.cicibyte.com")]
        [TestCase("ws://hadal-gateway:8080/gateway", "http://hadal-gateway:8080")]
        [TestCase("ws://127.0.0.1:19001/gateway/", "http://127.0.0.1:19001")]
        public void ApiBaseUrl_FollowsGatewayUrl(string gateway, string expected)
        {
            var previous = Environment.GetEnvironmentVariable("HADAL_GATEWAY_URL");
            var config = ScriptableObject.CreateInstance<GameConfigSO>();
            try
            {
                Environment.SetEnvironmentVariable("HADAL_GATEWAY_URL", gateway);
                Assert.AreEqual(expected, config.ApiBaseUrl);
            }
            finally
            {
                Environment.SetEnvironmentVariable("HADAL_GATEWAY_URL", previous);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DefaultGateway_IsTheGameHost_NotTheTeaserSite()
        {
            var previous = Environment.GetEnvironmentVariable("HADAL_GATEWAY_URL");
            var config = ScriptableObject.CreateInstance<GameConfigSO>();
            try
            {
                Environment.SetEnvironmentVariable("HADAL_GATEWAY_URL", null);
                Assert.AreEqual("wss://hadal-api.cicibyte.com/gateway", config.GatewayUrl);
            }
            finally
            {
                Environment.SetEnvironmentVariable("HADAL_GATEWAY_URL", previous);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
