using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Ruidos lejanos del edificio (crujido de madera, golpeteo en una tubería, un golpe sordo) que suenan de vez en
    /// cuando por un lado u otro. Solo dan ambiente: son distintos a los sonidos de movimiento de los animatrónicos
    /// (que son los que sirven para detectarlos) y con el apagón se hacen un poco más frecuentes.
    /// </summary>
    public class AmbientEvents : MonoBehaviour
    {
        static readonly Sfx[] Pool = { Sfx.Amb1, Sfx.Amb2, Sfx.Amb3 };

        [SerializeField] float minSeconds = 28f;
        [SerializeField] float maxSeconds = 60f;

        float timer;
        int last = -1;

        void Start() => timer = Random.Range(minSeconds * 0.5f, maxSeconds * 0.5f);

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            bool blackout = PowerSystem.Instance != null && !PowerSystem.Instance.HasLight;
            timer = Random.Range(minSeconds, maxSeconds) * (blackout ? 0.7f : 1f);

            int i = Random.Range(0, Pool.Length);
            if (i == last) i = (i + 1) % Pool.Length;
            last = i;
            AudioManager.PlayAmbientEvent(Pool[i], Random.Range(0.35f, 0.8f), Random.Range(-0.9f, 0.9f));
        }
    }
}
