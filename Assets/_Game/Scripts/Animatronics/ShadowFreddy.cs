using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Alfredo
{
    /// <summary>
    /// Shadow Alfredo: siempre quieto en el cuarto secreto y nunca atrapa al jugador. Solo despierta
    /// cuando quedan 2 fusibles o menos en el panel: entonces, cada cierto tiempo, lanza un susto
    /// (imagen) y una frase amenazante. Al reparar la luz vuelve a dormir.
    /// </summary>
    public class ShadowFreddy : MonoBehaviour
    {
        public static bool IsActive { get; private set; }

        [SerializeField] SpriteRenderer body;
        [SerializeField] Light2D eyes;
        [SerializeField] ScreamerView view;
        [SerializeField] Sprite[] screamers;
        [SerializeField] AudioClip[] voices;
        [SerializeField] string[] lines;
        [SerializeField] AudioClip screamSound;
        [SerializeField] float firstDelay = 10f;
        [SerializeField] Vector2 intervalSeconds = new Vector2(24f, 40f);

        float timer;
        int lastLine = -1;

        void OnDestroy() => IsActive = false;

        void Start()
        {
            IsActive = false;
            if (eyes != null) eyes.intensity = 0f;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) return;
            bool should = PowerSystem.Instance.ActiveFuses <= 2;
            if (should != IsActive)
            {
                IsActive = should;
                timer = firstDelay;
            }
            if (eyes != null) eyes.intensity = IsActive ? 0.8f + Mathf.Sin(Time.time * 3f) * 0.3f : 0f;
            if (body != null) body.color = IsActive && Random.value < 0.03f ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
            if (!IsActive) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Random.Range(intervalSeconds.x, intervalSeconds.y);
            Haunt();
        }

        void Haunt()
        {
            if (lines == null || lines.Length == 0) return;
            int i;
            do i = Random.Range(0, lines.Length); while (lines.Length > 1 && i == lastLine);
            lastLine = i;
            Sprite s = screamers != null && screamers.Length > 0 ? screamers[Random.Range(0, screamers.Length)] : null;
            AudioClip voice = voices != null && i < voices.Length ? voices[i] : null;
            if (screamSound != null) AudioManager.PlayClip(screamSound, 0.8f);
            if (voice != null) AudioManager.PlayClip(voice, 1f);
            if (view != null) view.Show(s, lines[i], voice != null ? voice.length : 4f);
        }
    }
}
