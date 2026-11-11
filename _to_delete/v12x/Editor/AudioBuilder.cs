using UnityEditor;
using UnityEngine;

namespace Alfredo.EditorTools
{
    /// <summary>Asigna los sonidos del original al AudioManager.</summary>
    public static class AudioBuilder
    {
        static readonly (Sfx id, string file, float volume)[] Clips =
        {
            (Sfx.Jumpscare, "susto.wav", 1f), // chillido distorsionado al estilo FNaF 4 (el original queda en snd_jumpscare_1.mp3)
            (Sfx.SixAM, "snd_6am.mp3", 1f),
            (Sfx.PowerOff, "snd_poweroff.wav", 0.9f),
            (Sfx.PowerOutHum, "snd_power_out.mp3", 0.7f),
            (Sfx.AlfredoLaugh, "snd_freddy_laugh.mp3", 0.9f),
            (Sfx.PhoneRing, "snd_telefono_ring.wav", 0.7f),
            (Sfx.PhoneVoice, "snd_phone_guy_hello.mp3", 1f),
            (Sfx.CameraOn, "snd_camera_on.wav", 0.6f),
            (Sfx.CameraOpen, "snd_open_camera.mp3", 0.7f),
            (Sfx.CameraStatic, "snd_camera.wav", 0.35f),
            (Sfx.DoorClose, "puerta_cerrar.wav", 0.9f),
            (Sfx.DoorOpen, "puerta_abrir.wav", 0.7f),
            // Efectos nuevos (generados por código con numpy, ver Audio/)
            (Sfx.Step1, "paso_1.wav", 0.32f),
            (Sfx.Step2, "paso_2.wav", 0.32f),
            (Sfx.Step3, "paso_3.wav", 0.32f),
            (Sfx.Step4, "paso_4.wav", 0.32f),
            (Sfx.FlashlightOn, "linterna_on.wav", 0.8f),
            (Sfx.FlashlightOff, "linterna_off.wav", 0.7f),
            (Sfx.CrankLoop, "manivela_loop.wav", 0.55f),
            (Sfx.BatteryLow, "bateria_baja.wav", 0.6f),
            (Sfx.BatteryEmpty, "bateria_vacia.wav", 0.7f),
            (Sfx.HideIn, "esconderse_entrar.wav", 0.7f),
            (Sfx.HideOut, "esconderse_salir.wav", 0.6f),
            (Sfx.VentIn, "ventila_entrar.wav", 0.8f),
            (Sfx.VentOut, "ventila_salir.wav", 0.75f),
            (Sfx.DoorBump, "puerta_golpe.wav", 0.8f),
            (Sfx.PassDoor, "pasar_puerta.wav", 0.5f),
            (Sfx.Heartbeat, "latido_loop.wav", 0.9f),
            (Sfx.Gasp, "jadeo.wav", 0.7f),
            (Sfx.AirTick, "aire_aviso.wav", 0.6f),
            (Sfx.Tip, "consejo.wav", 0.45f),
            (Sfx.FuseAppear, "fusible_aparece.wav", 0.35f),
            (Sfx.FuseGrab, "fusible_recoger.wav", 0.7f),
            (Sfx.PowerOn, "luz_vuelve.wav", 0.85f),
            (Sfx.Denied, "denegado.wav", 0.55f),
            (Sfx.NightStart, "noche_inicio.wav", 0.8f),
            (Sfx.Amb1, "amb_crujido.wav", 0.8f),
            (Sfx.Amb2, "amb_golpeteo.wav", 0.8f),
            (Sfx.Amb3, "amb_golpe_lejano.wav", 0.8f),
            // Foley con modelado físico (sustituyen a los tonos sintéticos de ProceduralAudio)
            (Sfx.Pickup, "linterna_recoger.wav", 0.75f),
            (Sfx.FuseInstalled, "fusible_instalar.wav", 0.85f),
            (Sfx.Switch, "interruptor.wav", 0.8f),
            (Sfx.TiredBreath, "respiracion_cansada.wav", 0.75f),
        };

        /// <summary>Música del menú (solo el menú). Se acepta .ogg, .mp3 o .wav con el nombre musica_menu.</summary>
        static readonly string[] MenuMusicFiles = { "musica_menu.ogg", "musica_menu.mp3", "musica_menu.wav" };

        public static void Configure(AudioManager audio, bool menu = false)
        {
            var so = new SerializedObject(audio);
            var list = so.FindProperty("clips");
            list.arraySize = Clips.Length;
            for (int i = 0; i < Clips.Length; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)Clips[i].id;
                e.FindPropertyRelative("clip").objectReferenceValue = Load(Clips[i].file);
                e.FindPropertyRelative("volume").floatValue = Clips[i].volume;
            }
            so.FindProperty("ambience").objectReferenceValue = Load("snd_ambience.mp3");
            so.FindProperty("menuMusic").objectReferenceValue = menu ? LoadMenuMusic() : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static AudioClip LoadMenuMusic()
        {
            foreach (var f in MenuMusicFiles)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/" + f);
                if (clip != null) return clip;
            }
            Debug.LogWarning("[Alfredo] No hay música de menú (Assets/_Game/Audio/musica_menu.ogg). El menú quedará sin música.");
            return null;
        }

        static AudioClip Load(string file)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/" + file);
            if (clip == null) Debug.LogError("[Alfredo] Falta el audio " + file);
            return clip;
        }

        /// <summary>Música y voces largas en streaming para no ocupar memoria; efectos cortos descomprimidos.</summary>
        public static void ConfigureImport()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Game/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                var s = imp.defaultSampleSettings;
                bool longClip = clip != null && clip.length > 8f;
                s.loadType = longClip ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                // Ajusta la frecuencia de muestreo al contenido real del sonido: pesa menos sin perder calidad audible.
                s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                imp.defaultSampleSettings = s;
                bool music = System.IO.Path.GetFileName(path).StartsWith("musica_");
                if (music) { s.loadType = AudioClipLoadType.Streaming; s.quality = 0.6f; imp.defaultSampleSettings = s; }
                imp.forceToMono = !music; // la música del menú se queda en estéreo
                imp.SaveAndReimport();
            }
        }
    }
}
