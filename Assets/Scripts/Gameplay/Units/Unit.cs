using System;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Units
{
    
    public enum UnitState
    {
        Default,
        Reloading,
    }
    [RequireComponent(typeof(Rigidbody))]
    public class Unit : MonoBehaviour, IHealth
    {
        private Module[] _modules;

        public UnityEvent<float, float> onHealthChange;

        private Rigidbody _rb;
        
        public UnityEvent<Unit> onUnitDeath;
        
        [Header("Sounds")]
        [SerializeField] private AudioSource engineAudioSource;
        
           public virtual float Health { get; set; } = 100;
           public float MaxHealth { get; set; } = 100;
        [SerializeField] private Faction faction;
        public Faction Faction { get => faction; set => faction = value; }
        public virtual UnitState State { get; set; } = UnitState.Default;
        public virtual float Priority => 5;

        protected virtual void Start()
        {
            _modules = GetComponentsInChildren<Module>();
            foreach (var module in _modules)
                module.faction = Faction;
            onHealthChange.Invoke(Health, MaxHealth); // Initialize health bar
            
            _rb = GetComponent<Rigidbody>();
            engineAudioSource.Play();
        }

        protected virtual void OnEnable()
        {
            EventManager.AddListener("OnPause", OnPause);
            EventManager.AddListener("OnResume", OnResume);
        }
        
        protected virtual void OnDisable()
        {
            EventManager.RemoveListener("OnPause", OnPause);
            EventManager.RemoveListener("OnResume", OnResume);
        }

        protected virtual void OnPause()
        {
            engineAudioSource.Pause();
            Debug.Log("Pause Unit" + engineAudioSource.isPlaying);
        }
        
        protected virtual void OnResume()
        {
            engineAudioSource.UnPause();
        }

     
        public virtual void OnTakeDamage(DamagePackage damagePackage)
        {
            onHealthChange?.Invoke(Health, MaxHealth);
        }

        protected virtual void Update()
        {
            if (_rb.velocity.magnitude < 5)
                engineAudioSource.volume = 0.3f;
            else
                engineAudioSource.volume = 0.1f;
        }

        public virtual void Die()
        {
            Destroy(gameObject);
            onUnitDeath.Invoke(this);
            Factions.RemoveMember(faction, this);
        }

        public virtual void Awake()
        {
            Factions.AddMember(faction, this);
        }
    }
}