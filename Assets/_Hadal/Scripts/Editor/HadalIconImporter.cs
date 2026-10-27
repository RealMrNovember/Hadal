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
