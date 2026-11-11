using UnityEngine;

namespace Alfredo
{
    /// <summary>Reproduce los cuadros de un sprite del original sin necesidad de un Animator.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameAnimator : MonoBehaviour
    {
        public float fps = 8f;
        [Tooltip("Con 3 cuadros (parado, paso izq., paso der.) usa 0-1-0-2: un ciclo de caminata natural en vez de 0-1-2")]
        public bool walkCycleForThree = true;

        static readonly int[] ThreeCycle = { 0, 1, 0, 2 };
        int step;

        SpriteRenderer sr;
        Sprite[] frames;
        int index;
        float time;
        bool playing;

        /// <summary>Cambió el juego de cuadros (p. ej. otra dirección).</summary>
        public event System.Action FramesChanged;

        public SpriteRenderer Renderer => sr != null ? sr : sr = GetComponent<SpriteRenderer>();

        public void SetFrames(Sprite[] newFrames)
        {
            if (newFrames == null || newFrames.Length == 0 || newFrames == frames) return;
            bool had = frames != null;
            frames = newFrames;
            if (had) FramesChanged?.Invoke();
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
                step = 0;
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
                if (walkCycleForThree && frames.Length == 3)
                {
                    step = (step + 1) % ThreeCycle.Length;
                    index = ThreeCycle[step];
                }
                else index = (index + 1) % frames.Length;
                Renderer.sprite = frames[index];
            }
        }
    }
}
