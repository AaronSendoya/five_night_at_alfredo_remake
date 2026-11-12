using System.Collections.Generic;
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
        /// <summary>Fusibles que hay ahora mismo en el mapa (incluye el que ya está puesto al empezar la noche).</summary>
        public static readonly List<FusePickup> Active = new List<FusePickup>();

        /// <summary>Momento en que apareció o se cambió de sitio por última vez: sirve para saber cuál es el más viejo.</summary>
        public float SpawnTime { get; private set; }

        protected override void OnEnable() { base.OnEnable(); CountOnMap++; Active.Add(this); SpawnTime = Time.time; }
        protected override void OnDisable() { base.OnDisable(); CountOnMap--; Active.Remove(this); }

        /// <summary>Cambia el fusible de sitio y lo cuenta como recién aparecido.</summary>
        public void MoveTo(Vector2 position)
        {
            transform.position = position;
            SpawnTime = Time.time;
        }

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
