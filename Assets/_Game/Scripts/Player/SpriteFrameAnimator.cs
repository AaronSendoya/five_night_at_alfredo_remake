using UnityEngine;

namespace Alfredo
{
    /// <summary>Reproduce los cuadros de un sprite del original sin necesidad de un Animator.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        public float fps = 8f;

        SpriteRenderer sr;
        Sprite[] frames;
        int index;
        float time;
        bool playing;

        public SpriteRenderer Renderer => sr != null ? sr : sr = GetComponent<SpriteRenderer>();

        public void SetFrames(Sprite[] newFrames)
        {
            if (newFrames == null || newFrames.Length == 0 || newFrames == frames) return;
            frames = newFrames;
            index = Mathf.Min(index, frames.Length - 1);
            Renderer.sprite = frames[index];
        }

        public void Play(bool play)
        {
            if (play == playing) return;
            playing = play;
            if (!play)
            {
                index = 0;
                time = 0f;
                if (frames != null && frames.Length > 0) Renderer.sprite = frames[0];
            }
        }

        void Update()
        {
            if (!playing || frames == null || frames.Length < 2) return;
            time += Time.deltaTime * fps;
            while (time >= 1f)
            {
                time -= 1f;
                index = (index + 1) % frames.Length;
                Renderer.sprite = frames[index];
            }
        }
    }
}
