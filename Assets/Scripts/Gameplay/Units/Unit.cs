using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Units
{
    
    public enum UnitState
    {
        Default,
        Reloading,
    }
    public class Unit : MonoBehaviour, IHealth
    {
        private Module[] _modules;

        public UnityEvent<float, float> onHealthChange;
        
        public UnityEvent<Unit> onUnitDeath;
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
        public virtual UnitState State { get; set; } = UnitState.Default;

        public virtual void OnTakeDamage()
        {
            onHealthChange?.Invoke(Health, MaxHealth);
        }

        public virtual void Die()
        {
            Destroy(gameObject);
            onUnitDeath.Invoke(this);
            Factions.RemoveMember(faction, gameObject);
        }

        public virtual void Awake()
        {
            Factions.AddMember(faction, gameObject);
        }
    }
}