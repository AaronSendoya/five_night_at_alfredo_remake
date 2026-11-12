using UnityEngine;

namespace Alfredo
{
    public enum AnimatronicId { Alfredo, Matilda, Bongo, Rufus }

    /// <summary>Dificultad de una noche. Los valores por defecto vienen de night_settings de la rama aaron.</summary>
    [CreateAssetMenu(menuName = "Alfredo/Configuración de noche", fileName = "Noche")]
    public class NightConfig : ScriptableObject
    {
        public int night = 1;

        [Header("Reloj")]
        public float secondsPerHour = 70f;
        public int hours = 6;

        [Header("Nivel de IA (0 = no se activa)")]
        public int aiAlfredo;
        public int aiMatilda = 5;
        public int aiBongo = 5;
        public int aiRufus = 5;

        [Header("Segundos de espera antes de activarse")]
        public float bootAlfredo;
        public float bootMatilda = 130f;
        public float bootBongo = 110f;
        public float bootRufus = 140f;

        [Header("Persecuciones simultáneas permitidas")]
        public int maxChases = 2;

        [Header("Sistema eléctrico")]
        public float fuseBurnInterval = 70f;
        [Range(0, 100)] public float fuseBurnChance = 40f;
        public int startBrokenFuses;
        public float fuseSpawnSeconds = 60f;
        [Tooltip("Zonas donde NO aparecen fusibles de repuesto en esta noche (nombres del mapa: Escenario, Cuarto secreto...)")]
        public string[] fuseExcludedZones = new string[0];

        public int AiLevel(AnimatronicId id)
        {
            switch (id)
            {
                case AnimatronicId.Alfredo: return aiAlfredo;
                case AnimatronicId.Matilda: return aiMatilda;
                case AnimatronicId.Bongo: return aiBongo;
                default: return aiRufus;
            }
        }

        public float BootSeconds(AnimatronicId id)
        {
            switch (id)
            {
                case AnimatronicId.Alfredo: return bootAlfredo;
                case AnimatronicId.Matilda: return bootMatilda;
                case AnimatronicId.Bongo: return bootBongo;
                default: return bootRufus;
            }
        }
    }
}
