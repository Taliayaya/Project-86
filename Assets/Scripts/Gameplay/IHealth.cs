using System;
using UnityEngine;

namespace Gameplay
{
    public interface IHealth
    {
        public float Health { get; set; }
        public float MaxHealth { get; set; }

        public void OnTakeDamage();

        public void TakeDamage(float damageAmount)
        {
            Health = Mathf.Clamp(Health - damageAmount, 0, MaxHealth);
            OnTakeDamage();
            
            if (!Alive)
                Die();
        }

        protected void Die();

        public bool Alive => Health > 0;
    }
}