using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Atajos de prueba (solo en el editor y en compilaciones de desarrollo):
    /// F1 reloj x10, F2 despierta a los animatrónicos, F3 quema un fusible, F4 da un fusible,
    /// F5 lleva al jugador al escenario, F6 recorre puntos clave del mapa, F9 guarda una captura en Capturas/,
    /// F11 fuerza un turno de aparición de fusibles.
    /// </summary>
    public class DebugTools : MonoBehaviour
    {
        [SerializeField] NightClock clock;
        [SerializeField] AnimatronicDirector director;
        [SerializeField] PlayerController player;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        int spot = -1;
        static readonly Vector2[] Spots =
        {
            new Vector2(1824f, 2250f), new Vector2(5745f, 420f), new Vector2(4900f, 2700f), new Vector2(1406f, 7900f),
            new Vector2(3760f, 6140f), new Vector2(2700f, 2100f), new Vector2(1420f, 1100f)
        };
        static readonly string[] SpotNames = { "Máquina del arcade", "Rejilla del comedor", "Mantenimiento", "Despensa", "Escondite del cuarto secreto", "Rejilla del arcade", "Oficina" };

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) { clock.TestSpeed = clock.TestSpeed > 1f ? 1f : 10f; HudMessages.Show("Reloj x" + clock.TestSpeed); }
            if (Input.GetKeyDown(KeyCode.F2)) { foreach (var a in director.All) a.SkipBoot(); HudMessages.Show("Animatrónicos activos"); }
            if (Input.GetKeyDown(KeyCode.F3)) PowerSystem.Instance.BurnRandomFuse();
            if (Input.GetKeyDown(KeyCode.F4)) player.Inventory.AddFuse();
            if (Input.GetKeyDown(KeyCode.F5)) player.Teleport(GameUnits.FromGm(1400f, 4500f), Facing.Up);
            if (Input.GetKeyDown(KeyCode.F6))
            {
                spot = (spot + 1) % Spots.Length;
                player.Teleport(GameUnits.FromGm(Spots[spot].x, Spots[spot].y), Facing.Up);
                HudMessages.Show(SpotNames[spot]);
            }
            if (Input.GetKeyDown(KeyCode.F7)) showInfo = !showInfo;
            if (Input.GetKeyDown(KeyCode.F11)) { var fs = FindFirstObjectByType<FuseSpawner>(); if (fs != null) fs.DebugTick(); }
            if (Input.GetKeyDown(KeyCode.F10) && player.TryGetComponent<NoiseMeter>(out var nm)) nm.DebugMaxNoise();
            if (Input.GetKeyDown(KeyCode.F8)) { AnimatronicBrain.DebugFreeze = !AnimatronicBrain.DebugFreeze; HudMessages.Show(AnimatronicBrain.DebugFreeze ? "Animatrónicos congelados" : "Animatrónicos libres"); }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                System.IO.Directory.CreateDirectory("Capturas");
                string file = $"Capturas/captura_{System.DateTime.Now:HHmmss}.png";
                ScreenCapture.CaptureScreenshot(file);
                Debug.Log("[Alfredo] Captura: " + file);
            }
        }

        bool showInfo;
        void OnGUI()
        {
            if (!showInfo || player == null) return;
            var p = player.Position; var f = player.FootRect;
            float gx = p.x * 32f, gy = -p.y * 32f;
            var w = Warp.Overlapping(f);
            Warp near = null; float best = 1e9f;
            foreach (var x in Warp.All) { float d = Vector2.Distance(x.area.center, p); if (d < best) { best = d; near = x; } }
            string nearTxt = near == null ? "-" : $"x {near.area.xMin * 32f:0}..{near.area.xMax * 32f:0}  y {-near.area.yMax * 32f:0}..{-near.area.yMin * 32f:0}  activo={near.isActiveAndEnabled}";
            GUI.Box(new Rect(10, 120, 620, 110), "");
            GUI.Label(new Rect(20, 125, 600, 100),
                $"Jugador GM ({gx:0}, {gy:0})  pies y {-f.yMax * 32f:0}..{-f.yMin * 32f:0}  x {f.xMin * 32f:0}..{f.xMax * 32f:0}\n" +
                $"warping={player.IsWarping} cooldown={player.WarpCooldown:0.00} oculto={player.IsHidden} puedeMover={player.CanMove}\n" +
                $"Paso solapado: {(w != null ? w.name + " -> " + w.ToZone?.name : "ninguno")}\nPaso más cercano: {nearTxt}");
        }
#endif
    }
}
