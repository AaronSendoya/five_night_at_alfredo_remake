using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>Susto de Shadow Alfredo: imagen que aparece un instante y la frase en pantalla mientras habla.</summary>
    public class ScreamerView : MonoBehaviour
    {
        [SerializeField] Image image;
        [SerializeField] Image tint;
        [SerializeField] Text phrase;
        [SerializeField] float imageSeconds = 0.9f;

        Coroutine running;

        void Awake()
        {
            image.gameObject.SetActive(false);
            tint.color = new Color(0.25f, 0f, 0.35f, 0f);
            phrase.text = "";
        }

        public void Show(Sprite sprite, string line, float voiceSeconds)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Routine(sprite, line, voiceSeconds));
        }

        IEnumerator Routine(Sprite sprite, string line, float voiceSeconds)
        {
            phrase.text = "\"" + line + "\"";
            var rt = image.rectTransform;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.gameObject.SetActive(true);
                for (float t = 0f; t < imageSeconds; t += Time.deltaTime)
                {
                    float k = t / imageSeconds;
                    rt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.25f, k);
                    rt.anchoredPosition = Random.insideUnitCircle * 18f;
                    image.color = new Color(1f, 1f, 1f, (Mathf.FloorToInt(t * 20f) % 3 == 0) ? 0.55f : 0.95f * (1f - k * 0.5f));
                    tint.color = new Color(0.25f, 0f, 0.35f, 0.45f * (1f - k));
                    yield return null;
                }
                image.gameObject.SetActive(false);
                tint.color = new Color(0.25f, 0f, 0.35f, 0f);
            }
            float rest = Mathf.Max(1.5f, voiceSeconds - imageSeconds);
            for (float t = 0f; t < rest; t += Time.deltaTime)
            {
                phrase.color = new Color(0.85f, 0.6f, 1f, Mathf.Clamp01((rest - t) * 2f));
                yield return null;
            }
            phrase.text = "";
            running = null;
        }
    }
}
