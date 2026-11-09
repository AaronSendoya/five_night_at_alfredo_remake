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
        int index;
        float glitchTimer;
        float recTimer;

        public bool IsOpen => viewer != null;
        public override InteractIcon Icon => IsOpen ? InteractIcon.Exit : InteractIcon.Camera;

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
            zoneCamera.ClearOverride();
            AudioManager.StopLoop(Sfx.CameraStatic);
        }

        /// <summary>Cambia a la cámara tocada en el minimapa (0 = CAM 01 ... 6 = CAM 07).</summary>
        public void SelectCamera(int camIndex)
        {
            if (!IsOpen || camIndex < 0 || camIndex >= cameraZones.Length || camIndex == index) return;
            index = camIndex;
            AudioManager.PlaySfx(Sfx.CameraOn);
            glitchTimer = 0.15f;
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

            if (glitchTimer > 0f) glitchTimer -= Time.deltaTime;
            else if (Random.value < 0.8f * Time.deltaTime * 60f / 100f) glitchTimer = Random.Range(5f, 15f) / 60f;
            signalLost.SetActive(glitchTimer > 0f);

            if (staticOverlay != null)
            {
                staticOverlay.uvRect = new Rect(Random.value, Random.value, 1f, 1f);
                var c = staticOverlay.color; c.a = glitchTimer > 0f ? 0.75f : 0.1f; staticOverlay.color = c;
            }
            recTimer += Time.deltaTime;
            recText.enabled = Mathf.Repeat(recTimer, 1f) < 0.6f;
        }

        static Texture2D MakeNoise(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) { byte v = (byte)Random.Range(40, 200); px[i] = new Color32(v, v, v, 255); }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
