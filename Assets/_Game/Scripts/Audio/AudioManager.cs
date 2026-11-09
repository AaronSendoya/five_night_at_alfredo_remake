using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    public enum Sfx
    {
        Jumpscare, SixAM, PowerOff, PowerOutHum, AlfredoLaugh, PhoneRing, PhoneVoice,
        CameraOn, CameraOpen, CameraStatic, FuseInstalled, FuseBurned, Pickup, Switch, UiClick, DoorClose, DoorOpen,
        // Efectos añadidos después (van al final para no mover los valores del enum ya guardados en las escenas)
        Step1, Step2, Step3, Step4, FlashlightOn, FlashlightOff, CrankLoop, BatteryLow, BatteryEmpty,
        HideIn, HideOut, VentIn, VentOut, DoorBump, PassDoor, Heartbeat, Gasp, AirTick, Tip, FuseAppear,
        FuseGrab, PowerOn, Denied, NightStart, Amb1, Amb2, Amb3
    }

    [Serializable]
    public class SfxClip
    {
        public Sfx id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    /// <summary>
    /// Audio con tres canales independientes (música, ambiente y efectos). Los sonidos que el original
    /// no tenía (clic, recoger, instalar, chispazo y el zumbido de tensión) se generan por código.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        static AudioManager instance;

        [SerializeField] List<SfxClip> clips = new List<SfxClip>();
        [SerializeField] AudioClip ambience;
        [SerializeField, Range(0f, 1f)] float ambienceVolume = 0.5f;
        [SerializeField] AudioClip menuMusic;
        [SerializeField, Range(0f, 1f)] float menuMusicVolume = 1f;
        [SerializeField, Range(0f, 1f)] float droneMaxVolume = 0.55f;

        readonly Dictionary<Sfx, SfxClip> map = new Dictionary<Sfx, SfxClip>();
        readonly Dictionary<Sfx, AudioSource> loops = new Dictionary<Sfx, AudioSource>();
        AudioSource musicSource, ambienceSource, sfxSource, menuMusicSource, eventSource;
        float tension;
        int lastStep = -1;

        void Awake()
        {
            instance = this;
            musicSource = NewSource("Musica", true);
            ambienceSource = NewSource("Ambiente", true);
            sfxSource = NewSource("Efectos", false);
            eventSource = NewSource("EventosAmbiente", false);
            menuMusicSource = NewSource("MusicaMenu", true);
            menuMusicSource.clip = menuMusic;

            foreach (var c in clips) if (c.clip != null) map[c.id] = c;
            AddGenerated(Sfx.UiClick, ProceduralAudio.Blip(900f, 0.05f), 0.5f);
            AddGenerated(Sfx.Pickup, ProceduralAudio.Rise(600f, 950f, 0.16f), 0.6f);
            AddGenerated(Sfx.FuseInstalled, ProceduralAudio.Clunk(0.25f), 0.9f);
            AddGenerated(Sfx.FuseBurned, ProceduralAudio.Zap(0.4f), 0.8f);
            AddGenerated(Sfx.Switch, ProceduralAudio.Blip(320f, 0.07f), 0.7f);

            musicSource.clip = ProceduralAudio.Drone(8f);
            musicSource.volume = 0f;
            musicSource.Play();
            ambienceSource.clip = ambience;
            ApplyVolumes();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        AudioSource NewSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            return s;
        }

        void AddGenerated(Sfx id, AudioClip clip, float volume)
        {
            if (!map.ContainsKey(id)) map[id] = new SfxClip { id = id, clip = clip, volume = volume };
        }

        void Update()
        {
            float target = tension * droneMaxVolume * SaveSystem.MusicVolume;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, Time.unscaledDeltaTime * 0.5f);
        }

        // ---------- API estática ----------

        public static void PlaySfx(Sfx id, float volumeScale = 1f)
        {
            if (instance == null || !instance.map.TryGetValue(id, out var c)) return;
            instance.sfxSource.PlayOneShot(c.clip, c.volume * volumeScale * SaveSystem.SfxVolume);
        }

        /// <summary>Un paso al azar entre cuatro variantes (nunca repite la misma dos veces seguidas).</summary>
        public static void PlayStep(float volumeScale = 1f)
        {
            if (instance == null) return;
            int n = UnityEngine.Random.Range(0, 4);
            if (n == instance.lastStep) n = (n + 1) % 4;
            instance.lastStep = n;
            PlaySfx((Sfx)((int)Sfx.Step1 + n), volumeScale);
        }

        /// <summary>Sonido lejano del edificio, con paneo a un lado (crujidos, golpes en las tuberías...).</summary>
        public static void PlayAmbientEvent(Sfx id, float volumeScale, float pan)
        {
            if (instance == null || !instance.map.TryGetValue(id, out var c)) return;
            instance.eventSource.panStereo = Mathf.Clamp(pan, -1f, 1f);
            instance.eventSource.PlayOneShot(c.clip, c.volume * volumeScale * SaveSystem.SfxVolume);
        }

        /// <summary>Latido del jugador: 0 = silencio, 1 = a todo volumen. Solo suena con el peligro cerca.</summary>
        public static void SetHeartbeat(float t)
        {
            if (instance == null) return;
            const float threshold = 0.3f;
            if (t < threshold)
            {
                if (IsPlaying(Sfx.Heartbeat)) StopLoop(Sfx.Heartbeat);
                return;
            }
            PlayLoop(Sfx.Heartbeat);
            if (instance.loops.TryGetValue(Sfx.Heartbeat, out var src) && instance.map.TryGetValue(Sfx.Heartbeat, out var c))
            {
                float k = Mathf.InverseLerp(threshold, 1f, t);
                src.volume = c.volume * (0.25f + 0.75f * k) * SaveSystem.SfxVolume;
                src.pitch = Mathf.Lerp(0.9f, 1.25f, k);
            }
        }

        public static void PlayMenuMusic()
        {
            if (instance == null || instance.menuMusicSource.clip == null) return;
            ApplyVolumes();
            if (!instance.menuMusicSource.isPlaying) instance.menuMusicSource.Play();
        }

        public static void StopMenuMusic()
        {
            if (instance != null) instance.menuMusicSource.Stop();
        }

        /// <summary>Reproduce un clip suelto en el canal de efectos (voces de Shadow Alfredo, etc.).</summary>
        public static void PlayClip(AudioClip clip, float volume = 1f)
        {
            if (instance == null || clip == null) return;
            instance.sfxSource.PlayOneShot(clip, volume * SaveSystem.SfxVolume);
        }

        public static void PlayLoop(Sfx id, bool loop = true)
        {
            if (instance == null || !instance.map.TryGetValue(id, out var c)) return;
            if (!instance.loops.TryGetValue(id, out var src))
            {
                src = instance.NewSource("Loop_" + id, loop);
                instance.loops[id] = src;
            }
            src.loop = loop;
            if (src.clip != c.clip) src.clip = c.clip;
            src.volume = c.volume * SaveSystem.SfxVolume;
            if (!src.isPlaying) src.Play();
        }

        public static void StopLoop(Sfx id)
        {
            if (instance != null && instance.loops.TryGetValue(id, out var src)) src.Stop();
        }

        public static bool IsPlaying(Sfx id) => instance != null && instance.loops.TryGetValue(id, out var src) && src.isPlaying;

        public static void StopAllLoops()
        {
            if (instance == null) return;
            foreach (var s in instance.loops.Values) s.Stop();
        }

        public static void SetAmbience(bool on)
        {
            if (instance == null || instance.ambienceSource.clip == null) return;
            if (on && !instance.ambienceSource.isPlaying) instance.ambienceSource.Play();
            if (!on) instance.ambienceSource.Stop();
        }

        /// <summary>0 = silencio, 1 = máxima tensión (zumbido grave).</summary>
        public static void SetTension(float value)
        {
            if (instance != null) instance.tension = Mathf.Clamp01(value);
        }

        public static void ApplyVolumes()
        {
            if (instance == null) return;
            instance.ambienceSource.volume = instance.ambienceVolume * SaveSystem.AmbienceVolume;
            instance.menuMusicSource.volume = instance.menuMusicVolume * SaveSystem.MusicVolume;
        }
    }
}
