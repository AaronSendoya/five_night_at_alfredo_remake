using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>Pantallas superpuestas: inicio de noche, susto, victoria, derrota y pausa.</summary>
    public class ScreenPanels : MonoBehaviour
    {
        [SerializeField] GameObject introPanel;
        [SerializeField] Text introNightText;
        [SerializeField] Text introLoreText;

        static readonly string[] NightLore =
        {
            "Primera noche. Contesta el teléfono: el guardia anterior te dejó un mensaje.",
            "Los animatrónicos están más inquietos que ayer. Vigila los fusibles.",
            "Alguien ha estado manipulando el panel eléctrico... y no fuiste tú.",
            "Última noche. Algo despertó en el cuarto que hay detrás del escenario."
        };

        [SerializeField] GameObject jumpscarePanel;
        [SerializeField] Image jumpscareImage;
        [SerializeField] Image jumpscareFlash;

        [SerializeField] GameObject victoryPanel;
        [SerializeField] Text victoryDetail;
        [SerializeField] GameObject nextNightButton;
        [SerializeField] ConfettiView confetti;

        [SerializeField] GameObject gameOverPanel;
        [SerializeField] Text gameOverDetail;
        [SerializeField] Text gameOverTip;

        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject pauseButton;

        void Start()
        {
            var gm = GameManager.Instance;
            gm.StateChanged += OnState;
            gm.PauseChanged += p => pausePanel.SetActive(p);
            HideAll();
            OnState(gm.State);
        }

        void HideAll()
        {
            introPanel.SetActive(false);
            jumpscarePanel.SetActive(false);
            victoryPanel.SetActive(false);
            gameOverPanel.SetActive(false);
            pausePanel.SetActive(false);
        }

        void OnState(GameState s)
        {
            var gm = GameManager.Instance;
            introPanel.SetActive(s == GameState.Intro);
            if (s == GameState.Intro)
            {
                introNightText.text = "Noche " + gm.Night.night;
                if (introLoreText != null) introLoreText.text = NightLore[Mathf.Clamp(gm.Night.night - 1, 0, NightLore.Length - 1)];
            }
            pauseButton.SetActive(s == GameState.Playing);

            if (s == GameState.Victory)
            {
                victoryPanel.SetActive(true);
                bool last = gm.Night.night >= SaveSystem.MaxNight;
                victoryDetail.text = last ? "Sobreviviste a las cuatro noches." : $"Sobreviviste a la noche {gm.Night.night}.";
                nextNightButton.SetActive(!last);
                if (confetti != null) confetti.Play();
            }
            if (s == GameState.GameOver)
            {
                gameOverPanel.SetActive(true);
                gameOverDetail.text = $"{gm.LastKiller} te atrapó en la noche {gm.Night.night} a las {gm.Clock.DisplayTime}.";
                if (gameOverTip != null) gameOverTip.text = "CONSEJO: " + gm.LastDeathTip;
            }
        }

        public void ShowJumpscare(Sprite sprite, float seconds) => StartCoroutine(JumpscareRoutine(sprite, seconds));

        IEnumerator JumpscareRoutine(Sprite sprite, float seconds)
        {
            jumpscarePanel.SetActive(true);
            jumpscareImage.sprite = sprite;
            jumpscareImage.preserveAspect = true;
            var rt = jumpscareImage.rectTransform;
            Vector2 basePos = rt.anchoredPosition;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / 0.25f);
                rt.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.15f, k);
                rt.anchoredPosition = basePos + Random.insideUnitCircle * Mathf.Lerp(40f, 12f, t / seconds);
                jumpscareFlash.color = new Color(0.6f, 0f, 0f, (Mathf.FloorToInt(t * 15f) % 2 == 0) ? 0.55f : 0f);
                yield return null;
            }
            rt.anchoredPosition = basePos;
            jumpscarePanel.SetActive(false);
        }
    }
}
