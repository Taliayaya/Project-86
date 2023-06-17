using Gameplay;
using UnityEngine;

namespace DefaultNamespace.Gameplay
{
    public class Enemy : MonoBehaviour, IHealth
    {
        private float _health;
        private float _maxHealth;

        float IHealth.Health
        {
            get => _health;
            set => _health = value;
        }

        float IHealth.MaxHealth
        {
            get => _maxHealth;
            set => _maxHealth = value;
        }

        public void OnTakeDamage()
        {
        }

        void IHealth.Die()
        {
            Destroy(gameObject);
        }
    }
}