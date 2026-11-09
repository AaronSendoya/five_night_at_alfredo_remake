using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>Confeti de la victoria de las 6:00 AM (como en el original).</summary>
    public class ConfettiView : MonoBehaviour
    {
        [SerializeField] int count = 80;
        static readonly Color[] Colors = { Color.red, Color.green, Color.yellow, Color.cyan, Color.magenta, Color.white };
        RectTransform[] pieces;
        float[] speeds;
        RectTransform area;

        public void Play()
        {
            area = (RectTransform)transform;
            if (pieces == null)
            {
                pieces = new RectTransform[count];
                speeds = new float[count];
                for (int i = 0; i < count; i++)
                {
                    var go = new GameObject("c", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(transform, false);
                    var img = go.GetComponent<Image>();
                    img.color = Colors[i % Colors.Length];
                    img.raycastTarget = false;
                    pieces[i] = (RectTransform)go.transform;
                    pieces[i].sizeDelta = new Vector2(12f, 18f);
                }
            }
            for (int i = 0; i < count; i++) Respawn(i, true);
        }

        void Respawn(int i, bool anywhere)
        {
            Rect r = area.rect;
            float y = anywhere ? Random.Range(r.yMax, r.yMax + r.height) : r.yMax + 20f;
            pieces[i].anchoredPosition = new Vector2(Random.Range(r.xMin, r.xMax), y);
            speeds[i] = Random.Range(180f, 360f);
        }

        void Update()
        {
            if (pieces == null) return;
            Rect r = area.rect;
            for (int i = 0; i < count; i++)
            {
                var p = pieces[i];
                p.anchoredPosition += Vector2.down * speeds[i] * Time.unscaledDeltaTime;
                p.Rotate(0f, 0f, speeds[i] * Time.unscaledDeltaTime);
                if (p.anchoredPosition.y < r.yMin - 20f) Respawn(i, false);
            }
        }
    }
}
