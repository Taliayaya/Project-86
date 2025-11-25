using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay
{
    public enum DamageType
    {
        Bullet,
        Explosion,
        EffectSlow,
        Blade
    }
    
    public struct BulletData : INetworkSerializable
    {
        public float Damage;
        public float Size;
        public Vector3 HitPoint;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref Size);
            serializer.SerializeValue(ref HitPoint);
        }
    }

    public struct BladeData : INetworkSerializable
    {
        public float Damage;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Damage);
        }
    }

    public struct ExplosionData : INetworkSerializable
    {
        public float Damage;
        public float Radius;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref Radius);
        }
    }

    public struct SlowEffectData : INetworkSerializable
    {
        public float Strength;
        public float Duration;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Strength);
            serializer.SerializeValue(ref Duration);
        }
    }

    public struct DamagePackage : INetworkSerializable
    {
        public DamageType Type;
        public Faction Faction;
        public Vector3 SourcePosition;
        public AudioClip Audio;

        // Union fields:
        public BulletData Bullet;
        public ExplosionData Explosion;
        public SlowEffectData Slow;
        public BladeData Blade;
        
        public float GetDamage()
        {
            if (Type == DamageType.Bullet) return Bullet.Damage;
            if (Type == DamageType.Blade) return Blade.Damage;
            if (Type == DamageType.Explosion) return Explosion.Damage;
            return 0;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Type);
            serializer.SerializeValue(ref Faction);
            serializer.SerializeValue(ref SourcePosition);
            // serializer.SerializeValue(ref Audio);

            // Serialize only the relevant block
            switch (Type)
            {
                case DamageType.Bullet:
                    serializer.SerializeValue(ref Bullet);
                    break;
                case DamageType.Explosion:
                    serializer.SerializeValue(ref Explosion);
                    break;
                case DamageType.EffectSlow:
                    serializer.SerializeValue(ref Slow);
                    break;
                case DamageType.Blade:
                    serializer.SerializeValue(ref Blade);
                    break;
            }
        }
    }

    public struct DamageResponse : INetworkSerializable
    {
        public enum DamageStatus
        {
            Taken,
            Deflected,
        }

        public DamageStatus Status;
        public float DamageReceived;
        public float RemainingHealth;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Status);
            serializer.SerializeValue(ref DamageReceived);
            serializer.SerializeValue(ref RemainingHealth);
        }
    }
    public interface IHealth
    {
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public float Armor { get; set; }

        public Faction Faction { get; set; }

        public void OnTakeDamage(DamagePackage damagePackage);

        public DamageResponse TakeDamage(DamagePackage damagePackage);

        public void Die();
    }
}