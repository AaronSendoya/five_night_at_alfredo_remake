using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Hace aparecer fusibles de repuesto cada cierto tiempo. Corrige el original, que podía
    /// ponerlos fuera de las zonas jugables: aquí siempre caen en suelo transitable de una zona.
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

        public void Setup(NightConfig config, PlayerController p)
        {
            interval = config.fuseSpawnSeconds;
            player = p;
            timer = interval * 0.5f; // el primer repuesto aparece antes
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying || interval <= 0f || template == null) return;
            timer += Time.deltaTime;
            if (timer < interval) return;
            timer = 0f;
            if (FusePickup.CountOnMap >= maxOnMap) return;

            var zones = MapZones.Instance.Zones;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var zone = zones[Random.Range(0, zones.Count)];
                if (!NavGrid.Instance.RandomPointInZone(zone, out Vector2 p)) continue;
                if (Vector2.Distance(p, player.Position) < minDistanceFromPlayer) continue;
                var pickup = Instantiate(template, p, Quaternion.identity, container);
                pickup.gameObject.SetActive(true);
                AudioManager.PlaySfx(Sfx.FuseAppear);
                return;
            }
        }
    }
}
