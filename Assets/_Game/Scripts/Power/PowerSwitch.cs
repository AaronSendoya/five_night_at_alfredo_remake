using UnityEngine;

namespace Alfredo
{
    /// <summary>Interruptor general (obj_switch_general).</summary>
    public class PowerSwitch : Interactable
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite offSprite;
        [SerializeField] Sprite onSprite;

        public override InteractIcon Icon => InteractIcon.Power;

        void Start() => Refresh();

        public override string GetPrompt(PlayerController player) =>
            PowerSystem.Instance.SwitchOn ? "Apagar el interruptor general" : "Encender el interruptor general";

        public override void Interact(PlayerController player)
        {
            PowerSystem.Instance.ToggleSwitch();
            AudioManager.PlaySfx(Sfx.Switch);
            Refresh();
        }

        void Refresh()
        {
            if (spriteRenderer != null) spriteRenderer.sprite = PowerSystem.Instance.SwitchOn ? onSprite : offSprite;
        }
    }
}
