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
                return "Alfredo te rastreó por quedarte mucho tiempo en la misma sala. Cuando te persiga en modo Rastreo, busca un escondite: es lo único que lo despista (también la ventilación o cerrarle la puerta).";
            return For(who.Id);
        }

        public static string For(AnimatronicId id)
        {
            switch (id)
            {
                case AnimatronicId.Alfredo: return "Alfredo es lento, pero cada cierto tiempo se ríe y embiste muy rápido. Cuando oigas su risa, escóndete o cierra las puertas de la oficina. Y no te quedes mucho tiempo en una misma sala: te rastreará.";
                case AnimatronicId.Matilda: return "Matilda va al panel a quemar fusibles y te persigue si te ve. Si oyes ollas golpeando cerca de Mantenimiento, espera escondido.";
                case AnimatronicId.Bongo: return "Bongo patrulla sin descanso y te persigue en cuanto te ve. Escucha su guitarra: si suena fuerte, está cerca. Rompe su línea de visión y escóndete.";
                default: return "Rufus solo caza sin luz y es el más rápido. Apúntale con la linterna para que vuelva al escenario y repara la luz cuanto antes.";
            }
        }

        public static string Name(AnimatronicId id) => id.ToString();
    }
}
