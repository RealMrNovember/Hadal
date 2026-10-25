using System.IO;
using System.Linq;
using HADAL.Shared.Catalog;
using Hadal.Gameplay.Art;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Hadal.Tests.EditMode
{
    /// <summary>Generated art (art/blender) imported with palette materials and ambient motion.</summary>
    public sealed class ArtPipelineTests
    {
        [TestCase("ANIM_spin_z_30__orbit", ArtAnimator.Mode.Spin, 30f)]
        [TestCase("ANIM_spin_x_25", ArtAnimator.Mode.Spin, 25f)]
        [TestCase("ANIM_pulse_0.8__core", ArtAnimator.Mode.Pulse, 0.8f)]
        [TestCase("ANIM_sway_0.55__kelp2", ArtAnimator.Mode.Sway, 0.55f)]
        public void Parses_anim_node_names(string name, ArtAnimator.Mode mode, float speed)
        {
            Assert.IsTrue(ArtAnimator.TryParse(name, out var parsedMode, out _, out var parsedSpeed));
            Assert.AreEqual(mode, parsedMode);
            Assert.AreEqual(speed, parsedSpeed, 1e-4f);
        }

        [Test]
        public void Spin_axis_maps_blender_z_to_unity_up()
        {
            ArtAnimator.TryParse("ANIM_spin_z_10", out _, out var axis, out _);
            Assert.AreEqual(Vector3.up, axis);
        }

        [TestCase("hull")]
        [TestCase("ANIM_wobble_1")]
        [TestCase("ANIM_spin_z")]
        public void Ignores_other_names(string name)
        {
            Assert.IsFalse(ArtAnimator.TryParse(name, out _, out _, out _));
        }

        [Test]
        public void Every_catalog_building_has_art_with_palette_materials()
        {
            var catalog = JsonUtility.FromJson<GameCatalogData>(
                File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "shared/catalog/catalog.json")));
            foreach (var building in catalog.buildings)
            {
                var path = $"Assets/_Hadal/Art/Buildings/{building.id}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(model, $"missing {path} — run art/blender/generate.py");

                var materials = model.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).ToArray();
                Assert.IsNotEmpty(materials, building.id);
                foreach (var material in materials)
                {
                    Assert.IsNotNull(material, $"{building.id}: empty material slot");
                    StringAssert.StartsWith("Assets/_Hadal/Art/Materials/", AssetDatabase.GetAssetPath(material),
                        $"{building.id}: {material.name} was not remapped to the palette");
                }
            }
        }

        [Test]
        public void Animated_parts_get_an_animator_and_spin_in_place()
        {
            var reactor = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hadal/Art/Buildings/core_reactor.fbx");
            Assert.IsNotNull(reactor);
            var animators = reactor.GetComponentsInChildren<ArtAnimator>(true);
            Assert.AreEqual(2, animators.Length);
            Assert.IsTrue(animators.Any(a => a.AnimationMode == ArtAnimator.Mode.Pulse));
            Assert.AreEqual(Quaternion.identity, reactor.transform.localRotation, "FBX axis conversion left a root rotation");
        }
        [TestCase("crew_engineer")]
        [TestCase("crew_medic")]
        [TestCase("crew_scientist")]
        [TestCase("mech_worker")]
        public void Characters_import_with_named_looping_clips(string id)
        {
            var path = $"Assets/_Hadal/Art/Characters/{id}.fbx";
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);

            Assert.IsTrue(clips.ContainsKey("Idle"), $"{id}: clips are {string.Join(", ", clips.Keys)}");
            Assert.IsTrue(clips.ContainsKey("Walk"), id);
            Assert.IsTrue(clips["Walk"].isLooping, $"{id}: Walk must loop");
        }

        [Test]
        public void Character_materials_are_urp_lit_with_the_atlas()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hadal/Art/Characters/crew_engineer.fbx");
            Assert.IsNotNull(model);
            var material = model.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterial;
            Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name);
            Assert.IsNotNull(material.GetTexture("_BaseMap"), "atlas texture not found next to the FBX");
        }

        [Test]
        public void Character_prefab_has_a_controller_with_a_state_per_clip()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Hadal/Prefabs/Characters/crew_medic.prefab");
            Assert.IsNotNull(prefab, "run Hadal > Art > Build Character Prefabs (part of Create Bootstrap Scene)");
            var controller = (UnityEditor.Animations.AnimatorController)prefab.GetComponentInChildren<Animator>().runtimeAnimatorController;
            var states = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToArray();
            CollectionAssert.Contains(states, "Walk");
            Assert.AreEqual("Idle", controller.layers[0].stateMachine.defaultState.name);
        }
    }
}
