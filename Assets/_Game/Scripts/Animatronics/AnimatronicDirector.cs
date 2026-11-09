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
        int maxChases = 2;

        public IReadOnlyList<AnimatronicBrain> All => animatronics;
        public int ActiveChases => chasers.Count;

        public void Setup(NightConfig night, PlayerController player)
        {
            maxChases = Mathf.Max(1, night.maxChases);
            chasers.Clear();
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

        public void ResetAll(float grace)
        {
            chasers.Clear();
            foreach (var a in animatronics) a.ResetToHome(grace);
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
