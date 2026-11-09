using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Alfredo
{
    /// <summary>Focos de colores del escenario (obj_luz_disco): cambian de color al ritmo.</summary>
    public class DiscoLight : MonoBehaviour
    {
        [SerializeField] Light2D light2D;
        [SerializeField] float interval = 0.5f;
        static readonly Color[] Colors = { Color.red, Color.green, Color.cyan, Color.magenta, Color.yellow, new Color(1f, 0.5f, 0f) };
        float timer;

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = interval;
            light2D.color = Colors[Random.Range(0, Colors.Length)];
        }
    }
}
