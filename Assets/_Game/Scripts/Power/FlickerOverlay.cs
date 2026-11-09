using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Falla de luz de Mantenimiento (obj_luz_mantenimiento): una capa oscura sobre la zona que parpadea
    /// de forma brusca. El original fallaba el 30 % de los cuadros con negro casi total; aquí se suaviza
    /// (20 %, oscuridad 0.6 a 0.85) para no producir un estroboscopio molesto en pantallas de móvil.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FlickerOverlay : MonoBehaviour
    {
        [SerializeField, Range(0, 1)] float failChance = 0.2f;
        [SerializeField] float ticksPerSecond = 15f;
        SpriteRenderer sr;
        float timer;

        void Awake() => sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.IsPaused) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = 1f / ticksPerSecond;
            float a;
            if (Random.value < failChance) { float[] dark = { 0.6f, 0.75f, 0.85f }; a = dark[Random.Range(0, 3)]; }
            else a = Random.value < 0.25f ? 0.1f : 0f;
            var c = sr.color; c.a = a; sr.color = c;
        }
    }
}
