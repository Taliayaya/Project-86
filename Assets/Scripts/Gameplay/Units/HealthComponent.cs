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
        [SerializeField] private GameObject deathEffect;
        
        [SerializeField] private List<MonoBehaviour> componentsToDisableOnDeath;
        
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
            if (damagePackage.IsBullet && damagePackage.DamageAmount < Armor)
                return new DamageResponse() { Status = DamageResponse.DamageStatus.Deflected, DamageReceived = 0 };
            Debug.Log($"{Faction} took {damagePackage.DamageAmount} damage. Health: {Health}");
            
            // its speculative health calculation but mostly correct
            float remainingHealth = Mathf.Clamp(Health - damagePackage.DamageAmount, 0, MaxHealth);
            TakeDamageRpc(damagePackage);

            return new DamageResponse()
            {
                Status = DamageResponse.DamageStatus.Taken, DamageReceived = damagePackage.DamageAmount,
                RemainingHealth = remainingHealth
            };
        }

        [Rpc(SendTo.Owner)]
        public void TakeDamageRpc(DamagePackage damagePackage)
        {
            Health = Mathf.Clamp(Health - damagePackage.DamageAmount, 0, MaxHealth);
            if (!IsAlive)
                Die();
            OnTakeDamage(damagePackage);
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
                Destroy(script);
            }
            Destroy(this);
        }

        protected virtual void Start()
        {
            MaxHealth = health.Value;
        }
    }
}