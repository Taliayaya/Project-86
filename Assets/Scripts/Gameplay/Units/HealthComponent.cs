using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Units
{
    public class HealthComponent : MonoBehaviour, IHealth
    {
        [SerializeField] private float health = 100;
        [SerializeField] private float armor = 10;
        [SerializeField] private Faction faction = Faction.Legion;
        [SerializeField] private GameObject deathEffect;
        
        [SerializeField] private List<MonoBehaviour> componentsToDisableOnDeath;
        
        [Header("Events")]
        public UnityEvent<HealthComponent> OnDeath;
        public UnityEvent<HealthComponent, DamagePackage> OnHealthChange;
        
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Armor { get; set; }
        public Faction Faction { get; set; }
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
            Health = health;
            MaxHealth = health;
            Faction = faction;
            Armor = armor;
        }
    }
}