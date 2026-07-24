using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Units
{
    public class HealthComponent : NetworkBehaviour, IHealth
    {
        [SerializeField] private NetworkVariable<float> health = new(100);
        [SerializeField] private float armor = 10;
        [SerializeField] private Faction faction = Faction.Legion;
        [SerializeField] protected GameObject deathEffect;
        
        [SerializeField] private List<MonoBehaviour> componentsToDisableOnDeath;
        [SerializeField] protected bool destroyOnDeath = true;
        
        [Header("Events")]
        public UnityEvent<HealthComponent> OnDeath;
        public UnityEvent<HealthComponent, DamagePackage> OnHealthChange;
        
        public float Health { get => health.Value; set => health.Value = value; }
        public float MaxHealth { get; set; }
        public float Armor { get => armor; set => armor = value; }
        public bool IsAlive => Health > 0;
        public Faction Faction { get => faction; set => faction = value; }

        public DamageResponse TakeDamage(DamagePackage damagePackage)
        {
            if (damagePackage.Type == DamageType.Bullet && damagePackage.GetDamage() < Armor)
                return new DamageResponse() { Status = DamageResponse.DamageStatus.Deflected, DamageReceived = 0 };
            Debug.Log($"{Faction} took {damagePackage.GetDamage()} damage. Health: {Health}");
            
            // its speculative health calculation but mostly correct
            float remainingHealth = Mathf.Clamp(Health - damagePackage.GetDamage(), 0, MaxHealth);
            TakeDamageRpc(damagePackage);

            return new DamageResponse()
            {
                Status = DamageResponse.DamageStatus.Taken, DamageReceived = damagePackage.GetDamage(),
                RemainingHealth = remainingHealth
            };
        }

        [Rpc(SendTo.Owner)]
        public void TakeDamageRpc(DamagePackage damagePackage)
        {
            Health = Mathf.Clamp(Health - damagePackage.GetDamage(), 0, MaxHealth);
            if (!IsAlive)
                Die();
            OnTakeDamage(damagePackage);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // TakeDamageRpc only runs on the authority; other clients just see the
            // NetworkVariable change. Fire the same events there so damage visuals
            // and death replicate. ponytail: damagePackage is default on remotes —
            // listeners needing hit direction/type must stay authority-side.
            health.OnValueChanged += OnHealthReplicated;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            health.OnValueChanged -= OnHealthReplicated;
        }

        private void OnHealthReplicated(float previous, float current)
        {
            if (HasAuthority || Mathf.Approximately(previous, current))
                return;
            if (current <= 0)
                Die();
            OnTakeDamage(default);
        }

        public virtual void OnTakeDamage(DamagePackage damagePackage)
        
        {
            OnHealthChange.Invoke(this, damagePackage);
        }

        public virtual void Die()
        {
            var effect = Instantiate(deathEffect, transform.parent);
            effect.transform.rotation = Quaternion.LookRotation(Vector3.up);
            OnDeath.Invoke(this);
            
            foreach (var script in componentsToDisableOnDeath)
            {
                if (destroyOnDeath)
                    Destroy(script);
                else
                    script.enabled = false;
            }
            if (destroyOnDeath)
                Destroy(this);
            else
                enabled = false;
        }

        protected virtual void Start()
        {
            MaxHealth = health.Value;
        }
    }
}