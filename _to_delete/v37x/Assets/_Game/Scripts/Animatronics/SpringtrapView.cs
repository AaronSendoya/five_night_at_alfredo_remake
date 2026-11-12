using UnityEngine;

namespace Alfredo
{
    public enum SpringtrapAction { Walk, Run, Attack }

    /// <summary>Cuadros de una acción de Springtrap en las tres vistas que trae la hoja de sprites.</summary>
    [System.Serializable]
    public class SpringtrapSet
    {
        public Sprite[] front;
        public Sprite[] right;
        public Sprite[] left;

        /// <summary>Por ahora no existe vista de espaldas: "arriba" usa los cuadros de frente.</summary>
        public Sprite[] Get(Facing f)
        {
            switch (f)
            {
                case Facing.Right: return right;
                case Facing.Left: return left;
                default: return front;
            }
        }
    }

    /// <summary>
    /// FASE 1 (solo arte): reproduce las animaciones de Springtrap (caminar, correr, atacar). Todavía NO está conectado
    /// a ninguna escena del juego ni a la noche 5; sirve para ver el personaje en la escena de vista previa.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpringtrapView : MonoBehaviour
    {
        /// <summary>La hoja original no trae la vista de espaldas.</summary>
        public const bool HasBackView = false;

        [SerializeField] SpringtrapSet walk = new SpringtrapSet();
        [SerializeField] SpringtrapSet run = new SpringtrapSet();
        [SerializeField] SpringtrapSet attack = new SpringtrapSet();
        [SerializeField] float walkFps = 12f;
        [SerializeField] float runFps = 14f;
        [SerializeField] float attackFps = 10f;
        [SerializeField] SpringtrapAction action = SpringtrapAction.Walk;
        [SerializeField] Facing facing = Facing.Down;
        [SerializeField] bool autoPlay = true;
        [Tooltip("Texto que se dibuja bajo el personaje (solo para la escena de vista previa).")]
        [SerializeField] string label = "";

        SpriteRenderer sr;
        float time;
        GUIStyle style;

        SpriteRenderer Renderer => sr != null ? sr : sr = GetComponent<SpriteRenderer>();

        public SpringtrapAction Action => action;
        public Facing Facing => facing;

        public void Play(SpringtrapAction newAction, Facing newFacing)
        {
            if (newAction != action || newFacing != facing) time = 0f;
            action = newAction;
            facing = newFacing;
            Show();
        }

        /// <summary>Mismo formato que usan los demás animatrónicos, para conectarlo al motor en la fase 2.</summary>
        public DirectionalSprites AsDirectional(SpringtrapAction a)
        {
            var set = Set(a);
            return new DirectionalSprites { down = set.front, up = set.front, right = set.right, left = set.left };
        }

        SpringtrapSet Set(SpringtrapAction a)
        {
            switch (a)
            {
                case SpringtrapAction.Run: return run;
                case SpringtrapAction.Attack: return attack;
                default: return walk;
            }
        }

        float Fps(SpringtrapAction a)
        {
            switch (a)
            {
                case SpringtrapAction.Run: return runFps;
                case SpringtrapAction.Attack: return attackFps;
                default: return walkFps;
            }
        }

        void Show()
        {
            var frames = Set(action).Get(facing);
            if (frames == null || frames.Length == 0) return;
            int index = autoPlay ? (int)(time * Fps(action)) % frames.Length : 0;
            Renderer.sprite = frames[index];
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            time += Time.deltaTime;
            Show();
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(label) || Camera.main == null) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 14, fontStyle = FontStyle.Bold };
                style.normal.textColor = Color.white;
            }
            var p = Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(p.x - 110f, Screen.height - p.y + 4f, 220f, 26f), label, style);
        }
    }

    /// <summary>Rótulo bajo un objeto (escena de vista previa).</summary>
    public class GalleryLabel : MonoBehaviour
    {
        public string text = "";
        GUIStyle style;

        void OnGUI()
        {
            if (string.IsNullOrEmpty(text) || Camera.main == null) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 14, fontStyle = FontStyle.Bold };
                style.normal.textColor = Color.white;
            }
            var p = Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(p.x - 110f, Screen.height - p.y + 4f, 220f, 26f), text, style);
        }
    }
}
