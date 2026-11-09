using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Conversión entre las unidades del proyecto original (píxeles de GameMaker, 60 cuadros por segundo,
    /// eje Y hacia abajo) y las de Unity (32 píxeles = 1 unidad, eje Y hacia arriba, segundos).
    /// </summary>
    public static class GameUnits
    {
        public const float PixelsPerUnit = 32f;
        public const float GmFps = 60f;

        public static Vector2 FromGm(float x, float y) => new Vector2(x / PixelsPerUnit, -y / PixelsPerUnit);
        public static float Dist(float pixels) => pixels / PixelsPerUnit;
        public static float Speed(float pixelsPerFrame) => pixelsPerFrame * GmFps / PixelsPerUnit;
        public static float Seconds(float frames) => frames / GmFps;
    }
}
