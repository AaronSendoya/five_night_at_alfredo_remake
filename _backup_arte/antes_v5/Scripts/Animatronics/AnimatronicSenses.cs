using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Visión de un animatrónico: distancia, cono hacia donde mira y línea sin paredes.
    /// Corrige el original, que usaba 150° a cada lado (casi 300°) y dejaba sin efecto acercarse por detrás.
    /// </summary>
    public class AnimatronicSenses : MonoBehaviour
    {
        [SerializeField] AnimatronicMotor motor;
        [SerializeField] LayerMask sightBlockers;
        [Tooltip("Píxeles del original")] public float viewDistancePx = 1100f;
        [Tooltip("Mitad del cono de visión, en grados")] public float halfAngle = 70f;
        [SerializeField] float eyeHeight = 0.5f;
        [SerializeField] float proximitySense = 2.5f;

        public bool CanSeePlayer { get; private set; }
        public Vector2 LastSeenPosition { get; private set; }
        public float TimeSinceSeen { get; private set; } = float.PositiveInfinity;

        public void Tick(PlayerController player, float dt)
        {
            CanSeePlayer = Check(player);
            if (CanSeePlayer)
            {
                LastSeenPosition = player.Position;
                TimeSinceSeen = 0f;
            }
            else TimeSinceSeen += dt;
        }

        public void Forget()
        {
            CanSeePlayer = false;
            TimeSinceSeen = float.PositiveInfinity;
        }

        bool Check(PlayerController player)
        {
            if (player == null || player.IsHidden) return false;
            var zones = MapZones.Instance;
            if (zones.NearestZone(motor.Position) != zones.NearestZone(player.Position)) return false;
            Vector2 to = player.Position - motor.Position;
            float dist = to.magnitude;
            if (dist > GameUnits.Dist(viewDistancePx)) return false;
            if (dist > proximitySense && Vector2.Angle(motor.FacingDir, to) > halfAngle) return false;
            Vector2 eye = Vector2.up * eyeHeight;
            return !Physics2D.Linecast(motor.Position + eye, player.Position + eye, sightBlockers);
        }
    }
}
