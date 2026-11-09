using UnityEngine;

namespace Alfredo
{
    /// <summary>Placa junto a la puerta de la oficina: abre o cierra la cortina.</summary>
    public class DoorPlate : Interactable
    {
        [SerializeField] OfficeDoor door;

        public override InteractIcon Icon => InteractIcon.Power;

        public override bool CanInteract(PlayerController player) => door.IsClosed || PowerSystem.Instance.HasLight;

        public override string GetPrompt(PlayerController player)
        {
            if (door.IsClosed) return "Abrir puerta";
            return PowerSystem.Instance.HasLight ? "Cerrar puerta (gasta más luz)" : "Sin luz: la puerta no funciona";
        }

        public override void Interact(PlayerController player) => door.Toggle();
    }
}
