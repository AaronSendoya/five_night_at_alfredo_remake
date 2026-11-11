using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Alfredo (Freddy): el acechador. Lento y con visión corta (650 px, 90°), pero cada 20 a 30 s
    /// embiste durante 9 s con su risa: corre al doble y, si estás en su zona, va directo hacia ti.
    /// Corrige el original, donde tras embestir pasaba a un estado que no existía y se quedaba congelado.
    ///
    /// Modo Rastreo: desde la noche 2, si el jugador pasa 60 s (~1 hora del juego) seguidos en la misma sala, Alfredo sabe dónde está y va
    /// por él desde cualquier parte del mapa, tan rápido como Rufus, sin soltarlo. Solo lo pierde si el
    /// jugador se esconde (escondite, ventilación o máquina arcade) o si una puerta cerrada le corta
    /// el camino y además ya no lo ve. Evita que alguien gane quedándose quieto en un punto ciego.
    /// </summary>
    public class AlfredoBrain : AnimatronicBrain
    {
        [SerializeField] float stalkSpeedPx = 2.5f;
        [SerializeField] float rushSpeedPx = 4.6f;
        [SerializeField] float rushSeconds = 9f;
        [SerializeField] Vector2 firstRushRange = new Vector2(20f, 30f);
        [SerializeField] Vector2 rushCooldownRange = new Vector2(25f, 35f);
        [Tooltip("Segundos del jugador en la misma sala para que empiece el Rastreo")]
        [SerializeField] float trackAfterSeconds = 60f; // ~1 hora del reloj del juego
        [Tooltip("Primera noche en que funciona el Rastreo")]
        [SerializeField] int trackFromNight = 2;
        [Tooltip("Velocidad del Rastreo: la misma que la caza de Rufus")]
        [SerializeField] float trackSpeedPx = 4.8f;

        bool tracking;
        bool returningHome;
        float blockedTimer;

        int nightNumber;
        bool WantsToTrack => nightNumber >= trackFromNight && player != null && player.ZoneSeconds >= trackAfterSeconds && !player.IsUndetectable;
        // Desde la noche 2: aunque Alfredo aún no haya despertado en el escenario,
        // despierta para ir por quien se queda quieto; al terminar vuelve a su sitio.
        protected override bool ForceAwake => tracking || returningHome || WantsToTrack;
        bool rushing;
        float rushTimer;
        float cooldown;

        public bool IsRushing => rushing;
        public bool IsTracking => tracking;

        protected override void OnConfigured(NightConfig night)
        {
            nightNumber = night.night;
            rushing = false;
            tracking = false;
            returningHome = false;
            cooldown = Random.Range(firstRushRange.x, firstRushRange.y);
        }

        protected override void OnResetToHome() { StopTracking(false); returningHome = false; StopRush(); }

        protected override void Think(float dt)
        {
            if (tracking) { Track(dt); return; }
            if (WantsToTrack)
            {
                StartTracking();
                Track(dt);
                return;
            }

            if (rushing)
            {
                rushTimer -= dt;
                if (rushTimer <= 0f) { StopRush(); return; }
                float speed = Px(rushSpeedPx);
                bool sameZone = MapZones.Instance.NearestZone(player.Position) == MapZones.Instance.NearestZone(motor.Position);
                if (!player.IsUndetectable && sameZone && director.RequestChase(this))
                {
                    MarkChasing();
                    NavigateTo(player.Position, speed, dt);
                    return;
                }
                if (HuntPlayer(speed, dt)) return;
                Patrol(dt, speed);
                return;
            }

            if (returningHome)
            {
                // Dormido esta noche (o aún no despierta): vuelve al escenario tras el Rastreo.
                if (Arrived(home, 1.2f) || !NavigateTo(home, Px(stalkSpeedPx), dt, 0.5f)) { returningHome = false; motor.Stop(); }
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
            float stalk = Px(stalkSpeedPx);
            if (HuntPlayer(stalk, dt)) return;
            Patrol(dt, stalk);
        }

        void StartTracking()
        {
            if (rushing) { rushing = false; AudioManager.StopLoop(Sfx.AlfredoLaugh); }
            tracking = true;
            blockedTimer = 0f;
            MarkChasing();
            AudioManager.PlayLoop(Sfx.AlfredoLaugh);
            HudMessages.Show("¡Alfredo te está rastreando! Escóndete");
        }

        void Track(float dt)
        {
            // Se esconde (escondite, ventilación o arcade): Alfredo pierde el rastro.
            if (player.IsUndetectable) { StopTracking(true); return; }
            MarkChasing();
            if (NavigateTo(player.Position, Px(trackSpeedPx), dt, 0.2f)) { blockedTimer = 0f; return; }
            // Sin camino (puerta cerrada): si además ya no lo ve, se rinde.
            motor.Stop();
            if (senses.CanSeePlayer) { blockedTimer = 0f; return; }
            blockedTimer += dt;
            if (blockedTimer > 0.6f) StopTracking(true);
        }

        void StopTracking(bool lostPlayer)
        {
            if (!tracking) return;
            tracking = false;
            AudioManager.StopLoop(Sfx.AlfredoLaugh);
            if (player != null) player.ResetZoneTimer();
            EndHunt();
            cooldown = Mathf.Max(cooldown, rushCooldownRange.x);
            if (lostPlayer) HudMessages.Show("Alfredo perdió tu rastro");
            returningHome = !IsActive;
        }

        void StopRush()
        {
            if (rushing) AudioManager.StopLoop(Sfx.AlfredoLaugh);
            rushing = false;
            cooldown = Random.Range(rushCooldownRange.x, rushCooldownRange.y);
            EndHunt();
        }

        void OnDisable() => AudioManager.StopLoop(Sfx.AlfredoLaugh);
    }
}
