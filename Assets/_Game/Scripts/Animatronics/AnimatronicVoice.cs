using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Sonido propio de cada animatrónico al moverse. No es un ruido constante: suena cada pocos segundos
    /// y su volumen depende de la distancia por el recorrido real (también desde la sala de al lado,
    /// a través de la puerta), con paneo izquierda/derecha. Así se sabe quién está cerca y por dónde.
    /// </summary>
    /// <summary>Curva común de volumen por distancia (pasos, risa de Alfredo, canción de Rufus, rugidos).</summary>
    public static class SoundFalloff
    {
        /// <summary>1 muy cerca, baja de forma progresiva y queda en 'floor' fuera de rango (0 = no se oye).</summary>
        public static float Volume(float d, float range, float floor)
        {
            if (float.IsInfinity(d) || float.IsNaN(d) || d >= range) return floor;
            float k = 1f - d / range;
            return Mathf.Lerp(Mathf.Max(floor, 0.08f), 1f, k * k);
        }

        static float nextLog;
        /// <summary>Solo en el editor: registro para comprobar que los sonidos por distancia se disparan.</summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(string msg, bool always = false)
        {
            if (!always && Time.time < nextLog) return;
            nextLog = Time.time + 1.5f;
            Debug.Log("[Sonido] " + msg);
        }

        public static float Pan(Vector2 source, Vector2 listener) => Mathf.Clamp((source.x - listener.x) / 30f, -0.8f, 0.8f);
    }

    public class AnimatronicVoice : MonoBehaviour
    {
        [SerializeField] AnimatronicBrain brain;
        [SerializeField] AudioClip clip;
        [SerializeField] float hearRange = 120f;          // una sala mide ~94 x 48 unidades
        [SerializeField] Vector2 intervalSeconds = new Vector2(3f, 4.5f);
        [SerializeField] float idleMultiplier = 1.8f;
        [SerializeField, Range(0f, 1f)] float volume = 0.9f;
        [Tooltip("Rugido o gemido al empezar a perseguirte")]
        [SerializeField] AudioClip huntClip;
        [SerializeField] float huntHearRange = 150f;
        [SerializeField] float huntCooldown = 7f;

        [Header("Pasos")]
        [Tooltip("Variaciones de la pisada; suenan al ritmo real del paso (más seguidas al correr)")]
        [SerializeField] AudioClip[] steps;
        [SerializeField] float stepPitch = 1f;
        [SerializeField] float stepStride = 2.2f;          // unidades recorridas por pisada
        [SerializeField] float stepHearRange = 110f;       // por el recorrido real (misma sala y la vecina)
        [SerializeField, Range(0f, 1f)] float stepVolume = 0.85f;

        public AudioClip Clip => clip;
        AudioSource stepSource;
        Vector2 lastPos;
        float stepAcc;
        int lastStep = -1;

        bool wasChasing;
        float huntTimer;

        AudioSource source;
        PlayerController player;
        float timer;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            timer = Random.Range(0f, intervalSeconds.y);
            stepSource = gameObject.AddComponent<AudioSource>();
            stepSource.playOnAwake = false;
            stepSource.spatialBlend = 0f;
        }

        void Start() => player = FindFirstObjectByType<PlayerController>();

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying || clip == null || player == null || !brain.IsAwake) { wasChasing = false; return; }
            huntTimer -= Time.deltaTime;
            bool chasing = brain.IsChasing;
            if (chasing && !wasChasing && huntClip != null && huntTimer <= 0f)
            {
                huntTimer = huntCooldown;
                float hd = PathDistance(player.Position, brain.Motor.Position);
                float hk = SoundFalloff.Volume(hd, huntHearRange, 0f);
                if (hk > 0f)
                {
                    source.panStereo = SoundFalloff.Pan(brain.Motor.Position, player.Position);
                    source.PlayOneShot(huntClip, hk * volume * SaveSystem.SfxVolume);
                }
            }
            wasChasing = chasing;
            Steps();
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            bool moving = brain.Motor.IsMoving;
            timer = Random.Range(intervalSeconds.x, intervalSeconds.y) * (moving ? 1f : idleMultiplier);

            float d = PathDistance(player.Position, brain.Motor.Position);
            float k = SoundFalloff.Volume(d, hearRange, 0f);
            if (k <= 0f) return;
            source.panStereo = SoundFalloff.Pan(brain.Motor.Position, player.Position);
            source.PlayOneShot(clip, k * volume * (moving ? 1f : 0.6f) * SaveSystem.SfxVolume);
        }

        /// <summary>
        /// Una pisada cada 'stepStride' unidades recorridas: al correr suenan más seguidas. El volumen crece de forma
        /// gradual cuanto más cerca está (por el recorrido real, también desde la sala vecina) y se panea a su lado.
        /// </summary>
        void Steps()
        {
            Vector2 p = brain.Motor.Position;
            float moved = Vector2.Distance(p, lastPos);
            lastPos = p;
            if (steps == null || steps.Length == 0 || moved > 3f || !brain.Motor.IsMoving) return;   // >3: cruzó una puerta
            stepAcc += moved;
            if (stepAcc < stepStride) return;
            stepAcc = 0f;
            float d = PathDistance(player.Position, p);
            float k = SoundFalloff.Volume(d, stepHearRange, 0f);   // progresivo: suave lejos, fuerte cerca
            if (k <= 0f) return;
            int i = Random.Range(0, steps.Length);
            if (steps.Length > 1 && i == lastStep) i = (i + 1) % steps.Length;
            lastStep = i;
            stepSource.pitch = stepPitch * Random.Range(0.94f, 1.06f) * (brain.IsChasing ? 1.06f : 1f);
            stepSource.panStereo = SoundFalloff.Pan(p, player.Position);
            stepSource.PlayOneShot(steps[i], k * stepVolume * (brain.IsChasing ? 1.15f : 1f) * SaveSystem.SfxVolume);
            SoundFalloff.Log($"Pasos {brain.DisplayName}: dist {d:0} vol {k:0.00}");
        }

        /// <summary>
        /// Distancia a pie por el recorrido real, cruzando TODAS las puertas necesarias (antes solo contaba la sala
        /// vecina y devolvía infinito si estaba a 2 salas o más: por eso casi nunca se oía nada).
        /// </summary>
        public static float PathDistance(Vector2 from, Vector2 to)
        {
            var zones = MapZones.Instance;
            if (zones == null) return Vector2.Distance(from, to);
            var a = zones.NearestZone(from);
            var b = zones.NearestZone(to);
            float total = 0f;
            Vector2 p = from;
            for (int hop = 0; hop < 8 && a != b; hop++)
            {
                var w = ZoneRouter.NextWarp(a, b);
                if (w == null) return float.PositiveInfinity;
                total += Vector2.Distance(p, w.area.center);
                p = w.destination;
                a = w.ToZone;
            }
            if (a != b) return float.PositiveInfinity;
            return total + Vector2.Distance(p, to);
        }
    }
}
