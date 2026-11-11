using UnityEngine;
using UnityEngine.UI;

namespace Alfredo
{
    /// <summary>
    /// Mapa de ventilaciones (obj_vent_ui): dentro de una ventilación se muestra el mismo mapa del monitor.
    /// Solo las salas con rejilla se pueden tocar para salir por ahí; la sala actual se marca y el
    /// botón con el ícono de salida deja al jugador donde entró.
    /// </summary>
    public class VentView : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] string[] roomNames;
        [SerializeField] Button[] roomButtons;
        [SerializeField] GameObject[] hereMarkers;
        [SerializeField] Image airFill;

        [Tooltip("Distancia a la rejilla para el aviso fuerte (te espera en la salida)")]
        [SerializeField] float grilleDangerRange = 9f;

        HidingSpot current;
        float pulse;
        Image[] danger;
        Text[] dangerText;
        int[] dangerLevel;      // 0 nada, 1 hay un animatrónico en la sala, 2 está junto a la rejilla
        float scan;
        AnimatronicDirector director;

        void Awake()
        {
            HidingSpot.VentEntered += Open;
            HidingSpot.VentExited += Close;
            panel.SetActive(false);
            BuildDangerMarks();
        }

        void Start() => director = FindFirstObjectByType<AnimatronicDirector>();

        /// <summary>Capa roja + rótulo sobre cada sala del mapa (se crean en tiempo de ejecución).</summary>
        void BuildDangerMarks()
        {
            int n = roomButtons.Length;
            danger = new Image[n]; dangerText = new Text[n]; dangerLevel = new int[n];
            for (int i = 0; i < n; i++)
            {
                if (roomButtons[i] == null) continue;
                var parent = roomButtons[i].transform;
                var go = new GameObject("Peligro", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(4f, 4f); rt.offsetMax = new Vector2(-4f, -4f);
                go.transform.SetAsFirstSibling(); // debajo del nombre de la sala
                danger[i] = go.GetComponent<Image>();
                danger[i].color = new Color(0.9f, 0.08f, 0.06f, 0f);
                danger[i].raycastTarget = false;

                var label = parent.GetComponentInChildren<Text>();
                var tgo = new GameObject("Aviso", typeof(RectTransform), typeof(Text), typeof(Shadow));
                tgo.transform.SetParent(parent, false);
                var trt = (RectTransform)tgo.transform;
                trt.anchorMin = new Vector2(0f, 0f); trt.anchorMax = new Vector2(1f, 0f); trt.pivot = new Vector2(0.5f, 0f);
                trt.anchoredPosition = new Vector2(0f, 6f); trt.sizeDelta = new Vector2(-8f, 30f);
                var t = tgo.GetComponent<Text>();
                t.font = label != null ? label.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 18; t.fontStyle = FontStyle.Bold; t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white; t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                tgo.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
                t.text = "";
                dangerText[i] = t;
            }
        }

        /// <summary>¿Hay un animatrónico despierto en esa sala? ¿Está junto a su rejilla?</summary>
        void ScanDanger()
        {
            var zones = MapZones.Instance;
            for (int i = 0; i < roomNames.Length && i < dangerLevel.Length; i++)
            {
                int level = 0;
                var vent = VentIn(roomNames[i]);
                if (director != null && zones != null && vent != null && vent != current)
                {
                    var zone = zones.NearestZone(vent.transform.position);
                    foreach (var a in director.All)
                    {
                        if (a == null || !a.IsAwake || zones.NearestZone(a.Motor.Position) != zone) continue;
                        level = Mathf.Max(level, Vector2.Distance(a.Motor.Position, vent.ExitPoint) <= grilleDangerRange ? 2 : 1);
                    }
                }
                dangerLevel[i] = level;
                if (dangerText[i] != null) dangerText[i].text = level == 2 ? "¡TE ESPERA EN LA REJILLA!" : level == 1 ? "ANIMATRÓNICO AQUÍ" : "";
            }
        }

        void OnDestroy()
        {
            HidingSpot.VentEntered -= Open;
            HidingSpot.VentExited -= Close;
        }

        static HidingSpot VentIn(string room)
        {
            foreach (var v in HidingSpot.Vents)
                if (v != null && v.DisplayName == room) return v;
            return null;
        }

        void Open(HidingSpot vent)
        {
            current = vent;
            panel.SetActive(true);
            scan = 0f;
            for (int i = 0; i < roomButtons.Length; i++)
            {
                var v = VentIn(roomNames[i]);
                if (roomButtons[i] != null) roomButtons[i].interactable = v != null && v != vent;
                if (hereMarkers[i] != null) hereMarkers[i].SetActive(v == vent);
            }
        }

        void Close()
        {
            current = null;
            panel.SetActive(false);
        }

        void Update()
        {
            if (current == null) return;
            if (airFill != null) airFill.fillAmount = current.Air01;
            pulse += Time.unscaledDeltaTime;
            float a = 0.3f + 0.35f * Mathf.Abs(Mathf.Sin(pulse * 3f));
            foreach (var m in hereMarkers)
                if (m != null && m.activeSelf && m.TryGetComponent<Image>(out var img)) { var c = img.color; c.a = a; img.color = c; }

            scan -= Time.unscaledDeltaTime;
            if (scan <= 0f) { scan = 0.2f; ScanDanger(); }
            for (int i = 0; i < danger.Length; i++)
            {
                if (danger[i] == null) continue;
                int lv = dangerLevel[i];
                float speed = lv == 2 ? 9f : 4f;
                float alpha = lv == 0 ? 0f : (lv == 2 ? 0.45f : 0.25f) + (lv == 2 ? 0.3f : 0.2f) * Mathf.Abs(Mathf.Sin(pulse * speed));
                var c = danger[i].color; c.a = Mathf.MoveTowards(c.a, alpha, Time.unscaledDeltaTime * 4f); danger[i].color = c;
            }
        }

        /// <summary>Tocar una sala del mapa: salir por su ventilación.</summary>
        public void Choose(int index)
        {
            if (current == null || index < 0 || index >= roomNames.Length) return;
            var target = VentIn(roomNames[index]);
            if (target == null) return;
            AudioManager.PlaySfx(Sfx.UiClick);
            current.ExitThrough(target);
        }

        /// <summary>Botón con el ícono de salida: salir por la misma rejilla.</summary>
        public void ExitHere()
        {
            if (current == null) return;
            AudioManager.PlaySfx(Sfx.UiClick);
            current.ExitThrough(current);
        }
    }
}
