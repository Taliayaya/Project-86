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

    public struct DamageResponse
    {
        public enum DamageStatus
        {
            Taken,
            Deflected,
        }

        public DamageStatus Status;
        public float DamageReceived;
        public float RemainingHealth;

    }
    public interface IHealth
    {
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Armor { get; set; }
        
        public Faction Faction { get; set; }

        public void OnTakeDamage(DamagePackage damagePackage);

        public DamageResponse TakeDamage(DamagePackage damagePackage)
        {
            if (damagePackage.IsBullet && damagePackage.DamageAmount < Armor)
                return new DamageResponse() { Status = DamageResponse.DamageStatus.Deflected, DamageReceived = 0};
            Health = Mathf.Clamp(Health - damagePackage.DamageAmount, 0, MaxHealth);
            Debug.Log($"{Faction} took {damagePackage.DamageAmount} damage. Health: {Health}");
            OnTakeDamage(damagePackage);
            
            if (!Alive)
                Die();
            return new DamageResponse() { Status = DamageResponse.DamageStatus.Taken, DamageReceived = damagePackage.DamageAmount, RemainingHealth = Health};
        }

        public void Die();

        public bool Alive => Health > 0;
    }
}