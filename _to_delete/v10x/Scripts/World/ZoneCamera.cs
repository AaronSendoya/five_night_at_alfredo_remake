using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Cámara por zonas, como el original: muestra la zona donde está el jugador. En móvil se ajusta
    /// para llenar la pantalla sin franjas negras y sigue al jugador dentro de la zona.
    /// El monitor de cámaras puede tomar el control para mostrar otra zona.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ZoneCamera : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] float followSharpness = 8f;
        [SerializeField] float maxOrthoSize = 24f;
        [Header("Monitor de cámaras")]
        [Tooltip("Acercamiento en las cámaras de seguridad para que haya margen para el paneo")]
        [SerializeField] float monitorZoom = 0.8f;
        [Tooltip("Segundos de un barrido completo izquierda -> derecha -> izquierda")]
        [SerializeField] float panPeriod = 18f;

        float panStart;
        Vector2 lastJitter;
        /// <summary>Sacudida momentánea (interferencias del monitor), en unidades.</summary>
        public Vector2 Jitter { get; set; }

        Camera cam;
        ZoneInfo currentZone;
        ZoneInfo overrideZone;

        public ZoneInfo ShownZone => overrideZone ?? currentZone;

        void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        void Start()
        {
            player.Warped += _ => Snap();
            Snap();
        }

        public void ShowZone(ZoneInfo zone)
        {
            if (zone != overrideZone) panStart = Time.unscaledTime;
            overrideZone = zone;
            Snap();
        }

        public void ClearOverride()
        {
            overrideZone = null;
            Snap();
        }

        public void Snap()
        {
            currentZone = MapZones.Instance.NearestZone(player.Position);
            Apply(1f);
        }

        void LateUpdate()
        {
            if (overrideZone == null)
            {
                var z = MapZones.Instance.NearestZone(player.Position);
                if (z != currentZone) { currentZone = z; Apply(1f); return; }
            }
            Apply(1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime));
        }

        void Apply(float blend)
        {
            var zone = ShownZone;
            if (zone == null) return;
            Rect r = zone.rect;
            float aspect = cam.aspect;
            float size = Mathf.Min(maxOrthoSize, r.height * 0.5f, r.width * 0.5f / aspect);
            if (overrideZone != null) size *= monitorZoom;
            cam.orthographicSize = size;
            float halfW = size * aspect, halfH = size;
            Vector2 focus = player.Position + Vector2.up * 2.5f;
            if (overrideZone != null)
            {
                // Cámara de seguridad: barre despacio de izquierda a derecha y vuelve, con una pausa en cada extremo.
                float ph = (Time.unscaledTime - panStart) / Mathf.Max(1f, panPeriod);
                float k = Mathf.Clamp(-Mathf.Cos(ph * Mathf.PI * 2f) * 1.25f, -1f, 1f);
                float range = Mathf.Max(0f, r.width * 0.5f - halfW);
                focus = new Vector2(r.center.x + k * range, r.center.y);
                if (blend < 1f) blend = Mathf.Max(blend, 0.25f);
            }
            Vector2 target = new Vector2(
                Mathf.Clamp(focus.x, r.xMin + halfW, r.xMax - halfW),
                Mathf.Clamp(focus.y, r.yMin + halfH, r.yMax - halfH));
            Vector2 pos = Vector2.Lerp((Vector2)transform.position - lastJitter, target, blend);
            lastJitter = overrideZone != null ? Jitter : Vector2.zero;
            pos += lastJitter;
            transform.position = new Vector3(pos.x, pos.y, -10f);
        }
    }
}
