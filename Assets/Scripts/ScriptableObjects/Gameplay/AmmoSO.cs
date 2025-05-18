using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "Ammo", menuName = "Scriptable Objects/Ammo")]
    [Serializable]
    public class AmmoSO : ScriptableObject, IEquatable<AmmoSO>
    {
        [Header("Information")] public string gunTypeName = "Main Gun : 57mm APFSDS";
        [Header("Ammo Properties")] public AnimationCurve damageCurve;
        public float fireRate = 1f;
        public int maxAmmo = 20;
        public float reloadTime = 1f;
        public float maxLifetime = 5;
        public float forcePower = 500f;
        [Tooltip("In mm")] public float bulletSize = 10f;

        [Header("Explosion Properties")] [Range(0, 200)]
        public float explosionRadius = 1f;

        public LayerMask explosionLayerMask = 1;

        [Header("Ammo Prefabs")] public GameObject prefab;
        public List<AudioClip> fireSounds;
        public AudioClip reloadSound;
        public GameObject explosionPrefab;
        public GameObject muzzleFlashPrefab;


        public AudioClip onHitSound;
        public GameObject missEffect;

        public float missEffectSizeMult = 1f;

        // if the miss effect should always look upward or the player
        public bool missEffectLookTop = false;

        [Header("Armor Miss Effect")] public GameObject armorMissEffect;
        public float armorMissEffectSizeMult = 1f;

        [Header("Explosion quake")] public float explosionForce = 0;
        public float explosionRadiusQuake = 0;

        public AudioClip GetRandomFireSound()
        {
            return fireSounds[Random.Range(0, fireSounds.Count)];
        }

        public float Damage(int time) => damageCurve.Evaluate(time);

        #region IEquateable

        public bool Equals(AmmoSO other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && gunTypeName == other.gunTypeName && Equals(damageCurve, other.damageCurve) &&
                   fireRate.Equals(other.fireRate) && maxAmmo == other.maxAmmo && reloadTime.Equals(other.reloadTime) &&
                   bulletSize.Equals(other.bulletSize);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AmmoSO)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), gunTypeName, damageCurve, fireRate, maxAmmo, reloadTime,
                bulletSize);
        }

        #endregion
    }
}