using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alfredo.EditorTools
{
    /// <summary>Construcción de interfaz sobria: colores sólidos, solo texto, sin íconos.</summary>
    public static class UiFactory
    {
        public static readonly Color Panel = new Color(0.07f, 0.07f, 0.08f, 1f);
        public static readonly Color ButtonColor = new Color(0.16f, 0.16f, 0.18f, 1f);
        public static readonly Color Accent = new Color(0.72f, 0.12f, 0.12f, 1f);
        public static readonly Color TextColor = new Color(0.92f, 0.9f, 0.86f, 1f);
        public static readonly Color MutedText = new Color(0.6f, 0.6f, 0.6f, 1f);

        static Font font;
        public static Font Font => font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas MakeCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return c;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return rt;
        }

        public static Image AddImage(RectTransform rt, Color color, bool raycast = false, Sprite sprite = null)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            img.sprite = sprite;
            return img;
        }

        public static Image FullPanel(string name, Transform parent, Color color)
        {
            var rt = Stretch(name, parent);
            return AddImage(rt, color, true);
        }

        public static Text AddText(string name, Transform parent, string content, int size, Vector2 anchor, Vector2 pos, Vector2 boxSize, TextAnchor align = TextAnchor.MiddleCenter, Color? color = null, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent, anchor, anchor, pos, boxSize);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = content;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? TextColor;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Button AddButton(string name, Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, UnityAction onClick, int fontSize = 40, Color? color = null)
        {
            var rt = Rect(name, parent, anchor, anchor, pos, size);
            var img = AddImage(rt, color ?? ButtonColor, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            b.colors = colors;
            var t = AddText("Texto", rt, label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(20f, 8f));
            if (onClick != null) UnityEventTools.AddPersistentListener(b.onClick, onClick);
            return b;
        }

        public static Text Label(Button b) => b.GetComponentInChildren<Text>();

        /// <summary>Barra horizontal: fondo oscuro y relleno que se controla con fillAmount.</summary>
        public static Image Bar(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color fill, Sprite white)
        {
            var rt = Rect(name, parent, anchor, anchor, pos, size);
            AddImage(rt, new Color(0f, 0f, 0f, 0.65f));
            var fillRt = Rect("Relleno", rt, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-6f, -6f));
            var img = AddImage(fillRt, fill, false, white);
            img.type = UnityEngine.UI.Image.Type.Filled;
            img.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            img.fillAmount = 1f;
            return img;
        }

        public static Slider AddSlider(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Sprite white)
        {
            var rt = Rect(name, parent, anchor, anchor, pos, size);
            AddImage(rt, new Color(0f, 0f, 0f, 0.6f), true, white);
            var fillArea = Rect("Area", rt, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20f, -20f));
            var fill = Rect("Relleno", fillArea, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f));
            AddImage(fill, Accent, false, white);
            var handleArea = Rect("Zona", rt, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-30f, 0f));
            var handle = Rect("Tirador", handleArea, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(30f, 10f));
            var handleImg = AddImage(handle, TextColor, true, white);
            var s = rt.gameObject.AddComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = handleImg;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = 0.8f;
            return s;
        }
    }
}
