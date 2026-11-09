using UnityEngine;

namespace Alfredo
{
    /// <summary>Sonidos sencillos generados por código para cubrir los que faltan en el original.</summary>
    public static class ProceduralAudio
    {
        const int Rate = 44100;

        static AudioClip Make(string name, float seconds, System.Func<float, float> wave)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Env(float t, float len) => Mathf.Clamp01(t / 0.004f) * Mathf.Exp(-5f * t / len);

        public static AudioClip Blip(float freq, float len) =>
            Make("blip", len, t => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.35f * Env(t, len));

        public static AudioClip Rise(float from, float to, float len) =>
            Make("rise", len, t =>
            {
                float f = Mathf.Lerp(from, to, t / len);
                return Mathf.Sin(2f * Mathf.PI * f * t) * 0.4f * Env(t, len);
            });

        public static AudioClip Clunk(float len)
        {
            var rng = new System.Random(7);
            float lp = 0f;
            return Make("clunk", len, t =>
            {
                lp += (((float)rng.NextDouble() * 2f - 1f) - lp) * 0.08f;
                return (Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.6f + lp * 1.5f) * Env(t, len * 0.6f);
            });
        }

        public static AudioClip Zap(float len)
        {
            var rng = new System.Random(3);
            return Make("zap", len, t =>
            {
                float crackle = rng.NextDouble() < 0.12 ? (float)rng.NextDouble() * 2f - 1f : 0f;
                float buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 60f * t)) * 0.25f;
                return (crackle + buzz) * Env(t, len);
            });
        }

        /// <summary>Zumbido grave en bucle (frecuencias con ciclos enteros para que el bucle no haga clic).</summary>
        public static AudioClip Drone(float len)
        {
            var rng = new System.Random(11);
            float noise = 0f;
            return Make("drone", len, t =>
            {
                noise += (((float)rng.NextDouble() * 2f - 1f) - noise) * 0.02f;
                float lfo = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
                float tone = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 82.5f * t) * 0.25f;
                return (tone * lfo + noise * 0.6f) * 0.6f;
            });
        }
    }
}
