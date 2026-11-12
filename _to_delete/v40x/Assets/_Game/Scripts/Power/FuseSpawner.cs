using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Hace aparecer fusibles de repuesto cada cierto tiempo. Corrige el original, que podía
    /// ponerlos fuera de las zonas jugables: aquí siempre caen en suelo transitable de una zona.
    /// Cuando el mapa ya está lleno no se salta el turno: el fusible más viejo que no esté en tu zona se
    /// cambia de sitio, así los repuestos no se quedan atascados en zonas que nadie visita. Algunas noches
    /// excluyen zonas (NightConfig.fuseExcludedZones).
    /// </summary>
    public class FuseSpawner : MonoBehaviour
    {
        [SerializeField] FusePickup template;
        [SerializeField] Transform container;
        [SerializeField] int maxOnMap = 4;
        [SerializeField] float minDistanceFromPlayer = 10f;

        float interval;
        float timer;
        PlayerController player;
        readonly HashSet<string> excludedZones = new HashSet<string>();
        readonly List<ZoneInfo> candidates = new List<ZoneInfo>();

        public void Setup(NightConfig config, PlayerController p)
        {
            interval = config.fuseSpawnSeconds;
            player = p;
            timer = interval * 0.5f; // el primer repuesto aparece antes
            excludedZones.Clear();
            if (config.fuseExcludedZones != null)
                foreach (var z in config.fuseExcludedZones) excludedZones.Add(z);
            Log($"Noche {config.night}: cada {interval} s, zonas sin fusibles: {(excludedZones.Count == 0 ? "ninguna" : string.Join(", ", excludedZones))}");
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying || interval <= 0f || template == null) return;
            timer += Time.deltaTime;
            if (timer < interval) return;
            timer = 0f;
            Tick();
        }

        void Tick()
        {
            if (FusePickup.CountOnMap >= maxOnMap)
            {
                Rotate();
                return;
            }
            if (!TryFindSpot(null, out Vector2 p)) return;
            var pickup = Instantiate(template, p, Quaternion.identity, container);
            pickup.gameObject.SetActive(true);
            AudioManager.PlaySfx(Sfx.FuseAppear);
            Log($"Nuevo fusible en {ZoneName(p)} ({FusePickup.CountOnMap} en el mapa)");
        }

        /// <summary>Mapa lleno: el fusible más viejo que no esté en la zona del jugador (esa zona se ve entera en pantalla) pasa a otro sitio.</summary>
        void Rotate()
        {
            var zones = MapZones.Instance;
            var playerZone = zones.NearestZone(player.Position);
            FusePickup oldest = null;
            foreach (var f in FusePickup.Active)
            {
                if (f == null || zones.NearestZone(f.transform.position) == playerZone) continue;
                if (oldest == null || f.SpawnTime < oldest.SpawnTime) oldest = f;
            }
            if (oldest == null) { Log("Mapa lleno y todos los fusibles están en tu zona: no se cambia ninguno"); return; }
            var from = zones.NearestZone(oldest.transform.position);
            if (!TryFindSpot(from, out Vector2 p)) return;
            Log($"Mapa lleno: el fusible más viejo (en {from?.name}, {Time.time - oldest.SpawnTime:0} s) pasa a {ZoneName(p)}");
            oldest.MoveTo(p);
            AudioManager.PlaySfx(Sfx.FuseAppear);
        }

        /// <summary>Punto transitable al azar en una zona permitida (distinta de "avoid"), lejos del jugador.</summary>
        bool TryFindSpot(ZoneInfo avoid, out Vector2 point)
        {
            candidates.Clear();
            foreach (var z in MapZones.Instance.Zones)
                if (!excludedZones.Contains(z.name) && z != avoid) candidates.Add(z);
            point = default;
            if (candidates.Count == 0) return false;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var zone = candidates[Random.Range(0, candidates.Count)];
                if (!NavGrid.Instance.RandomPointInZone(zone, out Vector2 p)) continue;
                if (Vector2.Distance(p, player.Position) < minDistanceFromPlayer) continue;
                point = p;
                return true;
            }
            return false;
        }

        static string ZoneName(Vector2 p) => MapZones.Instance.NearestZone(p)?.name ?? "?";

        [Conditional("UNITY_EDITOR")]
        static void Log(string message) => UnityEngine.Debug.Log("[Fusibles] " + message);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>Prueba: fuerza un turno de aparición sin esperar el intervalo (tecla F11).</summary>
        public void DebugTick()
        {
            if (template == null || player == null) return;
            Tick();
        }
#endif
    }
}
