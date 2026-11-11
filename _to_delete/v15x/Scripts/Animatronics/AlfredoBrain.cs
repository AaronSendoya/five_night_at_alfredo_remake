using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Alfredo (Freddy): el acechador. Lento y con visión corta (650 px, 90°), pero cada 20 a 30 s
    /// embiste durante 9 s con su risa: corre al doble y, si estás en su zona, va directo hacia ti.
    /// Corrige el original, donde tras embestir pasaba a un estado que no existía y se quedaba congelado.
    ///
    /// Modo Rastreo (desde la noche 2): es una habilidad con enfriamiento que se activa SIEMPRE, se mueva
    /// o no el jugador: noche 2 cada hora del juego, noche 3 cada 50 minutos, noche 4 cada 40 minutos.
    /// Al activarse, Alfredo despierta (aunque no le toque esa noche) y va a por el jugador esté donde esté,
    /// un poco más rápido que en su caza normal. Solo lo pierde si el jugador se esconde o usa una
    /// ventilación (cambia de sala sin que lo vea) o si una puerta cerrada de la oficina le corta el paso.
    /// Al perderlo, el enfriamiento vuelve a empezar.
    /// </summary>
    public class AlfredoBrain : AnimatronicBrain
    {
        [SerializeField] float stalkSpeedPx = 2.5f;
        [SerializeField] float rushSpeedPx = 4.6f;
        [SerializeField] float rushSeconds = 9f;
        [SerializeField] Vector2 firstRushRange = new Vector2(20f, 30f);
        [SerializeField] Vector2 rushCooldownRange = new Vector2(25f, 35f);
        [Tooltip("Minutos del juego entre Rastreos en las noches 2, 3 y 4")]
        [SerializeField] float[] trackEveryGameMinutes = { 60f, 50f, 40f };
        [Tooltip("Primera noche en que funciona el Rastreo")]
        [SerializeField] int trackFromNight = 2;
        [Tooltip("Velocidad del Rastreo: la misma que la caza de Rufus")]
        [SerializeField] float trackSpeedPx = 5.3f;   // más rápido que su caza normal (Rufus sigue siendo el más rápido)

        bool tracking;
        bool returningHome;
        float blockedTimer;

        int nightNumber;
        float secondsPerHour = 70f;
        float trackTimer = float.PositiveInfinity;
        float TrackInterval
        {
            get
            {
                int i = Mathf.Clamp(nightNumber - trackFromNight, 0, trackEveryGameMinutes.Length - 1);
                return trackEveryGameMinutes[i] / 60f * secondsPerHour;
            }
        }
        bool WantsToTrack => nightNumber >= trackFromNight && player != null && trackTimer <= 0f && !player.IsUndetectable;
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
            secondsPerHour = night.secondsPerHour > 0f ? night.secondsPerHour : 70f;
            trackTimer = nightNumber >= trackFromNight ? TrackInterval : float.PositiveInfinity;
            rushing = false;
            tracking = false;
            returningHome = false;
            cooldown = Random.Range(firstRushRange.x, firstRushRange.y);
        }

        protected override void OnResetToHome()
        {
            StopTracking(false); returningHome = false; StopRush();
            if (nightNumber >= trackFromNight) trackTimer = TrackInterval;
        }

        // El enfriamiento corre siempre (aunque Alfredo esté dormido en el escenario).
        protected override void AlwaysTick(float dt)
        {
            if (!tracking && nightNumber >= trackFromNight && trackTimer > 0f && trackTimer < float.PositiveInfinity) trackTimer -= dt;
        }

        // Ruido: si ya está despierto, embiste.
        protected override void OnHeardNoise(float seconds)
        {
            if (tracking || rushing || returningHome) return;
            rushing = true;
            rushTimer = Mathf.Max(seconds, rushSeconds * 0.6f);
            AudioManager.PlayLoop(Sfx.AlfredoLaugh);
        }

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
                float speed = PxChase(rushSpeedPx);
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
            if (NavigateTo(player.Position, PxChase(trackSpeedPx), dt, 0.2f)) { blockedTimer = 0f; return; }
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
            trackTimer = TrackInterval;   // vuelve a empezar el enfriamiento
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
