using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Alfredo (Freddy): el acechador. Lento y con visión corta (650 px, 90°), pero cada 20 a 30 s
    /// embiste durante 9 s con su risa: corre al doble y, si estás en su zona, va directo hacia ti.
    /// Corrige el original, donde tras embestir pasaba a un estado que no existía y se quedaba congelado.
    /// </summary>
    public class AlfredoBrain : AnimatronicBrain
    {
        [SerializeField] float stalkSpeedPx = 2.5f;
        [SerializeField] float rushSpeedPx = 5f;
        [SerializeField] float rushSeconds = 9f;
        [SerializeField] Vector2 firstRushRange = new Vector2(20f, 30f);
        [SerializeField] Vector2 rushCooldownRange = new Vector2(25f, 35f);

        bool rushing;
        float rushTimer;
        float cooldown;

        public bool IsRushing => rushing;

        protected override void OnConfigured(NightConfig night)
        {
            rushing = false;
            cooldown = Random.Range(firstRushRange.x, firstRushRange.y);
        }

        protected override void OnResetToHome() => StopRush();

        protected override void Think(float dt)
        {
            if (rushing)
            {
                rushTimer -= dt;
                if (rushTimer <= 0f) { StopRush(); return; }
                float speed = GameUnits.Speed(rushSpeedPx);
                bool sameZone = MapZones.Instance.NearestZone(player.Position) == MapZones.Instance.NearestZone(motor.Position);
                if (!player.IsHidden && sameZone && director.RequestChase(this))
                {
                    IsChasing = true;
                    NavigateTo(player.Position, speed, dt);
                    return;
                }
                if (HuntPlayer(speed, dt)) return;
                Patrol(dt, speed);
                return;
            }

            cooldown -= dt;
            if (cooldown <= 0f)
            {
                rushing = true;
                rushTimer = rushSeconds;
                AudioManager.PlayLoop(Sfx.AlfredoLaugh);
                return;
            }
            float stalk = GameUnits.Speed(stalkSpeedPx);
            if (HuntPlayer(stalk, dt)) return;
            Patrol(dt, stalk);
        }

        void StopRush()
        {
            if (rushing) AudioManager.StopLoop(Sfx.AlfredoLaugh);
            rushing = false;
            cooldown = Random.Range(rushCooldownRange.x, rushCooldownRange.y);
            EndChase();
        }

        void OnDisable() => AudioManager.StopLoop(Sfx.AlfredoLaugh);
    }
}
