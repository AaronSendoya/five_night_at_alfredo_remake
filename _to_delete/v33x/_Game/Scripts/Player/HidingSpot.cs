using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Escondite (obj_escondite) o ventilación (obj_ventilation). Mientras el jugador está dentro,
    /// los animatrónicos no lo ven, pero el aire se acaba: al agotarse sale a la fuerza y debe esperar.
    /// Escondite: 11 s de aire, 10 s de espera. Ventilación: hay que mantener 1 s para entrar,
    /// 5 s de aire, 9,3 s de espera, y se puede salir por cualquier otra ventilación.
    /// </summary>
    public class HidingSpot : Interactable
    {
        public static readonly List<HidingSpot> Vents = new List<HidingSpot>();

        [SerializeField] bool isVent;
        [SerializeField] string displayName = "Escondite";
        [SerializeField] float airSeconds = 11f;
        [SerializeField] float cooldownSeconds = 10f;
        [SerializeField] float enterHoldSeconds;
        [SerializeField] Vector2 exitOffset;

        PlayerController occupant;
        Vector2 savedPosition;
        float air;
        float cooldown;
        int lastTick = int.MaxValue;

        public bool IsVent => isVent;
        public string DisplayName => displayName;
        public bool Occupied => occupant != null;
        public float Air01 => airSeconds > 0f ? air / airSeconds : 0f;
        public Vector2 ExitPoint => (Vector2)transform.position + exitOffset;

        public override bool RequiresHold => !Occupied && enterHoldSeconds > 0f;
        public override InteractIcon Icon => Occupied ? InteractIcon.Exit : isVent ? InteractIcon.Vent : InteractIcon.Hide;
        public override float HoldSeconds => enterHoldSeconds;

        public static event System.Action<HidingSpot> VentEntered;
        public static event System.Action VentExited;
        public static event System.Action<string> Message;

        // ---- Anti-abuso: entrar y salir sin parar de escondites/ventilaciones para pasar la noche sin jugar ----
        const int MaxEntries = 3;          // entradas permitidas seguidas...
        const float EntryWindow = 45f;     // ...dentro de esta ventana de tiempo
        const float BanSeconds = 10f;      // bloqueo de todos los escondites y ventilaciones
        static readonly List<float> recentEntries = new List<float>();
        static float bannedUntil = -1f;
        public static bool Banned => Time.time < bannedUntil;
        public static float BanLeft => Mathf.Max(0f, bannedUntil - Time.time);

        /// <summary>Registra un intento de entrar. Devuelve false (y bloquea 10 s) si ya se abusó de los escondites.</summary>
        static bool RegisterEntry()
        {
            float now = Time.time;
            recentEntries.RemoveAll(t => now - t > EntryWindow);
            if (recentEntries.Count >= MaxEntries)
            {
                recentEntries.Clear();
                bannedUntil = now + BanSeconds;
                AudioManager.PlaySfx(Sfx.Denied);
                HudMessages.Show($"Escondites bloqueados {BanSeconds:0} s");
                HudTips.Show("Te escondiste muchas veces seguidas. Espera 10 segundos.", true);
                return false;
            }
            recentEntries.Add(now);
            return true;
        }

        /// <summary>Nueva noche o captura: se olvida el historial.</summary>
        public static void ResetAbuse() { recentEntries.Clear(); bannedUntil = -1f; }

        protected override void OnEnable() { base.OnEnable(); if (isVent) Vents.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Vents.Remove(this); }

        public override bool CanInteract(PlayerController player) => Occupied || (cooldown <= 0f && !player.IsHidden && !Banned);

        public override string GetPrompt(PlayerController player)
        {
            if (Occupied) return "Salir";
            if (Banned) return $"Escondites bloqueados ({Mathf.CeilToInt(BanLeft)} s)";
            if (cooldown > 0f) return $"Recuperando el aliento ({Mathf.CeilToInt(cooldown)} s)";
            return isVent ? "Entrar a la ventilación (mantén)" : "Esconderse";
        }

        public override void Interact(PlayerController player)
        {
            if (Occupied) Exit(false, savedPosition);
            else if (!Banned && RegisterEntry()) Enter(player);
        }

        void Enter(PlayerController player)
        {
            occupant = player;
            savedPosition = player.Position;
            air = airSeconds;
            lastTick = int.MaxValue;
            AudioManager.PlaySfx(isVent ? Sfx.VentIn : Sfx.HideIn);
            player.Hide(this);
            if (isVent) VentEntered?.Invoke(this);
        }

        void Exit(bool forced, Vector2 at)
        {
            if (occupant == null) return;
            var p = occupant;
            occupant = null;
            cooldown = forced ? cooldownSeconds : 0f;
            AudioManager.PlaySfx(isVent ? Sfx.VentOut : Sfx.HideOut);
            if (forced) AudioManager.PlaySfx(Sfx.Gasp);
            p.Unhide(at);
            if (isVent) VentExited?.Invoke();
            if (forced) Message?.Invoke("Te quedaste sin aire");
        }

        /// <summary>Salir por otra ventilación de la red.</summary>
        public void ExitThrough(HidingSpot otherVent)
        {
            if (!Occupied || otherVent == null) return;
            Exit(false, otherVent == this ? savedPosition : otherVent.ExitPoint);
        }

        /// <summary>Vacía el escondite sin mover al jugador (tras una captura).</summary>
        public void ForceRelease()
        {
            occupant = null;
            cooldown = 0f;
            if (isVent) VentExited?.Invoke();
        }

        void Update()
        {
            if (cooldown > 0f) cooldown -= Time.deltaTime;
            if (!Occupied) return;
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) return;
            air -= Time.deltaTime;
            // Tictac en los últimos 3 s para avisar de que el aire se acaba.
            int sec = Mathf.CeilToInt(air);
            if (sec <= 3 && sec > 0 && sec < lastTick)
            {
                lastTick = sec;
                AudioManager.PlaySfx(Sfx.AirTick);
            }
            if (air <= 0f) Exit(true, savedPosition);
        }
    }
}
