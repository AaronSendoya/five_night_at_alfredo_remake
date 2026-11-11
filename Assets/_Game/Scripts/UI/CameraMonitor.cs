using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>
    /// Monitor de cámaras del PC de la oficina (obj_pc_trigger + obj_sistema_camaras).
    /// Vista limpia de monitor antiguo: sin el HUD del jugador, con líneas de barrido, viñeta y un tinte
    /// frío; la cámara barre la sala despacio de izquierda a derecha. La estática NO es constante:
    /// aparecen ráfagas cortas de ruido y distorsión cada pocos segundos (y al cambiar de cámara).
    /// Sin luz el monitor no funciona.
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
        [Tooltip("HUD del jugador (reloj, vidas, barras...): se oculta mientras se mira el monitor")]
        [SerializeField] CanvasGroup hudGroup;
        [SerializeField] Vector2 burstEverySeconds = new Vector2(5f, 11f);
        [SerializeField] Vector2 burstSeconds = new Vector2(0.15f, 0.45f);

        /// <summary>Hay un monitor abierto: la iluminación sube para que las salas oscuras se vean en cámara.</summary>
        public static bool AnyOpen { get; private set; }

        PlayerController viewer;
        int index;
        float burstTimer, burstLeft, burstLength, recTimer, lostTimer;
        RectTransform band;
        RawImage bandImage, scan;
        Text stamp;
        NightClock clock;

        public bool IsOpen => viewer != null;
        public override InteractIcon Icon => IsOpen ? InteractIcon.Exit : InteractIcon.Camera;

        void OnDestroy() => AnyOpen = false;

        void Start()
        {
            monitorPanel.SetActive(false);
            if (staticOverlay != null)
            {
                staticOverlay.texture = MakeNoise(160, 90);
                staticOverlay.color = new Color(1f, 1f, 1f, 0f);
            }
            BuildCrt();
            clock = FindFirstObjectByType<NightClock>();
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
            SetHud(false);
            AudioManager.PlaySfx(Sfx.CameraOpen);
            AudioManager.PlayLoop(Sfx.CameraStatic);
            burstTimer = Random.Range(burstEverySeconds.x, burstEverySeconds.y);
            StartBurst(0.3f);
            ShowCurrent();
        }

        public void Close()
        {
            if (!IsOpen) return;
            viewer.Unlock(this);
            viewer = null;
            monitorPanel.SetActive(false);
            AnyOpen = false;
            SetHud(true);
            AudioManager.PlaySfx(Sfx.CameraOpen);   // el mismo sonido al cerrar que al abrir
            zoneCamera.Jitter = Vector2.zero;
            zoneCamera.ClearOverride();
            AudioManager.StopLoop(Sfx.CameraStatic);
        }

        void SetHud(bool visible)
        {
            if (hudGroup == null) return;
            hudGroup.alpha = visible ? 1f : 0f;
            hudGroup.blocksRaycasts = visible;
            hudGroup.interactable = visible;
        }

        /// <summary>Cambia a la cámara tocada en el minimapa (0 = CAM 01 ... 6 = CAM 07).</summary>
        public void SelectCamera(int camIndex)
        {
            if (!IsOpen || camIndex < 0 || camIndex >= cameraZones.Length || camIndex == index) return;
            index = camIndex;
            AudioManager.PlaySfx(Sfx.CameraOn);
            lostTimer = 0.1f;
            StartBurst(0.25f);
            ShowCurrent();
        }

        void ShowCurrent()
        {
            var zone = MapZones.Instance.ByName(cameraZones[index]);
            zoneCamera.ShowZone(zone);
            string label = cameraZones[index] == "Almacen" ? "ALMACÉN" : cameraZones[index].ToUpperInvariant();
            cameraName.text = $"CAM {index + 1:00}: {label}";
            if (mapHighlights != null)
                for (int i = 0; i < mapHighlights.Length; i++)
                    if (mapHighlights[i] != null) mapHighlights[i].SetActive(i == index);
        }

        void StartBurst(float seconds)
        {
            burstLength = burstLeft = seconds;
        }

        void Update()
        {
            if (!IsOpen) return;
            var gm = GameManager.Instance;
            if (!PowerSystem.Instance.HasLight || gm.State != GameState.Playing) { Close(); return; }
            float dt = Time.unscaledDeltaTime;

            // Ráfagas de interferencia: momentáneas y espaciadas.
            burstTimer -= dt;
            if (burstTimer <= 0f)
            {
                burstTimer = Random.Range(burstEverySeconds.x, burstEverySeconds.y);
                StartBurst(Random.Range(burstSeconds.x, burstSeconds.y));
                if (Random.value < 0.5f) AudioManager.PlaySfx(Sfx.CameraOn);
            }
            bool burst = burstLeft > 0f;
            if (burst) burstLeft -= dt;
            float k = burst ? Mathf.Clamp01(burstLeft / Mathf.Max(0.01f, burstLength)) : 0f;

            if (lostTimer > 0f) lostTimer -= dt;
            signalLost.SetActive(lostTimer > 0f);

            if (staticOverlay != null)
            {
                staticOverlay.uvRect = new Rect(Random.value, Random.value, 1f, 1f);
                var c = staticOverlay.color; c.a = burst ? Mathf.Lerp(0.15f, 0.6f, k) * (0.7f + 0.3f * Random.value) : 0f; staticOverlay.color = c;
            }
            if (band != null)
            {
                band.gameObject.SetActive(burst || Random.value < 0.004f);
                band.anchoredPosition = new Vector2(Random.Range(-30f, 30f) * k, Mathf.Repeat(Time.unscaledTime * 900f, 1400f) - 700f);
                bandImage.color = new Color(1f, 1f, 1f, burst ? 0.35f : 0.12f);
            }
            zoneCamera.Jitter = burst ? new Vector2(Random.Range(-0.35f, 0.35f) * k, Random.Range(-0.08f, 0.08f) * k) : Vector2.zero;
            if (scan != null) scan.uvRect = new Rect(0f, Mathf.Repeat(Time.unscaledTime * 0.6f, 1f), 1f, Screen.height / 4f);

            recTimer += dt;
            recText.enabled = Mathf.Repeat(recTimer, 1f) < 0.6f;
            if (stamp != null && clock != null) stamp.text = clock.DisplayTime + "   NOCHE " + (gm.Night != null ? gm.Night.night : 1);
        }

        /// <summary>Capas del monitor antiguo: tinte, líneas de barrido, viñeta, franja de distorsión y hora.</summary>
        void BuildCrt()
        {
            var parent = monitorPanel.transform;
            int at = staticOverlay != null ? staticOverlay.transform.GetSiblingIndex() + 1 : 0;

            var tint = Layer("Tinte", parent, at++);
            tint.color = new Color(0.55f, 0.85f, 0.75f, 0.07f);

            scan = Layer("Barrido", parent, at++);
            var lines = new Texture2D(1, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            lines.SetPixels(new[] { new Color(0f, 0f, 0f, 0.28f), new Color(0f, 0f, 0f, 0.05f), new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, 0.05f) });
            lines.Apply();
            scan.texture = lines;

            var vig = Layer("Viñeta", parent, at++);
            const int N = 128;
            var vt = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.1f);
                    vt.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Clamp01((d - 0.55f) * 1.6f) * 0.85f));
                }
            vt.Apply();
            vig.texture = vt;

            var bandGo = new GameObject("Distorsion", typeof(RectTransform), typeof(RawImage));
            bandGo.transform.SetParent(parent, false);
            bandGo.transform.SetSiblingIndex(at++);
            band = (RectTransform)bandGo.transform;
            band.anchorMin = new Vector2(0f, 0.5f); band.anchorMax = new Vector2(1f, 0.5f);
            band.sizeDelta = new Vector2(0f, 90f);
            bandImage = bandGo.GetComponent<RawImage>();
            bandImage.texture = staticOverlay != null ? staticOverlay.texture : null;
            bandImage.raycastTarget = false;
            bandGo.SetActive(false);

            var stampGo = new GameObject("Hora", typeof(RectTransform), typeof(Text));
            stampGo.transform.SetParent(parent, false);
            var srt = (RectTransform)stampGo.transform;
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0f, 0f);
            srt.anchoredPosition = new Vector2(60f, 50f);
            srt.sizeDelta = new Vector2(700f, 60f);
            stamp = stampGo.GetComponent<Text>();
            stamp.font = cameraName.font;
            stamp.fontSize = 34;
            stamp.color = new Color(0.9f, 0.95f, 0.9f, 0.85f);
            stamp.alignment = TextAnchor.LowerLeft;
            stamp.raycastTarget = false;
        }

        static RawImage Layer(string name, Transform parent, int sibling)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(sibling);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            var img = go.GetComponent<RawImage>();
            img.raycastTarget = false;
            return img;
        }

        static Texture2D MakeNoise(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) { byte v = (byte)Random.Range(30, 230); px[i] = new Color32(v, v, v, 255); }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
