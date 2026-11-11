using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Alfredo.EditorTools
{
    /// <summary>Menú "Alfredo" del editor: construye todo el proyecto y genera el APK.</summary>
    public static class AlfredoBuildMenu
    {
        const string NightsFolder = "Assets/_Game/Data/Noches";
        const string ApkPath = "Builds/CincoNochesConAlfredo.apk";

        [MenuItem("Alfredo/1. Construir escenas", priority = 10)]
        public static void BuildScenes()
        {
            BuilderUtil.EnsureLayers();
            AudioBuilder.ConfigureImport();
            var nights = CreateNights();
            MenuSceneBuilder.Build();
            // Al crear una escena nueva Unity descarga los objetos recién creados: se recargan desde disco.
            for (int i = 0; i < nights.Length; i++)
                nights[i] = AssetDatabase.LoadAssetAtPath<NightConfig>($"{NightsFolder}/Noche{i + 1}.asset");
            GameSceneBuilder.Build(nights);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuSceneBuilder.ScenePath, true),
                new EditorBuildSettingsScene(GameSceneBuilder.ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[Alfredo] Listo: escenas Menú y Juego creadas y añadidas a la compilación.");
        }

        /// <summary>Configuración de las 4 noches, tal como night_settings en la rama aaron.</summary>
        static NightConfig[] CreateNights()
        {
            Directory.CreateDirectory(NightsFolder);
            var data = new[]
            {
                // ai: Alfredo, Matilda, Bongo, Rufus | espera (s) | persecuciones | fusibles: intervalo, %, rotos al inicio, aparición
                // Fusibles ajustados: antes se quemaban demasiado rápido (70 s / 40 %); ahora el intervalo es
                // más largo, la probabilidad menor y aparecen más repuestos.
                new { n = 1, ai = new[] { 0, 5, 5, 5 },     boot = new[] { 0f, 130f, 110f, 140f }, chases = 2, burn = 110f, chance = 30f, broken = 0, spawn = 35f },
                new { n = 2, ai = new[] { 10, 10, 10, 10 }, boot = new[] { 130f, 80f, 70f, 50f },  chases = 3, burn = 95f, chance = 35f, broken = 0, spawn = 30f },
                new { n = 3, ai = new[] { 15, 15, 15, 15 }, boot = new[] { 70f, 40f, 30f, 10f },   chases = 4, burn = 85f, chance = 40f, broken = 1, spawn = 25f },
                new { n = 4, ai = new[] { 20, 20, 20, 20 }, boot = new[] { 40f, 30f, 20f, 10f },   chases = 4, burn = 75f, chance = 45f, broken = 2, spawn = 20f },
            };
            var result = new NightConfig[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                var d = data[i];
                string path = $"{NightsFolder}/Noche{d.n}.asset";
                var cfg = AssetDatabase.LoadAssetAtPath<NightConfig>(path);
                if (cfg == null)
                {
                    cfg = ScriptableObject.CreateInstance<NightConfig>();
                    AssetDatabase.CreateAsset(cfg, path);
                }
                cfg.night = d.n;
                cfg.secondsPerHour = 70f;
                cfg.hours = 6;
                cfg.aiAlfredo = d.ai[0]; cfg.aiMatilda = d.ai[1]; cfg.aiBongo = d.ai[2]; cfg.aiRufus = d.ai[3];
                cfg.bootAlfredo = d.boot[0]; cfg.bootMatilda = d.boot[1]; cfg.bootBongo = d.boot[2]; cfg.bootRufus = d.boot[3];
                cfg.maxChases = d.chases;
                cfg.fuseBurnInterval = d.burn;
                cfg.fuseBurnChance = d.chance;
                cfg.startBrokenFuses = d.broken;
                cfg.fuseSpawnSeconds = d.spawn;
                EditorUtility.SetDirty(cfg);
                result[i] = cfg;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        [MenuItem("Alfredo/2. Configurar Android", priority = 20)]
        public static void ConfigureAndroid()
        {
            PlayerSettings.companyName = "UNIVALLE";
            PlayerSettings.productName = "Cinco Noches con Alfredo";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.univalle.cinconochesalfredo");
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            // Solo ARM64: es lo que usan los teléfonos reales. x86-64 (emulador) duplicaba las librerías nativas.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Peso del APK: código C# y del motor que no se usa fuera, IL2CPP generando código más compacto
            // y sin símbolos de depuración. No se quita contenido del juego.
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.Android, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
            PlayerSettings.runInBackground = false;
            EditorUserBuildSettings.buildAppBundle = false;
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            Debug.Log("[Alfredo] Android configurado: horizontal, Android 8 o superior, IL2CPP, ARM64, optimizado para peso.");
        }

        /// <summary>
        /// Android no admite "Both" como manejo de entrada. El juego usa la entrada clásica (Input) y el
        /// EventSystem estándar, así que se fija en "Input Manager (Old)" y se reinicia el editor.
        /// </summary>
        [MenuItem("Alfredo/5. Usar entrada clásica (reinicia Unity)", priority = 50)]
        public static void UseLegacyInput()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null) { Debug.LogError("[Alfredo] No se encontró activeInputHandler."); return; }
            if (prop.intValue == 0) { Debug.Log("[Alfredo] La entrada clásica ya está activa."); return; }
            prop.intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            EditorApplication.OpenProject(Directory.GetCurrentDirectory());
        }

        [MenuItem("Alfredo/3. Compilar APK", priority = 30)]
        public static void BuildApk() => Build(false);

        [MenuItem("Alfredo/4. Compilar e instalar en dispositivo", priority = 31)]
        public static void BuildAndRun() => Build(true);

        static void Build(bool run)
        {
            ConfigureAndroid();
            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MenuSceneBuilder.ScenePath, GameSceneBuilder.ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4HC | (run ? BuildOptions.AutoRunPlayer : BuildOptions.None)
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"[Alfredo] APK generado: {Path.GetFullPath(ApkPath)} ({report.summary.totalSize / (1024 * 1024)} MB)");
            else
                Debug.LogError("[Alfredo] La compilación falló: " + report.summary.result);
        }
    }
}
