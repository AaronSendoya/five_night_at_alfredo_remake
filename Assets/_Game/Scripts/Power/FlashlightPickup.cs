namespace Alfredo
{
    /// <summary>Linterna en la oficina (obj_linterna_pickup).</summary>
    public class FlashlightPickup : Interactable
    {
        [UnityEngine.SerializeField] Flashlight flashlight;

        public override string GetPrompt(PlayerController player) => "Recoger la linterna";

        public override void Interact(PlayerController player)
        {
            flashlight.Give();
            AudioManager.PlaySfx(Sfx.Pickup);
            HudMessages.Show("Linterna: botón Linterna. Si se agota, mantén Manivela");
            Destroy(gameObject);
        }
    }
}
