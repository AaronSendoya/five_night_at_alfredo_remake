using UnityEngine;

namespace Alfredo
{
    /// <summary>Progreso y opciones guardados en el dispositivo (funciona sin conexión).</summary>
    public static class SaveSystem
    {
        public const int MaxNight = 4;

        const string KeyUnlocked = "alfredo.unlocked_night";
        const string KeyCompleted = "alfredo.all_completed";
        const string KeyTutorial = "alfredo.tutorial_seen";
        const string KeyMusic = "alfredo.vol_music";
        const string KeyAmbience = "alfredo.vol_ambience";
        const string KeySfx = "alfredo.vol_sfx";

        public static int UnlockedNight => Mathf.Clamp(PlayerPrefs.GetInt(KeyUnlocked, 1), 1, MaxNight);
        public static bool AllNightsCompleted => PlayerPrefs.GetInt(KeyCompleted, 0) == 1;

        public static bool TutorialSeen
        {
            get => PlayerPrefs.GetInt(KeyTutorial, 0) == 1;
            set { PlayerPrefs.SetInt(KeyTutorial, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static float MusicVolume { get => PlayerPrefs.GetFloat(KeyMusic, 0.8f); set { PlayerPrefs.SetFloat(KeyMusic, value); PlayerPrefs.Save(); } }
        public static float AmbienceVolume { get => PlayerPrefs.GetFloat(KeyAmbience, 0.8f); set { PlayerPrefs.SetFloat(KeyAmbience, value); PlayerPrefs.Save(); } }
        public static float SfxVolume { get => PlayerPrefs.GetFloat(KeySfx, 1f); set { PlayerPrefs.SetFloat(KeySfx, value); PlayerPrefs.Save(); } }

        /// <summary>Al empezar una noche: queda guardada para que Continuar vuelva al inicio de esa noche.</summary>
        public static void MarkNightStarted(int night)
        {
            night = Mathf.Clamp(night, 1, MaxNight);
            if (night > UnlockedNight) PlayerPrefs.SetInt(KeyUnlocked, night);
            PlayerPrefs.Save();
        }

        public static void CompleteNight(int night)
        {
            if (night >= MaxNight) PlayerPrefs.SetInt(KeyCompleted, 1);
            int next = Mathf.Min(night + 1, MaxNight);
            if (next > UnlockedNight) PlayerPrefs.SetInt(KeyUnlocked, next);
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            PlayerPrefs.SetInt(KeyUnlocked, 1);
            PlayerPrefs.SetInt(KeyCompleted, 0);
            PlayerPrefs.SetInt(KeyTutorial, 0);
            PlayerPrefs.Save();
        }

        public static void SaveOptions() => PlayerPrefs.Save();
    }

    /// <summary>Datos que pasan del menú a la escena de juego.</summary>
    public static class GameSession
    {
        public static int SelectedNight = 1;
    }
}
