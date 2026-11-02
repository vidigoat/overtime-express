using UnityEditor;

namespace Overtime.EditorTools
{
    /// <summary>UI icons (Lucide, rasterised to white PNGs) import as crisp, uncompressed sprites so they can be tinted any colour.</summary>
    public class IconImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Icons/") && !assetPath.EndsWith("AppIcon.png")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.alphaIsTransparency = true; ti.mipmapEnabled = !assetPath.EndsWith("AppIcon.png");
            if (assetPath.Contains("/Resources/Icons/")) { ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; }
        }
    }
}
