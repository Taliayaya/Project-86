using System;
using Cinemachine;
using ScriptableObjects;
using UnityEngine;

namespace Gameplay
{
   public class Bullet : MonoBehaviour
   {
      private Vector3 _origin;
      private Faction _factionOrigin; // To avoid killing each other
      public AmmoSO Ammo { get; set; }

      private float Damage => Ammo.damageCurve.Evaluate((_origin - transform.position).magnitude);

      private DamagePackage _damagePackage;
      private float _lifeTime;
      private CinemachineImpulseSource _impulseSource;

      #region Unity Callbacks

      private void Start()
      {
         _origin = transform.position;
         _impulseSource = GetComponent<CinemachineImpulseSource>();
         var layer = LayerMask.NameToLayer("Projectiles");
         Physics.IgnoreLayerCollision(layer, layer, true);
      }

      public void Init(AmmoSO ammoSo, Faction factionOrigin)
      {
         Ammo = ammoSo;
         _factionOrigin = factionOrigin;
      }

      public void InitLifeTime(float lifeTime)
      {
         Invoke(nameof(Expire), lifeTime);
      }
      
      private void ApplyCameraShake()
      {
         if (_impulseSource == null) return;
         _impulseSource.GenerateImpulse(Ammo.explosionForce);
      }

      private void ApplyExplosionQuake(Vector3 origin)
      {
         Collider[] results = new Collider[20];
         var size = Physics.OverlapSphereNonAlloc(origin, Ammo.explosionRadiusQuake, results);
         for (int i = 0; i < size; i++)
         {
            var col = results[i];
            if (col.TryGetComponent(out Rigidbody rb))
            {
               rb.AddExplosionForce(Ammo.explosionForce, origin, Ammo.explosionRadiusQuake);
            }
         }
      }
      

      private void OnCollisionEnter(Collision other)
      {
         _damagePackage = new DamagePackage()
         {
            Faction = _factionOrigin,
            DamageAmount = Damage,
            DamageSourcePosition = _origin,
            DamageAudioClip = Ammo.onHitSound,
            BulletSize = Ammo.bulletSize,
            IsBullet = true
         };
         ApplyCameraShake();

         //Debug.Log("bullet hit " + other.gameObject.name + other.collider.name);
         // commented to avoid damaging twice
         bool isHealthComponent = other.collider.CompareTag("HealthComponent");
         IHealth health;
         if (((isHealthComponent && other.collider.TryGetComponent(out health)) ||
              other.gameObject.TryGetComponent(out health)) && Ammo.explosionRadius == 0)
         {
            // If the bullet hit a non-hitbox, don't damage it and play a special effect
            // like the bullet ricocheting off the armor
            if (other.collider.CompareTag("NonHitbox"))
            {
               DeflectBullet(other);
               return;
            }

            Debug.Log("Bullet hit " + other.collider.name + " on " + other.gameObject.name + " for " +
                      _damagePackage.DamageAmount + " damage");
            DamageResponse response = health.TakeDamage(_damagePackage);
            if (response.Status == DamageResponse.DamageStatus.Deflected)
            {
               DeflectBullet(other);
               return;
            }

            // implement on it effect
            Destroy(gameObject);
            return;
         }
         else
         {
            if (Ammo.missEffect != null)
            {
               GameObject missEffect;
               if (Ammo.missEffectLookTop)
                  missEffect = Instantiate(Ammo.missEffect, transform.position, Quaternion.Euler(-90, 0, 0));
               else
                  missEffect = Instantiate(Ammo.missEffect, transform.position,
                     Quaternion.LookRotation(transform.position - _origin));
               missEffect.transform.localScale *= Ammo.missEffectSizeMult;
            }
         }

         if (Ammo.explosionRadius == 0)
         {
            Destroy(gameObject);
            return;
         }

         var colliders = new Collider[5];
         var position = other.contacts[0].point;
         var size = Physics.OverlapSphereNonAlloc(position, Ammo.explosionRadius, colliders, Ammo.explosionLayerMask);
         Debug.DrawLine(position, position + Vector3.up * 10, Color.red, 10f);
         for (int i = 0; i < size; i++)
         {
            var col = colliders[i];
            if (col.TryGetComponent(out IHealth health2))
            {
               health2.TakeDamage(_damagePackage);
            }
         }

         Instantiate(Ammo.explosionPrefab, position, Quaternion.LookRotation(position - _origin));
         Destroy(gameObject);
      }

      private void DeflectBullet(Collision other)
      {
         if (Ammo.armorMissEffect != null)
         {
            Vector3 reflectedAngle = Vector3.Reflect(transform.forward, other.contacts[0].normal);
            var effect = Instantiate(Ammo.armorMissEffect, other.contacts[0].point,
               Quaternion.LookRotation(reflectedAngle));
            effect.transform.localScale *= Ammo.armorMissEffectSizeMult;
            Debug.Log("Armor Miss Effect");
         }

         Debug.Log("Armor Hit" + other.collider.name + " on " + other.gameObject.name + " for " +
                   _damagePackage.DamageAmount + " damage");
         Destroy(gameObject);
      }

      #endregion

      private void FixedUpdate()
      {
         _lifeTime += Time.fixedDeltaTime;
      }

      private void Expire()
      {
         Destroy(gameObject);
      }

   }
}