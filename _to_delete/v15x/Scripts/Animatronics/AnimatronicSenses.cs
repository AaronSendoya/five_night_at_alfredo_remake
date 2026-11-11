using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Sentidos de un animatrónico. Vista: distancia, cono hacia donde mira y línea sin paredes.
    /// Oído: los pasos del jugador (correr se oye lejos, caminar solo de cerca).
    /// Mientras está alerta (persiguiendo o buscando) ve en todas direcciones y un poco más lejos:
    /// así no "olvida" al jugador solo porque el camino lo obligó a girar.
    /// </summary>
    public class AnimatronicSenses : MonoBehaviour
    {
        [SerializeField] AnimatronicMotor motor;
        [SerializeField] LayerMask sightBlockers;
        [Tooltip("Píxeles del original")] public float viewDistancePx = 1100f;
        [Tooltip("Mitad del cono de visión, en grados")] public float halfAngle = 70f;
        [SerializeField] float eyeHeight = 0.5f;
        [SerializeField] float proximitySense = 3.5f;
        [SerializeField] float alertRangeMultiplier = 1.35f;
        [SerializeField] float hearRunning = 13f;
        [SerializeField] float hearWalking = 5f;

        /// <summary>Lo activa el cerebro mientras persigue o busca.</summary>
        public bool Alerted { get; set; }
        /// <summary>Multiplicador de alcance de la vista según la noche (lo fija el cerebro).</summary>
        public float NightRange { get; set; } = 1f;
        public bool CanSeePlayer { get; private set; }
        public bool HeardPlayer { get; private set; }
        public Vector2 LastSeenPosition { get; private set; }
        public float TimeSinceSeen { get; private set; } = float.PositiveInfinity;
        /// <summary>Última posición conocida por vista u oído.</summary>
        public Vector2 LastKnownPosition { get; private set; }

        public void Tick(PlayerController player, float dt)
        {
            CanSeePlayer = Check(player);
            HeardPlayer = !CanSeePlayer && Hear(player);
            if (CanSeePlayer)
            {
                LastSeenPosition = player.Position;
                LastKnownPosition = player.Position;
                TimeSinceSeen = 0f;
            }
            else
            {
                TimeSinceSeen += dt;
                if (HeardPlayer) LastKnownPosition = player.Position;
            }
        }

        public void Forget()
        {
            CanSeePlayer = false;
            HeardPlayer = false;
            Alerted = false;
            TimeSinceSeen = float.PositiveInfinity;
        }

        bool SameZone(PlayerController player)
        {
            var zones = MapZones.Instance;
            return zones.NearestZone(motor.Position) == zones.NearestZone(player.Position);
        }

        bool Check(PlayerController player)
        {
            if (player == null || player.IsUndetectable || !SameZone(player)) return false;
            Vector2 to = player.Position - motor.Position;
            float dist = to.magnitude;
            float range = GameUnits.Dist(viewDistancePx) * NightRange * (Alerted ? alertRangeMultiplier : 1f);
            if (dist > range) return false;
            if (!Alerted && dist > proximitySense && Vector2.Angle(motor.FacingDir, to) > halfAngle) return false;
            Vector2 eye = Vector2.up * eyeHeight;
            return !Physics2D.Linecast(motor.Position + eye, player.Position + eye, sightBlockers);
        }

        bool Hear(PlayerController player)
        {
            if (player == null || player.IsUndetectable || !player.IsMoving || !SameZone(player)) return false;
            float range = player.IsRunning ? hearRunning : hearWalking;
            return Vector2.Distance(player.Position, motor.Position) <= range;
        }
    }
}
