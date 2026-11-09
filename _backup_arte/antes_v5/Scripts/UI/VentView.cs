using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>Mapa de ventilaciones: dentro de una, eliges por cuál salir (obj_vent_ui).</summary>
    public class VentView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Button[] exitButtons;
        [SerializeField] Text[] exitLabels;

        HidingSpot current;

        void Awake()
        {
            HidingSpot.VentEntered += Open;
            HidingSpot.VentExited += Close;
            panel.SetActive(false);
        }

        void OnDestroy()
        {
            HidingSpot.VentEntered -= Open;
            HidingSpot.VentExited -= Close;
        }

        void Open(HidingSpot vent)
        {
            current = vent;
            panel.SetActive(true);
            var vents = HidingSpot.Vents;
            for (int i = 0; i < exitButtons.Length; i++)
            {
                bool valid = i < vents.Count;
                exitButtons[i].gameObject.SetActive(valid);
                if (!valid) continue;
                bool here = vents[i] == vent;
                exitButtons[i].interactable = true;
                exitLabels[i].text = here ? "Salir aquí (" + vents[i].DisplayName + ")" : "Salir en " + vents[i].DisplayName;
            }
        }

        void Close()
        {
            current = null;
            panel.SetActive(false);
        }

        public void Choose(int index)
        {
            if (current == null || index < 0 || index >= HidingSpot.Vents.Count) return;
            AudioManager.PlaySfx(Sfx.UiClick);
            current.ExitThrough(HidingSpot.Vents[index]);
        }
    }
}
