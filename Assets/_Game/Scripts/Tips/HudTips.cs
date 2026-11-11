using System;
using UnityEngine;

namespace Alfredo
{
    /// <summary>Consejos breves en pantalla. Respeta una pausa mínima entre consejos salvo que se fuercen.</summary>
    public static class HudTips
    {
        public static event Action<string> Shown;
        static float lastShown = -999f;
        public const float MinGap = 12f;

        public static bool Show(string text, bool force = false)
        {
            if (!force && Time.time - lastShown < MinGap) return false;
            lastShown = Time.time;
            Shown?.Invoke(text);
            return true;
        }

        public static void ResetGap() => lastShown = -999f;
    }

    /// <summary>Consejo según el animatrónico que atrapó al jugador.</summary>
    public static class DeathTips
    {
        public static string For(AnimatronicBrain who)
        {
            if (who is AlfredoBrain a && a.IsTracking)
                return "Alfredo te rastreaba: escóndete, entra a una ventila o cierra la puerta para perderlo.";
            return For(who.Id);
        }

        public static string For(AnimatronicId id)
        {
            switch (id)
            {
                case AnimatronicId.Alfredo: return "Si oyes la risa de Alfredo, va a embestir: escóndete ya.";
                case AnimatronicId.Matilda: return "¿Ollas cerca de Mantenimiento? Es Matilda: espera escondido.";
                case AnimatronicId.Bongo: return "Guitarra fuerte = Bongo cerca. Rompe su vista y escóndete.";
                default: return "Rufus caza a oscuras: apúntale con la linterna y no la gastes en vano.";
            }
        }

        public static string Name(AnimatronicId id) => id.ToString();
    }
}
