using System;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay
{
    public struct DamagePackage : INetworkSerializable
    {
        public float DamageAmount;
        public Faction Faction;
        public Vector3 DamageSourcePosition;
        public AudioClip DamageAudioClip;
        public bool IsBullet;
        public float BulletSize;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref DamageAmount);
            serializer.SerializeValue(ref Faction);
            serializer.SerializeValue(ref DamageSourcePosition);
            //serializer.SerializeValue(ref DamageAudioClip);
            serializer.SerializeValue(ref IsBullet);
            serializer.SerializeValue(ref BulletSize);
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