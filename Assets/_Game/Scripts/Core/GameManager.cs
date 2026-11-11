using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alfredo
{
    public enum GameState { Tutorial, Intro, Playing, Caught, Victory, GameOver }

    /// <summary>
    /// Estado de la partida. Corrige el sistema de vidas del original (no se mostraban, reiniciaban la
    /// noche y al llegar a 0 volvían a 3): ahora al perder una vida se reaparece en la oficina con el
    /// reloj intacto y al llegar a 0 aparece la pantalla de derrota.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] NightConfig[] nights;
        [SerializeField] NightClock clock;
        [SerializeField] PlayerController player;
        [SerializeField] AnimatronicDirector director;
        [SerializeField] PowerSystem power;
        [SerializeField] FuseSpawner fuseSpawner;
        [SerializeField] ScreenPanels screens;
        [SerializeField] ZoneCamera zoneCamera;
        [SerializeField] Transform playerSpawn;

        [SerializeField] int startingLives = 3;
        [SerializeField] float introSeconds = 4.5f;
        [SerializeField] float jumpscareSeconds = 2.2f;
        [Tooltip("Segundos tras el grito en que el animatrónico dice su frase")]
        [SerializeField] float lineDelay = 0.9f;
        [SerializeField] float respawnGraceSeconds = 6f;

        public GameState State { get; private set; } = GameState.Intro;
        public NightConfig Night { get; private set; }
        public NightClock Clock => clock;
        public int Lives { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsPlaying => State == GameState.Playing && !IsPaused;
        /// <summary>Quién atrapó al jugador la última vez (para el consejo de la pantalla de derrota).</summary>
        public AnimatronicId LastKiller { get; private set; }
        /// <summary>Consejo para la última muerte (depende de quién y cómo te atrapó).</summary>
        public string LastDeathTip { get; private set; } = "";

        public event Action<GameState> StateChanged;
        public event Action<bool> PauseChanged;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            TouchInput.ResetAll();
            int n = Mathf.Clamp(GameSession.SelectedNight, 1, nights.Length);
            Night = nights[n - 1];
            Lives = startingLives;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            SaveSystem.MarkNightStarted(Night.night);
            HudTips.ResetGap();
            clock.Setup(Night);
            power.Setup(Night);
            director.Setup(Night, player);
            fuseSpawner.Setup(Night, player);
            clock.SixAM += OnSixAM;
            NavGrid.Instance.IsWalkable(Vector2.zero); // construye la cuadrícula de rutas al cargar, no en mitad de la partida
            player.Teleport(playerSpawn.position, Facing.Down);
            zoneCamera.Snap();

            if (Night.night == 1 && !SaveSystem.TutorialSeen) SetState(GameState.Tutorial);
            else StartCoroutine(IntroRoutine());
        }

        public void FinishTutorial()
        {
            if (State != GameState.Tutorial) return;
            SaveSystem.TutorialSeen = true;
            StartCoroutine(IntroRoutine());
        }

        IEnumerator IntroRoutine()
        {
            SetState(GameState.Intro);
            AudioManager.PlaySfx(Sfx.NightStart);
            yield return new WaitForSeconds(introSeconds);
            SetState(GameState.Playing);
            AudioManager.SetAmbience(true);
        }

        void SetState(GameState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }

        void OnSixAM()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Victory);
            SaveSystem.CompleteNight(Night.night);
            AudioManager.StopAllLoops();
            AudioManager.SetAmbience(false);
            AudioManager.PlaySfx(Sfx.SixAM);
        }

        public void PlayerCaught(AnimatronicBrain who)
        {
            if (State != GameState.Playing) return;
            StartCoroutine(CaughtRoutine(who));
        }

        IEnumerator CaughtRoutine(AnimatronicBrain who)
        {
            LastKiller = who.Id;
            LastDeathTip = DeathTips.For(who);
            SetState(GameState.Caught);
            AudioManager.StopAllLoops();
            AudioManager.SetAmbience(false);
            // Solo habla el que te atrapó: su grito y, encima, una de sus frases.
            if (who.Scream != null) AudioManager.PlayClip(who.Scream, 1f);
            else AudioManager.PlaySfx(Sfx.Jumpscare);
            var line = who.NextDeathLine();
            // La frase entra cuando termina el grito (como mucho a los 3 s, para gritos largos).
            float delay = who.Scream != null ? Mathf.Clamp(who.Scream.length, lineDelay, 3f) : lineDelay;
            float total = line != null ? Mathf.Max(jumpscareSeconds, delay + line.length + 0.2f) : Mathf.Max(jumpscareSeconds, who.Scream != null ? Mathf.Min(who.Scream.length, 3.5f) : 0f);
            screens.ShowJumpscare(who.JumpscareSprite, total);
            if (line != null)
            {
                yield return new WaitForSeconds(delay);
                AudioManager.PlayClip(line, 1f);
                yield return new WaitForSeconds(total - delay);
            }
            else yield return new WaitForSeconds(total);

            Lives--;
            if (Lives <= 0)
            {
                SetState(GameState.GameOver);
                yield break;
            }

            var fader = ScreenFader.Instance;
            if (fader != null) yield return fader.FadeOut(0.3f);
            player.ResetAfterCatch();
            player.Teleport(playerSpawn.position, Facing.Down);
            director.ResetAll(respawnGraceSeconds);
            zoneCamera.ClearOverride();
            HudMessages.Show(Lives == 1 ? "Te queda 1 vida" : $"Te quedan {Lives} vidas");
            HudTips.Show(LastDeathTip, true);
            if (fader != null) yield return fader.FadeIn(0.5f);
            SetState(GameState.Playing);
            AudioManager.SetAmbience(true);
        }

        public void SetPaused(bool paused)
        {
            if (State != GameState.Playing && paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            PauseChanged?.Invoke(paused);
        }

        public void Pause() => SetPaused(true);
        public void Resume() => SetPaused(false);

        void OnApplicationPause(bool paused)
        {
            if (paused) PlayerPrefs.Save();
            if (paused && State == GameState.Playing) SetPaused(true);
        }

        public void RetryNight() => LoadNight(Night.night);
        public void NextNight() => LoadNight(Mathf.Min(Night.night + 1, nights.Length));

        void LoadNight(int night)
        {
            GameSession.SelectedNight = night;
            LoadScene("Game");
        }

        public void GoToMenu() => LoadScene("Menu");

        void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            AudioManager.StopAllLoops();
            SceneManager.LoadScene(sceneName);
        }
    }
}
