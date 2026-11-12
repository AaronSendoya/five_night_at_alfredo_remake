using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Rufus (Foxy): el cazador de la oscuridad. Con luz espera en su sitio del escenario.
    /// En un apagón sale a cazar: si te ve, te persigue (es el más rápido, pero solo un poco: ~13 % más que los demás) y te sigue entre zonas.
    /// Si la linterna lo alumbra directamente (cualquier parte de su cuerpo), huye a su sitio del
    /// escenario sin poder atraparte. Mientras lo sigas alumbrando allí se queda quieto; cuando deja
    /// de estar alumbrado espera un poco y, si sigue sin luz, vuelve a patrullar.
    /// </summary>
    public class RufusBrain : AnimatronicBrain
    {
        [SerializeField] float roamSpeedPx = 3.3f;
        [SerializeField] float huntSpeedPx = 5.5f;   // el más rápido (Alfredo rastreando: 5,3)
        [SerializeField] float retreatSpeedPx = 6.5f;
        [SerializeField] float huntMemorySeconds = 15f;
        [SerializeField] float stayHomeSeconds = 6f;
        [SerializeField] Flashlight flashlight;
        [Tooltip("Alto del sprite en unidades (para saber si el haz le toca pies, cuerpo o cabeza)")]
        [SerializeField] float bodyHeight = 7.2f;

        [Header("Canción en la oscuridad")]
        [Tooltip("Suena mientras patrulla en un apagón")]
        [SerializeField] AudioClip songClip;
        [SerializeField] float songEveryGameMinutes = 15f;
        [Tooltip("Velocidad extra al PATRULLAR mientras suena (no afecta a la caza)")]
        [SerializeField] float songSpeedMultiplier = 1.3f;
        [SerializeField] float songHearRange = 220f;     // ~2 salas; más lejos queda un hilo de volumen
        [SerializeField, Range(0f, 1f)] float songFarVolume = 0.1f;
        [SerializeField, Range(0f, 1f)] float songVolume = 1f;

        float huntTimer;
        float homeTimer;
        float songTimer;
        float songLeft;
        float secondsPerHour = 70f;
        AudioSource songSource;
        bool SongPlaying => songLeft > 0f;
        bool retreating;

        protected override bool CanCatch => !retreating;
        bool AtHome => Arrived(home, 1.5f);

        // Rufus solo caza a oscuras: con luz el ruido no lo saca de su sitio.
        protected override void OnHeardNoise(float seconds)
        {
            if (!PowerSystem.Instance.HasLight && !retreating) huntTimer = Mathf.Max(huntTimer, seconds);
        }

        protected override void OnConfigured(NightConfig night)
        {
            secondsPerHour = night.secondsPerHour > 0f ? night.secondsPerHour : 70f;
            songTimer = SongInterval * 0.5f;
            StopSong();
        }

        float SongInterval => songEveryGameMinutes / 60f * secondsPerHour;

        void StartSong()
        {
            if (songClip == null) return;
            if (songSource == null)
            {
                songSource = gameObject.AddComponent<AudioSource>();
                songSource.playOnAwake = false;
                songSource.spatialBlend = 0f;
            }
            songSource.clip = songClip;
            songSource.Play();
            UpdateSongVolumeNow();
            SoundFalloff.Log($"Canción de Rufus: vol {songSource.volume:0.00}", true);
            songLeft = songClip.length;
            songTimer = SongInterval;
        }

        void UpdateSongVolumeNow()
        {
            songLeft = songClip.length;
            UpdateSongVolume();
        }

        void StopSong()
        {
            songLeft = 0f;
            if (songSource != null) songSource.Stop();
        }

        /// <summary>Volumen por distancia real (también desde salas vecinas) y paneo hacia su lado.</summary>
        void UpdateSongVolume()
        {
            if (songSource == null || !SongPlaying || player == null) return;
            float d = AnimatronicVoice.PathDistance(player.Position, motor.Position);
            songSource.volume = SoundFalloff.Volume(d, songHearRange, songFarVolume) * songVolume * SaveSystem.SfxVolume;
            songSource.panStereo = SoundFalloff.Pan(motor.Position, player.Position);
        }

        protected override void OnResetToHome()
        {
            StopSong();
            huntTimer = 0f; homeTimer = 0f; retreating = false;
        }

        /// <summary>El haz toca los pies, el cuerpo o la cabeza.</summary>
        bool Lit()
        {
            if (flashlight == null || !flashlight.IsOn) return false;
            Vector2 p = motor.Position;
            return flashlight.Illuminates(p + Vector2.up * bodyHeight * 0.12f) || flashlight.Illuminates(p + Vector2.up * bodyHeight * 0.48f) || flashlight.Illuminates(p + Vector2.up * bodyHeight * 0.82f);
        }

        protected override void Think(float dt)
        {
            bool lit = Lit();
            if (SongPlaying) { songLeft -= dt; UpdateSongVolume(); if (songLeft <= 0f) StopSong(); }
            if (PowerSystem.Instance.HasLight && SongPlaying) StopSong();

            if (retreating)
            {
                if (AtHome)
                {
                    retreating = false;
                    homeTimer = stayHomeSeconds;
                    StandHome();
                }
                else NavigateTo(home, Px(retreatSpeedPx), dt, 1f);
                return;
            }

            if (PowerSystem.Instance.HasLight)
            {
                huntTimer = 0f;
                GoHome(Px(huntSpeedPx), dt);
                return;
            }

            if (lit && !AtHome)
            {
                retreating = true;
                huntTimer = 0f;
                EndHunt();
                HudMessages.Show("¡Rufus huye de la luz!");
                NavigateTo(home, Px(retreatSpeedPx), dt, 1f);
                return;
            }

            if (homeTimer > 0f)
            {
                if (lit) homeTimer = stayHomeSeconds; // alumbrado en su sitio: no se mueve
                homeTimer -= dt;
                StandHome();
                return;
            }

            float fast = PxChase(huntSpeedPx);
            if (senses.CanSeePlayer) huntTimer = huntMemorySeconds;
            if (huntTimer > 0f)
            {
                huntTimer -= dt;
                if (!player.IsUndetectable && (IsChasing || director.RequestChase(this)))
                {
                    MarkChasing();
                    NavigateTo(player.Position, fast, dt);
                    return;
                }
                if (HuntPlayer(fast, dt)) return;
            }
            else EndHunt();
            // Patrullando a oscuras: cada ~15 minutos del juego canta, y mientras suena patrulla más rápido.
            songTimer -= dt;
            if (!SongPlaying && songTimer <= 0f) StartSong();
            Patrol(dt, Px(roamSpeedPx) * (SongPlaying ? songSpeedMultiplier : 1f));
        }

        void OnDisable() => StopSong();

        void StandHome()
        {
            EndHunt();
            motor.Stop();
            motor.Face(Facing.Down);
        }

        void GoHome(float speed, float dt)
        {
            EndHunt();
            if (Arrived(home, 0.4f)) { StandHome(); return; }
            NavigateTo(home, speed, dt, 1f);
        }
    }
}
