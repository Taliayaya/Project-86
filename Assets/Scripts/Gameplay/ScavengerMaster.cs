using System;
using Gameplay.Units;
using UnityEngine;

namespace Gameplay
{
    public class ScavengerMaster : MonoBehaviour
    {
        [SerializeField] private Scavenger scavengerOwned;
        private ScavengerController _scavengerController; 
        
        
        [Header("Settings")]
        [SerializeField] private GameObject scavengerPrefab;
        [SerializeField] private bool autoCreateScavenger;
        [SerializeField] private float spawnDistance = 8;

        private void Awake()
        {
            if (autoCreateScavenger && !scavengerOwned)
            {
                scavengerOwned = Instantiate(scavengerPrefab, transform.position + Vector3.right * spawnDistance, transform.rotation).GetComponent<Scavenger>();
                if (!scavengerOwned.TryGetComponent(out _scavengerController))
                    _scavengerController = scavengerOwned.gameObject.AddComponent<ScavengerController>();
                _scavengerController.master = this;         }
           
        }

        private void OnEnable()
        {
            EventManager.AddListener("OnCallScavenger", OnCallScavenger);
            EventManager.AddListener("OnStopScavenger", OnStopScavenger);

        }

        private void OnDisable()
        {
            EventManager.RemoveListener("OnCallScavenger", OnCallScavenger);
            EventManager.RemoveListener("OnStopScavenger", OnStopScavenger);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.right * spawnDistance, 0.5f);
        }

        #region Event Callbacks

 
        private void OnCallScavenger()
        {
            if (!scavengerOwned)
                return;
            _scavengerController.State = ScavengerState.Follow;
        }

        private void OnStopScavenger()
        {
            if (!scavengerOwned)
                return;
            _scavengerController.State = ScavengerState.Idle;
        }
       

        #endregion
        
    }
}