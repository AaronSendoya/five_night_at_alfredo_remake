using System.Collections.Generic;
using UnityEngine;

namespace Alfredo
{
    /// <summary>Ícono que muestra el botón Usar según lo que se va a hacer.</summary>
    public enum InteractIcon { Hand, Hide, Exit, Phone, Camera, Fuse, Power, Vent }

    /// <summary>Base de todo lo que el jugador puede usar con el botón Usar.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        [SerializeField] protected float radius = 2.5f;

        public float Radius => radius;
        public virtual Vector2 InteractPoint => transform.position;
        public virtual bool RequiresHold => false;
        public virtual InteractIcon Icon => InteractIcon.Hand;
        public virtual float HoldSeconds => 0f;

        public abstract string GetPrompt(PlayerController player);
        public virtual bool CanInteract(PlayerController player) => true;
        /// <summary>False si ahora mismo no debe ofrecerse (por ejemplo, un teléfono que ya no suena).</summary>
        public virtual bool IsTargetable(PlayerController player) => true;
        public abstract void Interact(PlayerController player);

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(InteractPoint, radius);
        }
    }
}
