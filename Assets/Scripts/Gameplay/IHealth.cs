using System;
using UnityEngine;

namespace Gameplay
{
    public struct DamagePackage
    {
        public float DamageAmount;
        public Faction Faction;
        public Vector3 DamageSourcePosition;
    }
    public interface IHealth
    {
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        
        public Faction Faction { get; set; }

        public void OnTakeDamage(DamagePackage damagePackage);

        public void TakeDamage(DamagePackage damagePackage)
        {
            Health = Mathf.Clamp(Health - damagePackage.DamageAmount, 0, MaxHealth);
            OnTakeDamage(damagePackage);
            
            if (!Alive)
                Die();
        }

        public void Die();

        public bool Alive => Health > 0;
    }
}