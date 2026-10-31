using UnityEditor;
using UnityEngine;

namespace LabQuimicaVR.EditorTools
{
    /// <summary>
    /// Ajustes de importacion de las fotos reales, retratos y paisajes 360: tamano maximo y
    /// compresion "crunch" para que el APK no crezca demasiado.
    /// </summary>
    public class LabTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            var ti = (TextureImporter)assetImporter;
            string p = assetPath.Replace('\\', '/');
            if (p.StartsWith("Assets/Resources/Fotos/"))
            {
                Configure(ti, 512, TextureWrapMode.Clamp);
            }
            else if (p.StartsWith("Assets/Resources/Paisajes/"))
            {
                Configure(ti, 2048, TextureWrapMode.Repeat);
                ti.wrapModeV = TextureWrapMode.Clamp;
            }
        }

        [MenuItem("Quimikaze/Reimportar fotos y paisajes", priority = 30)]
        public static void ReimportMedia()
        {
            foreach (var folder in new[] { "Assets/Resources/Fotos", "Assets/Resources/Paisajes" })
                if (AssetDatabase.IsValidFolder(folder))
                    AssetDatabase.ImportAsset(folder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            LabAutoSync.WriteStatus("OK: fotos y paisajes reimportados");
        }

        static void Configure(TextureImporter ti, int maxSize, TextureWrapMode wrap)
        {
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.mipmapEnabled = true;
            ti.wrapMode = wrap;
            ti.filterMode = FilterMode.Bilinear;
            ti.anisoLevel = 2;
            ti.npotScale = TextureImporterNPOTScale.ToNearest;
            ti.maxTextureSize = maxSize;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.crunchedCompression = true;
            ti.compressionQuality = 60;
            var android = ti.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = maxSize;
            android.format = TextureImporterFormat.ETC_RGB4Crunched;
            android.crunchedCompression = true;
            android.compressionQuality = 60;
            ti.SetPlatformTextureSettings(android);
        }
    }
}
