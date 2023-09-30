using System;
using System.Collections;
using Cinemachine;
using Gameplay.Units;
using ScriptableObjects.GameParameters;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay
{
    public class ScavengerMaster : MonoBehaviour
    {
        [SerializeField] private Scavenger scavengerOwned;
        public ScavengerParameters scavengerParameters;
        private ScavengerController _scavengerController; 
        public bool emitOrListen = true;
        
        
        [Header("Settings")]
        [SerializeField] private GameObject scavengerPrefab;
        [SerializeField] private bool autoCreateScavenger;
        [SerializeField] private float spawnDistance = 8;
        [SerializeField] private float maxRaycastDistance = 4000;
        [SerializeField] private LayerMask raycastLayerMask;
        [SerializeField] private bool respawnScavengerOnDeath = true;
        
       
        [Header("References")] [SerializeField]
        private GameObject goToTargetIndicatorPrefab;

        [SerializeField] private Transform juggCamera;

        #region Unity Callbacks

        private void Awake()
        {
            if (autoCreateScavenger && !scavengerOwned)
            {
                scavengerOwned = Instantiate(scavengerPrefab, transform.position + Vector3.right * spawnDistance, transform.rotation).GetComponent<Scavenger>();
                if (!scavengerOwned.TryGetComponent(out _scavengerController))
                    _scavengerController = scavengerOwned.gameObject.AddComponent<ScavengerController>();
                _scavengerController.master = this;         
                InitListeners();
            }
           
        }

        private void InitListeners()
        {
            scavengerOwned.onHealthChange.AddListener((health, maxHealth) =>
            {
                if (emitOrListen)
                    EventManager.TriggerEvent("OnScavengerHealthChange", health / maxHealth);
            });
            _scavengerController.onScavengerStateChange.AddListener(state =>
            {
                if (emitOrListen)
                    EventManager.TriggerEvent("OnScavengerStateChange", state);
            });
            if (emitOrListen)
            {
                EventManager.TriggerEvent("OnScavengerHealthChange", scavengerOwned.Health / scavengerOwned.MaxHealth);
                EventManager.TriggerEvent("OnScavengerStateChange", _scavengerController.State);
                EventManager.TriggerEvent("OnScavengerNameChange", scavengerParameters.scavengerName);
            }

        }

        private void OnEnable()
        {
            EventManager.AddListener("OnCallScavenger", OnCallScavenger);
            EventManager.AddListener("OnStopScavenger", OnStopScavenger);
            EventManager.AddListener("OnOrderScavenger", OnOrderScavenger);
            EventManager.AddListener("OnOrderScavengerSubmit", OnPrimaryFire);

        }

        private void OnDisable()
        {
            EventManager.RemoveListener("OnCallScavenger", OnCallScavenger);
            EventManager.RemoveListener("OnStopScavenger", OnStopScavenger);
            EventManager.RemoveListener("OnOrderScavenger", OnOrderScavenger);
            EventManager.RemoveListener("OnOrderScavengerSubmit", OnPrimaryFire);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.right * spawnDistance, 0.5f);
        }
        
        private void Update()
        {
            if (scavengerOwned)
            {
                var distanceFromScavenger = Vector3.Distance(transform.position, scavengerOwned.transform.position);
                EventManager.TriggerEvent("OnScavengerDistanceChange", distanceFromScavenger);
            }
        }
        
        #endregion

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



        private bool _isOrdering = false;
        private Coroutine _orderCoroutine;
        private void OnOrderScavenger()
        {
            _isOrdering = !_isOrdering;
            if (_isOrdering)
            {
                _orderCoroutine = StartCoroutine(OrderingScavengerCoroutine());
            }
        }
        
        private void OnPrimaryFire()
        {
            if (!_isOrdering)
                return;
            if (!scavengerOwned)
                return;
            _scavengerController.State = ScavengerState.GoTo;
            _scavengerController.GoTo(_targetPosition);
            _isOrdering = false;
            //if (_orderCoroutine != null)
            //    StopCoroutine(_orderCoroutine);
            //if (_targetIndicator)
            //    Destroy(_targetIndicator);
        }


        #endregion

        private Vector3 _targetPosition;
        private GameObject _targetIndicator;
        private IEnumerator OrderingScavengerCoroutine()
        {
            _targetPosition = transform.position;
            
            while (_isOrdering)
            {
                if (!GetLookAtPosition(out var hit))
                    yield return null;
                if (NavMesh.SamplePosition(hit.point, out var navMeshHit, 40f, -1))
                    _targetPosition = navMeshHit.position;
                if (!_targetIndicator)
                    _targetIndicator = Instantiate(goToTargetIndicatorPrefab);
                _targetIndicator.transform.position = _targetPosition + Vector3.up * 0.1f;
                yield return null;
            }
            if (_targetIndicator)
                Destroy(_targetIndicator);
        }
        
        private bool GetLookAtPosition(out RaycastHit hitOut)
        {
            if (!Physics.Raycast(juggCamera.position, juggCamera.forward, out var hit, maxRaycastDistance, raycastLayerMask))
            {
                hitOut = new RaycastHit();
                return false;
            }
            hitOut = hit;
            return true;
        }
        
    }
}