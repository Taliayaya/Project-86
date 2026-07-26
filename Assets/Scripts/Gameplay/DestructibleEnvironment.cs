using System;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay
{
    public class DestructibleEnvironment : MonoBehaviour
    {
        public float health = 100f;
        public UnityEvent onDamage;
        public UnityEvent<DestructibleEnvironment> onDestroyed;

        public void Damage(float damage)
        {
            if (health <= 0) return;
            health -= damage;
            onDamage?.Invoke();
            if (health <= 0)
            {
                onDestroyed?.Invoke(this);
            }
        }

        public void SetHealth(float health)
        {
            this.health = health;
        }
        private void OnDestroy()
        {
            onDestroyed?.Invoke(this);
        }
    }
}