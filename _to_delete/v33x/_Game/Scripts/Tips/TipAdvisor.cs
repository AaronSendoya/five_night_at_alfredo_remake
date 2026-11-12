using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Observa la partida y da consejos breves cuando el jugador parece atascado o hace algo mal
    /// (apagón largo, puertas cerradas mucho tiempo, linterna sin batería...). Cada consejo tiene su
    /// propia espera para no repetirse.
    /// </summary>
    public class TipAdvisor : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] Flashlight flashlight;
        [SerializeField] AnimatronicDirector director;

        readonly Dictionary<string, float> nextAllowed = new Dictionary<string, float>();
        float blackoutTime, flickerTime, doorsTime, camTime, playTime, exhaustedTime;
        float check;

        void Start()
        {
            HidingSpot.Message += OnHideMessage;
            if (flashlight != null) flashlight.BatteryEmpty += OnBatteryEmpty;
        }

        void OnDestroy()
        {
            HidingSpot.Message -= OnHideMessage;
            if (flashlight != null) flashlight.BatteryEmpty -= OnBatteryEmpty;
        }

        void OnHideMessage(string m) => Tip("aire", "Sal del escondite antes de que se acabe el aire.", 60f);
        void OnBatteryEmpty() => Tip("bateria", "Sin batería: mantén Manivela para recargar.", 60f);

        bool Tip(string key, string text, float cooldown)
        {
            if (nextAllowed.TryGetValue(key, out float t) && Time.time < t) return false;
            if (!HudTips.Show(text)) return false;
            nextAllowed[key] = Time.time + cooldown;
            return true;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying) return;
            float dt = Time.deltaTime;
            var power = PowerSystem.Instance;
            playTime += dt;
            blackoutTime = power.State == PowerState.Off ? blackoutTime + dt : 0f;
            flickerTime = power.State == PowerState.Flickering ? flickerTime + dt : 0f;
            doorsTime = OfficeDoor.ClosedCount > 0 ? doorsTime + dt : 0f;
            camTime = CameraMonitor.AnyOpen ? camTime + dt : 0f;
            if (camTime > 12f) Tip("camaras", "Las cámaras gastan luz. Úsalas poco.", 90f);
            exhaustedTime = player.IsExhausted ? exhaustedTime + dt : 0f;

            check -= dt;
            if (check > 0f) return;
            check = 1f;

            if (blackoutTime > 12f)
            {
                if (!power.SwitchOn) Tip("switch", "Enciende el interruptor en Mantenimiento.", 30f);
                else if (player.Inventory.Fuses > 0) Tip("instalar", "Lleva tus fusibles al panel de Mantenimiento.", 30f);
                else Tip("buscar", "Sin luz: busca fusibles por el mapa (brillan).", 35f);
            }
            if (blackoutTime > 20f && flashlight != null && flashlight.HasFlashlight)
                Tip("rufus", "En un apagón, alumbra a Rufus para que se vaya.", 90f);
            if (flickerTime > 15f) Tip("parpadeo", "Se quemó un fusible: cámbialo pronto.", 60f);
            if (doorsTime > 20f) Tip("puertas", "Las puertas cerradas gastan luz. Ábrelas si no las necesitas.", 60f);
            if (exhaustedTime > 0.5f) Tip("agotado", "Corre solo cuando haga falta.", 120f);
            if (playTime > 45f && flashlight != null && !flashlight.HasFlashlight)
                Tip("linterna", "Recoge la linterna en la oficina.", 90f);
            if (ShadowFreddy.IsActive) Tip("shadow", "Shadow Alfredo no te mata. Repara la luz para que se vaya.", 120f);
        }
    }
}
