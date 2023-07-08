using System;
using UnityEngine;

namespace Gameplay
{
    public class Explosion : MonoBehaviour
    {
        public float delayBeforeExplosion = 0.1f;
        public int maxAllowedCollisions = 20;
        public int deathExplodeRadius = 10;
        public LayerMask deathExplodeLayerMask = 1;
        public AnimationCurve damageCurve;
        
        public float Damage(float time) => damageCurve.Evaluate(time);

        public void Awake()
        {
            Invoke(nameof(AOEExplode), delayBeforeExplosion);
        }
        
        private void AOEExplode()
        {
            var colliders = new Collider[maxAllowedCollisions];
            var size = Physics.OverlapSphereNonAlloc(transform.position, deathExplodeRadius, colliders, deathExplodeLayerMask);
            for (int i = 0; i < size; i++)
            {
                var hit = colliders[i];
                if (hit.TryGetComponent(out IHealth health))
                {
                    var distance = Vector3.Distance(transform.position, hit.transform.position);
                    var damagePackage = new DamagePackage
                    {
                        DamageAmount = Damage(distance),
                        DamageSourcePosition = transform.position,
                        Faction = Faction.Neutral
                    };
                    health.TakeDamage(damagePackage);
                }
            }
        }
    }
}