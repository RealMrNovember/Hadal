using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Hadal.UI
{
    /// <summary>
    /// TextMeshPro fonts and text materials. Labels draw through runtime SDF font assets built from the bundled TTFs
    /// (glyphs are added to the atlas as they are first used, so nothing is pre-baked): Fredoka for Latin-script
    /// languages, the Noto merge (Latin, Cyrillic, Vietnamese, Arabic, Thai) for the rest. Each font falls back to the
    /// Noto merge, the phone's CJK font (the face for the language) and the emoji font, so a player name in any script
    /// still draws. Outlines are shared material presets per font and style, never one material per label.
    /// </summary>
    public static class UiText
    {
        // 48 pt with an 8 px spread: crisp at every UI size, and room for the thick HUD outline (the spread caps it).
        private const int Sampling = 48;
        private const int Spread = 8;

        /// <summary>Outline thickness in SDF units; ~2 reference pixels at caption size, like the old Outline effect.</summary>
        public const float OutlineWidth = 0.45f;

        private const float BoldWeight = 1.1f;

        private static readonly Dictionary<string, TMP_FontAsset> Fonts = new();
        private static readonly Dictionary<(TMP_FontAsset Font, Color32 Color, int Width), Material> Presets = new();
        private static string[] _osFonts;

        /// <summary>The font for the current language (switching language restarts the scene).</summary>
        public static TMP_FontAsset Font
        {
            get
            {
                var font = FontFor(Loc.Current);
                // A TextMeshProUGUI picks the default font in Awake, before UiKit sets ours: make that one ours too.
                var settings = TMP_Settings.instance;
                if (settings != null && font != null && TMP_Settings.defaultFontAsset != font)
                    TMP_Settings.defaultFontAsset = font;
                return font;
            }
        }

        /// <summary>The font with which <paramref name="language"/> is drawn (with its fallback chain).</summary>
        public static TMP_FontAsset FontFor(string language)
        {
            var round = language is not ("ar" or "th" or "ru" or "vi" or "ja" or "ko" or "zh-Hans" or "zh-Hant");
            var face = CjkFace(language);
            var key = (round ? "round-" : "world-") + face;
            if (Fonts.TryGetValue(key, out var font) && font != null)
                return font;

            font = round ? FromResource("Fonts/Fredoka-SemiBold", key) : null;
            font ??= FromResource("Fonts/HadalSansWorld-Regular", key);
            if (font == null)
                return null;

            var chain = new List<TMP_FontAsset>();
            if (round)
                Add(chain, Leaf("world", () => FromResource("Fonts/HadalSansWorld-Regular", "world")));
            Add(chain, Leaf("cjk-" + face, () => Cjk(face)));
            Add(chain, Leaf("emoji", () => FromResource("Fonts/HadalEmoji-Regular", "emoji")));
            // Arrows, stars, check marks, triangles: none of the bundled fonts has them; the legacy fonts drew them
            // through the phone's fonts as well.
            foreach (var file in SymbolFonts)
                Add(chain, Leaf("os-" + file, () => FromOs(file, 0, 1024)));
            font.fallbackFontAssetTable = chain;
            Fonts[key] = font;
            return font;
        }

        /// <summary>Whether the label wears an outline preset (InkSurface leaves those alone: they sit on dark art).</summary>
        public static bool HasOutline(TMP_Text label)
        {
            var material = label != null ? label.fontSharedMaterial : null;
            return material != null && material.IsKeywordEnabled("OUTLINE_ON");
        }

        /// <summary>Draws the label with an outline (shared preset for its font, colour and width).</summary>
        public static TMP_Text Outline(TMP_Text label, Color color, float width = OutlineWidth)
        {
            if (label != null && label.font != null)
                label.fontSharedMaterial = Preset(label.font, color, width);
            return label;
        }

        /// <summary>Back to the font's plain material (no outline).</summary>
        public static void Plain(TMP_Text label)
        {
            if (label != null && label.font != null)
                label.fontSharedMaterial = label.font.material;
        }

        /// <summary>Changes the label's font and keeps its outline (a preset belongs to one font's atlas).</summary>
        public static void SetFont(TMP_Text label, TMP_FontAsset font)
        {
            if (label == null || font == null || label.font == font)
                return;
            var outlined = HasOutline(label);
            var material = label.fontSharedMaterial;
            var color = outlined ? material.GetColor("_OutlineColor") : default;
            var width = outlined ? material.GetFloat("_OutlineWidth") : 0f;
            label.font = font;
            label.fontSharedMaterial = outlined ? Preset(font, color, width) : font.material;
        }

        private static Material Preset(TMP_FontAsset font, Color color, float width)
        {
            var key = (font, (Color32)color, Mathf.RoundToInt(width * 100f));
            if (Presets.TryGetValue(key, out var material) && material != null)
                return material;

            material = new Material(font.material) { name = $"{font.name} Outline {ColorUtility.ToHtmlStringRGBA(color)} {width:0.00}" };
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor("_OutlineColor", color);
            material.SetFloat("_OutlineWidth", width);
            // The outline is centred on the glyph edge: dilate the face by half of it so letters keep their weight.
            material.SetFloat("_FaceDilate", width * 0.5f);
            ShaderUtilities.UpdateShaderRatios(material);
            Presets[key] = material;
            return material;
        }

        private static void Add(List<TMP_FontAsset> chain, TMP_FontAsset font)
        {
            if (font != null)
                chain.Add(font);
        }

        private static TMP_FontAsset Leaf(string key, Func<TMP_FontAsset> make)
        {
            if (Fonts.TryGetValue(key, out var font) && font != null)
                return font;
            font = make();
            if (font != null)
                Fonts[key] = font;
            return font;
        }

        private static TMP_FontAsset FromResource(string path, string name)
        {
            var source = Resources.Load<Font>(path);
            if (source == null)
                return null;
            return Tune(TMP_FontAsset.CreateFontAsset(source, Sampling, Spread, GlyphRenderMode.SDFAA, 1024, 1024), "Hadal " + name);
        }

        /// <summary>Which face of the phone's NotoSansCJK collection draws the language (Android's fonts.xml order).</summary>
        private static int CjkFace(string language) => language switch
        {
            "ko" => 1,
            "zh-Hans" => 2,
            "zh-Hant" => 3,
            _ => 0 // Japanese; also the face for CJK names shown in other languages
        };

        /// <summary>
        /// CJK comes from the phone (a bundled CJK font would add 15+ MB): the NotoSansCJK collection on Android, older
        /// phones' Droid fallback otherwise. Larger atlas pages, since a CJK screen uses hundreds of glyphs.
        /// </summary>
        private static TMP_FontAsset Cjk(int face)
        {
            foreach (var (file, index) in new[]
                     {
                         ("NotoSansCJK-Regular.ttc", face), ("NotoSansCJKjp-Regular.otf", 0), ("NotoSansSC-Regular.otf", 0),
                         ("DroidSansFallbackFull.ttf", 0), ("DroidSansFallback.ttf", 0)
                     })
            {
                var font = FromOs(file, index, 2048);
                if (font != null)
                    return font;
            }

            return null;
        }

        /// <summary>The phone's fonts that carry the symbols UI strings use (Android ships all of these).</summary>
        private static readonly string[] SymbolFonts =
        {
            "NotoSansSymbols-Regular-Subsetted2.ttf", "NotoSansSymbols-Regular-Subsetted.ttf", "RobotoStatic-Regular.ttf", "Roboto-Regular.ttf"
        };

        private static TMP_FontAsset FromOs(string file, int face, int atlas)
        {
            var path = OsFont(file);
            if (path == null)
                return null;
            return Tune(TMP_FontAsset.CreateFontAsset(path, face, Sampling, Spread, GlyphRenderMode.SDFAA, atlas, atlas), $"Hadal OS {file}#{face}");
        }

        /// <summary>
        /// Bold like the legacy faux bold the screens were designed with: heavier, and without TMP's default extra
        /// letter spacing (7/100 em made bold captions look spaced out).
        /// </summary>
        private static TMP_FontAsset Tune(TMP_FontAsset font, string name)
        {
            if (font == null)
                return null;
            font.name = name;
            font.boldStyle = BoldWeight;
            font.boldSpacing = 1f;
            font.material.SetFloat("_WeightBold", BoldWeight);
            ShaderUtilities.UpdateShaderRatios(font.material);
            return font;
        }

        private static string OsFont(string file)
        {
            if (_osFonts == null)
            {
                try
                {
                    _osFonts = UnityEngine.Font.GetPathsToOSFonts() ?? Array.Empty<string>();
                }
                catch (Exception)
                {
                    _osFonts = Array.Empty<string>();
                }
            }

            foreach (var path in _osFonts)
            {
                if (string.Equals(Path.GetFileName(path), file, StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            foreach (var dir in new[] { "/system/fonts/", "/product/fonts/" })
            {
                if (File.Exists(dir + file))
                    return dir + file;
            }

            return null;
        }
    }
}
