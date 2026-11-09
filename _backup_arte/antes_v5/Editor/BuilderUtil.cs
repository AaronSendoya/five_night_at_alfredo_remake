using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Alfredo.EditorTools
{
    /// <summary>Utilidades del constructor: capas, sprites, materiales y texturas generadas.</summary>
    public static class BuilderUtil
    {
        public const string GameRoot = "Assets/_Game/";
        public const string GeneratedRoot = "Assets/_Game/Generated/";

        public static int WallsLayer => LayerMask.NameToLayer("Walls");
        public static int ObstaclesLayer => LayerMask.NameToLayer("Obstacles");

        public static void EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            SetLayer(layers, 8, "Walls");
            SetLayer(layers, 9, "Obstacles");
            tagManager.ApplyModifiedProperties();
        }

        static void SetLayer(SerializedProperty layers, int index, string name)
        {
            var p = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(p.stringValue) || p.stringValue == name) p.stringValue = name;
            else Debug.LogWarning($"[Alfredo] La capa {index} ya se llama '{p.stringValue}'; se esperaba '{name}'.");
        }

        public static MapData LoadMap() =>
            JsonUtility.FromJson<MapData>(File.ReadAllText(GameRoot + "Data/map.json"));

        /// <summary>Cuadros de un sprite del original en orden.</summary>
        public static Sprite[] Frames(string spriteName)
        {
            string folder = null;
            foreach (var group in new[] { "Characters", "Backgrounds", "Props" })
            {
                string candidate = $"Assets/_Game/Art/{group}/{spriteName}";
                if (AssetDatabase.IsValidFolder(candidate)) { folder = candidate; break; }
            }
            if (folder == null) { Debug.LogError("[Alfredo] Falta el sprite " + spriteName); return new Sprite[0]; }
            var list = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; ; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{spriteName}_{i}.png");
                if (s == null) break;
                list.Add(s);
            }
            if (list.Count == 0) Debug.LogError("[Alfredo] Sin cuadros para " + spriteName + " (¿falta reimportar el arte?)");
            return list.ToArray();
        }

        public static Sprite First(string spriteName)
        {
            var f = Frames(spriteName);
            return f.Length > 0 ? f[0] : null;
        }

        public static DirectionalSprites Directional(string prefix, string suffix = "") => new DirectionalSprites
        {
            down = Frames($"{prefix}_caminar_frente{suffix}"),
            up = Frames($"{prefix}_caminar_atras{suffix}"),
            right = Frames($"{prefix}_caminar_derecha{suffix}"),
            left = Frames($"{prefix}_caminar_izquierda{suffix}")
        };

        static Material litMaterial, unlitMaterial;

        public static Material SpriteLit => litMaterial != null ? litMaterial : (litMaterial = FindMaterial("Sprite-Lit-Default"));
        public static Material SpriteUnlit => unlitMaterial != null ? unlitMaterial : (unlitMaterial = FindMaterial("Sprite-Unlit-Default"));

        static Material FindMaterial(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Material"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            Debug.LogWarning("[Alfredo] No se encontró el material " + name);
            return null;
        }

        public static SpriteRenderer AddSprite(GameObject go, Sprite sprite, bool lit = true)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            var mat = lit ? SpriteLit : SpriteUnlit;
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }

        public static GameObject Child(string name, Transform parent, Vector3 localPos = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        /// <summary>Sprite blanco o circular generado y guardado como recurso.</summary>
        /// <summary>Íconos y minimapa de la interfaz: filtro bilineal (están suavizados) y sin compresión.</summary>
        public static Sprite UiSprite(string file)
        {
            string path = "Assets/_Game/UI/" + file + ".png";
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogError("[Alfredo] Falta " + path); return null; }
            if (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single || ti.filterMode != FilterMode.Bilinear || ti.mipmapEnabled)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.filterMode = FilterMode.Bilinear;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Sprite GeneratedSprite(string name, int size, bool circle)
        {
            Directory.CreateDirectory(GeneratedRoot);
            string path = GeneratedRoot + name + ".png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float r = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float a = 1f;
                        if (circle)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                            a = Mathf.Clamp01(r - d);
                        }
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType = TextureImporterType.Sprite;
                ti.spritePixelsPerUnit = size;
                ti.mipmapEnabled = false;
                ti.filterMode = circle ? FilterMode.Bilinear : FilterMode.Point;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Alfredo] {target.GetType().Name} no tiene el campo '{field}'"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetRefs(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Alfredo] {target.GetType().Name} no tiene el campo '{field}'"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetValue(Object target, string field, System.Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Alfredo] {target.GetType().Name} no tiene el campo '{field}'"); return; }
            set(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetDirectional(Object target, string field, DirectionalSprites d)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Alfredo] {target.GetType().Name} no tiene el campo '{field}'"); return; }
            Fill(p.FindPropertyRelative("down"), d.down);
            Fill(p.FindPropertyRelative("up"), d.up);
            Fill(p.FindPropertyRelative("right"), d.right);
            Fill(p.FindPropertyRelative("left"), d.left);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Fill(SerializedProperty arr, Sprite[] sprites)
        {
            arr.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }
}
