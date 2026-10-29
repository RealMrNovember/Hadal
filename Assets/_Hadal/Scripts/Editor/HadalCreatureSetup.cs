#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hadal.Core.Art;
using UnityEditor;
using UnityEngine;

namespace Hadal.EditorTools
{
    /// <summary>
    /// World-map creatures (art/gpu/creatures.json → TRELLIS → art/blender/props.py → Art/Creatures): one
    /// <c>Hadal/Creature</c> material per creature with its motion numbers (<see cref="CreatureProfiles"/>), a shared lure
    /// material, and the <see cref="CreatureLibrary"/> Resources asset the map loads them from.
    ///
    ///     server/unity/unity.sh exec Hadal.EditorTools.HadalCreatureSetup.Build
    /// </summary>
    public static class HadalCreatureSetup
    {
        public const string MaterialsPath = HadalArtImporter.CreaturesRoot + "Materials/";
        public const string LibraryPath = "Assets/_Hadal/Resources/" + CreatureLibrary.ResourcePath + ".asset";
        public const string ShaderName = "Hadal/Creature";

        [MenuItem("Hadal/Setup/Creatures")]
        public static void Build()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[Hadal] creatures: shader " + ShaderName + " not found");
                return;
            }

            EnsureFolder(MaterialsPath.TrimEnd('/'));
            EnsureFolder(Path.GetDirectoryName(LibraryPath)!.Replace('\\', '/'));

            var entries = new List<CreatureLibrary.Entry>();
            foreach (var profile in CreatureProfiles.All)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(HadalArtImporter.CreaturesRoot + profile.Id + ".fbx");
                if (model == null)
                {
                    Debug.LogWarning("[Hadal] creatures: no model for " + profile.Id);
                    continue;
                }

                var (min, size, triangles) = Measure(model);
                var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(HadalArtImporter.CreaturesRoot + profile.Id + "_albedo.png");
                var material = MaterialFor(profile, shader, albedo, min, size);
                entries.Add(new CreatureLibrary.Entry
                {
                    id = profile.Id, model = model, material = material, boundsMin = min, boundsSize = size, triangles = triangles
                });
                Debug.Log($"[Hadal] creatures: {profile.Id} {triangles} tris, {(albedo != null ? albedo.width : 0)} px, size {size}");
            }

            var lure = LoadOrCreate(MaterialsPath + "creature_lure.mat", shader);
            lure.SetTexture("_BaseMap", Texture2D.whiteTexture);
            lure.SetColor("_GlowColor", Color.white);
            lure.SetFloat("_Glow", 2.6f);
            lure.SetFloat("_GlowThreshold", 0f);
            lure.enableInstancing = true;
            EditorUtility.SetDirty(lure);

            var library = AssetDatabase.LoadAssetAtPath<CreatureLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<CreatureLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.Configure(entries.ToArray(), lure);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Hadal] creatures: library with {entries.Count} creatures at {LibraryPath}");
        }

        /// <summary>The model's bounds in its own space (metres) and its triangle count.</summary>
        public static (Vector3 Min, Vector3 Size, int Triangles) Measure(GameObject model)
        {
            var go = Object.Instantiate(model);
            try
            {
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                go.transform.localScale = Vector3.one;
                var renderers = go.GetComponentsInChildren<Renderer>();
                var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
                foreach (var r in renderers.Skip(1))
                    bounds.Encapsulate(r.bounds);
                var triangles = go.GetComponentsInChildren<MeshFilter>().Where(f => f.sharedMesh != null).Sum(f => f.sharedMesh.triangles.Length / 3);
                return (bounds.min, bounds.size, triangles);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        public static Material MaterialFor(CreatureProfile p, Shader shader, Texture2D albedo, Vector3 min, Vector3 size)
        {
            var material = LoadOrCreate(MaterialsPath + p.Id + ".mat", shader);
            if (albedo != null)
                material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_GlowColor", p.GlowColor);
            material.SetFloat("_Glow", p.Glow);
            material.SetFloat("_GlowThreshold", p.GlowThreshold);
            material.SetFloat("_HalfLength", Mathf.Max(0.05f, size.z * 0.5f));
            material.SetFloat("_HalfWidth", Mathf.Max(0.05f, size.x * 0.5f));
            material.SetFloat("_SwimAmp", p.SwimAmp);
            material.SetFloat("_SwimFreq", p.SwimFreq);
            material.SetFloat("_SwimWaves", p.SwimWaves);
            var tentacles = p.TentacleTop >= 0f;
            material.SetFloat("_TentacleTop", tentacles ? min.y + p.TentacleTop * size.y : min.y - 100f);
            material.SetFloat("_TentacleLen", tentacles ? Mathf.Max(0.05f, p.TentacleTop * size.y) : 1f);
            material.SetFloat("_TentacleAmp", tentacles ? p.TentacleAmp * size.y : 0f);
            material.SetFloat("_PulseAmp", p.PulseAmp);
            material.SetFloat("_ClawAmp", p.ClawAmp * size.y);
            material.SetFloat("_ClawFront", p.ClawFront);
            material.SetFloat("_ClawSide", p.ClawSide);
            material.SetFloat("_Flash", 0f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreate(string path, Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
