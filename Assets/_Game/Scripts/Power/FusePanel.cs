using UnityEngine;

namespace Alfredo
{
    /// <summary>Panel de fusibles: mantener Usar 3 s para instalar un fusible del inventario.</summary>
    public class FusePanel : Interactable
    {
        [SerializeField] SpriteRenderer[] fuseSlots = new SpriteRenderer[PowerSystem.FuseCount];
        [SerializeField] Sprite fuseOk;
        [SerializeField] Sprite fuseBroken;
        [SerializeField] float repairSeconds = 3f;
        [SerializeField] Vector2 workSpotOffset = new Vector2(1f, -1.5f);

        PowerSystem power;

        public override bool RequiresHold => true;
        public override InteractIcon Icon => InteractIcon.Fuse;
        public override float HoldSeconds => repairSeconds;
        /// <summary>Punto frente al panel donde Matilda se para a sabotear.</summary>
        public Vector2 WorkSpot => (Vector2)transform.position + workSpotOffset;

        void Start()
        {
            power = PowerSystem.Instance;
            power.FusesChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (power != null) power.FusesChanged -= Refresh;
        }

        void Refresh()
        {
            for (int i = 0; i < fuseSlots.Length && i < PowerSystem.FuseCount; i++)
                if (fuseSlots[i] != null) fuseSlots[i].sprite = power.IsFuseOk(i) ? fuseOk : fuseBroken;
        }

        public override bool CanInteract(PlayerController player) => power.ActiveFuses < PowerSystem.FuseCount && player.Inventory.Fuses > 0;

        public override string GetPrompt(PlayerController player)
        {
            int missing = PowerSystem.FuseCount - power.ActiveFuses;
            if (missing == 0) return "Panel de fusibles: todo en orden";
            if (player.Inventory.Fuses == 0) return missing == 1 ? "Falta 1 fusible. Busca un repuesto" : $"Faltan {missing} fusibles. Busca repuestos";
            return "Instalar fusible (mantén)";
        }

        public override void Interact(PlayerController player)
        {
            if (player.Inventory.UseFuse() && power.RepairOne())
                AudioManager.PlaySfx(Sfx.FuseInstalled);
        }
    }
}
