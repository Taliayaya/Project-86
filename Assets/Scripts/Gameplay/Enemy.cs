using System;
using UnityEngine;

namespace Gameplay
{
    public class Enemy : MonoBehaviour, IHealth
    {
        public float Health { get; set; } = 100;
        public float MaxHealth { get; set; } = 100;

        public void OnTakeDamage()
        {
        }

        void IHealth.Die()
        {
            Destroy(gameObject);
        }

        private void Awake()
        {
            Health = 300;
            MaxHealth = 300;
        }
    }
}