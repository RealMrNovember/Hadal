using UnityEditor;
using UnityEngine;

namespace Hadal.EditorTools
{
    /// <summary>
    /// Import settings for the rendered HUD icons (art/blender/icons.py → Resources/Icons): clean alpha
    /// edges, no mipmaps (they are drawn near their native size), clamped so edges do not bleed.
    /// </summary>
    public sealed class HadalIconImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Hadal/Resources/Icons/"))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 256;
        }
    }
}
