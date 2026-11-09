using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Escondite (obj_escondite) o ventilación (obj_ventilation). Mientras el jugador está dentro,
    /// los animatrónicos no lo ven, pero el aire se acaba: al agotarse sale a la fuerza y debe esperar.
    /// Escondite: 10 s de aire, 10 s de espera. Ventilación: hay que mantener 1 s para entrar,
    /// 4 s de aire, 9,3 s de espera, y se puede salir por cualquier otra ventilación.
    /// </summary>
    public class HidingSpot : Interactable
    {
        public static readonly List<HidingSpot> Vents = new List<HidingSpot>();

        [SerializeField] bool isVent;
        [SerializeField] string displayName = "Escondite";
        [SerializeField] float airSeconds = 10f;
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

        protected override void OnEnable() { base.OnEnable(); if (isVent) Vents.Add(this); }
        protected override void OnDisable() { base.OnDisable(); Vents.Remove(this); }

        public override bool CanInteract(PlayerController player) => Occupied || (cooldown <= 0f && !player.IsHidden);

        public override string GetPrompt(PlayerController player)
        {
            if (Occupied) return "Salir";
            if (cooldown > 0f) return $"Recuperando el aliento ({Mathf.CeilToInt(cooldown)} s)";
            return isVent ? "Entrar a la ventilación (mantén)" : "Esconderse";
        }

        public override void Interact(PlayerController player)
        {
            if (Occupied) Exit(false, savedPosition);
            else Enter(player);
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
