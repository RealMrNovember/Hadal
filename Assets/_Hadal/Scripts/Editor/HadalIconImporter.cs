using UnityEditor;
using UnityEngine;

namespace Hadal.EditorTools
{
    /// <summary>
    /// Import settings for the rendered HUD icons (art/blender/icons.py → Resources/Icons): clean alpha
    /// edges, no mipmaps (they are drawn near their native size), clamped so edges do not bleed. The UI skin
    /// (art/ui/make_skin.py → Resources/UiSkin) gets the same, kept at its exact size so 9-slice borders line up.
    /// </summary>
    public sealed class HadalIconImporter : AssetPostprocessor
    {
        /// <summary>The UI font (Fredoka): symbols it lacks (arrows, ✓, ★) come from the phone's own fonts.</summary>
        private void OnPreprocessAsset()
        {
            if (!assetPath.EndsWith("Fonts/Fredoka-SemiBold.ttf") || assetImporter is not TrueTypeFontImporter font)
                return;
            font.fontNames = new[] { "Roboto", "Noto Sans", "Noto Sans Symbols", "Noto Sans Symbols 2", "Arial" };
            font.includeFontData = true;
        }

        private void OnPreprocessTexture()
        {
            var skin = assetPath.StartsWith("Assets/_Hadal/Resources/UiSkin/");
            if (!skin && !assetPath.StartsWith("Assets/_Hadal/Resources/Icons/"))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 256;
            if (skin)
            {
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            }
        }
    }
}
