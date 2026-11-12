using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>Interfaz durante el juego: reloj, vidas, resistencia, batería, fusibles, botón Usar y avisos.</summary>
    public class HudView : MonoBehaviour
    {
        [Header("Referencias de juego")]
        [SerializeField] NightClock clock;
        [SerializeField] PlayerController player;
        [SerializeField] PlayerInteractor interactor;
        [SerializeField] Flashlight flashlight;
        [SerializeField] AnimatronicDirector director;
        [SerializeField] CameraMonitor monitor;

        [Header("Textos")]
        [SerializeField] Text clockText;
        [SerializeField] Text nightText;
        [SerializeField] Text livesText;
        [SerializeField] Text fusesText;
        [SerializeField] Text powerText;
        [SerializeField] Text messageText;
        [SerializeField] Text zoneText;

        [Header("Barras")]
        [SerializeField] Image staminaFill;
        [SerializeField] Text staminaLabel;
        [SerializeField] Image noiseFill;
        [SerializeField] Text noiseLabel;
        [SerializeField] NoiseMeter noise;
        [SerializeField] Image batteryFill;
        [SerializeField] GameObject batteryGroup;
        [SerializeField] Image airFill;
        [SerializeField] GameObject airGroup;

        [Header("Controles")]
        [SerializeField] GameObject controlsRoot;
        [SerializeField] GameObject interactButton;
        [SerializeField] Text interactLabel;
        [SerializeField] Image interactProgress;
        [SerializeField] Image interactBackground;
        [SerializeField] GameObject flashlightButton;
        [SerializeField] GameObject crankButton;

        [Header("Íconos")]
        [SerializeField] Image interactIcon;
        [SerializeField] GameObject interactHint;
        [SerializeField] Sprite[] interactIcons;
        [SerializeField] Image flashlightIcon;
        [SerializeField] Sprite flashTurnOnIcon;
        [SerializeField] Sprite flashTurnOffIcon;
        [SerializeField] Image runBackground;
        [SerializeField] Image runIcon;
        [SerializeField] Image crankIcon;
        [SerializeField] Image crankBackground;

        static readonly Color Usable = new Color(0.16f, 0.16f, 0.18f, 0.95f);
        static readonly Color NotUsable = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        static readonly Color StaminaOk = new Color(0.85f, 0.85f, 0.85f);
        static readonly Color UiFactory_Muted = new Color(0.75f, 0.75f, 0.75f);
        static readonly Color StaminaOut = new Color(0.8f, 0.2f, 0.2f);
        static readonly Color Active = new Color(0.72f, 0.12f, 0.12f, 0.95f);
        static readonly Color Idle = new Color(0.1f, 0.1f, 0.11f, 0.8f);

        float messageTimer;

        void OnEnable()
        {
            HudMessages.Shown += ShowMessage;
            HidingSpot.Message += ShowMessage;
        }

        void OnDisable()
        {
            HudMessages.Shown -= ShowMessage;
            HidingSpot.Message -= ShowMessage;
        }

        void Start()
        {
            if (flashlight != null) flashlight.BatteryEmpty += () => ShowMessage("Batería agotada: mantén Manivela");
            PowerSystem.Instance.FuseBurned += () => ShowMessage("¡Se quemó un fusible!");
            if (messageText != null) messageText.text = "";
        }

        public void ShowMessage(string text)
        {
            if (messageText == null) return;
            messageText.text = text;
            messageTimer = 3f;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            bool playing = gm.State == GameState.Playing;
            bool showControls = playing && !(monitor != null && monitor.IsOpen);
            if (controlsRoot != null && controlsRoot.activeSelf != showControls) controlsRoot.SetActive(showControls);

            clockText.text = clock.DisplayTime;
            nightText.text = "Noche " + gm.Night.night;
            livesText.text = "Vidas: " + gm.Lives;

            int active = PowerSystem.Instance.ActiveFuses;
            fusesText.text = $"Fusibles: {player.Inventory.Fuses}/{player.Inventory.MaxFuses}   Panel: {active}/{PowerSystem.FuseCount}";
            switch (PowerSystem.Instance.State)
            {
                case PowerState.On: powerText.text = "Luz: normal"; powerText.color = new Color(0.75f, 0.75f, 0.75f); break;
                case PowerState.Flickering: powerText.text = "Luz: fallando"; powerText.color = new Color(0.95f, 0.75f, 0.2f); break;
                default: powerText.text = PowerSystem.Instance.SwitchOn ? "Luz: APAGÓN" : "Luz: interruptor apagado"; powerText.color = new Color(0.9f, 0.25f, 0.25f); break;
            }
            if (zoneText != null) { var z = player.CurrentZone; zoneText.text = z != null ? z.name : ""; }

            staminaFill.fillAmount = player.Stamina01;
            staminaFill.color = player.IsExhausted ? StaminaOut : StaminaOk;
            if (staminaLabel != null) staminaLabel.text = player.IsExhausted ? "AGOTADO" : "RESISTENCIA";
            if (noise != null && noiseFill != null)
            {
                float n = noise.Noise01;
                noiseFill.fillAmount = Mathf.Max(0.02f, n);
                // verde -> amarillo -> rojo
                noiseFill.color = n < 0.55f ? Color.Lerp(new Color(0.3f, 0.85f, 0.35f), new Color(0.95f, 0.8f, 0.2f), n / 0.55f)
                                            : Color.Lerp(new Color(0.95f, 0.8f, 0.2f), new Color(0.92f, 0.15f, 0.12f), (n - 0.55f) / 0.45f);
                if (noiseLabel != null)
                {
                    noiseLabel.text = noise.IsLoud ? "¡RUIDO!" : "RUIDO";
                    noiseLabel.color = noise.IsLoud && Mathf.Repeat(Time.time, 0.5f) < 0.25f ? new Color(0.95f, 0.25f, 0.2f) : UiFactory_Muted;
                }
            }

            bool hasLight = flashlight != null && flashlight.HasFlashlight;
            if (batteryGroup != null) batteryGroup.SetActive(hasLight);
            if (hasLight) batteryFill.fillAmount = flashlight.Battery01;
            if (flashlightButton != null) flashlightButton.SetActive(hasLight && !player.IsHidden);
            if (crankButton != null) crankButton.SetActive(hasLight && !player.IsHidden);

            var spot = player.CurrentHidingSpot;
            if (airGroup != null) airGroup.SetActive(spot != null);
            if (spot != null) airFill.fillAmount = spot.Air01;

            var target = interactor.Target;
            interactButton.SetActive(playing && target != null);
            if (interactHint != null) interactHint.SetActive(playing && target != null);
            if (ControlsEditor.Editing)
            {
                // Editando los controles: se ven los 4 botones para poder colocarlos.
                interactButton.SetActive(true);
                if (flashlightButton != null) flashlightButton.SetActive(true);
                if (crankButton != null) crankButton.SetActive(true);
                if (interactHint != null) interactHint.SetActive(false);
                return;
            }
            if (target != null)
            {
                bool usable = interactor.TargetUsable;
                interactLabel.text = interactor.Prompt;
                interactBackground.color = usable ? Usable : NotUsable;
                interactProgress.fillAmount = interactor.HoldProgress01;
                int icon = (int)target.Icon;
                if (interactIcons != null && icon < interactIcons.Length) interactIcon.sprite = interactIcons[icon];
                interactIcon.color = new Color(1f, 1f, 1f, usable ? 1f : 0.35f);
            }
            UpdateActionButtons();

            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f) messageText.text = "";
            }

            UpdateTension(gm);
        }

        void UpdateActionButtons()
        {
            if (flashlight != null && flashlightIcon != null)
                flashlightIcon.sprite = flashlight.IsOn ? flashTurnOffIcon : flashTurnOnIcon;
            if (runBackground != null)
            {
                runBackground.color = player.IsRunning ? Active : Idle;
                runIcon.color = new Color(1f, 1f, 1f, player.IsExhausted ? 0.3f : 1f);
            }
            if (crankIcon != null && flashlight != null)
            {
                crankBackground.color = flashlight.IsCranking ? Active : Idle;
                if (flashlight.IsCranking) crankIcon.rectTransform.Rotate(0f, 0f, -300f * Time.deltaTime);
                else crankIcon.rectTransform.localRotation = Quaternion.identity;
            }
        }

        void UpdateTension(GameManager gm)
        {
            float t;
            if (gm.State != GameState.Playing) t = 0f;
            else if (player.IsHidden) t = 0.05f;
            else if (director.ActiveChases > 0) t = 1f;
            else
            {
                float d = director.NearestActiveDistance(player.Position);
                t = d < 25f ? Mathf.Lerp(0.6f, 0.15f, d / 25f) : 0.12f;
            }
            AudioManager.SetTension(t);
            // El latido solo aparece con un animatrónico cerca o persiguiéndote; escondido se calma.
            AudioManager.SetHeartbeat(player.IsHidden ? 0f : t);
        }
    }
}
