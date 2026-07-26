using Gameplay;
using UnityEngine;

namespace AI
{
    public class ApplyDamage : MonoBehaviour
    {
        [SerializeField] Faction faction;
        [SerializeField] private bool mainPlayerOnly = true;

        #region Explosion Damage
        
        [Header("Explosion Data")]
        [SerializeField] float explosionDamage;
        public void ApplyExplosionTo(GameObject target)
        {
            var targetHealth = target.GetComponentInParent<IHealth>();
            if (targetHealth == null)
                return;
            DamagePackage damagePackage = new DamagePackage()
            {
                Faction = faction,
                Type = DamageType.Explosion,
                Explosion = new ExplosionData()
                {
                    Damage = explosionDamage,
                    Radius = 10
                },
                SourcePosition = transform.position,
            };
            targetHealth.TakeDamage(damagePackage);
        }

        public void ApplyExplosionTo(Collider target)
        {
            ApplyExplosionTo(target.gameObject);
        }
        
        #endregion

        #region Knockback

        [Header("Knockback Data")] [SerializeField]
        private float knockbackStength;
        [SerializeField] private float knockbackUpwardModified;

        public void ApplyKnockbackFromThisTo(GameObject target)
        {
            Debug.Log("Applying knockback from " + gameObject.name + " to " + target.name);
            var targetHealth = target.GetComponentInParent<IHealth>();
            if (targetHealth == null)
                return;
            if (mainPlayerOnly && PlayerManager.Player != null && target.gameObject != PlayerManager.Player.gameObject)
                return;
            Vector3 knockbackDirection = target.transform.position - transform.position; 
            knockbackDirection.y = 0; 
            knockbackDirection.Normalize();
            
            knockbackDirection.y += knockbackUpwardModified;
            knockbackDirection.Normalize();

            DamagePackage damagePackage = new DamagePackage()
            {
                Faction = faction,
                Type = DamageType.EffectPush,
                SourcePosition = transform.position,
                Knockback = new PushEffectData()
                {
                    Direction = knockbackDirection,
                    Strength = knockbackStength
                }
            };
            targetHealth.TakeDamage(damagePackage);
        }

        public void ApplyKnockbackFromThisTo(Collider target)
        {
            ApplyKnockbackFromThisTo(target.gameObject);
        }


        #endregion
    }
}