using System;
using ScriptableObjects;
using UnityEngine;

namespace Gameplay
{
   public class Bullet : MonoBehaviour
   {
      private Vector3 _origin;
      private Faction _factionOrigin; // To avoid killing each other
      public AmmoSO Ammo { get; set; }
      
      private float Damage => Ammo.damageCurve.Evaluate(_lifeTime);

      private DamagePackage _damagePackage;
      private float _lifeTime;

      #region Unity Callbacks

      private void Start()
      {
         _origin = transform.position;
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

      private void OnCollisionEnter(Collision other)
      {
         _damagePackage = new DamagePackage()
         {
            Faction = _factionOrigin,
            DamageAmount = Damage,
            DamageSourcePosition = _origin
         };
         
         // commented to avoid damaging twice
         //if (other.gameObject.TryGetComponent(out IHealth health))
         //{
         //   health.TakeDamage(_damagePackage);
         //}

         var colliders = new Collider[5];
         var position = other.contacts[0].point;
         var size = Physics.OverlapSphereNonAlloc(position, Ammo.explosionRadius, colliders, Ammo.explosionLayerMask);
         Debug.Log($"Explosion size: {size} and collided with {other.gameObject.name}");
         Debug.DrawLine(position, position + Vector3.up * 10, Color.red, 10f);
         for(int i = 0; i < size; i++)
         {
            var col = colliders[i];
            Debug.Log($"Explosion collider: {col.name}");
            if (col.TryGetComponent(out IHealth health2))
            {
               health2.TakeDamage(_damagePackage);
            }
         }

         Instantiate(Ammo.explosionPrefab, position,  Quaternion.LookRotation(position - _origin));
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