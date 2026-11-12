using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>
    /// Posición y tamaño guardados de los 4 botones de acción (Usar, Correr, Manivela, Linterna).
    /// El joystick y el resto del HUD no se pueden mover.
    /// </summary>
    public static class ControlLayout
    {
        public static readonly string[] Names = { "Usar", "Correr", "Manivela", "Linterna" };
        public const float MinScale = 0.8f, MaxScale = 1.7f;

        static readonly Dictionary<string, (Vector2 pos, float scale)> defaults = new Dictionary<string, (Vector2, float)>();

        public static List<RectTransform> Find()
        {
            var list = new List<RectTransform>();
            foreach (var b in Object.FindObjectsByType<TouchButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (System.Array.IndexOf(Names, b.name) >= 0) list.Add((RectTransform)b.transform);
            return list;
        }

        static string Key(string name, string field) => $"ctl_{name}_{field}";

        public static void RememberDefaults(List<RectTransform> buttons)
        {
            foreach (var rt in buttons)
                if (!defaults.ContainsKey(rt.name)) defaults[rt.name] = (rt.anchoredPosition, rt.localScale.x);
        }

        /// <summary>Al empezar la partida: aplica lo que el jugador guardó.</summary>
        public static void ApplySaved()
        {
            var buttons = Find();
            RememberDefaults(buttons);
            foreach (var rt in buttons)
            {
                if (!PlayerPrefs.HasKey(Key(rt.name, "x"))) continue;
                rt.anchoredPosition = new Vector2(PlayerPrefs.GetFloat(Key(rt.name, "x")), PlayerPrefs.GetFloat(Key(rt.name, "y")));
                rt.localScale = Vector3.one * Mathf.Clamp(PlayerPrefs.GetFloat(Key(rt.name, "s"), 1f), MinScale, MaxScale);
                SyncRing(rt);
            }
        }

        public static void Save(List<RectTransform> buttons)
        {
            foreach (var rt in buttons)
            {
                PlayerPrefs.SetFloat(Key(rt.name, "x"), rt.anchoredPosition.x);
                PlayerPrefs.SetFloat(Key(rt.name, "y"), rt.anchoredPosition.y);
                PlayerPrefs.SetFloat(Key(rt.name, "s"), rt.localScale.x);
            }
            PlayerPrefs.Save();
        }

        public static void ResetAll(List<RectTransform> buttons)
        {
            foreach (var rt in buttons)
            {
                if (defaults.TryGetValue(rt.name, out var d)) { rt.anchoredPosition = d.pos; rt.localScale = Vector3.one * d.scale; }
                PlayerPrefs.DeleteKey(Key(rt.name, "x")); PlayerPrefs.DeleteKey(Key(rt.name, "y")); PlayerPrefs.DeleteKey(Key(rt.name, "s"));
                SyncRing(rt);
            }
            PlayerPrefs.Save();
        }

        /// <summary>El anillo de progreso de "Usar" acompaña al botón.</summary>
        public static void SyncRing(RectTransform rt)
        {
            if (rt.name != "Usar" || rt.parent == null) return;
            var ring = rt.parent.Find("Progreso") as RectTransform;
            if (ring == null) return;
            ring.anchoredPosition = rt.anchoredPosition;
            ring.localScale = rt.localScale;
        }

        /// <summary>Mantiene el botón entero en la mitad derecha de la pantalla.</summary>
        public static void Clamp(RectTransform rt)
        {
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            Rect r = parent.rect;
            float half = rt.rect.width * rt.localScale.x * 0.5f;
            Vector3 p = rt.localPosition;
            p.x = Mathf.Clamp(p.x, r.center.x + half, r.xMax - half);
            p.y = Mathf.Clamp(p.y, r.yMin + half, r.yMax - half);
            rt.localPosition = p;
            SyncRing(rt);
        }
    }

    /// <summary>Arrastre de un botón en el modo de edición de controles.</summary>
    public class ControlDragHandle : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public ControlsEditor editor;
        Vector2 grabOffset;

        public void OnPointerDown(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            editor.Select(rt);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out var local);
            grabOffset = (Vector2)rt.localPosition - local;
        }

        public void OnDrag(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out var local)) return;
            rt.localPosition = local + grabOffset;
            ControlLayout.Clamp(rt);
        }
    }

    /// <summary>
    /// Opción "Controles" de la pausa: permite mover (solo en la mitad derecha) y agrandar los 4 botones de acción.
    /// La interfaz del editor se crea en tiempo de ejecución.
    /// </summary>
    public class ControlsEditor : MonoBehaviour
    {
        [SerializeField] GameObject pausePanel;

        /// <summary>Mientras es true, el HUD muestra los 4 botones aunque no estén disponibles en ese momento.</summary>
        public static bool Editing { get; private set; }

        List<RectTransform> buttons;
        RectTransform selected;
        GameObject overlay;
        Text sizeLabel;
        readonly List<Image> highlights = new List<Image>();

        public void Open()
        {
            buttons = ControlLayout.Find();
            if (buttons.Count == 0) return;
            ControlLayout.RememberDefaults(buttons);
            Editing = true;
            foreach (var rt in buttons) rt.gameObject.SetActive(true);
            if (pausePanel != null) pausePanel.SetActive(false);
            BuildOverlay();
            foreach (var rt in buttons)
            {
                var tb = rt.GetComponent<TouchButton>(); if (tb != null) tb.enabled = false;
                var h = rt.gameObject.AddComponent<ControlDragHandle>(); h.editor = this;
                var ring = new GameObject("Seleccion", typeof(RectTransform), typeof(Image));
                ring.transform.SetParent(rt, false);
                var rrt = (RectTransform)ring.transform;
                rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one; rrt.offsetMin = new Vector2(-10f, -10f); rrt.offsetMax = new Vector2(10f, 10f);
                rrt.SetAsFirstSibling();
                var img = ring.GetComponent<Image>();
                img.sprite = rt.GetComponent<Image>() != null ? rt.GetComponent<Image>().sprite : null;
                img.color = new Color(1f, 0.8f, 0.2f, 0f);
                img.raycastTarget = false;
                highlights.Add(img);
            }
            Select(buttons[0]);
        }

        public void Select(RectTransform rt)
        {
            selected = rt;
            for (int i = 0; i < buttons.Count; i++) highlights[i].color = new Color(1f, 0.8f, 0.2f, buttons[i] == rt ? 0.9f : 0f);
            UpdateLabel();
        }

        void Resize(float delta)
        {
            if (selected == null) return;
            float s = Mathf.Clamp(Mathf.Round((selected.localScale.x + delta) * 10f) / 10f, ControlLayout.MinScale, ControlLayout.MaxScale);
            selected.localScale = Vector3.one * s;
            ControlLayout.Clamp(selected);
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (sizeLabel != null && selected != null) sizeLabel.text = $"{selected.name}: {Mathf.RoundToInt(selected.localScale.x * 100f)} %";
        }

        void Done()
        {
            ControlLayout.Save(buttons);
            Close();
        }

        void ResetLayout()
        {
            ControlLayout.ResetAll(buttons);
            UpdateLabel();
        }

        void OnDisable() { if (Editing && overlay != null) Close(); }

        void Close()
        {
            Editing = false;
            foreach (var rt in buttons)
            {
                var tb = rt.GetComponent<TouchButton>(); if (tb != null) tb.enabled = true;
                var h = rt.GetComponent<ControlDragHandle>(); if (h != null) Destroy(h);
                var sel = rt.Find("Seleccion"); if (sel != null) Destroy(sel.gameObject);
            }
            highlights.Clear();
            if (overlay != null) Destroy(overlay);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        void BuildOverlay()
        {
            var parent = (RectTransform)buttons[0].parent;
            overlay = new GameObject("EditorControles", typeof(RectTransform));
            overlay.transform.SetParent(parent, false);
            overlay.transform.SetAsLastSibling();    // encima del joystick; la mitad derecha no bloquea toques, así los botones se pueden arrastrar
            var ort = (RectTransform)overlay.transform;
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one; ort.offsetMin = ort.offsetMax = Vector2.zero;
            // Lienzo propio por encima del joystick (que tapa la mitad izquierda). La mitad derecha no bloquea toques,
            // así los 4 botones de abajo se siguen pudiendo arrastrar.
            var cv = overlay.AddComponent<Canvas>();
            cv.overrideSorting = true;
            cv.sortingOrder = 800;
            overlay.AddComponent<GraphicRaycaster>();

            Panel("Oscuro", ort, Vector2.zero, new Vector2(0.5f, 1f), new Color(0f, 0f, 0f, 0.85f));
            Panel("ZonaPermitida", ort, new Vector2(0.5f, 0f), Vector2.one, new Color(1f, 1f, 1f, 0.06f));
            Panel("Linea", ort, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Color(1f, 0.8f, 0.2f, 0.7f)).offsetMax = new Vector2(3f, 0f);

            var title = Label(ort, "CONTROLES", 64, new Vector2(0.25f, 1f), new Vector2(0f, -90f), new Vector2(800f, 90f), FontStyle.Bold);
            title.color = Color.white;
            Label(ort, "Arrastra un botón para moverlo.\nSolo puedes ponerlos en la mitad derecha.", 30, new Vector2(0.25f, 1f), new Vector2(0f, -200f), new Vector2(820f, 110f), FontStyle.Normal);
            sizeLabel = Label(ort, "", 36, new Vector2(0.25f, 0.5f), new Vector2(0f, 60f), new Vector2(800f, 60f), FontStyle.Bold);
            sizeLabel.color = new Color(1f, 0.8f, 0.2f);
            Button(ort, "−", new Vector2(0.25f, 0.5f), new Vector2(-110f, -30f), new Vector2(160f, 100f), () => Resize(-0.1f));
            Button(ort, "+", new Vector2(0.25f, 0.5f), new Vector2(110f, -30f), new Vector2(160f, 100f), () => Resize(0.1f));
            Button(ort, "Restablecer", new Vector2(0.25f, 0f), new Vector2(-170f, 110f), new Vector2(300f, 100f), ResetLayout);
            Button(ort, "Listo", new Vector2(0.25f, 0f), new Vector2(170f, 110f), new Vector2(300f, 100f), Done, true);
        }

        static RectTransform Panel(string name, RectTransform parent, Vector2 min, Vector2 max, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = name == "Oscuro";
            return rt;
        }

        static Text Label(RectTransform parent, string text, int size, Vector2 anchor, Vector2 pos, Vector2 box, FontStyle style)
        {
            var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = pos; rt.sizeDelta = box;
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size; t.fontStyle = style; t.alignment = TextAnchor.MiddleCenter; t.color = new Color(0.9f, 0.9f, 0.9f);
            t.raycastTarget = false; t.text = text;
            return t;
        }

        static void Button(RectTransform parent, string text, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, bool accent = false)
        {
            var go = new GameObject("Boton " + text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = accent ? new Color(0.75f, 0.12f, 0.12f) : new Color(0.2f, 0.2f, 0.22f);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            var t = Label(rt, text, 40, new Vector2(0.5f, 0.5f), Vector2.zero, size, FontStyle.Bold);
            t.color = Color.white;
        }
    }
}
