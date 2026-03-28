using System;
using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;

namespace Gameplay.Units
{
    public class MultiHealthComponent : HealthComponent
    {
        [SerializeField] private GameObject mainHealthGo;

        public IHealth MainHealth;
        public bool mainHealthTakesDamage = true;
        public bool destroyMainHealthOnDeath = false;
        public float damageMultiplier = 1f;
        
        
        public Transform deathEffectPosition;
        public bool parentToDeathPosition = true;
        
        protected bool IsDead;

        private void Awake()
        {
            if (mainHealthTakesDamage || destroyMainHealthOnDeath)
                MainHealth = mainHealthGo.GetComponent<Unit>();
        }

        public override void OnTakeDamage(DamagePackage damagePackage)
        {
            if (!IsDead)
                base.OnTakeDamage(damagePackage);
            // if dead and this set, kill the main body
            else if (destroyMainHealthOnDeath)
            {
                MainHealth.Die();   
                return;
            }
            // even if this component is dead, the main body still takes damage
            if (mainHealthTakesDamage)
            {
                if (damagePackage.Type == DamageType.Bullet)
                    damagePackage.Bullet.Damage *= damageMultiplier;
                MainHealth.TakeDamage(damagePackage);
            }
        }

        public override void Die()
        {
            if (IsDead)
                return;
            IsDead = true;
            
            if (deathEffect)
            {
                if (parentToDeathPosition)
                    Instantiate(deathEffect, deathEffectPosition);
                else
                    Instantiate(deathEffect, deathEffectPosition.position, deathEffectPosition.rotation);
            }

            OnDeath?.Invoke(this);
        }
    }
}