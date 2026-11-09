using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Puerta de la oficina con cortina metálica. Cerrada bloquea el paso en ambos sentidos (jugador y
    /// animatrónicos) pero sobrecarga el sistema eléctrico. En un apagón se abre sola y, al volver la luz,
    /// sigue abierta (su estado inicial).
    /// </summary>
    public class OfficeDoor : MonoBehaviour
    {
        public static readonly List<OfficeDoor> All = new List<OfficeDoor>();

        [SerializeField] string doorName = "Puerta";
        [SerializeField] Warp insideWarp;
        [SerializeField] Warp outsideWarp;
        [SerializeField] Transform shutter;
        [SerializeField] SpriteRenderer plateRenderer;
        [SerializeField] Sprite plateClosed;
        [SerializeField] Sprite plateOpen;
        [SerializeField] float animSeconds = 0.35f;

        float shown;
        float bumpTimer;
        PlayerController player;

        public bool IsClosed { get; private set; }
        public string DoorName => doorName;

        public static int ClosedCount
        {
            get
            {
                int c = 0;
                foreach (var d in All) if (d.IsClosed) c++;
                return c;
            }
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            if (PowerSystem.Instance != null) PowerSystem.Instance.StateChanged += OnPower;
            ApplyVisuals(true);
        }

        void OnDestroy()
        {
            if (PowerSystem.Instance != null) PowerSystem.Instance.StateChanged -= OnPower;
        }

        /// <summary>Lo usa la placa. Devuelve false si no se pudo (sin luz).</summary>
        public bool Toggle()
        {
            if (!IsClosed && !PowerSystem.Instance.HasLight)
            {
                HudMessages.Show("Sin luz las puertas no funcionan");
                AudioManager.PlaySfx(Sfx.Denied);
                return false;
            }
            SetClosed(!IsClosed, true);
            return true;
        }

        public void SetClosed(bool closed, bool sound)
        {
            if (closed == IsClosed) return;
            IsClosed = closed;
            if (insideWarp != null) insideWarp.gameObject.SetActive(!closed);
            if (outsideWarp != null) outsideWarp.gameObject.SetActive(!closed);
            if (sound) AudioManager.PlaySfx(closed ? Sfx.DoorClose : Sfx.DoorOpen);
            ApplyVisuals(false);
            PowerSystem.Instance.SetDoorLoad(ClosedCount);
        }

        void OnPower(PowerState s)
        {
            if (s != PowerState.Off || !IsClosed) return;
            SetClosed(false, true);
            HudMessages.Show("¡Apagón! Las puertas se abrieron");
        }

        void ApplyVisuals(bool instant)
        {
            if (plateRenderer != null) plateRenderer.sprite = IsClosed ? plateClosed : plateOpen;
            if (instant) { shown = IsClosed ? 1f : 0f; UpdateShutter(); }
        }

        void UpdateShutter()
        {
            if (shutter == null) return;
            shutter.localScale = new Vector3(1f, Mathf.Max(0.02f, shown), 1f);
        }

        void Update()
        {
            float target = IsClosed ? 1f : 0f;
            if (!Mathf.Approximately(shown, target))
            {
                shown = Mathf.MoveTowards(shown, target, Time.deltaTime / Mathf.Max(0.01f, animSeconds));
                UpdateShutter();
            }
            if (bumpTimer > 0f) bumpTimer -= Time.deltaTime;
            if (!IsClosed || player == null || bumpTimer > 0f) return;
            var body = player.FootRect;
            body.yMax += 1.2f;
            if ((insideWarp != null && insideWarp.area.Overlaps(body)) || (outsideWarp != null && outsideWarp.area.Overlaps(body)))
            {
                HudMessages.Show("La puerta está cerrada");
                AudioManager.PlaySfx(Sfx.DoorBump);
                bumpTimer = 3f;
            }
        }
    }
}
