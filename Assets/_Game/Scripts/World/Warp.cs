using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    /// <summary>Paso entre zonas (puerta). Quien entra en el área aparece en el destino.</summary>
    public class Warp : MonoBehaviour
    {
        public static readonly List<Warp> All = new List<Warp>();

        public Rect area;
        public Vector2 destination;
        public Facing arrivalFacing = Facing.Up;

        ZoneInfo fromZone, toZone;

        public ZoneInfo FromZone => fromZone ??= MapZones.Instance.NearestZone(area.center);
        public ZoneInfo ToZone => toZone ??= MapZones.Instance.NearestZone(destination);

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public static Warp At(Vector2 point)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].area.Contains(point)) return All[i];
            return null;
        }

        /// <summary>Paso que se superpone con un rectángulo (el cuerpo del jugador, como la colisión del original).</summary>
        public static Warp Overlapping(Rect body)
        {
            for (int i = 0; i < All.Count; i++)
                if (All[i].area.Overlaps(body)) return All[i];
            return null;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.6f);
            Gizmos.DrawCube(area.center, area.size);
            Gizmos.DrawLine(area.center, destination);
            Gizmos.DrawWireSphere(destination, 0.5f);
        }
    }

    /// <summary>Ruta entre zonas: qué paso tomar para llegar de una zona a otra (búsqueda en anchura).</summary>
    public static class ZoneRouter
    {
        public static Warp NextWarp(ZoneInfo from, ZoneInfo to)
        {
            if (from == null || to == null || from == to) return null;
            var firstStep = new Dictionary<ZoneInfo, Warp>();
            var queue = new Queue<ZoneInfo>();
            var visited = new HashSet<ZoneInfo> { from };
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var zone = queue.Dequeue();
                foreach (var w in Warp.All)
                {
                    if (w.FromZone != zone || w.ToZone == null || visited.Contains(w.ToZone)) continue;
                    visited.Add(w.ToZone);
                    firstStep[w.ToZone] = zone == from ? w : firstStep[zone];
                    if (w.ToZone == to) return firstStep[w.ToZone];
                    queue.Enqueue(w.ToZone);
                }
            }
            return null;
        }
    }
}
