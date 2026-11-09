using UnityEngine;
using UnityEngine.EventSystems;

namespace Alfredo
{
    /// <summary>Joystick flotante: aparece donde el pulgar toca dentro de la mitad izquierda de la pantalla.</summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform background;
        [SerializeField] RectTransform knob;
        [SerializeField] float radius = 110f;
        [SerializeField] float deadZone = 0.15f;

        RectTransform area;
        Vector2 restPosition;
        int pointerId = int.MinValue;

        void Awake()
        {
            area = (RectTransform)transform;
            restPosition = background.anchoredPosition;
        }

        void OnDisable() => Release();

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != int.MinValue) return;
            pointerId = e.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out Vector2 local);
            background.localPosition = local;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, e.pressEventCamera, out Vector2 local);
            Vector2 v = Vector2.ClampMagnitude(local, radius);
            knob.anchoredPosition = v;
            Vector2 n = v / radius;
            TouchInput.Joystick = n.magnitude < deadZone ? Vector2.zero : n;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == pointerId) Release();
        }

        void Release()
        {
            pointerId = int.MinValue;
            TouchInput.Joystick = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (background != null) background.anchoredPosition = restPosition;
        }
    }
}
