using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Alfredo.EditorTools
{
    /// <summary>
    /// Importación del pixel art: filtro Point, sin mipmaps, 32 píxeles por unidad y pivote igual al
    /// origen del sprite en GameMaker. Personajes sin compresión; fondos en ASTC 4x4 en Android para
    /// no gastar ~17 MB de memoria por fondo.
    /// </summary>
    public class ArtImportSettings : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/_Game/Art/";
        const string MetaPath = "Assets/_Game/Data/sprites.json";
        static Dictionary<string, SpriteMeta> meta;

        static Dictionary<string, SpriteMeta> Meta
        {
            get
            {
                if (meta != null) return meta;
                meta = new Dictionary<string, SpriteMeta>();
                if (!File.Exists(MetaPath)) return meta;
                var list = JsonUtility.FromJson<SpriteMetaList>(File.ReadAllText(MetaPath));
                foreach (var s in list.sprites) meta[s.name] = s;
                return meta;
            }
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            var ti = (TextureImporter)assetImporter;
            string spriteName = Path.GetFileName(Path.GetDirectoryName(assetPath));
            bool background = assetPath.Contains("/Backgrounds/");
            bool character = assetPath.Contains("/Characters/");

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 32f;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            Vector2 pivot = new Vector2(0.5f, 0.5f);
            if (character) pivot = new Vector2(0.5f, 0f);
            else if (Meta.TryGetValue(spriteName, out var m) && m.w > 0 && m.h > 0)
                pivot = new Vector2(m.ox / (float)m.w, 1f - m.oy / (float)m.h);
            settings.spritePivot = pivot;
            ti.SetTextureSettings(settings);

            var android = ti.GetPlatformTextureSettings("Android");
            android.overridden = background;
            android.maxTextureSize = 4096;
            android.format = TextureImporterFormat.ASTC_4x4;
            ti.SetPlatformTextureSettings(android);
        }

        [MenuItem("Alfredo/0. Reimportar arte", priority = 0)]
        public static void ReimportArt()
        {
            meta = null;
            AssetDatabase.ImportAsset(ArtRoot.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("[Alfredo] Arte reimportado con filtro Point y 32 px por unidad.");
        }
    }
}
