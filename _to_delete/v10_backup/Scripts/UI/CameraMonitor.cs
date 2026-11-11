using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>
    /// Monitor de cámaras del PC de la oficina (obj_pc_trigger + obj_sistema_camaras): muestra las
    /// 7 zonas con estática e interferencias. Sin luz el monitor no funciona.
    /// </summary>
    public class CameraMonitor : Interactable
    {
        [SerializeField] ZoneCamera zoneCamera;
        [SerializeField] GameObject monitorPanel;
        [SerializeField] Text cameraName;
        [SerializeField] RawImage staticOverlay;
        [SerializeField] GameObject signalLost;
        [SerializeField] Text recText;
        [SerializeField] GameObject[] mapHighlights;
        [SerializeField] string[] cameraZones = { "Comedor", "Mantenimiento", "Cocina", "Arcade", "Almacen", "Escenario", "Cuarto secreto" };

        PlayerController viewer;
        bool switchGlitch;

        /// <summary>Hay un monitor abierto: la iluminación sube para que las salas oscuras se vean en cámara.</summary>
        public static bool AnyOpen { get; private set; }
        int index;
        float glitchTimer;
        float recTimer;

        public bool IsOpen => viewer != null;
        public override InteractIcon Icon => IsOpen ? InteractIcon.Exit : InteractIcon.Camera;

        void OnDestroy() => AnyOpen = false;

        void Start()
        {
            monitorPanel.SetActive(false);
            if (staticOverlay != null) staticOverlay.texture = MakeNoise(128, 72);
        }

        public override bool CanInteract(PlayerController player) => IsOpen || PowerSystem.Instance.HasLight;
        public override string GetPrompt(PlayerController player) =>
            IsOpen ? "Cerrar el monitor" : PowerSystem.Instance.HasLight ? "Ver las cámaras" : "Monitor sin energía";

        public override void Interact(PlayerController player)
        {
            if (IsOpen) Close();
            else Open(player);
        }

        void Open(PlayerController player)
        {
            viewer = player;
            player.Lock(this);
            monitorPanel.SetActive(true);
            AnyOpen = true;
            AudioManager.PlaySfx(Sfx.CameraOpen);
            AudioManager.PlayLoop(Sfx.CameraStatic);
            ShowCurrent();
        }

        public void Close()
        {
            if (!IsOpen) return;
            viewer.Unlock(this);
            viewer = null;
            monitorPanel.SetActive(false);
            AnyOpen = false;
            zoneCamera.ClearOverride();
            AudioManager.StopLoop(Sfx.CameraStatic);
        }

        /// <summary>Cambia a la cámara tocada en el minimapa (0 = CAM 01 ... 6 = CAM 07).</summary>
        public void SelectCamera(int camIndex)
        {
            if (!IsOpen || camIndex < 0 || camIndex >= cameraZones.Length || camIndex == index) return;
            index = camIndex;
            AudioManager.PlaySfx(Sfx.CameraOn);
            glitchTimer = 0.12f;
            switchGlitch = true;
            ShowCurrent();
        }

        void ShowCurrent()
        {
            var zone = MapZones.Instance.ByName(cameraZones[index]);
            zoneCamera.ShowZone(zone);
            cameraName.text = $"CAM {index + 1:00}: {cameraZones[index].ToUpperInvariant()}";
            if (mapHighlights != null)
                for (int i = 0; i < mapHighlights.Length; i++)
                    if (mapHighlights[i] != null) mapHighlights[i].SetActive(i == index);
        }

        void Update()
        {
            if (!IsOpen) return;
            var gm = GameManager.Instance;
            if (!PowerSystem.Instance.HasLight || gm.State != GameState.Playing) { Close(); return; }

            // Interferencias suaves y poco frecuentes: la imagen de la sala debe leerse bien.
            if (glitchTimer > 0f) glitchTimer -= Time.deltaTime;
            else
            {
                switchGlitch = false;
                if (Random.value < 0.12f * Time.deltaTime) glitchTimer = Random.Range(4f, 9f) / 60f;
            }
            signalLost.SetActive(glitchTimer > 0f && switchGlitch);

            if (staticOverlay != null)
            {
                staticOverlay.uvRect = new Rect(Random.value, Random.value, 1f, 1f);
                var c = staticOverlay.color; c.a = glitchTimer > 0f ? 0.3f : 0.035f; staticOverlay.color = c;
            }
            recTimer += Time.deltaTime;
            recText.enabled = Mathf.Repeat(recTimer, 1f) < 0.6f;
        }

        static Texture2D MakeNoise(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) { byte v = (byte)Random.Range(90, 175); px[i] = new Color32(v, v, v, 255); }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
