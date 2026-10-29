using Hadal.Core.Services;
using Hadal.UI;
using NUnit.Framework;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>The incoming-attack alarm (docs/40 §1): tempo tiers, a smooth pulse, the vignette's shape and rest state.</summary>
    public sealed class AttackAlarmTests
    {
        [Test]
        public void Tempo_QuickensAndStrengthensAsImpactNears()
        {
            var far = AlarmTempo.Threat(120f);
            var mid = AlarmTempo.Threat(45f);
            var near = AlarmTempo.Threat(20f);
            var now = AlarmTempo.Threat(5f);
            Assert.Greater(far.Period, mid.Period);
            Assert.Greater(mid.Period, near.Period);
            Assert.Greater(near.Period, now.Period);
            Assert.Less(far.Peak, mid.Peak);
            Assert.Less(mid.Peak, near.Peak);
            Assert.Less(near.Peak, now.Peak);
            // Never a full-screen red.
            Assert.LessOrEqual(AlarmTempo.Threat(0f).Peak, AlarmTempo.MaxIntensity);
            Assert.Less(AlarmTempo.MaxIntensity, 0.95f);
        }

        [Test]
        public void Tempo_HasNoJumpsBetweenTiers()
        {
            for (var s = 0f; s < 130f; s += 0.25f)
            {
                var a = AlarmTempo.Threat(s);
                var b = AlarmTempo.Threat(s + 0.25f);
                Assert.Less(Mathf.Abs(a.Period - b.Period), 0.05f, $"period jumps at {s}s");
                Assert.Less(Mathf.Abs(a.Peak - b.Peak), 0.02f, $"peak jumps at {s}s");
            }
        }

        [Test]
        public void Heartbeat_IsSmoothBoundedAndContinuousAcrossTheWrap()
        {
            foreach (var urgency in new[] { 0f, 0.5f, 1f })
            {
                var previous = AlarmTempo.Heartbeat(0f, urgency);
                var max = 0f;
                for (var i = 1; i <= 1000; i++)
                {
                    var v = AlarmTempo.Heartbeat(i / 1000f, urgency);
                    Assert.That(v, Is.InRange(0f, 1f));
                    // At 60 fps the shortest period (0.72 s) steps the phase by ~0.023: no step may blink.
                    Assert.Less(Mathf.Abs(v - previous), 0.06f, $"pulse jumps at phase {i / 1000f} (urgency {urgency})");
                    previous = v;
                    max = Mathf.Max(max, v);
                }

                Assert.Greater(max, 0.95f, "the beat reaches its peak");
                Assert.AreEqual(AlarmTempo.Heartbeat(0f, urgency), AlarmTempo.Heartbeat(1f, urgency), 1e-4f);
            }
        }

        [Test]
        public void PingsComeFasterNearImpact()
        {
            Assert.Greater(AlarmTempo.PingInterval(90f), AlarmTempo.PingInterval(45f));
            Assert.Greater(AlarmTempo.PingInterval(45f), AlarmTempo.PingInterval(20f));
            Assert.Greater(AlarmTempo.PingInterval(20f), AlarmTempo.PingInterval(5f));
        }

        [Test]
        public void Clock_HasFixedFieldWidths()
        {
            Assert.AreEqual("00:05", AlarmTempo.Clock(4.2f));
            Assert.AreEqual("01:00", AlarmTempo.Clock(60f));
            Assert.AreEqual("59:59", AlarmTempo.Clock(3599f));
            Assert.AreEqual("1:00:00", AlarmTempo.Clock(3600f));
            Assert.AreEqual("00:00", AlarmTempo.Clock(-3f));
        }

        [Test]
        public void VignetteSprite_IsClearInTheMiddleAndDenserInTheCorners()
        {
            var sprite = AlertVignette.Make("Test", Color.red, Color.yellow, 2f, true);
            var t = sprite.texture;
            Assert.AreEqual(0f, t.GetPixel(64, 64).a, 0.01f, "centre is transparent");
            var edge = t.GetPixel(0, 64).a;
            var corner = t.GetPixel(0, 0).a;
            var inner = t.GetPixel(40, 64).a;
            Assert.Greater(edge, 0.8f);
            Assert.GreaterOrEqual(corner, edge);
            Assert.Less(inner, edge * 0.5f, "soft falloff, not a flat border");
            Assert.AreEqual(new Vector4(63, 63, 63, 63), sprite.border);
            Object.DestroyImmediate(t);
        }

        [Test]
        public void Vignette_PulsesWithinLimits_AndReturnsToRest()
        {
            var reduce = GameSettings.ReduceAlertEffects;
            var host = new GameObject("AlarmTestHost");
            try
            {
                GameSettings.ReduceAlertEffects = false;
                var vignette = AlertVignette.Create(host.transform);
                var max = 0f;
                for (var i = 0; i < 600; i++)
                {
                    vignette.Tick(AlarmMood.Threat, 8f, 1f / 60f);
                    max = Mathf.Max(max, vignette.Intensity);
                }

                Assert.Greater(max, 0.5f, "a strong pulse under 10 s");
                Assert.LessOrEqual(max, AlarmTempo.MaxIntensity + 1e-3f);

                for (var i = 0; i < 120; i++)
                    vignette.Tick(AlarmMood.None, 0f, 1f / 60f);
                Assert.AreEqual(0f, vignette.Intensity, 1e-4f, "back to rest once the threat is gone");

                // Reduced effects: steady, no pulse.
                GameSettings.ReduceAlertEffects = true;
                for (var i = 0; i < 120; i++)
                    vignette.Tick(AlarmMood.Threat, 20f, 1f / 60f);
                var a = vignette.Intensity;
                for (var i = 0; i < 37; i++)
                    vignette.Tick(AlarmMood.Threat, 20f, 1f / 60f);
                Assert.AreEqual(a, vignette.Intensity, 0.01f);
            }
            finally
            {
                GameSettings.ReduceAlertEffects = reduce;
                Object.DestroyImmediate(host);
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (canvas.name == "AlarmVignette")
                        Object.DestroyImmediate(canvas.gameObject);
            }
        }

        [Test]
        public void TabularClock_GivesEveryDigitTheSameCell()
        {
            var host = new GameObject("ClockHost", typeof(RectTransform));
            try
            {
                var clock = TabularClock.Create(host.transform, 50, Color.white);
                clock.Text = "11:11";
                var narrow = ((RectTransform)clock.transform.GetChild(0)).anchoredPosition;
                clock.Text = "88:88";
                var wide = ((RectTransform)clock.transform.GetChild(0)).anchoredPosition;
                Assert.AreEqual(narrow, wide, "digits do not shift when they change");
                var digit = ((RectTransform)clock.transform.GetChild(0)).sizeDelta.x;
                var colon = ((RectTransform)clock.transform.GetChild(2)).sizeDelta.x;
                Assert.Less(colon, digit);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void Demo_StandsInAMarchAndEnds()
        {
            AlarmDemo.Start("shield", 30f, 1000);
            Assert.IsTrue(AlarmDemo.Active);
            Assert.Greater(AlarmDemo.ShieldUntilMs, 31_000);
            AlarmDemo.Stop();
            Assert.IsFalse(AlarmDemo.Active);
        }
    }
}
