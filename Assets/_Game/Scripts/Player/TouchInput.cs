using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Entrada unificada: controles táctiles en pantalla y, para probar en el editor, teclado
    /// (WASD o flechas, Shift correr, E usar, F linterna, Espacio manivela).
    /// </summary>
    public static class TouchInput
    {
        public static Vector2 Joystick;
        public static bool RunButton;
        public static bool InteractButton;
        public static bool CrankButton;

        static bool interactPressed;
        static bool flashlightPressed;

        public static void PressInteract() => interactPressed = true;
        public static void PressFlashlight() => flashlightPressed = true;

        public static void ResetAll()
        {
            Joystick = Vector2.zero;
            RunButton = InteractButton = CrankButton = false;
            interactPressed = flashlightPressed = false;
        }

        public static Vector2 Move
        {
            get
            {
                if (Joystick.sqrMagnitude > 0.0001f) return Vector2.ClampMagnitude(Joystick, 1f);
                Vector2 k = new Vector2(
                    (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
                return k.normalized;
            }
        }

        public static bool RunHeld => RunButton || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public static bool InteractHeld => InteractButton || Input.GetKey(KeyCode.E);
        public static bool CrankHeld => CrankButton || Input.GetKey(KeyCode.Space);

        /// <summary>Devuelve true una sola vez por pulsación.</summary>
        public static bool ConsumeInteract()
        {
            bool pressed = interactPressed || Input.GetKeyDown(KeyCode.E);
            interactPressed = false;
            return pressed;
        }

        public static bool ConsumeFlashlight()
        {
            bool pressed = flashlightPressed || Input.GetKeyDown(KeyCode.F);
            flashlightPressed = false;
            return pressed;
        }
    }
}
