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
        private const string EmojiFont = "Assets/_Hadal/Resources/Fonts/HadalEmoji-Regular.ttf";

        /// <summary>
        /// The UI fonts: symbols Fredoka lacks (arrows, ✓, ★) come from the phone's own fonts; emoji from the bundled
        /// Hadal Emoji font (art/ui/make_emoji_font.py), the first fallback of Fredoka and of the world font.
        /// </summary>
        private void OnPreprocessAsset()
        {
            if (assetImporter is not TrueTypeFontImporter font)
                return;
            if (assetPath.EndsWith("Fonts/Fredoka-SemiBold.ttf"))
                font.fontNames = new[] { "Hadal Emoji", "Roboto", "Noto Sans", "Noto Sans Symbols", "Noto Sans Symbols 2", "Arial" };
            else if (assetPath.EndsWith("Fonts/HadalSansWorld-Regular.ttf"))
                font.fontNames = new[] { "Hadal Sans World", "Hadal Emoji" };
            else
                return;
            font.includeFontData = true;
            var emoji = AssetDatabase.LoadAssetAtPath<Font>(EmojiFont);
            if (emoji != null)
                font.fontReferences = new[] { emoji };
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
