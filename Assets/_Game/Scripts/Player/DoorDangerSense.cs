using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>
    /// Pista de peligro tras una puerta: si te acercas a una puerta y del otro lado hay un animatrónico
    /// cerca de la salida, oyes sus pasos apagados (filtro de paso bajo, como a través de la pared) y el
    /// borde de la pantalla de ese lado late en rojo suave. No dice quién ni dónde exactamente.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class DoorDangerSense : MonoBehaviour
    {
        [SerializeField] float doorRange = 17f;      // a qué distancia de la puerta empiezas a notarlo (más anticipación)
        [SerializeField] float otherSideRange = 22f; // a qué distancia de la salida debe estar el animatrónico
        [SerializeField, Range(0f, 1f)] float maxVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] float maxGlow = 0.6f;
        [SerializeField] float lowPassHz = 650f;

        PlayerController player;
        AnimatronicDirector director;
        AudioSource source;
        RawImage[] edges;            // 0 arriba, 1 abajo, 2 izquierda, 3 derecha
        float evalTimer, soundTimer;
        float danger, glow;
        int side = -1;
        AnimatronicBrain culprit;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            var lp = gameObject.AddComponent<AudioLowPassFilter>();
            lp.cutoffFrequency = lowPassHz;
            lp.lowpassResonanceQ = 1.2f;
            BuildEdges();
        }

        void Start() => director = FindFirstObjectByType<AnimatronicDirector>();

        void Update()
        {
            var gm = GameManager.Instance;
            bool live = gm != null && gm.IsPlaying && director != null && !player.IsUndetectable;
            evalTimer -= Time.deltaTime;
            if (evalTimer <= 0f) { evalTimer = 0.15f; Evaluate(live); }

            // Sonido: pasos apagados cada poco, más seguidos cuanto más peligro.
            soundTimer -= Time.deltaTime;
            if (live && danger > 0.05f && culprit != null && soundTimer <= 0f)
            {
                var voice = culprit.GetComponent<AnimatronicVoice>();
                var clip = voice != null ? voice.Clip : null;
                if (clip != null)
                {
                    source.panStereo = side == 2 ? -0.6f : side == 3 ? 0.6f : 0f;
                    source.PlayOneShot(clip, Mathf.Lerp(0.25f, 1f, danger) * maxVolume * SaveSystem.SfxVolume);
                }
                soundTimer = Mathf.Lerp(2.2f, 1.1f, danger) + Random.Range(0f, 0.4f);
            }

            // Borde rojo que late lento.
            float target = live ? danger * maxGlow : 0f;
            glow = Mathf.MoveTowards(glow, target, Time.deltaTime * 1.5f);
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * 4f);
            for (int i = 0; i < edges.Length; i++)
            {
                var c = edges[i].color;
                c.a = i == side ? glow * pulse : Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 2f);
                edges[i].color = c;
                edges[i].enabled = c.a > 0.003f;
            }
        }

        void Evaluate(bool live)
        {
            danger = 0f;
            culprit = null;
            if (!live) return;
            var zone = player.CurrentZone;
            if (zone == null) return;
            var zones = MapZones.Instance;
            Warp best = null;
            foreach (var w in Warp.All)
            {
                if (w == null || w.FromZone != zone || w.ToZone == null) continue;
                float dDoor = Vector2.Distance(player.Position, w.area.center);
                if (dDoor > doorRange) continue;
                foreach (var a in director.All)
                {
                    if (a == null || !a.IsAwake) continue;
                    if (zones.NearestZone(a.Motor.Position) != w.ToZone) continue;
                    float dA = Vector2.Distance(a.Motor.Position, w.destination);
                    if (dA > otherSideRange) continue;
                    // Se nota desde lejos (mínimo claro) y crece al acercarte a la puerta o al acercarse él a la salida.
                    float k = Mathf.Lerp(0.35f, 1f, 1f - dDoor / doorRange) * Mathf.Lerp(0.45f, 1f, 1f - dA / otherSideRange);
                    if (k > danger) { danger = k; culprit = a; best = w; }
                }
            }
            if (best != null)
            {
                Vector2 d = best.area.center - player.Position;
                side = Mathf.Abs(d.y) >= Mathf.Abs(d.x) ? (d.y > 0f ? 0 : 1) : (d.x < 0f ? 2 : 3);
            }
        }

        void BuildEdges()
        {
            var canvasGo = new GameObject("Peligro tras la puerta", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -5; // sobre el mundo, bajo el HUD y los paneles
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            var grad = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++)
            {
                float t = y / 63f;                 // 1 = borde de la pantalla
                grad.SetPixel(0, y, new Color(1f, 1f, 1f, t * t));
            }
            grad.Apply();
            var red = new Color(0.85f, 0.05f, 0.05f, 0f);
            edges = new RawImage[4];
            // anchorMin, anchorMax, tamaño, rotación del degradado
            (Vector2 min, Vector2 max, Vector2 size, float rot)[] cfg =
            {
                (new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 220f), 0f),
                (new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 220f), 180f),
                (new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(260f, 0f), 90f),
                (new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(260f, 0f), -90f),
            };
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Borde " + i, typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(canvasGo.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = cfg[i].min; rt.anchorMax = cfg[i].max;
                rt.pivot = new Vector2(cfg[i].min.x == cfg[i].max.x ? cfg[i].min.x : 0.5f, cfg[i].min.y == cfg[i].max.y ? cfg[i].min.y : 0.5f);
                rt.sizeDelta = cfg[i].size;
                rt.anchoredPosition = Vector2.zero;
                var img = go.GetComponent<RawImage>();
                img.texture = grad;
                img.raycastTarget = false;
                img.color = red;
                img.enabled = false;
                // El degradado es vertical (opaco arriba); en los bordes laterales se usa girado en UV.
                if (i == 1) img.uvRect = new Rect(0f, 1f, 1f, -1f);
                edges[i] = img;
            }
            // Laterales: textura horizontal propia (más simple que rotar el RectTransform).
            var gradH = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 64; x++) { float t = 1f - x / 63f; gradH.SetPixel(x, 0, new Color(1f, 1f, 1f, t * t)); }
            gradH.Apply();
            edges[2].texture = gradH;
            edges[3].texture = gradH;
            edges[3].uvRect = new Rect(1f, 0f, -1f, 1f);
        }
    }
}
