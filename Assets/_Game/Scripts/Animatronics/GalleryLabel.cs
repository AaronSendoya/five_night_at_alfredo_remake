using UnityEngine;

namespace Alfredo
{
    /// <summary>Rótulo de texto bajo un objeto (solo para la escena de vista previa de Springtrap).</summary>
    public class GalleryLabel : MonoBehaviour
    {
        public string text = "";
        public int fontSize = 13;
        GUIStyle style;

        void OnGUI()
        {
            if (string.IsNullOrEmpty(text) || Camera.main == null) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = fontSize, fontStyle = FontStyle.Bold };
                style.normal.textColor = Color.white;
            }
            var p = Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(p.x - 60f, Screen.height - p.y + 4f, 120f, 24f), text, style);
        }
    }
}
