using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Ruido del jugador. Solo sube al CORRER y solo después de 2,5 s seguidos corriendo; al dejar de correr baja.
    /// Al llegar al máximo (rojo), los animatrónicos despiertos de la sala del jugador y de las salas vecinas
    /// (conectadas por una puerta) lo oyen y van a cazarlo. Se les pierde con lo de siempre: escondite,
    /// ventilación o la puerta de la oficina.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class NoiseMeter : MonoBehaviour
    {
        [SerializeField] float graceRunSeconds = 2f;     // los primeros 2 s corriendo no hacen ruido
        [SerializeField] float riseSeconds = 1.2f;       // de verde a rojo: en rojo a los ~3,2 s corriendo
        [SerializeField] float fallPerSecond = 0.3f;     // baja al caminar o quedarse quieto
        [SerializeField] float alertCooldown = 2.5f;       // si sigue en rojo, vuelve a avisar cada tanto
        [SerializeField] float huntSeconds = 12f;         // cuánto lo buscan tras oírlo

        PlayerController player;
        AnimatronicDirector director;
        float runTime, alertTimer;
        bool tipShown;

        /// <summary>0 = silencio (verde) ... 1 = demasiado ruido (rojo).</summary>
        public float Noise01 { get; private set; }
        public bool IsLoud => Noise01 >= 0.999f;

        void Awake() => player = GetComponent<PlayerController>();
        void Start() => director = FindFirstObjectByType<AnimatronicDirector>();

        /// <summary>Solo pruebas (F10): ruido al máximo.</summary>
        public void DebugMaxNoise()
        {
            Noise01 = 1f; alertTimer = alertCooldown;
            int heard = director != null ? director.NoiseAlert(player, huntSeconds) : 0;
            HudMessages.Show(heard > 0 ? "¡Te oyeron! Vienen hacia ti" : "Demasiado ruido...");
            SoundFalloff.Log($"Ruido al máximo (F10): lo oyeron {heard}", true);
        }

        public void ResetNoise() { Noise01 = 0f; runTime = 0f; alertTimer = 0f; }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) return;
            float dt = Time.deltaTime;

            bool running = player.IsRunning && player.IsMoving && !player.IsUndetectable;
            if (running)
            {
                runTime += dt;
                if (runTime > graceRunSeconds) Noise01 = Mathf.Min(1f, Noise01 + dt / riseSeconds);
            }
            else
            {
                runTime = 0f;
                Noise01 = Mathf.Max(0f, Noise01 - dt * fallPerSecond * (player.IsUndetectable ? 2f : 1f));
            }

            if (!tipShown && Noise01 > 0.05f)
            {
                tipShown = true;
                HudTips.Show("Si corres más de 2 segundos, haces ruido y te oyen.", true);
            }

            alertTimer -= dt;
            if (IsLoud && alertTimer <= 0f && director != null)
            {
                alertTimer = alertCooldown;
                int heard = director.NoiseAlert(player, huntSeconds);
                HudMessages.Show(heard > 0 ? "¡Te oyeron! Vienen hacia ti" : "Demasiado ruido...");
                SoundFalloff.Log($"Ruido al máximo: lo oyeron {heard}", true);
            }
        }
    }
}
