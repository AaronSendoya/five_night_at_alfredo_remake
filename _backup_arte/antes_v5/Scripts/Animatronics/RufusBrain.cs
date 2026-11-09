using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Rufus (Foxy): el cazador de la oscuridad. Con luz espera en su sitio del escenario.
    /// En un apagón sale a cazar: si te ve, te persigue muy rápido y te sigue entre zonas.
    /// Si la linterna lo alumbra directamente (cualquier parte de su cuerpo), huye a su sitio del
    /// escenario sin poder atraparte. Mientras lo sigas alumbrando allí se queda quieto; cuando deja
    /// de estar alumbrado espera un poco y, si sigue sin luz, vuelve a patrullar.
    /// </summary>
    public class RufusBrain : AnimatronicBrain
    {
        [SerializeField] float roamSpeedPx = 3.5f;
        [SerializeField] float huntSpeedPx = 6f;
        [SerializeField] float retreatSpeedPx = 7f;
        [SerializeField] float huntMemorySeconds = 15f;
        [SerializeField] float stayHomeSeconds = 6f;
        [SerializeField] Flashlight flashlight;

        float huntTimer;
        float homeTimer;
        bool retreating;

        protected override bool CanCatch => !retreating;
        bool AtHome => Arrived(home, 1.5f);

        protected override void OnResetToHome()
        {
            huntTimer = 0f; homeTimer = 0f; retreating = false;
        }

        /// <summary>El haz toca los pies, el cuerpo o la cabeza.</summary>
        bool Lit()
        {
            if (flashlight == null || !flashlight.IsOn) return false;
            Vector2 p = motor.Position;
            return flashlight.Illuminates(p + Vector2.up * 0.6f) || flashlight.Illuminates(p + Vector2.up * 2.6f) || flashlight.Illuminates(p + Vector2.up * 4.4f);
        }

        protected override void Think(float dt)
        {
            bool lit = Lit();

            if (retreating)
            {
                if (AtHome)
                {
                    retreating = false;
                    homeTimer = stayHomeSeconds;
                    StandHome();
                }
                else NavigateTo(home, GameUnits.Speed(retreatSpeedPx), dt, 1f);
                return;
            }

            if (PowerSystem.Instance.HasLight)
            {
                huntTimer = 0f;
                GoHome(GameUnits.Speed(huntSpeedPx), dt);
                return;
            }

            if (lit && !AtHome)
            {
                retreating = true;
                huntTimer = 0f;
                EndChase();
                HudMessages.Show("¡Rufus huye de la luz!");
                NavigateTo(home, GameUnits.Speed(retreatSpeedPx), dt, 1f);
                return;
            }

            if (homeTimer > 0f)
            {
                if (lit) homeTimer = stayHomeSeconds; // alumbrado en su sitio: no se mueve
                homeTimer -= dt;
                StandHome();
                return;
            }

            float fast = GameUnits.Speed(huntSpeedPx);
            if (senses.CanSeePlayer) huntTimer = huntMemorySeconds;
            if (huntTimer > 0f)
            {
                huntTimer -= dt;
                if (!player.IsHidden && (IsChasing || director.RequestChase(this)))
                {
                    IsChasing = true;
                    NavigateTo(player.Position, fast, dt);
                    return;
                }
                if (HuntPlayer(fast, dt)) return;
            }
            else EndChase();
            Patrol(dt, GameUnits.Speed(roamSpeedPx));
        }

        void StandHome()
        {
            EndChase();
            motor.Stop();
            motor.Face(Facing.Down);
        }

        void GoHome(float speed, float dt)
        {
            EndChase();
            if (Arrived(home, 0.4f)) { StandHome(); return; }
            NavigateTo(home, speed, dt, 1f);
        }
    }
}
