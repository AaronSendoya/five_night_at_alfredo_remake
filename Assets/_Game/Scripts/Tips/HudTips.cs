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
                return "Cuando Alfredo te busca, escóndete o entra a una rejilla.";
            return For(who.Id);
        }

        public static string For(AnimatronicId id)
        {
            switch (id)
            {
                case AnimatronicId.Alfredo: return "Si oyes reír a Alfredo, escóndete.";
                case AnimatronicId.Matilda: return "Matilda va al panel de fusibles. No vayas mientras esté ahí.";
                case AnimatronicId.Bongo: return "Si oyes la guitarra fuerte, Bongo está cerca. Escóndete.";
                default: return "En un apagón, alumbra a Rufus para que se vaya.";
            }
        }

        public static string Name(AnimatronicId id) => id.ToString();
    }
}
