#if UNITY_EDITOR
using System.Collections.Generic;
using Hadal.Gameplay.Art;
using Hadal.Gameplay.Base;
using Hadal.Gameplay.Buildings;
using Hadal.Gameplay.Crew;
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

        /// <summary>Builds the world under a new root; returns the objects VContainer must inject.</summary>
        public static List<GameObject> CreateWorld()
        {
            var world = new GameObject("World");

            ConfigureAtmosphere();
            CreateSunlight(world.transform);
            var camera = CreateCamera(world.transform);
            CreateVolume(world.transform);
            CreateSeabed(world.transform);
            ScatterProps(world.transform);
            PlaceTrellisProps(world.transform);

            var buildingsGo = new GameObject("Buildings");
            buildingsGo.transform.SetParent(world.transform, false);
            var view = buildingsGo.AddComponent<BuildingStateView>();
            var so = new SerializedObject(view);
            so.FindProperty("_buildingsRoot").objectReferenceValue = buildingsGo.transform;
            so.FindProperty("_constructionMaterial").objectReferenceValue = HadalArtPalette.Load("Glass");
            so.ApplyModifiedPropertiesWithoutUndo();

            CreateCrew(world.transform, view);

            // Base management input: taps resolve to slots, free slots glow.
            var interaction = new GameObject("BaseInteraction").AddComponent<BaseInteraction>();
            interaction.transform.SetParent(world.transform, false);
            interaction.SetCamera(camera);
            var markers = new GameObject("SlotMarkers").AddComponent<SlotMarkers>();
            markers.transform.SetParent(world.transform, false);
            markers.Configure(HadalArtPalette.Load("GlowCyan"));
            EditorUtility.SetDirty(interaction);
            EditorUtility.SetDirty(markers);

            return new List<GameObject> { buildingsGo, interaction.gameObject, markers.gameObject };
        }

        private static void CreateCrew(Transform parent, BuildingStateView buildings)
        {
            var prefabs = HadalCharacterSetup.BuildPrefabs();
            GameObject[] Pick(params string[] ids) =>
                System.Array.FindAll(System.Array.ConvertAll(ids, id => prefabs.TryGetValue(id, out var p) ? p : null), p => p != null);

            var go = new GameObject("Crew");
            go.transform.SetParent(parent, false);
            var crew = go.AddComponent<CrewView>();
            crew.Configure(buildings, Pick("crew_engineer", "crew_medic", "crew_scientist"), Pick("mech_worker"));
            EditorUtility.SetDirty(crew);

            // Heroes screen: every character model and painted portrait, by id (docs/25).
            var library = go.AddComponent<Hadal.Core.Catalog.CharacterLibrary>();
            var models = new List<Hadal.Core.Catalog.CharacterLibrary.ModelEntry>();
            foreach (var pair in prefabs)
                models.Add(new Hadal.Core.Catalog.CharacterLibrary.ModelEntry { id = pair.Key, prefab = pair.Value });
            var portraits = new List<Hadal.Core.Catalog.CharacterLibrary.PortraitEntry>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Hadal/Art/Portraits" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                portraits.Add(new Hadal.Core.Catalog.CharacterLibrary.PortraitEntry
                {
                    id = System.IO.Path.GetFileNameWithoutExtension(path),
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path)
                });
            }

            library.Configure(models.ToArray(), portraits.ToArray());
            EditorUtility.SetDirty(library);
        }

        private static void ConfigureAtmosphere()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = AbyssColor;
            RenderSettings.fogDensity = 0.013f;
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

        private static Camera CreateCamera(Transform parent)
        {
            var rigGo = new GameObject("CameraRig");
            rigGo.transform.SetParent(parent, false);
            var rig = rigGo.AddComponent<BaseCameraRig>();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(rigGo.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = AbyssColor;
            // Portrait phones: a 38° vertical FOV left only ~18° horizontally (11 m of seabed); 50° from
            // 58 m shows the inner rings and the wildlife around them.
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 200f;
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;

            var so = new SerializedObject(rig);
            so.FindProperty("_camera").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();
            rig.Configure(cam, 58f);
            return cam;
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
            string[] kinds = { "rock_large", "rock_medium", "rock_small", "kelp_cluster", "coral_branch", "thermal_vent", "crystal_cluster", "cargo_stack" };
            int[] counts = { 7, 10, 14, 16, 9, 3, 5, 3 };

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

        [System.Serializable]
        private sealed class Roster
        {
            public RosterAsset[] assets = System.Array.Empty<RosterAsset>();
        }

        [System.Serializable]
        private sealed class RosterAsset
        {
            public string id = string.Empty;
            public string kind = string.Empty;
            public string motion = "none";
            public float height = 1f;
            public float yaw;
        }

        /// <summary>
        /// TRELLIS props (art/gpu/assets.json → art/blender/props.py): decor scattered beyond the
        /// built rings, wildlife and submarines circling the base on deterministic paths.
        /// </summary>
        private static void PlaceTrellisProps(Transform parent)
        {
            var rosterPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "art/gpu/assets.json");
            if (!System.IO.File.Exists(rosterPath))
                return;

            var roster = JsonUtility.FromJson<Roster>(System.IO.File.ReadAllText(rosterPath));
            var root = new GameObject("Wildlife");
            root.transform.SetParent(parent, false);

            foreach (var asset in roster.assets)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HadalArtImporter.PropsRoot + asset.id + ".fbx");
                if (prefab == null)
                    continue;

                var rng = new System.Random(StableSeed(asset.id));
                float Range(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
                var count = asset.motion switch
                {
                    "pulse_swim" => 4,
                    "swim" or "crawl" => 2,
                    "patrol" => 1,
                    _ => asset.id switch { "decor_hadalite_crystals" => 4, "decor_ruin_pillar" => 3, "decor_tube_worms" => 3, "decor_shipwreck_bow" or "creature_forgotten_sentinel" => 1, _ => 2 }
                };

                for (var i = 0; i < count; i++)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    go.transform.SetParent(root.transform, false);
                    go.name = $"{asset.id}_{i}";
                    var angle = Range(0f, Mathf.PI * 2f);

                    if (asset.motion is "none" or "sway")
                    {
                        // Ring 4 (22 m) holds the outermost buildings at start; decor stays beyond it.
                        var radius = Range(25f, 44f);
                        go.transform.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                        go.transform.localRotation = Quaternion.Euler(0f, Range(0f, 360f), 0f);
                        go.transform.localScale = Vector3.one * Range(0.85f, 1.2f);
                        go.isStatic = asset.motion == "none";
                        if (asset.motion == "sway")
                            go.AddComponent<AmbientMotion>().Configure(AmbientMotion.Mode.Sway, Vector3.zero, 1f, 0f, 0f, false, 0f, Range(0f, 1f));
                        continue;
                    }

                    var (mode, radiusMin, radiusMax, heightMin, heightMax, speed) = asset.motion switch
                    {
                        // Close enough to the base to be seen from the default camera.
                        "pulse_swim" => (AmbientMotion.Mode.PulseSwim, 8f, 20f, 5f, 9f, 0.3f),
                        "crawl" => (AmbientMotion.Mode.Crawl, 24f, 30f, 0f, 0f, 0.4f),
                        "patrol" when asset.height > 3.5f => (AmbientMotion.Mode.Patrol, 40f, 46f, 12f, 14f, 4f),
                        "patrol" => (AmbientMotion.Mode.Patrol, 18f, 26f, 6f, 9f, 3f),
                        _ => (AmbientMotion.Mode.Swim, 12f, 24f, 4f, 7f, 1.6f)
                    };

                    var motion = go.AddComponent<AmbientMotion>();
                    motion.Configure(mode, Vector3.zero, Range(radiusMin, radiusMax), Range(heightMin, heightMax), speed * Range(0.8f, 1.2f),
                        rng.Next(2) == 0, asset.yaw, Range(0f, 1f));
                    motion.Step(0f, 0f); // start on its path, not at the origin
                }
            }
        }

        /// <summary>string.GetHashCode is not stable across runtimes; the scene layout must be.</summary>
        private static int StableSeed(string text)
        {
            unchecked
            {
                var hash = 17;
                foreach (var c in text)
                    hash = hash * 31 + c;
                return hash & 0x7fffffff;
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
