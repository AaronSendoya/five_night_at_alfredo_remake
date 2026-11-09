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
            cam.orthographicSize = size;
            float halfW = size * aspect, halfH = size;
            Vector2 focus = overrideZone != null ? r.center : player.Position + Vector2.up * 2.5f;
            Vector2 target = new Vector2(
                Mathf.Clamp(focus.x, r.xMin + halfW, r.xMax - halfW),
                Mathf.Clamp(focus.y, r.yMin + halfH, r.yMax - halfH));
            Vector2 pos = Vector2.Lerp(transform.position, target, blend);
            transform.position = new Vector3(pos.x, pos.y, -10f);
        }
    }
}
