using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>
    /// Tutorial de la primera noche: pasos cortos, cada uno con una mini escena animada hecha con el arte
    /// del juego y un botón de práctica que reacciona al tocarlo o mantenerlo (como el del HUD).
    /// Si no se toca, la escena lo hace sola tras unos segundos para que se entienda igual.
    /// </summary>
    public class TutorialView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] Text stepText;
        [SerializeField] Text hintText;
        [SerializeField] Text nextLabel;
        [SerializeField] RectTransform stage;
        [SerializeField] GameObject actionButton;
        [SerializeField] Image actionIcon;
        [SerializeField] Image actionRing;
        [SerializeField] TutorialPress press;

        [Header("Arte")]
        [SerializeField] Sprite white;
        [SerializeField] Sprite bgOffice, bgDeposito, bgArcade, bgComedor, bgAlmacen, bgEscenario;
        [SerializeField] DirectionalSprites playerSprites;
        [SerializeField] Sprite[] alfredo, matilda, bongo, rufus, rufusUp;
        [SerializeField] Sprite shutter, plateOpen, plateClosed, fuseOk, fuseBroken, panelSprite, ventSprite, minimap;
        [SerializeField] Sprite iconHand, iconFuse, iconFlashlight, iconCrank, iconHide, iconExit, iconVent, iconCamera;

        public static readonly string[] Titles =
        {
            "TU TURNO DE NOCHE", "LA LUZ Y LOS FUSIBLES", "PUERTAS DE LA OFICINA", "LA LINTERNA",
            "ESCONDITES", "VENTILACIONES", "CÁMARAS Y PISTAS", "LOS ANIMATRÓNICOS"
        };

        public static readonly string[] Steps =
        {
            "Eres el guardia nocturno de Alfredo's Pizza.\n\nSobrevive de las <b>12:00</b> a las <b>6:00 AM</b>: que no te atrapen y que no se apague la luz.",
            "Cada cierto tiempo se quema un <b>fusible</b>: con uno menos la luz falla, con dos o más hay apagón.\n\nRecoge fusibles del suelo (brillan) y <b>mantén USAR</b> en el panel de Mantenimiento.\nLa máquina del Arcade también regala uno.",
            "Junto a cada puerta de la oficina hay una <b>palanca</b>: tócala para bajar o subir la cortina.\n\nCerrada, nadie entra por ahí... pero gasta más luz y quema fusibles antes.",
            "Toca el botón de la <b>linterna</b> para encenderla. En los apagones sale <b>Rufus</b>: si lo alumbras, huye.\n\nLa batería se gasta: <b>mantén la manivela</b> para recargarla.",
            "Casilleros, mesas y la piscina de pelotas: toca <b>ESCONDERSE</b> y nadie te verá.\n\nPero el <b>aire</b> se acaba: sal a tiempo.",
            "<b>Mantén</b> el botón junto a una <b>rejilla</b> para entrar.\n\nEn el mapa, toca una sala con rejilla y saldrás allí. Dentro no te ven, pero el aire dura poco.",
            "En el monitor de la oficina toca una sala del mapa para ver su <b>cámara</b> (sin luz no funciona).\n\nCerca de una puerta, si el borde de la pantalla <b>late en rojo</b> y oyes pasos apagados, hay alguien al otro lado.",
            "Cada uno tiene su truco. Al cruzar una puerta tienes un instante de margen.\n\nSi corres, te oyen. ¡Buena suerte!"
        };

        int step;
        Demo demo;
        Font font;

        void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameManager.Instance.StateChanged += OnState;
            OnState(GameManager.Instance.State);
        }

        void OnState(GameState s)
        {
            bool show = s == GameState.Tutorial;
            panel.SetActive(show);
            if (show) { step = 0; Refresh(); }
            else ClearDemo();
        }

        public void Next()
        {
            AudioManager.PlaySfx(Sfx.UiClick);
            step++;
            if (step >= Steps.Length) { ClearDemo(); GameManager.Instance.FinishTutorial(); return; }
            Refresh();
        }

        public void Back()
        {
            if (step == 0) return;
            AudioManager.PlaySfx(Sfx.UiClick);
            step--;
            Refresh();
        }

        public void Skip()
        {
            AudioManager.PlaySfx(Sfx.UiClick);
            ClearDemo();
            GameManager.Instance.FinishTutorial();
        }

        void Refresh()
        {
            titleText.text = Titles[step];
            bodyText.text = Steps[step];
            stepText.text = $"{step + 1} / {Steps.Length}";
            if (nextLabel != null) nextLabel.text = step == Steps.Length - 1 ? "¡A jugar!" : "Siguiente";
            ClearDemo();
            demo = CreateDemo(step);
            if (demo != null) demo.Init(this);
        }

        void ClearDemo()
        {
            demo = null;
            if (stage == null) return;
            for (int i = stage.childCount - 1; i >= 0; i--) Destroy(stage.GetChild(i).gameObject);
        }

        Demo CreateDemo(int i) => i switch
        {
            0 => new GoalDemo(), 1 => new FuseDemo(), 2 => new DoorDemo(), 3 => new FlashlightDemo(),
            4 => new HideDemo(), 5 => new VentDemo(), 6 => new CameraDemo(), 7 => new CastDemo(), _ => null
        };

        void Update()
        {
            if (demo == null || !panel.activeInHierarchy) return;
            bool pressed = press != null && press.ConsumePress();
            bool held = press != null && press.Held;
            demo.t += Time.unscaledDeltaTime;
            demo.Tick(Time.unscaledDeltaTime, pressed, held);
        }

        // ------------------------------------------------------------------ utilidades de la escena

        /// <summary>Configura el botón de práctica: ícono, si se ve, anillo de progreso y pista.</summary>
        void SetAction(Sprite icon, bool visible, float ring = 0f, string hint = "", bool pulse = false)
        {
            actionButton.SetActive(visible);
            if (!visible) { hintText.text = hint; return; }
            actionIcon.sprite = icon;
            actionRing.fillAmount = ring;
            hintText.text = hint;
            float s = pulse ? 1f + 0.07f * Mathf.Sin(Time.unscaledTime * 7f) : 1f;
            actionButton.transform.localScale = new Vector3(s, s, 1f);
        }

        RectTransform Node(string name, Vector2 pos, Vector2 size, RectTransform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent != null ? parent : stage, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        Image Img(string name, Sprite sprite, Vector2 pos, Vector2 size, Color? color = null, RectTransform parent = null, bool keepAspect = true)
        {
            var img = Node(name, pos, size, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? sprite : white;
            img.color = color ?? Color.white;
            img.preserveAspect = keepAspect && sprite != null;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Recorte de un fondo. x,y,w,h en píxeles del dibujo original (origen arriba a la izquierda).</summary>
        RawImage Bg(Sprite s, float x, float y, float w, float h, float origW = 2816f, float origH = 1536f, RectTransform parent = null)
        {
            var raw = Node("Fondo", Vector2.zero, new Vector2(1000f, 560f), parent).gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            if (s == null) { raw.color = new Color(0.05f, 0.05f, 0.06f); return raw; }
            var tex = s.texture;
            var tr = s.textureRect;
            float u = (tr.x + x / origW * tr.width) / tex.width;
            float v = (tr.y + (1f - (y + h) / origH) * tr.height) / tex.height;
            raw.texture = tex;
            raw.uvRect = new Rect(u, v, w / origW * tr.width / tex.width, h / origH * tr.height / tex.height);
            return raw;
        }

        Text Txt(string name, string content, Vector2 pos, Vector2 size, int fontSize, Color color, FontStyle style = FontStyle.Bold, RectTransform parent = null, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var t = Node(name, pos, size, parent).gameObject.AddComponent<Text>();
            t.font = font;
            t.text = content;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = t.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        static Sprite Frame(Sprite[] frames, float time, float fps = 7f)
        {
            if (frames == null || frames.Length == 0) return null;
            return frames[(int)(time * fps) % frames.Length];
        }

        static float Smooth(float a, float b, float t) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, t));

        static Texture2D gradient, cone, noise;

        /// <summary>Degradado vertical (opaco arriba) para el borde rojo.</summary>
        static Texture2D Gradient()
        {
            if (gradient != null) return gradient;
            gradient = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) { float k = y / 63f; gradient.SetPixel(0, y, new Color(1f, 1f, 1f, k * k)); }
            gradient.Apply();
            return gradient;
        }

        /// <summary>Haz de la linterna: triángulo suave que nace abajo.</summary>
        static Texture2D Cone()
        {
            if (cone != null) return cone;
            const int W = 64, H = 96;
            cone = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float fy = y / (H - 1f);
                    float half = 0.06f + 0.44f * fy;
                    float dx = Mathf.Abs(x / (W - 1f) - 0.5f);
                    float a = Mathf.Clamp01((half - dx) / 0.08f) * (1f - 0.75f * fy);
                    cone.SetPixel(x, y, new Color(1f, 0.95f, 0.75f, a * 0.85f));
                }
            cone.Apply();
            return cone;
        }

        static Texture2D Noise()
        {
            if (noise != null) return noise;
            noise = new Texture2D(96, 54, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            for (int y = 0; y < 54; y++) for (int x = 0; x < 96; x++) { float v = Random.Range(0.2f, 0.85f); noise.SetPixel(x, y, new Color(v, v, v, 1f)); }
            noise.Apply();
            return noise;
        }

        // ------------------------------------------------------------------ escenas

        abstract class Demo
        {
            protected TutorialView v;
            public float t;
            public void Init(TutorialView view) { v = view; t = 0f; Build(); }
            protected abstract void Build();
            public abstract void Tick(float dt, bool pressed, bool held);

            protected Image Player(Vector2 pos) => v.Img("Jugador", v.playerSprites.down[0], pos, new Vector2(120f, 175f));
            protected void Walk(Image who, Vector2 from, Vector2 to, float t0, float t1, float now)
            {
                float k = Mathf.Clamp01(Mathf.InverseLerp(t0, t1, now));
                who.rectTransform.anchoredPosition = Vector2.Lerp(from, to, k);
                Vector2 d = to - from;
                Sprite[] frames = Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? (d.x > 0f ? v.playerSprites.right : v.playerSprites.left) : (d.y > 0f ? v.playerSprites.up : v.playerSprites.down);
                bool moving = now > t0 && now < t1;
                who.sprite = moving ? Frame(frames, now, 9f) : frames[0];
            }
        }

        /// <summary>Paso 1: el reloj avanza de 12 a 6 mientras los animatrónicos pasean.</summary>
        class GoalDemo : Demo
        {
            Text clock; Image a1, a2;
            protected override void Build()
            {
                v.Bg(v.bgOffice, 0f, 0f, 2816f, 1536f);
                v.Img("Sombra", null, Vector2.zero, new Vector2(1000f, 560f), new Color(0f, 0f, 0f, 0.4f), null, false);
                a1 = v.Img("Alfredo", v.alfredo[0], new Vector2(-600f, 40f), new Vector2(150f, 220f), new Color(0f, 0f, 0f, 0.85f));
                a2 = v.Img("Bongo", v.bongo[0], new Vector2(600f, 20f), new Vector2(150f, 220f), new Color(0f, 0f, 0f, 0.85f));
                Player(new Vector2(0f, -150f));
                clock = v.Txt("Reloj", "12:00 AM", new Vector2(0f, 190f), new Vector2(900f, 150f), 76, Color.white);
                v.SetAction(null, false);
            }
            public override void Tick(float dt, bool pressed, bool held)
            {
                float c = t % 8f;
                int hour = Mathf.Min(6, (int)(c / 5.5f * 6f));
                clock.text = c < 5.5f ? (hour == 0 ? "12" : hour.ToString()) + ":00 AM" : "6:00 AM\n<size=40>¡Sobreviviste!</size>";
                clock.color = c < 5.5f ? Color.white : new Color(0.55f, 1f, 0.55f);
                a1.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-650f, 650f, c / 8f), 40f);
                a1.sprite = Frame(v.alfredo, t);
                a2.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(650f, -650f, (c + 2f) % 8f / 8f), 10f);
                a2.sprite = Frame(v.bongo, t);
            }
        }

        /// <summary>Paso 2: se quema un fusible, se recoge otro y se instala en el panel manteniendo USAR.</summary>
        class FuseDemo : Demo
        {
            Image dark, pickup, glow, carried, player; Image[] slots;
            float install; bool done; float doneAt; float waitStart = -1f;
            protected override void Build()
            {
                v.Bg(v.bgDeposito, 900f, 520f, 1000f, 560f, 3072f / 1.090909f, 1632f / 1.0625f);
                v.Img("Panel", v.panelSprite, new Vector2(330f, -10f), new Vector2(150f, 190f));
                glow = v.Img("Brillo", null, new Vector2(-170f, -185f), new Vector2(90f, 50f), new Color(1f, 0.85f, 0.3f, 0.35f), null, false);
                pickup = v.Img("Fusible", v.fuseOk, new Vector2(-170f, -175f), new Vector2(40f, 60f));
                player = Player(new Vector2(-420f, -150f));
                carried = v.Img("Llevado", v.fuseOk, new Vector2(0f, 0f), new Vector2(30f, 45f), null, player.rectTransform);
                carried.rectTransform.anchoredPosition = new Vector2(0f, 110f);
                dark = v.Img("Oscuridad", null, Vector2.zero, new Vector2(1000f, 560f), new Color(0f, 0f, 0f, 0f), null, false);
                slots = new Image[4];
                v.Txt("Etiqueta", "FUSIBLES", new Vector2(300f, 245f), new Vector2(300f, 40f), 24, Color.white);
                for (int i = 0; i < 4; i++) slots[i] = v.Img("Ranura " + i, v.fuseOk, new Vector2(225f + i * 50f, 200f), new Vector2(34f, 52f));
                Reset();
            }
            void Reset() { t = 0f; install = 0f; done = false; waitStart = -1f; }
            public override void Tick(float dt, bool pressed, bool held)
            {
                float burn = Smooth(1f, 1.4f, t);
                bool broken = t >= 1.2f && !done;
                slots[3].sprite = broken ? v.fuseBroken : v.fuseOk;
                slots[3].color = t > 1f && t < 1.35f && !done ? new Color(1f, 0.3f, 0.2f) : Color.white;
                float flicker = broken ? 0.42f + 0.12f * Mathf.PerlinNoise(t * 9f, 0f) : 0f;
                var dc = dark.color; dc.a = Mathf.MoveTowards(dc.a, done ? 0f : flicker * burn, dt * 3f); dark.color = dc;

                Walk(player, new Vector2(-420f, -150f), new Vector2(-170f, -150f), 1.6f, 3f, t);
                bool picked = t >= 3.1f;
                pickup.enabled = glow.enabled = !picked;
                glow.color = new Color(1f, 0.85f, 0.3f, 0.25f + 0.2f * Mathf.Sin(t * 6f));
                if (t >= 3.1f) Walk(player, new Vector2(-170f, -150f), new Vector2(230f, -150f), 3.3f, 5f, t);
                carried.enabled = picked && !done;

                bool atPanel = t >= 5f && !done;
                if (atPanel)
                {
                    if (waitStart < 0f) waitStart = t;
                    // Espera a que mantengas el botón; si no, a los 3 s lo hace sola, más despacio.
                    float rate = held ? 1.4f : (t - waitStart > 3f ? 0.6f : 0f);
                    install = Mathf.Min(1f, install + rate * dt);
                    if (install >= 1f) { done = true; doneAt = t; AudioManager.PlaySfx(Sfx.FuseInstalled); }
                }
                v.SetAction(v.iconFuse, true, install, atPanel ? "Mantén el botón para instalar el fusible" : done ? "¡Luz restaurada!" : "", atPanel && !held);
                if (done && t - doneAt > 2.2f) Reset();
            }
        }

        /// <summary>Paso 3: Alfredo se acerca por la puerta; con la palanca baja la cortina y se va.</summary>
        class DoorDemo : Demo
        {
            Image cortina, alf, plate, player; float frac, alfT; bool closed; bool bumped; float bumpAt; float waitStart = -1f; Text label;
            const float DoorX = -72f, DoorTop = 137f, DoorH = 367f, DoorW = 246f;
            protected override void Build()
            {
                v.Bg(v.bgOffice, 600f, 100f, 900f, 506f);
                alf = v.Img("Alfredo", v.alfredo[0], new Vector2(DoorX, 30f), new Vector2(150f, 220f));
                cortina = v.Img("Cortina", v.shutter, new Vector2(DoorX, DoorTop), new Vector2(DoorW, 0f), null, null, false);
                cortina.rectTransform.pivot = new Vector2(0.5f, 1f);
                cortina.rectTransform.anchoredPosition = new Vector2(DoorX, DoorTop);
                plate = v.Img("Palanca", v.plateOpen, new Vector2(142f, -37f), new Vector2(56f, 78f));
                player = Player(new Vector2(250f, -160f));
                player.sprite = v.playerSprites.left[0];
                label = v.Txt("Estado", "", new Vector2(DoorX, 210f), new Vector2(500f, 60f), 34, new Color(0.6f, 1f, 0.6f));
                Restart();
            }
            void Restart() { t = 0f; alfT = 0f; bumped = false; waitStart = -1f; closed = false; }
            public override void Tick(float dt, bool pressed, bool held)
            {
                // Alfredo avanza hacia la oficina; se detiene en el umbral a esperar tu reacción.
                if (!bumped)
                {
                    if (!closed) alfT = Mathf.Min(alfT + dt / 2.6f, 1f);
                    float k = Mathf.SmoothStep(0f, 1f, alfT);
                    alf.rectTransform.anchoredPosition = new Vector2(DoorX, Mathf.Lerp(40f, -110f, k));
                    float sc = Mathf.Lerp(0.55f, 1f, k);
                    alf.rectTransform.localScale = new Vector3(sc, sc, 1f);
                    alf.sprite = Frame(v.alfredo, t);
                    alf.color = Color.Lerp(new Color(0.25f, 0.25f, 0.3f), Color.white, k);
                    if (alfT >= 1f && waitStart < 0f) waitStart = t;
                }
                bool autoClose = waitStart >= 0f && t - waitStart > 2.5f && !closed;
                if (pressed || autoClose)
                {
                    closed = !closed;
                    AudioManager.PlaySfx(closed ? Sfx.DoorClose : Sfx.DoorOpen);
                    if (closed && !bumped) { bumped = true; bumpAt = t; AudioManager.PlaySfx(Sfx.DoorBump); }
                }
                frac = Mathf.MoveTowards(frac, closed ? 1f : 0f, dt * 4f);
                cortina.rectTransform.sizeDelta = new Vector2(DoorW, DoorH * frac);
                plate.sprite = closed ? v.plateClosed : v.plateOpen;
                if (bumped)
                {
                    float b = t - bumpAt;
                    float shake = b < 0.3f ? Mathf.Sin(b * 60f) * 8f : 0f;
                    alf.rectTransform.anchoredPosition = new Vector2(DoorX + shake, Mathf.Lerp(-110f, 60f, Smooth(0.5f, 2f, b)));
                    var c = alf.color; c.a = 1f - Smooth(1.2f, 2f, b); alf.color = c;
                    label.text = b < 2.5f ? "¡No puede pasar!" : "";
                    if (b > 3.6f) { closed = false; AudioManager.PlaySfx(Sfx.DoorOpen); Restart(); alf.color = Color.white; }
                }
                else label.text = "";
                bool waiting = waitStart >= 0f && !closed;
                v.SetAction(v.iconHand, true, 0f, waiting ? "¡Toca la palanca para cerrar!" : "Toca para bajar o subir la cortina", waiting);
            }
        }

        /// <summary>Paso 4: Rufus se acerca en la oscuridad, la linterna lo espanta y la manivela recarga.</summary>
        class FlashlightDemo : Demo
        {
            enum S { Approach, Lit, Empty, Charging }
            S s; Image ruf, player, battery; RawImage beam; float battery01 = 1f, ruf01, stateT, fleeT;
            protected override void Build()
            {
                v.Bg(v.bgEscenario, 800f, 500f, 1100f, 616f, 2816f, 1536f);
                v.Img("Apagón", null, Vector2.zero, new Vector2(1000f, 560f), new Color(0f, 0f, 0.02f, 0.86f), null, false);
                ruf = v.Img("Rufus", v.rufus[0], new Vector2(0f, 150f), new Vector2(160f, 230f));
                beam = v.Node("Haz", new Vector2(0f, -120f), new Vector2(380f, 430f)).gameObject.AddComponent<RawImage>();
                beam.rectTransform.pivot = new Vector2(0.5f, 0f);
                beam.rectTransform.anchoredPosition = new Vector2(0f, -150f);
                beam.texture = Cone(); beam.raycastTarget = false;
                player = Player(new Vector2(0f, -185f));
                player.sprite = v.playerSprites.up[0];
                v.Txt("Bateria txt", "BATERÍA", new Vector2(-370f, 245f), new Vector2(200f, 36f), 22, Color.white);
                v.Img("Bateria fondo", null, new Vector2(-370f, 215f), new Vector2(204f, 26f), new Color(0f, 0f, 0f, 0.7f), null, false);
                battery = v.Img("Bateria", null, new Vector2(-470f, 215f), new Vector2(200f, 20f), new Color(0.95f, 0.85f, 0.3f), null, false);
                battery.rectTransform.pivot = new Vector2(0f, 0.5f);
                battery.rectTransform.anchoredPosition = new Vector2(-470f, 215f);
                Go(S.Approach);
            }
            void Go(S n) { s = n; stateT = 0f; if (n == S.Approach) { ruf01 = 0f; fleeT = 0f; } }
            public override void Tick(float dt, bool pressed, bool held)
            {
                stateT += dt;
                bool on = s == S.Lit;
                switch (s)
                {
                    case S.Approach:
                        ruf01 = Mathf.Min(1f, ruf01 + dt / 2.4f);
                        if (pressed || stateT > 5f) { Go(S.Lit); AudioManager.PlaySfx(Sfx.FlashlightOn); }
                        break;
                    case S.Lit:
                        fleeT += dt;
                        battery01 = Mathf.Max(0f, battery01 - dt * 0.22f);
                        if (pressed) { Go(S.Approach); AudioManager.PlaySfx(Sfx.FlashlightOff); }
                        else if (battery01 <= 0.08f) { Go(S.Empty); AudioManager.PlaySfx(Sfx.BatteryEmpty); }
                        break;
                    case S.Empty:
                        if (held || stateT > 3f) Go(S.Charging);
                        break;
                    case S.Charging:
                        battery01 = Mathf.Min(1f, battery01 + dt * (held ? 0.55f : 0.3f));
                        if (battery01 >= 1f) Go(S.Approach);
                        break;
                }
                // Rufus: se acerca a oscuras; alumbrado se gira y huye.
                Vector2 near = new Vector2(0f, 10f), far = new Vector2(0f, 170f);
                if (s == S.Lit || (s != S.Approach && fleeT > 0f))
                {
                    float k = Smooth(0f, 1.1f, fleeT);
                    ruf.rectTransform.anchoredPosition = Vector2.Lerp(Vector2.Lerp(far, near, ruf01), far + new Vector2(0f, 120f), k);
                    ruf.sprite = Frame(v.rufusUp, t, 10f);
                    ruf.color = new Color(1f, 1f, 1f, 1f - k);
                }
                else
                {
                    ruf.rectTransform.anchoredPosition = Vector2.Lerp(far, near, Mathf.SmoothStep(0f, 1f, ruf01));
                    ruf.sprite = Frame(v.rufus, t);
                    ruf.color = s == S.Approach ? new Color(0.16f, 0.14f, 0.16f, 1f) : new Color(0f, 0f, 0f, 0f);
                }
                float sc = Mathf.Lerp(0.6f, 1f, ruf01);
                ruf.rectTransform.localScale = new Vector3(sc, sc, 1f);
                float flick = on && battery01 < 0.25f ? (Mathf.PerlinNoise(t * 20f, 1f) > 0.35f ? 1f : 0.2f) : 1f;
                beam.color = new Color(1f, 1f, 1f, on ? flick : 0f);
                battery.rectTransform.sizeDelta = new Vector2(200f * battery01, 20f);
                battery.color = battery01 < 0.25f ? new Color(0.95f, 0.3f, 0.2f) : new Color(0.95f, 0.85f, 0.3f);

                bool crank = s == S.Empty || s == S.Charging;
                string hint = s == S.Approach ? "Algo se acerca... ¡toca la linterna!" : on ? "Rufus huye de la luz" : "Sin batería: mantén la manivela";
                v.SetAction(crank ? v.iconCrank : v.iconFlashlight, true, crank ? battery01 : 0f, hint, s == S.Approach && ruf01 > 0.6f || s == S.Empty);
            }
        }

        /// <summary>Paso 5: el jugador se esconde en un casillero, pasa Bongo y sale antes de quedarse sin aire.</summary>
        class HideDemo : Demo
        {
            enum S { Walk, Ready, Hidden, Out }
            S s; Image player, bongo, air, airBack; Text airTxt, msg; float air01, stateT, bongoT;
            static readonly Vector2 Spot = new Vector2(-30f, -150f);
            protected override void Build()
            {
                v.Bg(v.bgArcade, 300f, 980f, 900f, 506f);
                bongo = v.Img("Bongo", v.bongo[0], new Vector2(650f, 40f), new Vector2(160f, 230f));
                player = Player(new Vector2(-420f, -150f));
                airBack = v.Img("Aire fondo", null, new Vector2(Spot.x, 60f), new Vector2(204f, 26f), new Color(0f, 0f, 0f, 0.75f), null, false);
                air = v.Img("Aire", null, new Vector2(Spot.x - 100f, 60f), new Vector2(200f, 20f), new Color(0.5f, 0.85f, 1f), null, false);
                air.rectTransform.pivot = new Vector2(0f, 0.5f);
                air.rectTransform.anchoredPosition = new Vector2(Spot.x - 100f, 60f);
                airTxt = v.Txt("Aire txt", "AIRE", new Vector2(Spot.x, 92f), new Vector2(200f, 34f), 22, Color.white);
                msg = v.Txt("Mensaje", "", new Vector2(0f, 220f), new Vector2(800f, 60f), 34, new Color(1f, 0.5f, 0.4f));
                Go(S.Walk);
            }
            void Go(S n) { s = n; stateT = 0f; if (n == S.Walk) { air01 = 1f; bongoT = 0f; msg.text = ""; player.color = Color.white; } }
            public override void Tick(float dt, bool pressed, bool held)
            {
                stateT += dt;
                switch (s)
                {
                    case S.Walk:
                        Walk(player, new Vector2(-420f, -150f), Spot, 0.2f, 1.8f, stateT);
                        if (stateT > 1.9f) Go(S.Ready);
                        break;
                    case S.Ready:
                        player.sprite = v.playerSprites.down[0];
                        if (pressed || stateT > 3f) { Go(S.Hidden); AudioManager.PlaySfx(Sfx.HideIn); }
                        break;
                    case S.Hidden:
                        air01 = Mathf.Max(0f, air01 - dt * 0.13f);
                        bongoT += dt;
                        bool bongoGone = bongoT > 4.2f;
                        if ((pressed && bongoGone) || (bongoGone && air01 < 0.3f)) { Go(S.Out); AudioManager.PlaySfx(Sfx.HideOut); }
                        else if (pressed) msg.text = "¡Espera a que se vaya!";
                        if (air01 <= 0f) { msg.text = "¡Sin aire! Te descubren"; Go(S.Out); }
                        break;
                    case S.Out:
                        if (msg.text == "" || msg.text.StartsWith("¡Espera")) msg.text = "¡A salvo!";
                        if (stateT > 2.2f) Go(S.Walk);
                        break;
                }
                bool hidden = s == S.Hidden;
                var pc = player.color; pc.a = Mathf.MoveTowards(pc.a, hidden ? 0f : 1f, dt * 5f); player.color = pc;
                airBack.enabled = air.enabled = airTxt.enabled = hidden;
                air.rectTransform.sizeDelta = new Vector2(200f * air01, 20f);
                air.color = air01 < 0.3f ? new Color(1f, 0.35f, 0.25f) : new Color(0.5f, 0.85f, 1f);
                bongo.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(620f, -620f, Mathf.Clamp01(bongoT / 4.2f)), 30f);
                bongo.sprite = Frame(v.bongo, t);
                bongo.enabled = hidden && bongoT < 4.2f;
                bool exitIcon = hidden && bongoT > 4.2f;
                string hint = s == S.Ready ? "Toca ESCONDERSE junto al casillero" : hidden ? (exitIcon ? "¡Ya se fue! Toca SALIR" : "Quieto... no te ve") : "";
                v.SetAction(exitIcon ? v.iconExit : v.iconHide, s != S.Walk, 0f, hint, s == S.Ready || exitIcon);
            }
        }

        /// <summary>Paso 6: mantener junto a la rejilla, elegir sala en el mapa y salir por otra rejilla.</summary>
        class VentDemo : Demo
        {
            enum S { Walk, Hold, Enter, Map, Exit }
            S s; Image player, vent, map, finger; RawImage bgA, bgB; RectTransform mapRoot; Image[] boxes; Image here;
            float stateT, hold01;
            static readonly string[] Rooms = { "Comedor", "Mantenimiento", "Almacen", "Arcade", "Despensa" };
            static readonly Rect[] Cells =
            {
                Rect.MinMaxRect(421, 202, 586, 378), Rect.MinMaxRect(421, 106, 586, 186), Rect.MinMaxRect(284, 106, 409, 186),
                Rect.MinMaxRect(10, 298, 409, 378), Rect.MinMaxRect(10, 10, 135, 90)
            };
            const float MapScale = 0.9f;
            protected override void Build()
            {
                bgA = v.Bg(v.bgComedor, 950f, 600f, 900f, 506f);
                bgB = v.Bg(v.bgAlmacen, 950f, 600f, 900f, 506f);
                vent = v.Img("Rejilla", v.ventSprite, new Vector2(120f, -130f), new Vector2(120f, 90f));
                player = Player(new Vector2(-420f, -150f));
                mapRoot = v.Node("Mapa", Vector2.zero, new Vector2(1000f, 560f));
                v.Img("Velo", null, Vector2.zero, new Vector2(1000f, 560f), new Color(0f, 0f, 0f, 0.85f), mapRoot, false);
                Vector2 size = new Vector2(596f, 484f) * MapScale;
                map = v.Img("Plano", v.minimap, new Vector2(0f, -10f), size, null, mapRoot, false);
                boxes = new Image[Rooms.Length];
                for (int i = 0; i < Rooms.Length; i++)
                {
                    var r = Cells[i];
                    Vector2 c = new Vector2(r.center.x - 298f, 242f - r.center.y) * MapScale + new Vector2(0f, -10f);
                    boxes[i] = v.Img("Sala " + Rooms[i], null, c, (r.size - new Vector2(14f, 14f)) * MapScale, new Color(0.35f, 0.75f, 1f, 0.3f), mapRoot, false);
                    v.Txt("Nombre", Rooms[i] == "Almacen" ? "Almacén" : Rooms[i], c, new Vector2(r.width * MapScale, 40f), 20, Color.white, FontStyle.Bold, mapRoot);
                }
                here = v.Img("Aqui", null, boxes[0].rectTransform.anchoredPosition, boxes[0].rectTransform.sizeDelta, new Color(0.85f, 0.15f, 0.15f, 0.55f), mapRoot, false);
                finger = v.Img("Toque", null, Vector2.zero, new Vector2(46f, 46f), new Color(1f, 1f, 1f, 0.8f), mapRoot, false);
                v.Txt("Ayuda", "Toca una sala con rejilla", new Vector2(0f, 250f), new Vector2(800f, 40f), 26, Color.white, FontStyle.Bold, mapRoot);
                Go(S.Walk);
            }
            void Go(S n)
            {
                s = n; stateT = 0f;
                if (n == S.Walk) { hold01 = 0f; bgA.enabled = true; bgB.enabled = false; player.rectTransform.localScale = Vector3.one; player.color = Color.white; }
                mapRoot.gameObject.SetActive(n == S.Map);
            }
            public override void Tick(float dt, bool pressed, bool held)
            {
                stateT += dt;
                switch (s)
                {
                    case S.Walk:
                        Walk(player, new Vector2(-420f, -150f), new Vector2(20f, -140f), 0.2f, 1.8f, stateT);
                        if (stateT > 1.9f) Go(S.Hold);
                        break;
                    case S.Hold:
                        player.sprite = v.playerSprites.right[0];
                        hold01 = Mathf.Min(1f, hold01 + dt * (held ? 1.1f : stateT > 3f ? 0.5f : 0f));
                        if (hold01 >= 1f) { Go(S.Enter); AudioManager.PlaySfx(Sfx.VentIn); }
                        break;
                    case S.Enter:
                    {
                        float k = Smooth(0f, 0.5f, stateT);
                        player.rectTransform.anchoredPosition = Vector2.Lerp(new Vector2(20f, -140f), vent.rectTransform.anchoredPosition, k);
                        player.rectTransform.localScale = Vector3.one * (1f - 0.8f * k);
                        if (stateT > 0.6f) Go(S.Map);
                        break;
                    }
                    case S.Map:
                    {
                        // El "dedo" va a Almacén y lo toca.
                        Vector2 from = boxes[0].rectTransform.anchoredPosition, to = boxes[2].rectTransform.anchoredPosition;
                        float k = Smooth(0.6f, 1.8f, stateT);
                        finger.rectTransform.anchoredPosition = Vector2.Lerp(from, to, k);
                        float tap = stateT > 1.9f ? 0.7f : 1f;
                        finger.rectTransform.localScale = new Vector3(tap, tap, 1f);
                        for (int i = 0; i < boxes.Length; i++)
                            boxes[i].color = new Color(0.35f, 0.75f, 1f, i == 2 && stateT > 1.9f ? 0.65f : 0.3f);
                        here.color = new Color(0.85f, 0.15f, 0.15f, 0.4f + 0.2f * Mathf.Sin(t * 6f));
                        if (stateT > 2.4f) { Go(S.Exit); bgA.enabled = false; bgB.enabled = true; AudioManager.PlaySfx(Sfx.VentOut); }
                        break;
                    }
                    case S.Exit:
                    {
                        float k = Smooth(0f, 0.5f, stateT);
                        player.rectTransform.anchoredPosition = Vector2.Lerp(vent.rectTransform.anchoredPosition, new Vector2(-60f, -150f), k);
                        player.rectTransform.localScale = Vector3.one * (0.2f + 0.8f * k);
                        player.sprite = v.playerSprites.down[0];
                        if (stateT > 2.6f) Go(S.Walk);
                        break;
                    }
                }
                string hint = s == S.Hold ? "Mantén el botón junto a la rejilla" : s == S.Map ? "Elige la sala de salida" : s == S.Exit ? "Saliste en el Almacén" : "";
                v.SetAction(v.iconVent, s == S.Hold || s == S.Walk, hold01, hint, s == S.Hold && !held);
            }
        }

        /// <summary>Paso 7: el monitor de cámaras y la pista del borde rojo junto a una puerta.</summary>
        class CameraDemo : Demo
        {
            RectTransform camRoot, doorRoot; RawImage camA, camB, stat, red; Text camName, rec, steps; Image mat, player; int cam; float glitch;
            protected override void Build()
            {
                camRoot = v.Node("Monitor", Vector2.zero, new Vector2(1000f, 560f));
                camA = v.Bg(v.bgEscenario, 0f, 0f, 2816f, 1536f, 2816f, 1536f, camRoot);
                camB = v.Bg(v.bgAlmacen, 0f, 0f, 2816f, 1536f, 2816f, 1536f, camRoot);
                mat = v.Img("Matilda", v.matilda[0], new Vector2(160f, -40f), new Vector2(120f, 175f), null, camRoot);
                stat = v.Node("Estatica", Vector2.zero, new Vector2(1000f, 560f), camRoot).gameObject.AddComponent<RawImage>();
                stat.texture = Noise(); stat.raycastTarget = false; stat.color = new Color(1f, 1f, 1f, 0.1f);
                camName = v.Txt("Camara", "", new Vector2(-260f, 235f), new Vector2(460f, 50f), 30, Color.white, FontStyle.Bold, camRoot, TextAnchor.MiddleLeft);
                rec = v.Txt("REC", "● REC", new Vector2(390f, 235f), new Vector2(180f, 50f), 28, new Color(0.95f, 0.2f, 0.2f), FontStyle.Bold, camRoot);

                doorRoot = v.Node("Puerta", Vector2.zero, new Vector2(1000f, 560f));
                v.Bg(v.bgArcade, 950f, 0f, 900f, 506f, 2816f, 1536f, doorRoot);
                player = v.Img("Jugador", v.playerSprites.up[0], new Vector2(-10f, 30f), new Vector2(120f, 175f), null, doorRoot);
                red = v.Node("Borde rojo", new Vector2(0f, 280f), new Vector2(1000f, 200f), doorRoot).gameObject.AddComponent<RawImage>();
                red.rectTransform.pivot = new Vector2(0.5f, 1f);
                red.rectTransform.anchoredPosition = new Vector2(0f, 280f);
                red.texture = Gradient(); red.raycastTarget = false;
                steps = v.Txt("Pasos", "...pasos apagados...", new Vector2(0f, 200f), new Vector2(700f, 50f), 28, new Color(1f, 0.75f, 0.7f), FontStyle.Italic, doorRoot);
                cam = 0;
                ShowCam();
            }
            void ShowCam()
            {
                camA.enabled = cam == 0; camB.enabled = cam == 1;
                mat.enabled = cam == 1;
                camName.text = cam == 0 ? "CAM 06: ESCENARIO" : "CAM 05: ALMACÉN";
                glitch = 0.18f;
            }
            public override void Tick(float dt, bool pressed, bool held)
            {
                float c = t % 12f;
                bool monitor = c < 6.5f;
                camRoot.gameObject.SetActive(monitor);
                doorRoot.gameObject.SetActive(!monitor);
                if (monitor)
                {
                    if (pressed || (c > 3f && c - dt <= 3f)) { cam = 1 - cam; ShowCam(); AudioManager.PlaySfx(Sfx.CameraOn); }
                    glitch -= dt;
                    stat.uvRect = new Rect(Random.value, Random.value, 1f, 1f);
                    stat.color = new Color(1f, 1f, 1f, glitch > 0f ? 0.7f : 0.1f);
                    rec.enabled = Mathf.Repeat(t, 1f) < 0.6f;
                    mat.sprite = Frame(v.matilda, t);
                    v.SetAction(v.iconCamera, true, 0f, "Toca para cambiar de cámara", false);
                }
                else
                {
                    if (c - dt < 6.5f) { cam = 0; ShowCam(); }
                    float pulse = 0.65f + 0.35f * Mathf.Sin(t * 4f);
                    float k = Smooth(6.5f, 7.5f, c);
                    red.color = new Color(0.85f, 0.05f, 0.05f, 0.6f * k * pulse);
                    steps.color = new Color(1f, 0.75f, 0.7f, k * (0.5f + 0.5f * pulse));
                    v.SetAction(null, false, 0f, "Borde rojo + pasos apagados = alguien espera tras la puerta");
                }
            }
        }

        /// <summary>Paso 8: los cuatro animatrónicos con su truco.</summary>
        class CastDemo : Demo
        {
            Image[] faces = new Image[4];
            protected override void Build()
            {
                v.Img("Fondo", null, Vector2.zero, new Vector2(1000f, 560f), new Color(0.06f, 0.05f, 0.07f, 1f), null, false);
                string[] names = { "ALFREDO", "MATILDA", "BONGO", "RUFUS" };
                string[] tips =
                {
                    "Desde la noche 2 te rastrea cada cierto tiempo, estés donde estés. Escóndete o usa una rejilla.",
                    "Va al panel a quemar fusibles. Vigila la luz.",
                    "Patrulla sin descanso y te persigue al verte.",
                    "Caza en los apagones. El más rápido: alúmbralo.",
                };
                for (int i = 0; i < 4; i++)
                {
                    float x = -375f + i * 250f;
                    v.Img("Tarjeta " + i, null, new Vector2(x, 0f), new Vector2(232f, 520f), new Color(1f, 1f, 1f, 0.06f), null, false);
                    faces[i] = v.Img(names[i], Frames(i)[0], new Vector2(x, 110f), new Vector2(170f, 250f));
                    v.Txt("Nombre " + i, names[i], new Vector2(x, -45f), new Vector2(230f, 40f), 30, new Color(0.95f, 0.75f, 0.35f));
                    v.Txt("Truco " + i, tips[i], new Vector2(x, -160f), new Vector2(214f, 180f), 22, Color.white, FontStyle.Normal, null, TextAnchor.UpperCenter);
                }
                v.SetAction(null, false);
            }
            Sprite[] Frames(int i) => i == 0 ? v.alfredo : i == 1 ? v.matilda : i == 2 ? v.bongo : v.rufus;
            public override void Tick(float dt, bool pressed, bool held)
            {
                for (int i = 0; i < 4; i++) faces[i].sprite = Frame(Frames(i), t + i * 0.3f, 5f);
            }
        }
    }
}
