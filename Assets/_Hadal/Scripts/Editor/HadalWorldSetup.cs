#if UNITY_EDITOR
using Hadal.Gameplay.Art;
using Hadal.Gameplay.Buildings;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hadal.EditorTools
{
    /// <summary>
    /// The visible world of the Bootstrap scene: camera rig, abyss lighting and fog, seabed,
    /// scattered props and the building view that renders server state with generated art.
    /// </summary>
    public static class HadalWorldSetup
    {
        private const string EnvironmentPath = "Assets/_Hadal/Art/Environment/";

        private static readonly Color AbyssColor = new(0.010f, 0.045f, 0.070f);

        /// <summary>Builds the world under a new root; returns the building view (needs injection).</summary>
        public static BuildingStateView CreateWorld()
        {
            var world = new GameObject("World");

            ConfigureAtmosphere();
            CreateSunlight(world.transform);
            CreateCamera(world.transform);
            CreateVolume(world.transform);
            CreateSeabed(world.transform);
            ScatterProps(world.transform);

            var buildingsGo = new GameObject("Buildings");
            buildingsGo.transform.SetParent(world.transform, false);
            var view = buildingsGo.AddComponent<BuildingStateView>();
            var so = new SerializedObject(view);
            so.FindProperty("_buildingsRoot").objectReferenceValue = buildingsGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static void ConfigureAtmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = AbyssColor;
            RenderSettings.fogDensity = 0.026f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.045f, 0.11f, 0.16f);
            RenderSettings.ambientEquatorColor = new Color(0.02f, 0.05f, 0.07f);
            RenderSettings.ambientGroundColor = new Color(0.01f, 0.015f, 0.02f);
        }

        private static void CreateSunlight(Transform parent)
        {
            // Light filtering down from the surface: cold, high, slightly from behind the camera.
            var go = new GameObject("SurfaceLight");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(62f, -25f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.55f, 0.82f, 1f);
            light.intensity = 0.8f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
        }

        private static void CreateCamera(Transform parent)
        {
            var rigGo = new GameObject("CameraRig");
            rigGo.transform.SetParent(parent, false);
            var rig = rigGo.AddComponent<BaseCameraRig>();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(rigGo.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = AbyssColor;
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 160f;
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;

            var so = new SerializedObject(rig);
            so.FindProperty("_camera").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();
            rig.Configure(cam, 34f);
        }

        private static void CreateVolume(Transform parent)
        {
            var go = new GameObject("AbyssVolume");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = HadalRenderSetup.EnsureVolumeProfile();
        }

        private static void CreateSeabed(Transform parent)
        {
            var tile = Load("seabed_tile");
            if (tile == null)
                return;

            var root = new GameObject("Seabed");
            root.transform.SetParent(parent, false);
            const float size = 20f;
            for (var x = -2; x <= 2; x++)
            {
                for (var z = -2; z <= 2; z++)
                {
                    var t = Place(tile, root.transform, new Vector3(x * size, -0.05f, z * size), (x * 5 + z) * 90f, 1f);
                    t.name = $"Seabed_{x}_{z}";
                }
            }
        }

        private static void ScatterProps(Transform parent)
        {
            var root = new GameObject("Props");
            root.transform.SetParent(parent, false);

            // Deterministic so re-running setup does not churn the scene file.
            var rng = new System.Random(42);
            string[] kinds = { "rock_large", "rock_medium", "rock_small", "kelp_cluster", "coral_branch", "thermal_vent" };
            int[] counts = { 7, 10, 14, 16, 9, 3 };

            for (var k = 0; k < kinds.Length; k++)
            {
                var prefab = Load(kinds[k]);
                if (prefab == null)
                    continue;

                for (var i = 0; i < counts[k]; i++)
                {
                    // Ring 4 (22 m) is the outermost built ring at start; keep props beyond it.
                    var angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
                    var radius = Mathf.Lerp(27f, 50f, (float)rng.NextDouble());
                    var position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                    var scale = Mathf.Lerp(0.8f, 1.35f, (float)rng.NextDouble());
                    Place(prefab, root.transform, position, (float)rng.NextDouble() * 360f, scale);
                }
            }
        }

        private static GameObject Load(string id) => AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPath + id + ".fbx");

        private static Transform Place(GameObject prefab, Transform parent, Vector3 position, float yaw, float scale)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            go.isStatic = go.GetComponentInChildren<ArtAnimator>() == null;
            return go.transform;
        }
    }
}
#endif
