using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>
    /// Mapa de ventilaciones (obj_vent_ui): dentro de una ventilación se muestra el mismo mapa del monitor.
    /// Solo las salas con rejilla se pueden tocar para salir por ahí; la sala actual se marca y el
    /// botón con el ícono de salida deja al jugador donde entró.
    /// </summary>
    public class VentView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] string[] roomNames;
        [SerializeField] Button[] roomButtons;
        [SerializeField] GameObject[] hereMarkers;
        [SerializeField] Image airFill;

        HidingSpot current;
        float pulse;

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

        static HidingSpot VentIn(string room)
        {
            foreach (var v in HidingSpot.Vents)
                if (v != null && v.DisplayName == room) return v;
            return null;
        }

        void Open(HidingSpot vent)
        {
            current = vent;
            panel.SetActive(true);
            for (int i = 0; i < roomButtons.Length; i++)
            {
                var v = VentIn(roomNames[i]);
                if (roomButtons[i] != null) roomButtons[i].interactable = v != null && v != vent;
                if (hereMarkers[i] != null) hereMarkers[i].SetActive(v == vent);
            }
        }

        void Close()
        {
            current = null;
            panel.SetActive(false);
        }

        void Update()
        {
            if (current == null) return;
            if (airFill != null) airFill.fillAmount = current.Air01;
            pulse += Time.unscaledDeltaTime;
            float a = 0.3f + 0.35f * Mathf.Abs(Mathf.Sin(pulse * 3f));
            foreach (var m in hereMarkers)
                if (m != null && m.activeSelf && m.TryGetComponent<Image>(out var img)) { var c = img.color; c.a = a; img.color = c; }
        }

        /// <summary>Tocar una sala del mapa: salir por su ventilación.</summary>
        public void Choose(int index)
        {
            if (current == null || index < 0 || index >= roomNames.Length) return;
            var target = VentIn(roomNames[index]);
            if (target == null) return;
            AudioManager.PlaySfx(Sfx.UiClick);
            current.ExitThrough(target);
        }

        /// <summary>Botón con el ícono de salida: salir por la misma rejilla.</summary>
        public void ExitHere()
        {
            if (current == null) return;
            AudioManager.PlaySfx(Sfx.UiClick);
            current.ExitThrough(current);
        }
    }
}
