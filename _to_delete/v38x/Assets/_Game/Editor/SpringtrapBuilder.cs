using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Alfredo.EditorTools
{
    /// <summary>Importación de los cuadros de Springtrap: 100 px por unidad, pivote en los pies y filtro suave.</summary>
    public class SpringtrapImportSettings : AssetPostprocessor
    {
        public const string SpritesRoot = "Assets/_Game/Springtrap/Sprites/";
        public const float PixelsPerUnit = 100f;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpritesRoot)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PixelsPerUnit;
            ti.filterMode = FilterMode.Bilinear; // la hoja tiene detalle fino: con Point parpadea al escalarla
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 512;
            ti.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            ti.SetTextureSettings(settings);
        }
    }

    /// <summary>
    /// FASE 1 de Springtrap: crea el prefab y una escena de vista previa. No toca las escenas del juego, ni la noche 5,
    /// ni la lista de escenas del APK.
    /// </summary>
    public static class SpringtrapBuilder
    {
        const string Folder = "Assets/_Game/Springtrap/";
        public const string PrefabPath = Folder + "Springtrap.prefab";
        public const string ScenePath = Folder + "SpringtrapVista.unity";
        // Mismo valor que usa GameSceneBuilder para Alfredo, Matilda, Bongo y Rufus.
        const float VisualScale = 4.9f;

        [MenuItem("Alfredo/Springtrap/Crear personaje y escena de vista previa", priority = 200)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.ImportAsset(Folder + "Sprites", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            var prefab = BuildPrefab();
            if (prefab == null) return;
            BuildPreviewScene(prefab);
            Debug.Log("[Alfredo] Springtrap listo (solo arte, sin conectar al juego): " + PrefabPath + " y " + ScenePath);
        }

        [MenuItem("Alfredo/Springtrap/Abrir escena de vista previa", priority = 201)]
        public static void OpenPreview()
        {
            if (!File.Exists(ScenePath)) { Build(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        /// <summary>Reproduce la escena de vista previa. El Play normal arranca siempre desde el menú (PlayFromMenu), por eso se desactiva solo esta vez.</summary>
        [MenuItem("Alfredo/Springtrap/Ver animaciones (abre la vista previa y da Play)", priority = 202)]
        public static void PlayPreview()
        {
            if (EditorApplication.isPlaying) return;
            if (!File.Exists(ScenePath)) Build();
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            EditorSceneManager.playModeStartScene = null; // se vuelve a poner el menú solo al recargar los scripts
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        static void RestoreMenuStartScene()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) PlayFromMenu.Apply();
            };
        }

        static Sprite[] Load(string anim)
        {
            string dir = Folder + "Sprites/" + anim;
            var paths = AssetDatabase.FindAssets("t:Sprite", new[] { dir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToArray();
            var list = paths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).Where(s => s != null).ToArray();
            if (list.Length == 0) Debug.LogError("[Alfredo] Springtrap: faltan los cuadros de " + dir);
            return list;
        }

        static SpringtrapSet LoadSet(string action) => new SpringtrapSet
        {
            front = Load(action + "_front"),
            right = Load(action + "_der"),
            left = Load(action + "_izq")
        };

        static GameObject BuildPrefab()
        {
            var walk = LoadSet("walk");
            var run = LoadSet("run");
            var attack = LoadSet("attack");
            if (walk.front.Length == 0) return null;

            var root = new GameObject("Springtrap");
            var visual = BuilderUtil.Child("Visual", root.transform);
            visual.transform.localScale = new Vector3(VisualScale, VisualScale, 1f);
            BuilderUtil.AddSprite(visual, walk.front[0]);
            var view = visual.AddComponent<SpringtrapView>();

            var so = new SerializedObject(view);
            FillSet(so.FindProperty("walk"), walk);
            FillSet(so.FindProperty("run"), run);
            FillSet(so.FindProperty("attack"), attack);
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        static void FillSet(SerializedProperty set, SpringtrapSet data)
        {
            Fill(set.FindPropertyRelative("front"), data.front);
            Fill(set.FindPropertyRelative("right"), data.right);
            Fill(set.FindPropertyRelative("left"), data.left);
        }

        static void Fill(SerializedProperty arr, Sprite[] sprites)
        {
            arr.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        static void BuildPreviewScene(GameObject prefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.17f);
            cam.orthographic = true;
            cam.orthographicSize = 23f;
            camGo.transform.position = new Vector3(0f, -13f, -10f);

            NewGlobalLight();

            // Fila superior: comparación de tamaño y estilo con los animatrónicos actuales (misma escala que en el juego).
            var cmp = new GameObject("Comparacion");
            string[] names = { "Alfredo", "Matilda", "Bongo", "Rufus" };
            string[] sprites = { "sp_freddy_caminar_frente", "sp_chica_caminar_frente", "sp_bonnie_caminar_frente_guitarra", "sp_foxy_caminar_frente" };
            for (int i = 0; i < names.Length; i++)
            {
                var go = new GameObject(names[i]);
                go.transform.SetParent(cmp.transform, false);
                go.transform.position = new Vector3(-16f + i * 8f, 0f, 0f);
                var v = BuilderUtil.Child("Visual", go.transform);
                v.transform.localScale = new Vector3(VisualScale, VisualScale, 1f);
                BuilderUtil.AddSprite(v, BuilderUtil.First(sprites[i]));
                go.AddComponent<GalleryLabel>().text = names[i];
            }
            var spring = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            spring.name = "Springtrap (comparación)";
            spring.transform.SetParent(cmp.transform, false);
            spring.transform.position = new Vector3(16f, 0f, 0f);
            SetView(spring, SpringtrapAction.Walk, Facing.Down, "Springtrap", false);

            // Tres filas (caminar, correr, atacar) x tres vistas (frente, derecha, izquierda), todas en bucle.
            string[] actionNames = { "Caminar", "Correr", "Atacar" };
            var actions = new[] { SpringtrapAction.Walk, SpringtrapAction.Run, SpringtrapAction.Attack };
            var facings = new[] { Facing.Down, Facing.Right, Facing.Left };
            string[] facingNames = { "frente", "derecha", "izquierda" };
            var grid = new GameObject("Animaciones");
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    inst.name = $"Springtrap {actionNames[r]} {facingNames[c]}";
                    inst.transform.SetParent(grid.transform, false);
                    inst.transform.position = new Vector3(-8f + c * 8f, -11f - r * 11f, 0f);
                    SetView(inst, actions[r], facings[c], $"{actionNames[r]} - {facingNames[c]}", true);
                }

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void SetView(GameObject instance, SpringtrapAction action, Facing facing, string label, bool autoPlay)
        {
            var view = instance.GetComponentInChildren<SpringtrapView>();
            var so = new SerializedObject(view);
            so.FindProperty("action").enumValueIndex = (int)action;
            so.FindProperty("facing").enumValueIndex = (int)facing;
            so.FindProperty("label").stringValue = label;
            so.FindProperty("autoPlay").boolValue = autoPlay;
            so.ApplyModifiedPropertiesWithoutUndo();
            // En modo edición se ve el primer cuadro de esa vista; en Play se anima.
            var frames = (action == SpringtrapAction.Run ? FirstFrames(so, "run") : action == SpringtrapAction.Attack ? FirstFrames(so, "attack") : FirstFrames(so, "walk"));
            var sprite = frames[facing == Facing.Right ? 1 : facing == Facing.Left ? 2 : 0];
            if (sprite != null) view.GetComponent<SpriteRenderer>().sprite = sprite;
        }

        static Sprite[] FirstFrames(SerializedObject so, string set)
        {
            var p = so.FindProperty(set);
            Sprite First(string n) { var a = p.FindPropertyRelative(n); return a.arraySize > 0 ? a.GetArrayElementAtIndex(0).objectReferenceValue as Sprite : null; }
            return new[] { First("front"), First("right"), First("left") };
        }

        static void NewGlobalLight()
        {
            var go = new GameObject("Luz global");
            var l = go.AddComponent<Light2D>();
            l.intensity = 1f;
            l.color = Color.white;
            var so = new SerializedObject(l);
            var typeProp = so.FindProperty("m_LightType");
            if (typeProp != null) typeProp.intValue = (int)Light2D.LightType.Global;
            var layers = so.FindProperty("m_ApplyToSortingLayers");
            if (layers != null)
            {
                var all = SortingLayer.layers;
                layers.arraySize = all.Length;
                for (int i = 0; i < all.Length; i++) layers.GetArrayElementAtIndex(i).intValue = all[i].id;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
