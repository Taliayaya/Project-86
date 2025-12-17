using System;
using System.Collections.Generic;
using Cinemachine;
using Gameplay.Units;
using UnityEngine;

namespace Gameplay
{
    public class Quake : MonoBehaviour
    {
        public int maxAllowedCollisions = 20;
        public int quakeRadius = 10;
        public Faction faction;
        public LayerMask quakeLayerMask = 1;
        public AnimationCurve effectStrengthCurve;
        public AnimationCurve damageCurve;
        public CinemachineImpulseSource impulseSource;
        public List<GameObject> selfToIgnore;
        public Vector3 impulseVelocity;
        public float duration;
        
        public float Damage(float time) => effectStrengthCurve.Evaluate(time);
        
        public void UseQuake()
        {
            Debug.Log("Using quake");
            var colliders = new Collider[maxAllowedCollisions];
            var size = Physics.OverlapSphereNonAlloc(transform.position, quakeRadius, colliders, quakeLayerMask);
            impulseSource.GenerateImpulseAt(transform.position, impulseVelocity);
            for (int i = 0; i < size; i++)
            {
                var hit = colliders[i];
                if (selfToIgnore.Contains(hit.gameObject))
                    continue;
                if (hit.TryGetComponent(out IHealth health))
                {
                    var distance = Vector3.Distance(transform.position, hit.transform.position);
                    var effectPackage = new DamagePackage
                    {
                        Type = DamageType.EffectSlow,
                        Slow = new SlowEffectData()
                        {
                            Strength = Damage(distance),
                            Duration = duration
                            
                        },
                        SourcePosition = transform.position,
                        Faction = faction
                    };
                    health.TakeDamage(effectPackage);
                    Debug.Log($"Applying damage to {hit.name} with distance {distance}");
                    var damagePackage = new DamagePackage()
                    {
                        Type = DamageType.Explosion,
                        Explosion = new ExplosionData()
                        {
                            Damage = damageCurve.Evaluate(distance),
                            Radius = 0,
                        },
                        SourcePosition = transform.position,
                        Faction = faction
                    };
                    health.TakeDamage(damagePackage);
                }
            }
        }
    }
}