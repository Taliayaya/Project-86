using System;
using UnityEngine;

namespace Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class KillerGround : MonoBehaviour
    {
        public float damage = 86000f;

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out IHealth health))
            {
                health.TakeDamage(new DamagePackage()
                {
                    DamageAmount = damage,
                    DamageSourcePosition = transform.position,
                    Faction = Faction.Neutral
                });
            }
        }
    }
}