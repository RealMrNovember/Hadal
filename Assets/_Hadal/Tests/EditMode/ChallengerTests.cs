using System.Linq;
using Hadal.Gameplay.Art;
using Hadal.Gameplay.Crew;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>The Challenger submersible: art import, flights, and giving a crew member a lift.</summary>
    public sealed class ChallengerTests
    {
        private const string ChallengerPath = "Assets/_Hadal/Art/Vehicles/challenger.fbx";

        private sealed class Rider : IChallengerRider
        {
            public Transform Body { get; set; }
            public bool Boarded;
            public Vector3? ArrivedAt;
            public void OnBoarded() => Boarded = true;
            public void OnArrived(Vector3 at) => ArrivedAt = at;
        }

        private static ChallengerVehicle MakeVehicle(Vector3 home)
        {
            var go = new GameObject("challenger");
            new GameObject("pilot").transform.SetParent(go.transform, false);
            var vehicle = go.AddComponent<ChallengerVehicle>();
            vehicle.Initialise(home, 0f);
            return vehicle;
        }

        private static float Run(ChallengerVehicle vehicle, float from, float seconds, System.Func<bool> until = null)
        {
            var t = from;
            for (; t < from + seconds; t += 0.05f)
            {
                vehicle.Step(t, 0.05f);
                if (until != null && until())
                    break;
            }

            return t;
        }

        [Test]
        public void Model_imports_with_palette_materials_animated_lights_and_a_pilot()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ChallengerPath);
            Assert.IsNotNull(model, $"missing {ChallengerPath} — run art/blender/generate.py --only challenger");

            foreach (var material in model.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials))
                StringAssert.StartsWith("Assets/_Hadal/Art/Materials/", AssetDatabase.GetAssetPath(material), $"{material?.name} not on the palette");

            var lights = model.GetComponentsInChildren<ArtAnimator>(true);
            Assert.GreaterOrEqual(lights.Count(a => a.AnimationMode == ArtAnimator.Mode.Pulse), 2, "light bar / strobe do not pulse");
            Assert.IsNotNull(model.transform.Find("pilot"), "no pilot in the sphere");

            var triangles = model.GetComponentsInChildren<MeshFilter>(true).Sum(f => f.sharedMesh.triangles.Length / 3);
            Assert.That(triangles, Is.InRange(3000, 10000), "outside the vehicle triangle budget");

            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var local = filter.sharedMesh.bounds;
                var toModel = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (var c = 0; c < 8; c++)
                {
                    var corner = local.center + Vector3.Scale(local.extents, new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                    bounds.Encapsulate(toModel.MultiplyPoint3x4(corner));
                }
            }
            Assert.AreEqual(2.75f, bounds.size.y, 0.25f, "a vertical hull about 2.6 m tall");
            Assert.Less(bounds.size.x, 1.5f, "should stand upright, not lie on its side");
            Assert.AreEqual(0f, bounds.min.y, 0.05f, "origin should be on the floor under the skids");
        }

        [Test]
        public void Flights_lift_off_arc_and_land_on_the_target()
        {
            var vehicle = MakeVehicle(new Vector3(0f, 4f, 0f));
            var rider = new Rider { Body = new GameObject("rider").transform };
            try
            {
                var pickup = new Vector3(10f, 0f, 0f);
                Assert.IsTrue(vehicle.Book(rider, pickup, new Vector3(-12f, 0f, 6f)));
                Assert.IsFalse(vehicle.IsFree);

                var peak = 0f;
                Run(vehicle, 0f, 30f, () =>
                {
                    peak = Mathf.Max(peak, vehicle.transform.position.y);
                    return vehicle.ReadyToBoard;
                });

                Assert.IsTrue(vehicle.ReadyToBoard, "never landed at the pickup");
                Assert.Less(Vector3.Distance(vehicle.transform.position, pickup), 0.01f);
                Assert.Greater(peak, 4f + 2f, "flew flat instead of rising over the base");
                Assert.Less(Vector3.Angle(vehicle.transform.up, Vector3.up), 10f, "lost its vertical attitude");
            }
            finally
            {
                Object.DestroyImmediate(rider.Body.gameObject);
                Object.DestroyImmediate(vehicle.gameObject);
            }
        }

        [Test]
        public void A_crew_member_rides_in_the_sphere_and_steps_out_at_the_drop_off()
        {
            var vehicle = MakeVehicle(new Vector3(0f, 4f, 0f));
            var crewRoot = new GameObject("crew");
            var rider = new Rider { Body = new GameObject("rider").transform };
            rider.Body.SetParent(crewRoot.transform, false);
            try
            {
                var dropoff = new Vector3(-14f, 0f, 3f);
                vehicle.Book(rider, new Vector3(8f, 0f, 0f), dropoff);
                var t = Run(vehicle, 0f, 30f, () => vehicle.ReadyToBoard);

                vehicle.Board();
                Assert.IsTrue(rider.Boarded);
                Assert.AreSame(vehicle.transform, rider.Body.parent, "passenger not seated in the vehicle");
                Assert.AreEqual(ChallengerVehicle.RiderScale, rider.Body.localScale.x, 1e-4f);
                Assert.IsFalse(vehicle.transform.Find("pilot").gameObject.activeSelf, "silhouette pilot should give way to the passenger");

                Run(vehicle, t, 40f, () => rider.ArrivedAt.HasValue);
                Assert.IsTrue(rider.ArrivedAt.HasValue, "never arrived");
                Assert.AreSame(crewRoot.transform, rider.Body.parent, "passenger not handed back to the crew");
                Assert.AreEqual(1f, rider.Body.localScale.x, 1e-4f);
                Assert.Less(Vector3.Distance(rider.ArrivedAt.Value, dropoff), 1.5f);
                Assert.AreEqual(0f, rider.Body.position.y, 1e-4f, "stepped out above the seabed");
                Assert.IsTrue(vehicle.transform.Find("pilot").gameObject.activeSelf);
                Assert.AreEqual(ChallengerVehicle.State.Returning, vehicle.Current);

                Run(vehicle, t + 40f, 40f, () => vehicle.IsFree);
                Assert.IsTrue(vehicle.IsFree, "did not return to its hangar");
                Assert.Less(Vector3.Distance(vehicle.transform.position, vehicle.Home), 0.5f);
            }
            finally
            {
                Object.DestroyImmediate(crewRoot);
                Object.DestroyImmediate(vehicle.gameObject);
            }
        }

        [Test]
        public void Hovering_drifts_gently_around_the_parking_spot()
        {
            var home = new Vector3(3f, 4.5f, -2f);
            var vehicle = MakeVehicle(home);
            try
            {
                var moved = 0f;
                for (var t = 0f; t < 60f; t += 0.1f)
                {
                    vehicle.Step(t, 0.1f);
                    var d = Vector3.Distance(vehicle.transform.position, home);
                    Assert.Less(d, 2f, "wandered off its hangar");
                    moved = Mathf.Max(moved, d);
                }

                Assert.Greater(moved, 0.2f, "hovering should drift, not freeze");
            }
            finally
            {
                Object.DestroyImmediate(vehicle.gameObject);
            }
        }

        [Test]
        public void Fleet_homes_are_above_and_beside_the_hangar()
        {
            var hangars = new[] { new Vector3(10f, 0f, 0f) };
            for (var i = 0; i < 3; i++)
            {
                var home = ChallengerFleet.HomeFor(i, hangars);
                var flat = new Vector2(home.x - 10f, home.z).magnitude;
                Assert.AreEqual(2.6f, flat, 0.01f);
                Assert.That(home.y, Is.InRange(3.5f, 6f), "should hover above the gantry");
            }
        }

        [Test]
        public void Bootstrap_scene_crew_has_a_challenger_fleet()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Hadal/Scenes/Bootstrap.unity");
            var crew = Object.FindFirstObjectByType<CrewView>();
            Assert.IsNotNull(crew);
            var fleet = crew.GetComponent<ChallengerFleet>();
            Assert.IsNotNull(fleet, "no ChallengerFleet next to the crew");
            var so = new SerializedObject(fleet);
            Assert.IsNotNull(so.FindProperty("_prefab").objectReferenceValue, "fleet has no Challenger model");
            Assert.IsNotNull(so.FindProperty("_buildings").objectReferenceValue, "fleet cannot find the hangars");
        }
    }
}
