using System;
using System.Collections;
using Managers;
using UnityEngine;
using UnityEngine.VFX;

namespace Utility
{
    public class AutoReturnPool : MonoBehaviour
    {
        private ParticleSystem _particleSystem;
        private VisualEffect _visualEffect;

        public float timeout = 0;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _visualEffect = GetComponent<VisualEffect>();
        }

        void OnEnable()
        {
            if (timeout > 0)
                Invoke(nameof(Return), timeout);
            else
                StartCoroutine(CheckIfAlive());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        IEnumerator CheckIfAlive()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                if (_particleSystem && !_particleSystem.IsAlive(true))
                    Return();

                if (_visualEffect && _visualEffect.aliveParticleCount == 0)
                    Return();
            }
        }

        void Return()
        {
            PoolManager.Instance.BackToPool(gameObject);
        }
    }
}