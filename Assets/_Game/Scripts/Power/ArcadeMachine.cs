using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Máquina del arcade (obj_arcade_trigger). Abre el minijuego de las pizzas: si se gana, da un fusible.
    /// Después de dar un premio necesita un tiempo para "reiniciarse", así no se puede abusar de ella.
    /// </summary>
    public class ArcadeMachine : Interactable
    {
        [SerializeField] ArcadeMinigame game;
        [SerializeField] float prizeCooldownSeconds = 90f;
        [SerializeField] Transform screenGlow;

        float cooldown;

        public override InteractIcon Icon => InteractIcon.Hand;

        public override bool CanInteract(PlayerController player) =>
            cooldown <= 0f && !game.IsOpen && !player.IsHidden && PowerSystem.Instance.HasLight && !player.Inventory.FusesFull;

        public override string GetPrompt(PlayerController player)
        {
            if (!PowerSystem.Instance.HasLight) return "Máquina sin energía";
            if (cooldown > 0f) return $"Máquina reiniciándose ({Mathf.CeilToInt(cooldown)} s)";
            if (player.Inventory.FusesFull) return "Ya llevas el máximo de fusibles";
            return "Jugar (premio: un fusible)";
        }

        public override void Interact(PlayerController player) => game.Open(player, this);

        public void Finished(bool won, PlayerController player)
        {
            if (!won) return;
            cooldown = prizeCooldownSeconds;
            if (player.Inventory.AddFuse())
            {
                AudioManager.PlaySfx(Sfx.FuseGrab);
                HudMessages.Show("¡Ganaste un fusible en el arcade!");
            }
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.IsPlaying && cooldown > 0f) cooldown -= Time.deltaTime;
            if (screenGlow != null) screenGlow.gameObject.SetActive(PowerSystem.Instance != null && PowerSystem.Instance.HasLight && cooldown <= 0f);
        }
    }
}
