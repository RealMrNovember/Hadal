using Hadal.Gameplay.Art;
using NUnit.Framework;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    public sealed class AmbientMotionTests
    {
        private static AmbientMotion Make(AmbientMotion.Mode mode, float radius, float height)
        {
            var motion = new GameObject("ambient").AddComponent<AmbientMotion>();
            motion.Configure(mode, new Vector3(1f, 0f, 2f), radius, height, 2f, false, 0f, 0.25f);
            return motion;
        }

        [TestCase(AmbientMotion.Mode.Swim)]
        [TestCase(AmbientMotion.Mode.Patrol)]
        [TestCase(AmbientMotion.Mode.Crawl)]
        [TestCase(AmbientMotion.Mode.PulseSwim)]
        public void Movers_stay_on_their_circle_and_near_their_depth(AmbientMotion.Mode mode)
        {
            var motion = Make(mode, 20f, 6f);
            try
            {
                var start = motion.transform.position;
                for (var i = 1; i <= 120; i++)
                {
                    motion.Step(i * 0.1f, 0.1f);
                    var p = motion.transform.position;
                    var flat = new Vector2(p.x - 1f, p.z - 2f);
                    Assert.AreEqual(20f, flat.magnitude, 0.01f, "left its circle");
                    Assert.AreEqual(6f, p.y, 1.3f, "drifted off its depth");
                }

                Assert.Greater(Vector3.Distance(start, motion.transform.position), 1f, "did not move");
            }
            finally
            {
                Object.DestroyImmediate(motion.gameObject);
            }
        }

        [Test]
        public void Swimmers_face_along_their_path()
        {
            var motion = Make(AmbientMotion.Mode.Patrol, 10f, 0f);
            try
            {
                motion.Step(0f, 0.01f);
                var before = motion.transform.position;
                motion.Step(0.05f, 0.05f);
                var moved = (motion.transform.position - before);
                moved.y = 0f;
                var forward = motion.transform.forward;
                forward.y = 0f;
                Assert.Greater(Vector3.Dot(moved.normalized, forward.normalized), 0.9f);
            }
            finally
            {
                Object.DestroyImmediate(motion.gameObject);
            }
        }

        [Test]
        public void Sway_stays_in_place()
        {
            var motion = Make(AmbientMotion.Mode.Sway, 1f, 0f);
            try
            {
                motion.transform.position = new Vector3(5f, 0f, 5f);
                motion.Step(3f, 0.1f);
                Assert.AreEqual(new Vector3(5f, 0f, 5f), motion.transform.position);
            }
            finally
            {
                Object.DestroyImmediate(motion.gameObject);
            }
        }
    }
}
