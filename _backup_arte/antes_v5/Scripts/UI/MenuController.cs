using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>Menú principal: continuar, elegir noche desbloqueada, opciones de volumen, créditos y salir.</summary>
    public class MenuController : MonoBehaviour
    {
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject nightsPanel;
        [SerializeField] GameObject optionsPanel;
        [SerializeField] GameObject creditsPanel;
        [SerializeField] Text continueLabel;
        [SerializeField] Button[] nightButtons;
        [SerializeField] Text[] nightLabels;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider ambienceSlider;
        [SerializeField] Slider sfxSlider;
        [SerializeField] Text progressText;

        void Start()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            musicSlider.SetValueWithoutNotify(SaveSystem.MusicVolume);
            ambienceSlider.SetValueWithoutNotify(SaveSystem.AmbienceVolume);
            sfxSlider.SetValueWithoutNotify(SaveSystem.SfxVolume);
            musicSlider.onValueChanged.AddListener(v => { SaveSystem.MusicVolume = v; AudioManager.ApplyVolumes(); });
            ambienceSlider.onValueChanged.AddListener(v => { SaveSystem.AmbienceVolume = v; AudioManager.ApplyVolumes(); });
            sfxSlider.onValueChanged.AddListener(v => SaveSystem.SfxVolume = v);
            ShowMain();
        }

        void Refresh()
        {
            int unlocked = SaveSystem.UnlockedNight;
            continueLabel.text = SaveSystem.AllNightsCompleted ? "Jugar noche 4" : $"Continuar: noche {unlocked}";
            for (int i = 0; i < nightButtons.Length; i++)
            {
                bool open = i + 1 <= unlocked;
                nightButtons[i].interactable = open;
                nightLabels[i].text = open ? $"Noche {i + 1}" : $"Noche {i + 1} (bloqueada)";
            }
            progressText.text = SaveSystem.AllNightsCompleted ? "Completaste las cuatro noches" : $"Noches desbloqueadas: {unlocked} de {SaveSystem.MaxNight}";
        }

        void Show(GameObject panel)
        {
            AudioManager.PlaySfx(Sfx.UiClick);
            mainPanel.SetActive(panel == mainPanel);
            nightsPanel.SetActive(panel == nightsPanel);
            optionsPanel.SetActive(panel == optionsPanel);
            creditsPanel.SetActive(panel == creditsPanel);
            Refresh();
        }

        public void ShowMain() => Show(mainPanel);
        public void ShowNights() => Show(nightsPanel);
        public void ShowOptions() => Show(optionsPanel);
        public void ShowCredits() => Show(creditsPanel);

        public void Continue() => Play(SaveSystem.UnlockedNight);
        public void PlayNight(int night) => Play(night);

        public void NewGame()
        {
            SaveSystem.ResetProgress();
            Play(1);
        }

        void Play(int night)
        {
            AudioManager.PlaySfx(Sfx.UiClick);
            GameSession.SelectedNight = Mathf.Clamp(night, 1, SaveSystem.UnlockedNight);
            SceneManager.LoadScene("Game");
        }

        public void SaveOptions() { SaveSystem.SaveOptions(); ShowMain(); }

        public void Quit()
        {
            SaveSystem.SaveOptions();
            Application.Quit();
        }
    }
}
