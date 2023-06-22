using System;
using Gameplay.Mecha;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay
{
    public class Unit : MonoBehaviour, IHealth
    {
        private Module[] _modules;

        [SerializeField]
        private UnityEvent<float, float> onHealthChange;
        protected virtual void Start()
        {
            _modules = GetComponentsInChildren<Module>();
            foreach (var module in _modules)
                module.faction = Faction;
            onHealthChange.Invoke(Health, MaxHealth); // Initialize health bar
        }

        public virtual float Health { get; set; } = 100;
        public float MaxHealth { get; set; } = 100;
        [SerializeField] private Faction faction;
        public Faction Faction { get => faction; set => faction = value; }

        public virtual void OnTakeDamage()
        {
            onHealthChange?.Invoke(Health, MaxHealth);
        }

        public virtual void Die()
        {
            Destroy(gameObject);
            Factions.RemoveMember(faction, gameObject);
        }

        public virtual void Awake()
        {
            Factions.AddMember(faction, gameObject);
        }
    }
}