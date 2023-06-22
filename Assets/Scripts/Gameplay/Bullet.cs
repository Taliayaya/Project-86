using System;
using UnityEngine;

namespace Gameplay
{
   public class Bullet : MonoBehaviour
   {
      private AnimationCurve _damageCurve;
      private Vector3 _origin;
      private Faction _factionOrigin; // To avoid killing each other
      private GameObject _explosionPrefab;
      
      private float Damage => _damageCurve.Evaluate(_lifeTime);

      private DamagePackage _damagePackage;
      private float _lifeTime;

      #region Unity Callbacks

      private void Start()
      {
         _origin = transform.position;
         var layer = LayerMask.NameToLayer("Projectiles");
         Physics.IgnoreLayerCollision(layer, layer, true);
      }
      
      public void Init(AnimationCurve damageCurve, Faction factionOrigin, GameObject explosionPrefab)
      {
         _damageCurve = damageCurve;
         _factionOrigin = factionOrigin;
         _explosionPrefab = explosionPrefab;
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
            DamageAmount = Damage
         };
         if (other.gameObject.TryGetComponent(out IHealth health))
         {
            health.TakeDamage(_damagePackage);
         }

         Debug.Log("Bullet hit something! " + other.gameObject.name);
         Instantiate(_explosionPrefab, transform.position, Quaternion.LookRotation(transform.position - _origin));
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