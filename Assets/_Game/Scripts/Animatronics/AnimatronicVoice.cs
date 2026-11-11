using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Sonido propio de cada animatrónico al moverse. No es un ruido constante: suena cada pocos segundos
    /// y su volumen depende de la distancia por el recorrido real (también desde la sala de al lado,
    /// a través de la puerta), con paneo izquierda/derecha. Así se sabe quién está cerca y por dónde.
    /// </summary>
    public class AnimatronicVoice : MonoBehaviour
    {
        [SerializeField] AnimatronicBrain brain;
        [SerializeField] AudioClip clip;
        [SerializeField] float hearRange = 32f;
        [SerializeField] Vector2 intervalSeconds = new Vector2(3f, 4.5f);
        [SerializeField] float idleMultiplier = 1.8f;
        [SerializeField, Range(0f, 1f)] float volume = 0.9f;
        [Tooltip("Rugido o gemido al empezar a perseguirte")]
        [SerializeField] AudioClip huntClip;
        [SerializeField] float huntHearRange = 44f;
        [SerializeField] float huntCooldown = 7f;

        [Header("Pasos")]
        [Tooltip("Variaciones de la pisada; suenan al ritmo real del paso (más seguidas al correr)")]
        [SerializeField] AudioClip[] steps;
        [SerializeField] float stepPitch = 1f;
        [SerializeField] float stepStride = 2.2f;          // unidades recorridas por pisada
        [SerializeField] float stepHearRange = 34f;        // por el recorrido real
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
                if (hd < huntHearRange)
                {
                    float hk = 1f - hd / huntHearRange;
                    source.panStereo = Mathf.Clamp((brain.Motor.Position.x - player.Position.x) / 18f, -0.8f, 0.8f);
                    source.PlayOneShot(huntClip, Mathf.Lerp(0.35f, 1f, hk) * volume * SaveSystem.SfxVolume);
                }
            }
            wasChasing = chasing;
            Steps();
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            bool moving = brain.Motor.IsMoving;
            timer = Random.Range(intervalSeconds.x, intervalSeconds.y) * (moving ? 1f : idleMultiplier);

            float d = PathDistance(player.Position, brain.Motor.Position);
            if (d >= hearRange) return;
            float k = 1f - d / hearRange;
            source.panStereo = Mathf.Clamp((brain.Motor.Position.x - player.Position.x) / 18f, -0.8f, 0.8f);
            source.PlayOneShot(clip, k * k * volume * (moving ? 1f : 0.6f) * SaveSystem.SfxVolume);
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
            if (d >= stepHearRange) return;
            float k = AlfredoBrain.DistanceVolume(d, stepHearRange);   // progresivo: suave lejos, fuerte cerca
            int i = Random.Range(0, steps.Length);
            if (steps.Length > 1 && i == lastStep) i = (i + 1) % steps.Length;
            lastStep = i;
            stepSource.pitch = stepPitch * Random.Range(0.94f, 1.06f) * (brain.IsChasing ? 1.06f : 1f);
            stepSource.panStereo = Mathf.Clamp((p.x - player.Position.x) / 18f, -0.85f, 0.85f);
            stepSource.PlayOneShot(steps[i], k * stepVolume * (brain.IsChasing ? 1.15f : 1f) * SaveSystem.SfxVolume);
        }

        /// <summary>Distancia a pie: misma sala = línea recta; sala vecina = hasta la puerta y desde su salida.</summary>
        public static float PathDistance(Vector2 from, Vector2 to)
        {
            var zones = MapZones.Instance;
            var a = zones.NearestZone(from);
            var b = zones.NearestZone(to);
            if (a == b) return Vector2.Distance(from, to);
            var w = ZoneRouter.NextWarp(a, b);
            if (w == null || w.ToZone != b) return float.PositiveInfinity;
            return Vector2.Distance(from, w.area.center) + Vector2.Distance(w.destination, to);
        }
    }
}
