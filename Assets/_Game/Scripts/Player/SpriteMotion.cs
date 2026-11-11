using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Animación procedural sobre los cuadros de un personaje (jugador o animatrónico), para que no se vea
    /// como una simple sucesión de imágenes:
    /// - rebote y ligero aplastamiento al pisar, proporcional a la velocidad (más marcado al correr);
    /// - inclinación hacia la dirección REAL del movimiento (el joystick gira 360°: en diagonal el cuerpo
    ///   se inclina aunque el sprite sea de 4 direcciones);
    /// - un pequeño "giro" (estrechamiento horizontal) al cambiar de dirección, que disimula el salto de cuadro;
    /// - sombra suave en el suelo;
    /// - embestida al atacar y transiciones al esconderse / entrar en una rejilla.
    /// Va en el objeto que tiene el SpriteRenderer (hijo del personaje, con pivote en los pies).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteMotion : MonoBehaviour
    {
        [SerializeField] float bobPixels = 1.6f;
        [SerializeField] float strideUnits = 1.5f;         // unidades recorridas por paso
        [SerializeField] float leanDegrees = 6f;
        [SerializeField] float leanSpeedRef = 10f;         // a esta velocidad se alcanza la inclinación máxima
        [SerializeField] float turnSquash = 0.22f;
        [SerializeField] float shadowWidth = 1.4f;
        [SerializeField] bool castShadow = true;

        SpriteRenderer sr, shadow;
        SpriteFrameAnimator anim;
        Transform root;
        Vector3 basePos, baseScale;
        Vector2 lastRoot;
        Vector2 velocity;
        float bobPhase, lean, turn, pixel;

        // Ataque (lo fija AnimatronicMotor)
        float attack01; Vector2 attackDir;

        // Transiciones de esconderse / rejilla
        float fx = 1f, fxDir; bool fxVent; Vector3 fxTarget;
        public float Alpha { get; private set; } = 1f;

        static Sprite shadowSprite;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<SpriteFrameAnimator>();
            root = transform.parent != null ? transform.parent : transform;
            basePos = transform.localPosition;
            baseScale = transform.localScale;
            pixel = baseScale.y / 32f;
            lastRoot = root.position;
            if (anim != null) anim.FramesChanged += () => turn = 1f;
        }

        void Start()
        {
            // Creada en Start: así YSort (que reúne sus renderers en Awake) no la ordena como al personaje.
            if (!castShadow) return;
            var go = new GameObject("Sombra");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            shadow = go.AddComponent<SpriteRenderer>();
            shadow.sprite = ShadowSprite();
            shadow.sharedMaterial = sr.sharedMaterial;
            shadow.color = new Color(0f, 0f, 0f, 0.35f);
            float w = shadowWidth * Mathf.Max(0.6f, baseScale.x / 1f);
            go.transform.localScale = new Vector3(w, w * 0.35f, 1f);
        }

        /// <summary>Postura de ataque (0-1) hacia una dirección.</summary>
        public void SetAttack(float amount01, Vector2 dir)
        {
            attack01 = amount01;
            if (dir.sqrMagnitude > 0.0001f) attackDir = dir.normalized;
        }

        /// <summary>Se mete en un escondite o rejilla situado en 'target' (mundo).</summary>
        public void Vanish(Vector3 target, bool vent) { fxDir = -1f; fxVent = vent; fxTarget = target; if (fx > 0.999f) fx = 1f; }
        /// <summary>Sale del escondite o rejilla.</summary>
        public void Appear(bool vent) { fxDir = 1f; fxVent = vent; fx = 0f; }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector2 p = root.position;
            Vector2 d = p - lastRoot;
            lastRoot = p;
            Vector2 v = d.sqrMagnitude > 9f ? Vector2.zero : d / dt;   // teletransporte: no cuenta
            velocity = Vector2.Lerp(velocity, v, 1f - Mathf.Exp(-14f * dt));
            float speed = velocity.magnitude;
            bool moving = speed > 0.4f;

            // Paso: la fase avanza con la distancia recorrida (no con el tiempo).
            if (moving) bobPhase += speed * dt / strideUnits * Mathf.PI;
            else bobPhase = Mathf.MoveTowards(bobPhase, Mathf.Round(bobPhase / Mathf.PI) * Mathf.PI, dt * 6f);
            float stepAmt = Mathf.Clamp01(speed / leanSpeedRef);
            float bob = Mathf.Abs(Mathf.Sin(bobPhase)) * pixel * bobPixels * (0.5f + stepAmt);
            float land = 1f - Mathf.Abs(Mathf.Sin(bobPhase));              // 1 justo al pisar
            float squashY = 1f - 0.035f * land * stepAmt;
            float squashX = 1f + 0.025f * land * stepAmt;

            // Inclinación hacia el movimiento real (incluye las diagonales del joystick).
            float targetLean = -Mathf.Clamp(velocity.x / leanSpeedRef, -1f, 1f) * leanDegrees;
            lean = Mathf.Lerp(lean, targetLean, 1f - Mathf.Exp(-10f * dt));

            // Giro al cambiar de dirección.
            turn = Mathf.MoveTowards(turn, 0f, dt / 0.12f);
            float turnX = 1f - turnSquash * Mathf.Sin(turn * Mathf.PI);

            // Ataque: se estira y se adelanta.
            Vector3 lunge = (Vector3)(attackDir * attack01 * pixel * 4f);
            float stretch = 1f + 0.08f * attack01;

            // Esconderse / rejilla.
            if (fxDir != 0f)
            {
                fx = Mathf.Clamp01(fx + fxDir * dt / (fxDir < 0f ? 0.28f : 0.22f));
                if ((fxDir < 0f && fx <= 0f) || (fxDir > 0f && fx >= 1f)) fxDir = 0f;
            }
            float e = Mathf.SmoothStep(0f, 1f, fx);
            Vector3 fxOffset = Vector3.zero; float fxSX = 1f, fxSY = 1f;
            if (fx < 1f)
            {
                if (fxVent) { fxSX = Mathf.Lerp(1.25f, 1f, e); fxSY = Mathf.Lerp(0.1f, 1f, e); }
                else { fxSX = fxSY = Mathf.Lerp(0.75f, 1f, e); }
                if (fxDir <= 0f) fxOffset = root.InverseTransformVector(fxTarget - root.position) * (1f - e) * 0.6f;
            }
            Alpha = Mathf.Lerp(0f, 1f, fxVent ? e : Mathf.Sqrt(e));

            transform.localPosition = basePos + Vector3.up * bob + lunge + fxOffset;
            transform.localRotation = Quaternion.Euler(0f, 0f, lean);
            transform.localScale = new Vector3(baseScale.x * squashX * turnX * stretch * fxSX, baseScale.y * squashY * stretch * fxSY, baseScale.z);
            var c = sr.color; c.a = Alpha; sr.color = c;

            if (shadow != null)
            {
                shadow.enabled = sr.enabled;
                shadow.sortingOrder = sr.sortingOrder - 1;
                float s = 1f - 0.15f * Mathf.Abs(Mathf.Sin(bobPhase)) * stepAmt;
                shadow.color = new Color(0f, 0f, 0f, 0.35f * Alpha * s);
            }
        }

        static Sprite ShadowSprite()
        {
            if (shadowSprite != null) return shadowSprite;
            const int N = 32;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            shadowSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            return shadowSprite;
        }
    }
}
