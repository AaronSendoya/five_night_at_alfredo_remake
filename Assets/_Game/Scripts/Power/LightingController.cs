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
        [SerializeField] float flickerLowIntensity = 0.4f;
        [SerializeField] float offIntensity = 0.13f;
        [SerializeField] float blendSpeed = 4f;
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
                HudMessages.Show("¡Apagón! Repara los fusibles");
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
            globalLight.intensity = Mathf.MoveTowards(globalLight.intensity, target, speed * Time.deltaTime);
            if (poweredLights != null)
                foreach (var l in poweredLights) if (l != null) l.enabled = power.HasLight;
        }
    }
}
