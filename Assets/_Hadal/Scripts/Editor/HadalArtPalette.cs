#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hadal.EditorTools
{
    /// <summary>
    /// URP Lit materials for the generated art. Mirrors PALETTE in art/blender/hadal_kit.py
    /// (linear colours); FBX materials named HAD_&lt;Name&gt; are remapped to these on import.
    /// </summary>
    public static class HadalArtPalette
    {
        public const string MaterialsPath = "Assets/_Hadal/Art/Materials";
        public const string Prefix = "HAD_";

        private readonly struct Entry
        {
            public readonly Color Base;
            public readonly float Metallic;
            public readonly float Smoothness;
            public readonly Color Emission;
            public readonly float Alpha;

            public Entry(float r, float g, float b, float metallic, float roughness,
                         float er = 0, float eg = 0, float eb = 0, float strength = 0, float alpha = 1)
            {
                Base = new Color(r, g, b);
                Metallic = metallic;
                Smoothness = 1f - roughness;
                // Blender strengths are tuned for Cycles; URP bloom needs roughly half.
                Emission = new Color(er, eg, eb) * (strength * 0.5f);
                Alpha = alpha;
            }
        }

        private static readonly Dictionary<string, Entry> Entries = new()
        {
            ["HullDark"] = new Entry(0.035f, 0.050f, 0.060f, 0.85f, 0.42f),
            ["HullMid"] = new Entry(0.090f, 0.120f, 0.135f, 0.80f, 0.38f),
            ["Steel"] = new Entry(0.300f, 0.320f, 0.330f, 1.00f, 0.28f),
            ["Hazard"] = new Entry(0.820f, 0.520f, 0.030f, 0.20f, 0.50f),
            ["Rubber"] = new Entry(0.015f, 0.015f, 0.018f, 0.00f, 0.80f),
            ["Glass"] = new Entry(0.050f, 0.180f, 0.200f, 0.00f, 0.05f, alpha: 0.35f),
            ["GlowCyan"] = new Entry(0.020f, 0.300f, 0.350f, 0f, 0.30f, 0.10f, 0.90f, 1.00f, 6f),
            ["GlowAmber"] = new Entry(0.400f, 0.200f, 0.020f, 0f, 0.30f, 1.00f, 0.55f, 0.12f, 5f),
            ["GlowGreen"] = new Entry(0.050f, 0.300f, 0.080f, 0f, 0.30f, 0.25f, 1.00f, 0.35f, 5f),
            ["GlowRed"] = new Entry(0.300f, 0.020f, 0.020f, 0f, 0.30f, 1.00f, 0.10f, 0.08f, 6f),
            ["Plant"] = new Entry(0.040f, 0.220f, 0.060f, 0f, 0.70f, 0.02f, 0.10f, 0.03f, 0.6f),
            ["Sand"] = new Entry(0.180f, 0.200f, 0.190f, 0f, 0.95f),
            ["Rock"] = new Entry(0.060f, 0.070f, 0.075f, 0f, 0.90f),
            ["Kelp"] = new Entry(0.030f, 0.120f, 0.060f, 0f, 0.60f, 0.01f, 0.05f, 0.02f, 0.4f),
            ["Coral"] = new Entry(0.450f, 0.120f, 0.180f, 0f, 0.70f, 0.30f, 0.05f, 0.12f, 0.8f),
            ["GlowViolet"] = new Entry(0.200f, 0.050f, 0.400f, 0f, 0.20f, 0.55f, 0.20f, 1.00f, 5f),
            ["PaintTeal"] = new Entry(0.040f, 0.300f, 0.300f, 0.30f, 0.55f),
            // The Challenger submersible (art/blender/challenger.py).
            ["PaintLime"] = new Entry(0.360f, 0.720f, 0.015f, 0.05f, 0.45f),
            ["PaintWhite"] = new Entry(0.620f, 0.650f, 0.640f, 0.10f, 0.45f),
            ["GlowWhite"] = new Entry(0.500f, 0.580f, 0.620f, 0f, 0.20f, 0.80f, 0.93f, 1.00f, 6f),
            ["GlowWarm"] = new Entry(0.300f, 0.180f, 0.080f, 0f, 0.40f, 1.00f, 0.70f, 0.40f, 3.5f),
        };

        public static IEnumerable<string> Names => Entries.Keys;

        public static string PathFor(string name) => $"{MaterialsPath}/{Prefix}{name}.mat";

        public static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>(PathFor(name));

        [MenuItem("Hadal/Art/Build Palette Materials")]
        public static void BuildMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new System.InvalidOperationException("URP Lit shader not found — is the URP package installed?");

            HadalProjectSetup.EnsureFolder(MaterialsPath);
            foreach (var pair in Entries)
            {
                var path = PathFor(pair.Key);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }

                material.shader = shader;
                Apply(material, pair.Value);
                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Hadal] Palette materials: {Entries.Count} in {MaterialsPath}");
        }

        private static void Apply(Material m, Entry e)
        {
            // Material.SetColor expects sRGB; the palette is linear.
            var baseColor = e.Base.gamma;
            baseColor.a = e.Alpha;
            m.SetColor("_BaseColor", baseColor);
            m.SetFloat("_Metallic", e.Metallic);
            m.SetFloat("_Smoothness", e.Smoothness);

            var emissive = e.Emission.maxColorComponent > 0f;
            m.SetColor("_EmissionColor", emissive ? e.Emission : Color.black);
            SetKeyword(m, "_EMISSION", emissive);
            m.globalIlluminationFlags = emissive ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            var transparent = e.Alpha < 1f;
            m.SetFloat("_Surface", transparent ? 1f : 0f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)(transparent ? BlendMode.SrcAlpha : BlendMode.One));
            m.SetFloat("_DstBlend", (float)(transparent ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            m.SetFloat("_ZWrite", transparent ? 0f : 1f);
            SetKeyword(m, "_SURFACE_TYPE_TRANSPARENT", transparent);
            m.renderQueue = transparent ? (int)RenderQueue.Transparent : -1;
            m.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
            m.enableInstancing = true;
        }

        private static void SetKeyword(Material m, string keyword, bool on)
        {
            if (on)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }
}
#endif
