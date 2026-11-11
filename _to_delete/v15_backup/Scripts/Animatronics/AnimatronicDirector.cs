using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Coordina a los animatrónicos: configura la noche, limita cuántos persiguen a la vez
    /// (max_chases existía en el original pero nunca se usaba) y los devuelve al escenario tras una captura.
    /// </summary>
    public class AnimatronicDirector : MonoBehaviour
    {
        [SerializeField] List<AnimatronicBrain> animatronics = new List<AnimatronicBrain>();

        readonly HashSet<AnimatronicBrain> chasers = new HashSet<AnimatronicBrain>();
        readonly Dictionary<ZoneInfo, float> lastVisit = new Dictionary<ZoneInfo, float>();
        readonly Dictionary<AnimatronicBrain, ZoneInfo> roamTargets = new Dictionary<AnimatronicBrain, ZoneInfo>();
        int maxChases = 2;
        PlayerController player;

        public IReadOnlyList<AnimatronicBrain> All => animatronics;
        public int ActiveChases => chasers.Count;

        public void Setup(NightConfig night, PlayerController player)
        {
            maxChases = Mathf.Max(1, night.maxChases);
            if (this.player != player)
            {
                if (this.player != null) this.player.Warped -= OnPlayerWarped;
                if (player != null) player.Warped += OnPlayerWarped;
            }
            this.player = player;
            chasers.Clear();
            roamTargets.Clear();
            lastVisit.Clear();
            foreach (var a in animatronics) a.Configure(player, this, night);
        }

        public bool RequestChase(AnimatronicBrain who)
        {
            if (chasers.Contains(who)) return true;
            if (chasers.Count >= maxChases) return false;
            chasers.Add(who);
            return true;
        }

        public void ReleaseChase(AnimatronicBrain who) => chasers.Remove(who);

        [Tooltip("Segundos sin poder atacar para los animatrónicos que están junto a la puerta por la que entras")]
        [SerializeField] float doorSafeSeconds = 1.5f;
        [SerializeField] float doorSafeRadius = 8f;

        void OnPlayerWarped(ZoneInfo zone)
        {
            if (player == null) return;
            var zones = MapZones.Instance;
            foreach (var a in animatronics)
            {
                if (!a.IsAwake) continue;
                if (zones != null && zones.NearestZone(a.Motor.Position) != zone) continue;
                if (Vector2.Distance(a.Motor.Position, player.Position) > doorSafeRadius) continue;
                a.HoldOff(player.Position, doorSafeSeconds);
            }
        }

        public void ResetAll(float grace)
        {
            chasers.Clear();
            roamTargets.Clear();
            // Escalonados: si salen todos a la vez del escenario terminan recorriendo juntos las mismas salas.
            for (int i = 0; i < animatronics.Count; i++)
                animatronics[i].ResetToHome(grace + i * 3f + Random.Range(0f, 2.5f));
        }

        void Update()
        {
            // Registro de qué salas se han visitado hace poco (para repartir las patrullas).
            var zones = MapZones.Instance;
            if (zones == null) return;
            foreach (var a in animatronics)
                if (a.IsActive) { var z = zones.NearestZone(a.Motor.Position); if (z != null) lastVisit[z] = Time.time; }
        }

        /// <summary>Cuántos animatrónicos activos (sin contar a 'except') están en la zona o van hacia ella.</summary>
        public int Occupancy(ZoneInfo zone, AnimatronicBrain except)
        {
            int n = 0;
            var zones = MapZones.Instance;
            foreach (var a in animatronics)
            {
                if (a == except || !a.IsActive) continue;
                if (zones.NearestZone(a.Motor.Position) == zone) n++;
                else if (roamTargets.TryGetValue(a, out var t) && t == zone) n++;
            }
            return n;
        }

        /// <summary>
        /// Elige la próxima sala a recorrer: prefiere las que nadie visita hace rato y las que no tienen
        /// a otro animatrónico; con más IA, se inclina hacia la zona del jugador (la presión crece en la noche).
        /// </summary>
        public ZoneInfo PickRoamZone(AnimatronicBrain who, ZoneInfo here)
        {
            var zones = MapZones.Instance;
            ZoneInfo best = null;
            float bestScore = float.NegativeInfinity;
            var playerZone = player != null && !player.IsUndetectable ? zones.NearestZone(player.Position) : null;
            float pressure = Mathf.Clamp01(who.AiLevel / 20f);
            foreach (var z in zones.Zones)
            {
                if (z == here) continue;
                int hops = ZoneRouter.Hops(here, z);
                if (hops == int.MaxValue) continue;
                float since = lastVisit.TryGetValue(z, out float t) ? Time.time - t : 90f;
                float score = Mathf.Min(since, 90f) + Random.Range(0f, 25f) - Occupancy(z, who) * 45f - hops * 5f;
                if (playerZone != null)
                {
                    int toPlayer = ZoneRouter.Hops(z, playerZone);
                    if (toPlayer == 0) score += 20f + 30f * pressure;
                    else if (toPlayer == 1) score += 10f * pressure;
                }
                if (score > bestScore) { bestScore = score; best = z; }
            }
            if (best != null) roamTargets[who] = best; else roamTargets.Remove(who);
            return best;
        }

        public void ClearRoam(AnimatronicBrain who) => roamTargets.Remove(who);

        /// <summary>Distancia al animatrónico activo más cercano que no sea 'except' (para separar sus patrullas).</summary>
        public float NearestOther(AnimatronicBrain except, Vector2 point)
        {
            float best = float.PositiveInfinity;
            foreach (var a in animatronics)
                if (a != except && a.IsActive) best = Mathf.Min(best, Vector2.Distance(a.Motor.Position, point));
            return best;
        }

        /// <summary>Distancia al animatrónico activo más cercano (para la tensión del sonido).</summary>
        public float NearestActiveDistance(Vector2 point)
        {
            float best = float.PositiveInfinity;
            foreach (var a in animatronics)
                if (a.IsActive) best = Mathf.Min(best, Vector2.Distance(a.Motor.Position, point));
            return best;
        }
    }
}
