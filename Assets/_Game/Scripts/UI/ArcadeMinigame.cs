using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>
    /// Minijuego de la máquina del arcade (rm_minijuego_pizza del original): atrapa las pizzas que caen.
    /// Cada pizza suma 10 puntos y con 100 se gana un fusible. Se juega arrastrando el dedo (o con
    /// A/D y las flechas en el editor). Si se escapan 3 pizzas, se pierde la partida.
    /// Mientras se juega, los animatrónicos no ven ni pueden atrapar al jugador, pero el reloj sigue.
    /// </summary>
    public class ArcadeMinigame : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] RectTransform playArea;
        [SerializeField] RectTransform catcher;
        [SerializeField] Image catcherImage;
        [SerializeField] Sprite[] idleFrames;
        [SerializeField] Sprite[] leftFrames;
        [SerializeField] Sprite[] rightFrames;
        [SerializeField] Sprite pizzaSprite;
        [SerializeField] Text scoreText;
        [SerializeField] Text livesText;
        [SerializeField] Text centerText;

        [SerializeField] int pointsPerPizza = 10;
        [SerializeField] int pointsToWin = 100;
        [SerializeField] int allowedMisses = 3;
        [SerializeField] float catcherSpeed = 1500f;
        [SerializeField] Vector2 pizzaSize = new Vector2(110f, 60f);

        readonly List<RectTransform> pizzas = new List<RectTransform>();
        readonly List<float> pizzaSpeeds = new List<float>();
        readonly Stack<RectTransform> pool = new Stack<RectTransform>();

        PlayerController player;
        ArcadeMachine machine;
        int score;
        int misses;
        float spawnTimer;
        float endTimer;
        bool finished;
        bool won;
        float animTimer;
        int animFrame;

        public bool IsOpen => player != null;

        // El panel empieza desactivado desde el constructor de la escena (sin Awake: se ejecutaría al abrirlo).

        public void Open(PlayerController p, ArcadeMachine m)
        {
            player = p;
            machine = m;
            player.Lock(this);
            player.SetInArcade(true);
            panel.SetActive(true);
            score = 0; misses = 0; spawnTimer = 0.8f; finished = false; won = false; endTimer = 0f;
            foreach (var pz in pizzas) Recycle(pz);
            pizzas.Clear(); pizzaSpeeds.Clear();
            catcher.anchoredPosition = new Vector2(0f, catcher.anchoredPosition.y);
            centerText.text = "¡Atrapa las pizzas!\nArrastra el dedo para moverte";
            RefreshTexts();
            AudioManager.PlaySfx(Sfx.UiClick);
        }

        /// <summary>Botón Salir (o fin de la partida).</summary>
        public void Close()
        {
            if (!IsOpen) return;
            foreach (var pz in pizzas) Recycle(pz);
            pizzas.Clear(); pizzaSpeeds.Clear();
            panel.SetActive(false);
            var p = player;
            player = null;
            p.SetInArcade(false);
            p.Unlock(this);
            if (machine != null) machine.Finished(won, p);
            machine = null;
        }

        void Update()
        {
            if (!IsOpen) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) { Close(); return; }
            if (gm.IsPaused) return;
            if (!PowerSystem.Instance.HasLight)
            {
                HudMessages.Show("Se cortó la luz: la máquina se apagó");
                Close();
                return;
            }
            float dt = Time.deltaTime;

            if (finished)
            {
                endTimer -= dt;
                if (endTimer <= 0f) Close();
                return;
            }

            float half = playArea.rect.width * 0.5f - catcher.rect.width * 0.5f;
            float dir = MoveInput(out float targetX, out bool hasTarget);
            Vector2 cp = catcher.anchoredPosition;
            float before = cp.x;
            if (hasTarget) cp.x = Mathf.MoveTowards(cp.x, targetX, catcherSpeed * dt);
            else cp.x += dir * catcherSpeed * dt;
            cp.x = Mathf.Clamp(cp.x, -half, half);
            catcher.anchoredPosition = cp;
            Animate(cp.x - before, dt);

            spawnTimer -= dt;
            if (spawnTimer <= 0f)
            {
                float progress = score / (float)pointsToWin;
                spawnTimer = Random.Range(0.55f, 1.25f) * Mathf.Lerp(1f, 0.6f, progress);
                Spawn(Mathf.Lerp(330f, 560f, progress) * Random.Range(0.9f, 1.15f));
            }

            Rect catchRect = RectIn(catcher);
            catchRect.yMin = catchRect.yMax - catchRect.height * 0.45f; // las manos/cabeza
            for (int i = pizzas.Count - 1; i >= 0; i--)
            {
                var pz = pizzas[i];
                var pos = pz.anchoredPosition;
                pos.y -= pizzaSpeeds[i] * dt;
                pz.anchoredPosition = pos;
                if (RectIn(pz).Overlaps(catchRect))
                {
                    score += pointsPerPizza;
                    AudioManager.PlaySfx(Sfx.Pickup);
                    Remove(i);
                    RefreshTexts();
                    if (score >= pointsToWin) Finish(true);
                }
                else if (pos.y < -playArea.rect.height * 0.5f - pizzaSize.y)
                {
                    misses++;
                    AudioManager.PlaySfx(Sfx.Denied);
                    Remove(i);
                    RefreshTexts();
                    if (misses >= allowedMisses) Finish(false);
                }
                if (finished) break;
            }
            if (centerText.text.Length > 0 && score > 0) centerText.text = "";
        }

        float MoveInput(out float targetX, out bool hasTarget)
        {
            targetX = 0f;
            hasTarget = false;
            Vector2 screen = Vector2.zero;
            bool pressed = false;
            if (Input.touchCount > 0) { screen = Input.GetTouch(0).position; pressed = true; }
            else if (Input.GetMouseButton(0)) { screen = Input.mousePosition; pressed = true; }
            if (pressed && RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea, screen, null, out Vector2 local)
                && local.y < playArea.rect.height * 0.5f)
            {
                targetX = local.x;
                hasTarget = true;
                return 0f;
            }
            float k = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) k -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) k += 1f;
            return k;
        }

        void Animate(float dx, float dt)
        {
            var frames = Mathf.Abs(dx) < 0.5f ? idleFrames : dx < 0f ? leftFrames : rightFrames;
            if (frames == null || frames.Length == 0) return;
            animTimer -= dt;
            if (animTimer <= 0f) { animTimer = 0.1f; animFrame++; }
            catcherImage.sprite = frames == idleFrames ? frames[0] : frames[animFrame % frames.Length];
        }

        void Spawn(float speed)
        {
            RectTransform pz;
            if (pool.Count > 0) { pz = pool.Pop(); pz.gameObject.SetActive(true); }
            else
            {
                var go = new GameObject("Pizza", typeof(RectTransform), typeof(Image));
                pz = (RectTransform)go.transform;
                pz.SetParent(playArea, false);
                pz.sizeDelta = pizzaSize;
                var img = go.GetComponent<Image>();
                img.sprite = pizzaSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
            float half = playArea.rect.width * 0.5f - pizzaSize.x * 0.5f;
            pz.anchoredPosition = new Vector2(Random.Range(-half, half), playArea.rect.height * 0.5f + pizzaSize.y);
            pizzas.Add(pz);
            pizzaSpeeds.Add(speed);
        }

        void Remove(int i)
        {
            Recycle(pizzas[i]);
            pizzas.RemoveAt(i);
            pizzaSpeeds.RemoveAt(i);
        }

        void Recycle(RectTransform pz)
        {
            pz.gameObject.SetActive(false);
            pool.Push(pz);
        }

        void Finish(bool victory)
        {
            finished = true;
            won = victory;
            endTimer = 1.8f;
            centerText.text = victory ? "¡GANASTE!\nPremio: un fusible" : "FIN DEL JUEGO\nSe te escaparon 3 pizzas";
            AudioManager.PlaySfx(victory ? Sfx.PowerOn : Sfx.Denied);
        }

        void RefreshTexts()
        {
            scoreText.text = $"PUNTOS: {score} / {pointsToWin}";
            livesText.text = "FALLOS: " + new string('X', misses) + new string('-', Mathf.Max(0, allowedMisses - misses));
        }

        Rect RectIn(RectTransform rt)
        {
            Vector2 size = rt.rect.size;
            return new Rect(rt.anchoredPosition - size * 0.5f, size);
        }
    }
}
