using System;
using UnityEngine;

namespace Alfredo
{
    public enum PowerState { On, Flickering, Off }

    /// <summary>
    /// Sistema eléctrico (obj_panel_fusibles + obj_light_controller): 4 fusibles y un interruptor general.
    /// 4 fusibles = luz, 3 = luz parpadeante, 2 o menos (o interruptor apagado) = apagón.
    /// Cada cierto intervalo hay una probabilidad de que se queme un fusible. Tras quemarse o repararse
    /// uno hay un margen sin quemas al azar, y las puertas cerradas de la oficina aumentan el consumo:
    /// el intervalo corre más rápido y la probabilidad sube (sobrecarga).
    /// </summary>
    public class PowerSystem : MonoBehaviour
    {
        public static PowerSystem Instance { get; private set; }
        public const int FuseCount = 4;

        readonly bool[] fuses = { true, true, true, true };
        float burnInterval = 70f;
        float burnChance = 40f;
        float timer;
        float graceTimer;
        int closedDoors;
        [SerializeField] float graceAfterChange = 25f;
        [SerializeField] float loadPerDoor = 0.8f;
        [SerializeField] float extraChancePerDoor = 8f;

        /// <summary>1 = consumo normal; cada puerta cerrada suma carga.</summary>
        public float Load => 1f + closedDoors * loadPerDoor;
        public int ClosedDoors => closedDoors;
        public event Action LoadChanged;

        public bool SwitchOn { get; private set; } = true;
        public PowerState State { get; private set; } = PowerState.On;
        public bool HasLight => State != PowerState.Off;

        public event Action<PowerState> StateChanged;
        public event Action FusesChanged;
        public event Action FuseBurned;

        void Awake() => Instance = this;

        public void Setup(NightConfig config)
        {
            burnInterval = config.fuseBurnInterval;
            burnChance = config.fuseBurnChance;
            timer = 0f;
            graceTimer = graceAfterChange;
            closedDoors = 0;
            for (int i = 0; i < FuseCount; i++) fuses[i] = true;
            for (int i = 0; i < config.startBrokenFuses; i++) BreakRandom();
            Recompute();
            FusesChanged?.Invoke();
        }

        public bool IsFuseOk(int index) => fuses[index];

        public int ActiveFuses
        {
            get
            {
                int c = 0;
                foreach (bool f in fuses) if (f) c++;
                return c;
            }
        }

        bool BreakRandom()
        {
            int healthy = ActiveFuses;
            if (healthy == 0) return false;
            int pick = UnityEngine.Random.Range(0, healthy);
            for (int i = 0; i < FuseCount; i++)
            {
                if (!fuses[i]) continue;
                if (pick-- == 0) { fuses[i] = false; return true; }
            }
            return false;
        }

        public bool BurnRandomFuse()
        {
            if (!BreakRandom()) return false;
            graceTimer = graceAfterChange;
            timer = 0f;
            FuseBurned?.Invoke();
            FusesChanged?.Invoke();
            Recompute();
            return true;
        }

        public bool RepairOne()
        {
            for (int i = 0; i < FuseCount; i++)
            {
                if (fuses[i]) continue;
                fuses[i] = true;
                graceTimer = graceAfterChange;
                FusesChanged?.Invoke();
                Recompute();
                return true;
            }
            return false;
        }

        public void SetDoorLoad(int doors)
        {
            if (doors == closedDoors) return;
            closedDoors = doors;
            LoadChanged?.Invoke();
        }

        public void ToggleSwitch()
        {
            SwitchOn = !SwitchOn;
            Recompute();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying || burnInterval <= 0f) return;
            if (graceTimer > 0f) { graceTimer -= Time.deltaTime; return; }
            timer += Time.deltaTime * Load;
            if (timer < burnInterval) return;
            timer = 0f;
            if (UnityEngine.Random.value * 100f < burnChance + closedDoors * extraChancePerDoor) BurnRandomFuse();
        }

        void Recompute()
        {
            int active = ActiveFuses;
            PowerState s = !SwitchOn ? PowerState.Off : active == FuseCount ? PowerState.On : active == FuseCount - 1 ? PowerState.Flickering : PowerState.Off;
            if (s == State) return;
            State = s;
            StateChanged?.Invoke(s);
        }
    }
}
