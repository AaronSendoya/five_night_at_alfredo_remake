using UnityEngine;
using UnityEngine.EventSystems;

namespace Alfredo
{
    /// <summary>Botón de práctica del tutorial: avisa al pulsar y mientras se mantiene.</summary>
    public class TutorialPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }
        int pressFrame = -10;
        bool consumed;

        public void OnPointerDown(PointerEventData e) { Held = true; pressFrame = Time.frameCount; consumed = false; }
        public void OnPointerUp(PointerEventData e) => Held = false;
        public void OnPointerExit(PointerEventData e) => Held = false;
        void OnDisable() => Held = false;

        /// <summary>Devuelve true una sola vez por pulsación.</summary>
        public bool ConsumePress()
        {
            if (consumed || Time.frameCount - pressFrame > 2) return false;
            consumed = true;
            return true;
        }
    }
}
