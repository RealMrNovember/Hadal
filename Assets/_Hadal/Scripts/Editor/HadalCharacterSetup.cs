#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Hadal.EditorTools
{
    /// <summary>
    /// One Animator Controller and one prefab per character FBX (art/blender/characters.py). Each
    /// clip becomes a state with the same name, so gameplay code drives animation with
    /// <c>Animator.CrossFade("Walk", ...)</c> and needs no transitions.
    /// </summary>
    public static class HadalCharacterSetup
    {
        public const string PrefabsPath = "Assets/_Hadal/Prefabs/Characters";
        private const string ControllersPath = HadalArtImporter.CharactersRoot + "Controllers";

        [MenuItem("Hadal/Art/Build Character Prefabs")]
        public static Dictionary<string, GameObject> BuildPrefabs()
        {
            HadalProjectSetup.EnsureFolder(PrefabsPath);
            HadalProjectSetup.EnsureFolder(ControllersPath);

            var prefabs = new Dictionary<string, GameObject>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { HadalArtImporter.CharactersRoot.TrimEnd('/') }))
            {
                var modelPath = AssetDatabase.GUIDToAssetPath(guid);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                    .OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__"))
                    .ToArray();
                if (model == null || clips.Length == 0)
                    continue;

                var id = model.name;
                var controller = BuildController(id, clips);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullCompletely;

                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabsPath}/{id}.prefab");
                Object.DestroyImmediate(instance);
                prefabs[id] = prefab;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Hadal] Character prefabs: {prefabs.Count} in {PrefabsPath}");
            return prefabs;
        }

        private static AnimatorController BuildController(string id, AnimationClip[] clips)
        {
            var path = $"{ControllersPath}/{id}.controller";

            // Rebuilding an unchanged controller only reshuffles its internal ids; keep it so
            // re-running the setup leaves git clean.
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null)
            {
                var states = existing.layers[0].stateMachine.states.Select(s => s.state).ToArray();
                var unchanged = states.Length == clips.Length
                    && clips.All(c => states.Any(s => s.name == c.name && s.motion == c));
                if (unchanged)
                    return existing;
            }

            AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;

            foreach (var clip in clips.OrderBy(c => c.name))
            {
                var state = machine.AddState(clip.name);
                state.motion = clip;
                if (clip.name is "Idle" or "Flying_Idle")
                    machine.defaultState = state;
            }

            return controller;
        }
    }
}
#endif
