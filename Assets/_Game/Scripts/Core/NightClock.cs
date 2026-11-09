using System;
using UnityEngine;

namespace Alfredo
{
    /// <summary>Reloj de la noche: de las 12:00 AM a las 6:00 AM. Solo avanza mientras se juega.</summary>
    public class NightClock : MonoBehaviour
    {
        [Tooltip("Multiplica la velocidad del reloj. Solo para pruebas; dejar en 1.")]
        [SerializeField] float testSpeed = 1f;

        float secondsPerHour = 70f;
        int hours = 6;
        bool finished;

        public float Elapsed { get; private set; }
        public int HoursPassed => Mathf.FloorToInt(Elapsed / secondsPerHour);
        public float Progress01 => Mathf.Clamp01(Elapsed / (secondsPerHour * hours));

        public float TestSpeed { get => testSpeed; set => testSpeed = value; }

        public event Action SixAM;
        public event Action<int> HourChanged;

        public void Setup(NightConfig config)
        {
            secondsPerHour = Mathf.Max(1f, config.secondsPerHour);
            hours = Mathf.Max(1, config.hours);
            Elapsed = 0f;
            finished = false;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (finished || gm == null || !gm.IsPlaying) return;

            int before = HoursPassed;
            Elapsed += Time.deltaTime * testSpeed;
            if (HoursPassed != before) HourChanged?.Invoke(HoursPassed);

            if (Elapsed >= secondsPerHour * hours)
            {
                finished = true;
                Elapsed = secondsPerHour * hours;
                SixAM?.Invoke();
            }
        }

        public string DisplayTime
        {
            get
            {
                int passed = Mathf.Min(HoursPassed, hours);
                int hour = 12 + passed;
                if (hour > 12) hour -= 12;
                int minute = passed >= hours ? 0 : Mathf.FloorToInt(Elapsed % secondsPerHour / secondsPerHour * 60f);
                return $"{hour}:{minute:00} AM";
            }
        }
    }
}
