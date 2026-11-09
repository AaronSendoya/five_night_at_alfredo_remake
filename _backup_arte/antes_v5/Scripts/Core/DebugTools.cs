using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Atajos de prueba (solo en el editor y en compilaciones de desarrollo):
    /// F1 reloj x10, F2 despierta a los animatrónicos, F3 quema un fusible, F4 da un fusible,
    /// F5 lleva al jugador al escenario.
    /// </summary>
    public class DebugTools : MonoBehaviour
    {
        [SerializeField] NightClock clock;
        [SerializeField] AnimatronicDirector director;
        [SerializeField] PlayerController player;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) { clock.TestSpeed = clock.TestSpeed > 1f ? 1f : 10f; HudMessages.Show("Reloj x" + clock.TestSpeed); }
            if (Input.GetKeyDown(KeyCode.F2)) { foreach (var a in director.All) a.SkipBoot(); HudMessages.Show("Animatrónicos activos"); }
            if (Input.GetKeyDown(KeyCode.F3)) PowerSystem.Instance.BurnRandomFuse();
            if (Input.GetKeyDown(KeyCode.F4)) player.Inventory.AddFuse();
            if (Input.GetKeyDown(KeyCode.F5)) player.Teleport(GameUnits.FromGm(1400f, 4500f), Facing.Up);
        }
#endif
    }
}
