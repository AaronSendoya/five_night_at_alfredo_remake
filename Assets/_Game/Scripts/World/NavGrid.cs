using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Cuadrícula de 32 px para las rutas de los animatrónicos (como mp_grid del original), compartida por todos.
    /// Una celda es transitable si está dentro de una zona y no la ocupa una pared u obstáculo.
    /// Las celdas pegadas a una pared cuestan más, así las rutas no rozan los muros.
    /// </summary>
    public class NavGrid : MonoBehaviour
    {
        public static NavGrid Instance { get; private set; }

        [SerializeField] int widthCells = 188;
        [SerializeField] int heightCells = 207;
        [SerializeField] LayerMask blockingLayers;
        [SerializeField] float wallPenalty = 3f;
        [SerializeField] int maxExpandedNodes = 12000;

        bool built;
        bool[] walkable;
        float[] extraCost;
        float[] gScore;
        int[] parent;
        int[] visitStamp;
        int[] closedStamp;
        int stamp;
        readonly MinHeap open = new MinHeap(1024);
        readonly List<int> cellPath = new List<int>(256);

        const float CellSize = 1f; // 32 px = 1 unidad

        void Awake() => Instance = this;

        void EnsureBuilt()
        {
            if (built) return;
            built = true;
            Physics2D.SyncTransforms();
            int n = widthCells * heightCells;
            walkable = new bool[n];
            extraCost = new float[n];
            gScore = new float[n];
            parent = new int[n];
            visitStamp = new int[n];
            closedStamp = new int[n];
            var zones = MapZones.Instance;
            Vector2 probe = new Vector2(CellSize * 0.9f, CellSize * 0.9f);
            for (int y = 0; y < heightCells; y++)
                for (int x = 0; x < widthCells; x++)
                {
                    Vector2 c = CellCenter(x, y);
                    bool inZone = zones != null && zones.ZoneAt(c) != null;
                    walkable[y * widthCells + x] = inZone && Physics2D.OverlapBox(c, probe, 0f, blockingLayers) == null;
                }
            for (int y = 0; y < heightCells; y++)
                for (int x = 0; x < widthCells; x++)
                {
                    int i = y * widthCells + x;
                    if (!walkable[i]) continue;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (!IsWalkableCell(x + dx, y + dy)) { extraCost[i] = wallPenalty; dy = 2; break; }
                }
        }

        // La celda (0,0) es la esquina superior izquierda del mapa original; y crece hacia abajo.
        public Vector2 CellCenter(int x, int y) => new Vector2(x * CellSize + CellSize * 0.5f, -(y * CellSize + CellSize * 0.5f));

        public bool WorldToCell(Vector2 p, out int x, out int y)
        {
            x = Mathf.FloorToInt(p.x / CellSize);
            y = Mathf.FloorToInt(-p.y / CellSize);
            return x >= 0 && y >= 0 && x < widthCells && y < heightCells;
        }

        bool IsWalkableCell(int x, int y) => x >= 0 && y >= 0 && x < widthCells && y < heightCells && walkable[y * widthCells + x];

        public bool IsWalkable(Vector2 p)
        {
            EnsureBuilt();
            return WorldToCell(p, out int x, out int y) && walkable[y * widthCells + x];
        }

        bool NearestWalkable(Vector2 p, int maxRadius, out int cx, out int cy)
        {
            WorldToCell(p, out cx, out cy);
            cx = Mathf.Clamp(cx, 0, widthCells - 1);
            cy = Mathf.Clamp(cy, 0, heightCells - 1);
            if (IsWalkableCell(cx, cy)) return true;
            for (int r = 1; r <= maxRadius; r++)
            {
                float best = float.MaxValue; int bx = -1, by = -1;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        if (!IsWalkableCell(cx + dx, cy + dy)) continue;
                        float d = (CellCenter(cx + dx, cy + dy) - p).sqrMagnitude;
                        if (d < best) { best = d; bx = cx + dx; by = cy + dy; }
                    }
                if (bx >= 0) { cx = bx; cy = by; return true; }
            }
            return false;
        }

        /// <summary>Ruta A* (8 direcciones, sin cortar esquinas) dentro de una misma zona. El resultado termina en el destino exacto.</summary>
        public bool FindPath(Vector2 from, Vector2 to, List<Vector2> result)
        {
            EnsureBuilt();
            result.Clear();
            if (!NearestWalkable(from, 4, out int sx, out int sy)) return false;
            if (!NearestWalkable(to, 4, out int tx, out int ty)) return false;
            int start = sy * widthCells + sx, goal = ty * widthCells + tx;

            stamp++;
            open.Clear();
            gScore[start] = 0f; parent[start] = -1; visitStamp[start] = stamp;
            open.Push(start, Heuristic(sx, sy, tx, ty));
            int expanded = 0;
            bool found = false;
            while (open.Count > 0)
            {
                int cur = open.Pop();
                if (closedStamp[cur] == stamp) continue;
                closedStamp[cur] = stamp;
                if (cur == goal) { found = true; break; }
                if (++expanded > maxExpandedNodes) break;
                int cx = cur % widthCells, cy = cur / widthCells;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx, ny = cy + dy;
                        if (!IsWalkableCell(nx, ny)) continue;
                        if (dx != 0 && dy != 0 && (!IsWalkableCell(cx + dx, cy) || !IsWalkableCell(cx, cy + dy))) continue;
                        int ni = ny * widthCells + nx;
                        if (closedStamp[ni] == stamp) continue;
                        float step = (dx != 0 && dy != 0 ? 1.4142f : 1f) + extraCost[ni];
                        float g = gScore[cur] + step;
                        if (visitStamp[ni] == stamp && g >= gScore[ni]) continue;
                        visitStamp[ni] = stamp; gScore[ni] = g; parent[ni] = cur;
                        open.Push(ni, g + Heuristic(nx, ny, tx, ty));
                    }
            }
            if (!found) return false;

            cellPath.Clear();
            for (int c = goal; c != -1; c = parent[c]) cellPath.Add(c);
            cellPath.Reverse();

            // Suavizado: se salta celdas mientras haya línea recta transitable.
            int anchor = 0;
            for (int i = 2; i < cellPath.Count; i++)
            {
                if (!LineWalkable(cellPath[anchor], cellPath[i]))
                {
                    anchor = i - 1;
                    result.Add(CellCenter(cellPath[anchor] % widthCells, cellPath[anchor] / widthCells));
                }
            }
            if (cellPath.Count > 1) result.Add(CellCenter(tx, ty));
            if ((to - CellCenter(tx, ty)).sqrMagnitude < 4f * 4f) result.Add(to);
            if (result.Count == 0) result.Add(to);
            return true;
        }

        static float Heuristic(int ax, int ay, int bx, int by)
        {
            int dx = Mathf.Abs(ax - bx), dy = Mathf.Abs(ay - by);
            return (dx + dy) + (1.4142f - 2f) * Mathf.Min(dx, dy);
        }

        bool LineWalkable(int a, int b)
        {
            int x0 = a % widthCells, y0 = a / widthCells, x1 = b % widthCells, y1 = b / widthCells;
            int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                int i = y0 * widthCells + x0;
                if (!walkable[i]) return false;
                if (x0 == x1 && y0 == y1) return true;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        /// <summary>Punto transitable al azar dentro de una zona.</summary>
        public bool RandomPointInZone(ZoneInfo zone, out Vector2 point, int attempts = 40)
        {
            EnsureBuilt();
            point = default;
            if (zone == null) return false;
            for (int i = 0; i < attempts; i++)
            {
                Vector2 p = new Vector2(Random.Range(zone.rect.xMin, zone.rect.xMax), Random.Range(zone.rect.yMin, zone.rect.yMax));
                if (WorldToCell(p, out int x, out int y) && walkable[y * widthCells + x] && extraCost[y * widthCells + x] == 0f)
                {
                    point = CellCenter(x, y);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Punto transitable al azar a cierta distancia, dentro de la misma zona.</summary>
        public bool RandomPointNear(Vector2 origin, float minDist, float maxDist, out Vector2 point, int attempts = 12)
        {
            EnsureBuilt();
            point = default;
            var zone = MapZones.Instance.NearestZone(origin);
            for (int i = 0; i < attempts; i++)
            {
                Vector2 p = origin + Random.insideUnitCircle.normalized * Random.Range(minDist, maxDist);
                if (zone != null && !zone.rect.Contains(p)) continue;
                if (WorldToCell(p, out int x, out int y) && walkable[y * widthCells + x])
                {
                    point = CellCenter(x, y);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Cola de prioridad mínima simple para A*.</summary>
        class MinHeap
        {
            int[] items; float[] keys; public int Count;
            public MinHeap(int capacity) { items = new int[capacity]; keys = new float[capacity]; }
            public void Clear() => Count = 0;
            public void Push(int item, float key)
            {
                if (Count == items.Length) { System.Array.Resize(ref items, Count * 2); System.Array.Resize(ref keys, Count * 2); }
                int i = Count++;
                while (i > 0)
                {
                    int p = (i - 1) / 2;
                    if (keys[p] <= key) break;
                    items[i] = items[p]; keys[i] = keys[p]; i = p;
                }
                items[i] = item; keys[i] = key;
            }
            public int Pop()
            {
                int top = items[0];
                int lastItem = items[--Count]; float lastKey = keys[Count];
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1;
                    if (l >= Count) break;
                    int r = l + 1;
                    int m = r < Count && keys[r] < keys[l] ? r : l;
                    if (keys[m] >= lastKey) break;
                    items[i] = items[m]; keys[i] = keys[m]; i = m;
                }
                items[i] = lastItem; keys[i] = lastKey;
                return top;
            }
        }
    }
}
