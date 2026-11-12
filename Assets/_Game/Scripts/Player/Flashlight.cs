using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Alfredo
{
    /// <summary>
    /// Linterna con batería y manivela (obj_linterna): 45 s de batería, se recarga girando la manivela
    /// (unos 3 s para llenarla) y mientras se gira el jugador no puede moverse.
    /// </summary>
    public class Flashlight : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] Light2D beam;
        [SerializeField] Light2D halo;
        [Tooltip("Punto caliente en el centro del haz")]
        [SerializeField] Light2D core;
        [SerializeField] LayerMask sightBlockers;
        [SerializeField] float batterySeconds = 45f;
        [SerializeField] float fullChargeSeconds = 3f;
        [Tooltip("Recarga completa = 3 s x 5 = 15 s")]
        [SerializeField] float rechargeSlowdown = 5f;
        [Tooltip("Batería llena encendida = 45 / 2,25 = 20 s")]
        [SerializeField] float drainMultiplier = 2.25f;
        [SerializeField] float beamIntensity = 2.3f;     // más brillante: en el apagón la sala queda casi negra
        [SerializeField] float coreIntensity = 1.6f;
        [SerializeField] float lightHeight = 1.1f;
        [Tooltip("Tamaño de un píxel del sprite del jugador en unidades (escala / 32)")]
        [SerializeField] float spritePixel = 0.134f;

        float battery;
        float flicker;
        bool lowWarned;

        public bool HasFlashlight { get; private set; }
        public bool IsOn { get; private set; }
        public bool IsCranking { get; private set; }
        public float Battery01 => battery / batterySeconds;
        public float Range => beam != null ? beam.pointLightOuterRadius : 0f;

        public event Action BatteryEmpty;

        void Awake()
        {
            battery = batterySeconds;
            SetLights(0f);
        }

        public void Give()
        {
            HasFlashlight = true;
            battery = batterySeconds;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool playing = gm != null && gm.IsPlaying;
            if (!HasFlashlight || !playing)
            {
                if (IsCranking) { IsCranking = false; AudioManager.StopLoop(Sfx.CrankLoop); }
                TouchInput.ConsumeFlashlight();
                SetLights(HasFlashlight && IsOn ? CurrentIntensity() : 0f);
                return;
            }

            // El haz nace en la mano que sostiene la linterna (no en el centro del cuerpo).
            transform.position = player.Position + HandOffset(player.Facing) * spritePixel;
            transform.rotation = Quaternion.Euler(0f, 0f, player.Facing.ToAngle());

            bool crank = TouchInput.CrankHeld && !player.IsHidden;
            if (crank != IsCranking)
            {
                IsCranking = crank;
                // Se puede caminar mientras se gira la manivela (no correr: lo bloquea PlayerController).
                if (crank) { IsOn = false; AudioManager.PlayLoop(Sfx.CrankLoop); }
                else AudioManager.StopLoop(Sfx.CrankLoop);
            }
            if (IsCranking) battery = Mathf.Min(batterySeconds, battery + batterySeconds / (fullChargeSeconds * rechargeSlowdown) * Time.deltaTime);

            if (TouchInput.ConsumeFlashlight() && !IsCranking && battery > 0f && !player.IsHidden)
            {
                IsOn = !IsOn;
                AudioManager.PlaySfx(IsOn ? Sfx.FlashlightOn : Sfx.FlashlightOff);
            }
            if (player.IsHidden) IsOn = false;
            if (Battery01 > 0.3f) lowWarned = false;

            if (IsOn)
            {
                battery -= Time.deltaTime * drainMultiplier;
                if (!lowWarned && Battery01 <= 0.2f)
                {
                    lowWarned = true;
                    AudioManager.PlaySfx(Sfx.BatteryLow);
                }
                if (battery <= 0f)
                {
                    battery = 0f;
                    IsOn = false;
                    AudioManager.PlaySfx(Sfx.BatteryEmpty);
                    BatteryEmpty?.Invoke();
                }
            }
            SetLights(IsOn ? CurrentIntensity() : 0f);
        }

        /// <summary>Posición de la mano con la linterna en píxeles del sprite (32x52, origen en los pies).</summary>
        static Vector2 HandOffset(Facing f)
        {
            switch (f)
            {
                case Facing.Down: return new Vector2(5f, 12f);
                case Facing.Up: return new Vector2(-5f, 16f);
                case Facing.Right: return new Vector2(9f, 13.5f);
                default: return new Vector2(-9f, 13.5f);
            }
        }

        float CurrentIntensity()
        {
            float pct = Battery01;
            if (pct > 0.5f) return 1f;
            if (pct > 0.15f) return 0.6f;
            flicker -= Time.deltaTime;
            if (flicker <= 0f) flicker = UnityEngine.Random.Range(0.05f, 0.2f);
            return UnityEngine.Random.value < 0.15f ? UnityEngine.Random.Range(0.1f, 0.3f) : 0.4f;
        }

        void SetLights(float level)
        {
            // Variación mínima constante (bombilla incandescente), no se nota como parpadeo.
            if (level > 0f) level *= 1f + (Mathf.PerlinNoise(Time.time * 7f, 0.3f) - 0.5f) * 0.06f;
            // Con la luz de la sala encendida el haz no satura la imagen; en la oscuridad brilla al máximo.
            if (level > 0f && PowerSystem.Instance != null && PowerSystem.Instance.HasLight) level *= 0.45f;
            if (beam != null) { beam.enabled = level > 0f; beam.intensity = level * beamIntensity; }
            if (core != null) { core.enabled = level > 0f; core.intensity = level * coreIntensity; }
            if (halo != null) { halo.enabled = level > 0f; halo.intensity = level * 0.45f; }
        }

        /// <summary>True si el haz encendido alcanza ese punto (cono, distancia y sin pared en medio).</summary>
        public bool Illuminates(Vector2 point)
        {
            if (!HasFlashlight || !IsOn || beam == null) return false;
            Vector2 origin = transform.position;
            Vector2 to = point - origin;
            if (to.magnitude > beam.pointLightOuterRadius) return false;
            if (Vector2.Angle(transform.up, to) > beam.pointLightOuterAngle * 0.5f) return false;
            return !Physics2D.Linecast(origin, point, sightBlockers);
        }
    }
}
