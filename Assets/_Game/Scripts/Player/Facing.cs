using System;
using UnityEngine;

namespace Alfredo
{
    public enum Facing { Down, Up, Right, Left }

    public static class FacingUtil
    {
        public static Vector2 ToVector(this Facing f)
        {
            switch (f)
            {
                case Facing.Up: return Vector2.up;
                case Facing.Right: return Vector2.right;
                case Facing.Left: return Vector2.left;
                default: return Vector2.down;
            }
        }

        /// <summary>Dirección dominante de un movimiento; prioridad vertical como en el original.</summary>
        public static Facing FromVector(Vector2 v, Facing fallback)
        {
            if (v.sqrMagnitude < 0.0001f) return fallback;
            // Histéresis: cerca de la diagonal se mantiene la dirección actual (con joystick no parpadea entre dos sprites).
            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y);
            bool fallbackVertical = fallback == Facing.Up || fallback == Facing.Down;
            bool sameSign = fallbackVertical ? (fallback == Facing.Up) == (v.y > 0f) : (fallback == Facing.Right) == (v.x > 0f);
            if (sameSign && (fallbackVertical ? ay * 1.35f >= ax : ax * 1.35f >= ay)) return fallback;
            if (ay >= ax) return v.y > 0 ? Facing.Up : Facing.Down;
            return v.x > 0 ? Facing.Right : Facing.Left;
        }

        /// <summary>Rotación en Z para que el eje "arriba" de un objeto mire en esa dirección.</summary>
        public static float ToAngle(this Facing f)
        {
            switch (f)
            {
                case Facing.Up: return 0f;
                case Facing.Left: return 90f;
                case Facing.Down: return 180f;
                default: return 270f;
            }
        }
    }

    [Serializable]
    public class DirectionalSprites
    {
        public Sprite[] down;
        public Sprite[] up;
        public Sprite[] right;
        public Sprite[] left;

        public Sprite[] Get(Facing f)
        {
            switch (f)
            {
                case Facing.Up: return up;
                case Facing.Right: return right;
                case Facing.Left: return left;
                default: return down;
            }
        }
    }
}
