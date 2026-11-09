using UnityEngine;
using UnityEngine.EventSystems;

namespace Alfredo
{
    public enum TouchAction { Run, Interact, Flashlight, Crank }

    /// <summary>Botón táctil que se puede mantener pulsado.</summary>
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] TouchAction action;
        bool held;

        public void OnPointerDown(PointerEventData e)
        {
            held = true;
            Set(true);
            if (action == TouchAction.Interact) TouchInput.PressInteract();
            if (action == TouchAction.Flashlight) TouchInput.PressFlashlight();
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();
        void OnDisable() => Release();

        void Release()
        {
            if (!held) return;
            held = false;
            Set(false);
        }

        void Set(bool value)
        {
            switch (action)
            {
                case TouchAction.Run: TouchInput.RunButton = value; break;
                case TouchAction.Interact: TouchInput.InteractButton = value; break;
                case TouchAction.Crank: TouchInput.CrankButton = value; break;
            }
        }
    }
}
