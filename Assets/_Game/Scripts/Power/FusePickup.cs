using UnityEngine;

namespace Alfredo
{
    /// <summary>Fusible de repuesto en el suelo (obj_fusible_pickup).</summary>
    public class FusePickup : Interactable
    {
        [SerializeField] Transform visual;
        float phase;
        Vector3 baseLocal;

        public static int CountOnMap { get; private set; }

        protected override void OnEnable() { base.OnEnable(); CountOnMap++; }
        protected override void OnDisable() { base.OnDisable(); CountOnMap--; }

        void Start()
        {
            if (visual != null) baseLocal = visual.localPosition;
            phase = Random.value * 6f;
        }

        void Update()
        {
            if (visual == null) return;
            phase += Time.deltaTime * 3f;
            visual.localPosition = baseLocal + Vector3.up * Mathf.Sin(phase) * 0.12f;
        }

        public override bool CanInteract(PlayerController player) => !player.Inventory.FusesFull;

        public override string GetPrompt(PlayerController player) =>
            player.Inventory.FusesFull ? $"Ya llevas {player.Inventory.MaxFuses} fusibles" : "Recoger fusible";

        public override void Interact(PlayerController player)
        {
            if (!player.Inventory.AddFuse()) return;
            AudioManager.PlaySfx(Sfx.FuseGrab);
            Destroy(gameObject);
        }
    }
}
