using System.Collections;
using UnityEngine;

namespace Alfredo
{
    /// <summary>Fundido a negro para pasos entre zonas, capturas y cambios de escena.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }
        CanvasGroup group;

        void Awake()
        {
            Instance = this;
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public IEnumerator FadeOut(float seconds) => Fade(1f, seconds);
        public IEnumerator FadeIn(float seconds) => Fade(0f, seconds);

        IEnumerator Fade(float target, float seconds)
        {
            group.blocksRaycasts = true;
            float start = group.alpha;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, t / seconds);
                yield return null;
            }
            group.alpha = target;
            group.blocksRaycasts = target > 0.5f;
        }
    }
}
