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
        public float damageMultiplier = 1f;
        
        public Transform deathEffectPosition;
        
        protected bool IsDead;

        private void Awake()
        {
            if (mainHealthTakesDamage)
                MainHealth = mainHealthGo.GetComponent<Unit>();
        }

        public override void OnTakeDamage(DamagePackage damagePackage)
        {
            if (IsDead) return;
            base.OnTakeDamage(damagePackage);
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
                Instantiate(deathEffect, deathEffectPosition);
            OnDeath?.Invoke(this);
        }
    }
}