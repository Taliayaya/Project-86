using System;
using UnityEngine;

namespace Gameplay
{
    public struct DamagePackage
    {
        public float DamageAmount;
        public Faction Faction;
        public Vector3 DamageSourcePosition;
        public AudioClip DamageAudioClip;
        public bool IsBullet;
        public float BulletSize;
    }
    public interface IHealth
    {
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Armor { get; set; }
        
        public Faction Faction { get; set; }

        public void OnTakeDamage(DamagePackage damagePackage);

        public void TakeDamage(DamagePackage damagePackage)
        {
            if (damagePackage.IsBullet && damagePackage.DamageAmount < Armor)
                return;
            Health = Mathf.Clamp(Health - damagePackage.DamageAmount, 0, MaxHealth);
            Debug.Log($"{Faction} took {damagePackage.DamageAmount} damage. Health: {Health}");
            OnTakeDamage(damagePackage);
            
            if (!Alive)
                Die();
        }

        public void Die();

        public bool Alive => Health > 0;
    }
}