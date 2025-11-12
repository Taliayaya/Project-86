using System;
using System.Collections;
using Managers;
using UnityEngine;

namespace Utility
{
    public class AutoReturnPool : MonoBehaviour
    {
        
        void OnEnable()
        {
            StartCoroutine(CheckIfAlive());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        IEnumerator CheckIfAlive ()
        {
            while(true)
            {
                yield return new WaitForSeconds(0.5f);
                if(!GetComponent<ParticleSystem>().IsAlive(true))
                {
                    PoolManager.Instance.BackToPool(gameObject);
                }
            }
        }    }
}