#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Hadal.EditorTools
{
    /// <summary>
    /// Creates the URP pipeline (mobile-tuned) and the abyss post-processing profile, and makes the
    /// pipeline active in Graphics and every quality level. Without it the player falls back to the
    /// built-in renderer and URP materials render magenta.
    /// </summary>
    public static class HadalRenderSetup
    {
        public const string RenderingPath = "Assets/_Hadal/Settings/Rendering";
        public const string PipelinePath = RenderingPath + "/HadalURP.asset";
        public const string RendererPath = RenderingPath + "/HadalURP_Renderer.asset";
        public const string ProfilePath = RenderingPath + "/AbyssVolumeProfile.asset";

        [MenuItem("Hadal/Setup/Render Pipeline")]
        public static UniversalRenderPipelineAsset EnsurePipeline()
        {
            HadalProjectSetup.EnsureFolder(RenderingPath);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            // Mobile budget: one main light with soft shadows, no additional-light shadows, HDR for bloom.
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 2;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 45f;
            pipeline.shadowCascadeCount = 1;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            var current = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);

            EnsureVolumeProfile();
            AssetDatabase.SaveAssets();
            Debug.Log("[Hadal] URP pipeline active: " + PipelinePath);
            return pipeline;
        }

        public static VolumeProfile EnsureVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            var bloom = Get<Bloom>(profile);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(0.75f, 0.95f, 1f));
            bloom.highQualityFiltering.Override(false);

            Get<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);

            var color = Get<ColorAdjustments>(profile);
            color.postExposure.Override(-0.4f);
            color.contrast.Override(18f);
            color.colorFilter.Override(new Color(0.86f, 0.97f, 1f));
            color.saturation.Override(-8f);

            var vignette = Get<Vignette>(profile);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0f, 0.03f, 0.06f));

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var existing))
                return existing;

            var component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
#endif
