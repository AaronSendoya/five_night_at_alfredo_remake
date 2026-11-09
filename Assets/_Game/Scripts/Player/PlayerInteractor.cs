using UnityEngine;

namespace Alfredo
{
    /// <summary>Elige el objeto usable más cercano y gestiona pulsar o mantener el botón Usar.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        PlayerController player;
        float holdTime;
        Interactable holdTarget;

        public Interactable Target { get; private set; }
        public float HoldProgress01 => Target != null && Target.RequiresHold && holdTarget == Target ? Mathf.Clamp01(holdTime / Target.HoldSeconds) : 0f;
        public string Prompt => Target != null ? Target.GetPrompt(player) : null;
        public bool TargetUsable => Target != null && Target.CanInteract(player);

        void Awake() => player = GetComponent<PlayerController>();

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.IsPlaying)
            {
                Target = null; holdTime = 0f; holdTarget = null;
                TouchInput.ConsumeInteract();
                return;
            }

            Target = player.IsHidden ? player.CurrentHidingSpot : FindNearest();
            bool pressed = TouchInput.ConsumeInteract();
            if (Target == null) { holdTime = 0f; holdTarget = null; return; }

            if (Target.RequiresHold)
            {
                if (TouchInput.InteractHeld && Target.CanInteract(player))
                {
                    if (holdTarget != Target) { holdTarget = Target; holdTime = 0f; }
                    holdTime += Time.deltaTime;
                    if (holdTime >= Target.HoldSeconds)
                    {
                        holdTime = 0f;
                        holdTarget = null;
                        Target.Interact(player);
                    }
                }
                else { holdTime = 0f; holdTarget = null; }
            }
            else if (pressed && Target.CanInteract(player))
            {
                Target.Interact(player);
            }
        }

        Interactable FindNearest()
        {
            Interactable best = null;
            float bestDist = float.MaxValue;
            Vector2 p = player.BodyCenter;
            foreach (var it in Interactable.All)
            {
                if (it == null || !it.isActiveAndEnabled || !it.IsTargetable(player)) continue;
                // Objetos del suelo se miden desde los pies; muebles altos, desde el centro del cuerpo.
                float d = Mathf.Min(Vector2.Distance(p, it.InteractPoint), Vector2.Distance(player.Position, it.InteractPoint));
                if (d <= it.Radius && d < bestDist) { bestDist = d; best = it; }
            }
            return best;
        }
    }
}
