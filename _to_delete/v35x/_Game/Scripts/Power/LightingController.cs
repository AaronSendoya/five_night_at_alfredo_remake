using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Alfredo
{
    /// <summary>
    /// Luz ambiental según el sistema eléctrico. En el original la oscuridad era 0.3 con luz,
    /// 0.6 en los parpadeos y 0.85 en el apagón; aquí se traduce a la intensidad de la luz global 2D.
    /// </summary>
    public class LightingController : MonoBehaviour
    {
        [SerializeField] Light2D globalLight;
        [SerializeField] float onIntensity = 0.75f;
        [SerializeField] float flickerLowIntensity = 0.25f;
        [Tooltip("Sin luz la sala casi no se ve: solo siluetas. La linterna es la única forma de ver bien")]
        [SerializeField] float offIntensity = 0.035f;
        [SerializeField] Color offColor = new Color(0.55f, 0.62f, 0.85f);   // tono frío de oscuridad
        [SerializeField] float blendSpeed = 4f;
        [Tooltip("Intensidad mientras se mira el monitor de cámaras (las salas son oscuras y no se distinguían)")]
        [SerializeField] float cameraIntensity = 1.25f;
        [SerializeField] Light2D[] poweredLights;

        PowerSystem power;
        float target;
        float flickerTimer;

        void Start()
        {
            power = PowerSystem.Instance;
            power.StateChanged += OnPowerChanged;
            target = onIntensity;
            if (globalLight != null) globalLight.intensity = target;
        }

        void OnDestroy()
        {
            if (power != null) power.StateChanged -= OnPowerChanged;
        }

        void OnPowerChanged(PowerState state)
        {
            if (state == PowerState.Off)
            {
                AudioManager.PlaySfx(Sfx.PowerOff);
                AudioManager.PlayLoop(Sfx.PowerOutHum);
                // Distingue el apagón por fusibles del interruptor general bajado a mano.
                HudMessages.Show(!power.SwitchOn ? "Bajaste el interruptor general: sin luz" : "¡Apagón! Repara los fusibles");
            }
            else
            {
                if (AudioManager.IsPlaying(Sfx.PowerOutHum)) AudioManager.PlaySfx(Sfx.PowerOn);
                AudioManager.StopLoop(Sfx.PowerOutHum);
            }
        }

        void Update()
        {
            if (power == null || globalLight == null) return;
            switch (power.State)
            {
                case PowerState.On: target = onIntensity; break;
                case PowerState.Off: target = offIntensity; break;
                case PowerState.Flickering:
                    flickerTimer -= Time.deltaTime;
                    if (flickerTimer <= 0f)
                    {
                        flickerTimer = Random.Range(0.05f, 0.35f);
                        target = Random.value < 0.5f ? flickerLowIntensity : onIntensity;
                    }
                    break;
            }
            float speed = power.State == PowerState.Flickering ? 30f : blendSpeed;
            float goal = target;
            if (CameraMonitor.AnyOpen && power.HasLight) { goal = cameraIntensity * (target / onIntensity); speed = Mathf.Max(speed, 12f); }
            globalLight.intensity = Mathf.MoveTowards(globalLight.intensity, goal, speed * Time.deltaTime);
            float dark = Mathf.InverseLerp(onIntensity, offIntensity, globalLight.intensity);
            globalLight.color = Color.Lerp(Color.white, offColor, dark);
            if (poweredLights != null)
                foreach (var l in poweredLights) if (l != null) l.enabled = power.HasLight;
        }
    }
}
