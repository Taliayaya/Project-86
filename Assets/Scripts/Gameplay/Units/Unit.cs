using System;
using Gameplay.Mecha;
using ScriptableObjects.Sound;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Units
{
    
    public enum UnitState
    {
        Default,
        Reloading,
    }

    public enum EngineStatus
    {
        Idle,
        Walking,
        Running,
    }
    [RequireComponent(typeof(Rigidbody))]
    public abstract class Unit : NetworkBehaviour, IHealth
    {
        private Module[] _modules;
        
        [Header("Base Unit Settings")]
        public UnitType unitType = UnitType.None;
        public float aimiYOffset = 2f;

        [Tooltip("Current heal, Max health")]
        public UnityEvent<float, float> onHealthChange;

        private Rigidbody _rb;
        [Tooltip("If the armor value is bigger than the bullet, damages are neglected.")]
        public float armor = 10;
        
        public UnityEvent<Unit> onUnitDeath;
        
        [Header("Sounds")]
        [SerializeField] private AudioSource engineAudioSource;
        [SerializeField] private EngineAudioSO engineAudioSo;
        
        private NetworkVariable<float> _health = new NetworkVariable<float>(100);
        public virtual float Health
        {
            get => _health.Value;
            set { _health.Value = value; }
        }

        public bool Alive => Health > 0;
        public virtual float Armor { get; set; } = 10;
        public float MaxHealth { get; set; } = 100;
        [SerializeField] private Faction faction;
        public Faction Faction { get => faction; set => faction = value; }
        public virtual UnitState State { get; set; } = UnitState.Default;
        
        private EngineStatus _engineStatus = EngineStatus.Idle;

        protected virtual EngineStatus EngineStatus
        {
            get => _engineStatus;
            set
            {
                if (value == _engineStatus) return;
                engineAudioSource.Stop();
                engineAudioSource.clip = value switch
                {
                    EngineStatus.Idle => engineAudioSo.engineIdle,
                    EngineStatus.Walking => engineAudioSo.engineWalking,
                    EngineStatus.Running => engineAudioSo.engineSpeeding,
                    _ => throw new ArgumentOutOfRangeException()
                };
                engineAudioSource.PlayOneShot((int)_engineStatus < (int)value
                    ? engineAudioSo.engineAcceleration
                    : engineAudioSo.engineDeceleration, 0.5f);

                ////engineAudioSource.loop = true;
                engineAudioSource.Play();
                _engineStatus = value;
            }
        }

        public virtual float Priority => 5;

        protected virtual void Start()
        {
            _modules = GetComponentsInChildren<Module>();
            foreach (var module in _modules)
            {
                module.faction = Faction;
                //if (!IsOwner)
                //{
                //    module.enabled = false;
                //}
            }
            _health.OnValueChanged += (old, curr) => onHealthChange.Invoke(curr, MaxHealth);
            onHealthChange.Invoke(Health, MaxHealth); // Initialize health bar
            
            _rb = GetComponent<Rigidbody>();
            engineAudioSource.clip = engineAudioSo.engineSpeeding;
            engineAudioSource.loop = true;
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

        
        public DamageResponse TakeDamage(DamagePackage damagePackage)
        {
            if (damagePackage.IsBullet && damagePackage.DamageAmount < Armor)
                return new DamageResponse() { Status = DamageResponse.DamageStatus.Deflected, DamageReceived = 0};
            Debug.Log($"{Faction} took {damagePackage.DamageAmount} damage. Health: {Health}");
            float remainingHealth = Mathf.Clamp(Health - damagePackage.DamageAmount, 0, MaxHealth);
            TakeDamageRpc(damagePackage);

            return new DamageResponse() { Status = DamageResponse.DamageStatus.Taken, DamageReceived = damagePackage.DamageAmount, RemainingHealth = remainingHealth};
        }
        
        [Rpc(SendTo.Owner)]
        public void TakeDamageRpc(DamagePackage damagePackage)
        {
            Health = Mathf.Clamp(Health - damagePackage.DamageAmount, 0, MaxHealth);
            if (!Alive)
                Die();
            OnTakeDamage(damagePackage);
        }
     
        public virtual void OnTakeDamage(DamagePackage damagePackage)
        {
            onHealthChange?.Invoke(Health, MaxHealth);
        }

        protected virtual void Update()
        {
            //Debug.Log("Velocity of " + name + " is " + _rb.velocity.magnitude + " and engine status is " + EngineStatus);
        }

        protected virtual void FixedUpdate()
        {
            EngineStatus = _rb.linearVelocity.magnitude switch
            {
                < 5 => EngineStatus.Idle,
                < 20 => EngineStatus.Walking,
                _ => EngineStatus.Running
            };
            //engineAudioSource.pitch = Mathf.Clamp(_rb.velocity.magnitude / 30, 0.5f, 1.5f);
        }


        public bool Died { get; protected set; } = false;

        public virtual void Die()
        {
            if (Died) return;
            if (!IsOwner) return;
            Died = true;
            EventManager.TriggerEvent("UnitDeath", this);
            Destroy(gameObject);
            onUnitDeath.Invoke(this);
            Factions.RemoveMember(faction, this);
        }
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            Factions.RemoveMember(faction, this);
        }

        public virtual void Awake()
        {
            Armor = armor;
            Factions.AddMember(faction, this);
        }
    }
}