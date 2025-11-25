using System;
using System.Collections;
using Gameplay.Units;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Gameplay
{
    public enum ScavengerState
    {
        Hide,
        Idle,
        Follow,
        Reloading,
        GoTo,
    }
    [RequireComponent(typeof(NavMeshAgent), typeof(Scavenger))]
    public class ScavengerController : NetworkBehaviour
    {
        private NavMeshAgent _scavengerAgent;
        private Scavenger _scavenger;
        private float _lastHealth;
        
        NetworkVariable<bool> _isStopped = new NetworkVariable<bool>(false);

        public bool IsStopped
        {
            get => _isStopped.Value;
            set => _isStopped.Value = value;
        }

        public UnityEvent<ScavengerState> onScavengerStateChange = new UnityEvent<ScavengerState>();
        [Header("Settings")]
        [SerializeField] private float followDistance = 20;

        public ScavengerMaster master;
        private ScavengerState _state = ScavengerState.Idle;
        
        

        private ScavengerState _previousState = ScavengerState.Idle;
        public ScavengerState State
        {
            get => _state;
            set
            {
                _previousState = _state;
                _state = value;
                if (!gameObject.activeSelf)
                    return;
                switch (_state)
                {
                    case ScavengerState.Hide:
                        if (State != _previousState && _scavenger.hideSound)
                            _scavenger.audioSource.PlayOneShot(_scavenger.hideSound);
                        IsStopped = false;
                        break;
                    case ScavengerState.Idle:
                        if (State != _previousState && _scavenger.idleSound) 
                            _scavenger.audioSource.PlayOneShot(_scavenger.idleSound);
                        IsStopped = true;
                        _scavengerAgent.ResetPath();
                        break;
                    case ScavengerState.Reloading:
                        IsStopped = true;
                        _scavengerAgent.ResetPath();
                        break;
                    case ScavengerState.Follow:
                        
                        if (State != _previousState && _scavenger.followSound)
                            _scavenger.audioSource.PlayOneShot(_scavenger.followSound);
                        IsStopped = false;
                        break;
                    case ScavengerState.GoTo:
                        IsStopped = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();

                }

                onScavengerStateChange?.Invoke(_state);
            }
        }

        private void Awake()
        {
            _scavengerAgent = GetComponent<NavMeshAgent>();
            _scavenger = GetComponent<Scavenger>();
            _isStopped.OnValueChanged += (old, current) => _scavengerAgent.isStopped = current;
            if (IsOwner)
                _scavenger.onTakeDamage.AddListener(OnHealthChange);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _scavengerAgent.Warp(transform.position);
            _scavengerAgent.enabled = true;
        }

        private void Start()
        {
            StartCoroutine(ScavengerActions());
        }

        private Vector3 _enemyOrigin;
        private void OnHealthChange(DamagePackage arg0)
        {
            Debug.Log("Take damage of " + arg0.GetDamage());
            if (arg0.GetDamage() >= 5)
                State = ScavengerState.Hide;
            _enemyOrigin = arg0.SourcePosition;
        }
        
        public void Seek(Vector3 location)
        {
            if (!NavMesh.SamplePosition(location, out var hit1, 30, -1))
                return;
            SetDestinationRpc(hit1.position);
        }
        
        public void GoTo(Transform target)
        {
            SetDestinationRpc(target.position);
            if (_scavenger.goToSound)
                _scavenger.audioSource.PlayOneShot(_scavenger.goToSound);
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        public void SetDestinationRpc(Vector3 destination)
        {
            _scavengerAgent.SetDestination(destination);
        }
        
        private IEnumerator ScavengerActions()
        {
            while (true)
            {
                if (!HasAuthority || !IsSpawned)
                {
                    yield break;
                }

                switch (State)
                {
                    case ScavengerState.Hide:
                        Hide();
                        break;
                    case ScavengerState.Idle:
                        Idle();
                        break;
                    case ScavengerState.Follow:
                        Follow();
                        break;
                    case ScavengerState.Reloading:
                        Reloading();
                        break;
                    case ScavengerState.GoTo:
                        GoTo();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                yield return new WaitForSeconds(0.5f);
            }
        }


        #region States Methods
        
        private void Hide()
        {
            Vector3 position = transform.position;
            Vector3 fleeVector = _enemyOrigin - position;
            Seek(position - fleeVector);
            
            
        }
        
        private void Idle()
        {
            if (!master)
            {
                State = ScavengerState.Hide;
                return;
            }
            if (_scavenger.IsReloading)
            {
                State = ScavengerState.Reloading;
                return;
            }
            
        }
        
        private void GoTo()
        {
            if (_scavengerAgent.pathStatus == NavMeshPathStatus.PathComplete && _scavengerAgent.remainingDistance < 1)
            {
                State = ScavengerState.Idle;
                return;
            }
            
        }

        public void GoTo(Vector3 dest)
        {
            State = ScavengerState.GoTo;
            SetDestinationRpc(dest);
            if (_scavenger.goToSound)
                _scavenger.audioSource.PlayOneShot(_scavenger.goToSound);
        }
        
        private void Follow()
        {
            if (!master)
            {
                State = ScavengerState.Idle;
                return;
            }

            var distance = Vector3.Distance(transform.position, master.transform.position);
            if (distance < followDistance)
            {
                State = ScavengerState.Idle;
                return;
            }
            SetDestinationRpc(master.transform.position);
            
        }
        
        private void Reloading()
        {
            if (!_scavenger.IsReloading)
            {
                State = ScavengerState.Idle;
                return;
            }
            if (State != _previousState && _scavenger.reloadSound)
                _scavenger.audioSource.PlayOneShot(_scavenger.reloadSound);
        }

        #endregion

        #region Events

        

        #endregion
        
    }
}