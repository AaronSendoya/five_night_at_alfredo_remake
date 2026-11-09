using System;

namespace Alfredo
{
    /// <summary>Avisos breves en pantalla ("¡Apagón!", "Te quedaste sin aire"...).</summary>
    public static class HudMessages
    {
        public static event Action<string> Shown;
        public static void Show(string text) => Shown?.Invoke(text);
    }
}
