using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    [Serializable]
    public class ZoneInfo
    {
        public string name;
        public Rect rect;
    }

    /// <summary>Las 8 zonas del mapa (islas separadas que se conectan con pasos).</summary>
    public class MapZones : MonoBehaviour
    {
        public static MapZones Instance { get; private set; }

        [SerializeField] List<ZoneInfo> zones = new List<ZoneInfo>();
        public IReadOnlyList<ZoneInfo> Zones => zones;

        void Awake() => Instance = this;

        public void SetZones(List<ZoneInfo> list) => zones = list;

        public ZoneInfo ZoneAt(Vector2 point)
        {
            foreach (var z in zones)
                if (z.rect.Contains(point)) return z;
            return null;
        }

        /// <summary>Zona que contiene el punto o, si cae fuera, la más cercana.</summary>
        public ZoneInfo NearestZone(Vector2 point)
        {
            var inside = ZoneAt(point);
            if (inside != null) return inside;
            ZoneInfo best = null;
            float bestDist = float.MaxValue;
            foreach (var z in zones)
            {
                Vector2 clamped = new Vector2(Mathf.Clamp(point.x, z.rect.xMin, z.rect.xMax), Mathf.Clamp(point.y, z.rect.yMin, z.rect.yMax));
                float d = (clamped - point).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = z; }
            }
            return best;
        }

        public ZoneInfo ByName(string zoneName) => zones.Find(z => z.name == zoneName);

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            foreach (var z in zones) Gizmos.DrawWireCube(z.rect.center, z.rect.size);
        }
    }
}
