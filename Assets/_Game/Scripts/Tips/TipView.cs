using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>Franja de consejos (abajo al centro). Muestra uno a la vez, con una pequeña cola.</summary>
    public class TipView : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Text tipText;
        [SerializeField] float seconds = 7f;

        readonly Queue<string> queue = new Queue<string>();
        float timer;

        void Awake()
        {
            HudTips.Shown += Enqueue;
            group.alpha = 0f;
        }

        void OnDestroy() => HudTips.Shown -= Enqueue;

        void Enqueue(string text)
        {
            if (queue.Count >= 2) queue.Dequeue();
            queue.Enqueue(text);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (timer > 0f)
            {
                timer -= dt;
                group.alpha = Mathf.Clamp01(Mathf.Min(timer, seconds - timer) * 3f);
                return;
            }
            group.alpha = 0f;
            if (queue.Count == 0) return;
            tipText.text = queue.Dequeue();
            timer = seconds;
            AudioManager.PlaySfx(Sfx.Tip);
        }
    }
}
