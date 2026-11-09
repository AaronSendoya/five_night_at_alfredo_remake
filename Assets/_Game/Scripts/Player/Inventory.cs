using System;
using UnityEngine;

namespace Alfredo
{
    /// <summary>
    /// Inventario del jugador. En el original era una mochila de 6 espacios que solo guardaba fusibles
    /// (límite total 2); aquí se simplifica a un contador con el mismo límite.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField] int maxFuses = 2;

        public int Fuses { get; private set; }
        public int MaxFuses => maxFuses;
        public bool FusesFull => Fuses >= maxFuses;

        public event Action Changed;

        public bool AddFuse()
        {
            if (FusesFull) return false;
            Fuses++;
            Changed?.Invoke();
            return true;
        }

        public bool UseFuse()
        {
            if (Fuses <= 0) return false;
            Fuses--;
            Changed?.Invoke();
            return true;
        }
    }
}
