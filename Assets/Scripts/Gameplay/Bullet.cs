using System;
using UnityEngine;

namespace Gameplay
{
   public class Bullet : MonoBehaviour
   {
      [HideInInspector] public float damage;
      [HideInInspector] public Vector3 origin;
      [HideInInspector] public GameObject explosionPrefab;

         #region Unity Callbacks

         private void Start()
         {
            origin = transform.position;
            var layer = LayerMask.NameToLayer("Projectiles");
            Physics.IgnoreLayerCollision(layer, layer, true);
         }
         
         public void InitLifeTime(float lifeTime)
         {
            Invoke(nameof(Expire), lifeTime);
         }

         private void OnCollisionEnter(Collision other)
         {
            if (other.gameObject.CompareTag("Enemy"))
            {
               other.gameObject.GetComponent<IHealth>().TakeDamage(damage);
            }
            Instantiate(explosionPrefab, transform.position, Quaternion.LookRotation(transform.position - origin));
            Destroy(gameObject);
         }

         #endregion

         private void Expire()
         {
            Destroy(gameObject);
         }

   }
}