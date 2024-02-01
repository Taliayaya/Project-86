using System;
using System.Collections;
using Gameplay.Units;
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
    public class ScavengerController : MonoBehaviour
    {
        private NavMeshAgent _scavengerAgent;
        private Scavenger _scavenger;
        private float _lastHealth;
        
        public UnityEvent<ScavengerState> onScavengerStateChange = new UnityEvent<ScavengerState>();
        [Header("Settings")]
        [SerializeField] private float followDistance = 10;

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
                        _scavengerAgent.isStopped = false;
                        break;
                    case ScavengerState.Idle:
                        if (State != _previousState && _scavenger.idleSound) 
                            _scavenger.audioSource.PlayOneShot(_scavenger.idleSound);
                        _scavengerAgent.isStopped = true;
                        _scavengerAgent.ResetPath();
                        break;
                    case ScavengerState.Reloading:
                        _scavengerAgent.isStopped = true;
                        _scavengerAgent.ResetPath();
                        break;
                    case ScavengerState.Follow:
                        
                        if (State != _previousState && _scavenger.followSound)
                            _scavenger.audioSource.PlayOneShot(_scavenger.followSound);
                        _scavengerAgent.isStopped = false;
                        break;
                    case ScavengerState.GoTo:
                        _scavengerAgent.isStopped = false;
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
            _scavenger.onTakeDamage.AddListener(OnHealthChange);
        }
        
        private void Start()
        {
            StartCoroutine(ScavengerActions());
        }

        private Vector3 _enemyOrigin;
        private void OnHealthChange(DamagePackage arg0)
        {
            Debug.Log("Take damage of " + arg0.DamageAmount);
            if (arg0.DamageAmount >= 5)
                State = ScavengerState.Hide;
            _enemyOrigin = arg0.DamageSourcePosition;
        }
        
        public void Seek(Vector3 location)
        {
            if (!NavMesh.SamplePosition(location, out var hit1, 30, -1))
                return;
            _scavengerAgent.SetDestination(hit1.position);
        }
        
        public void GoTo(Transform target)
        {
            _scavengerAgent.SetDestination(target.position);
            if (_scavenger.goToSound)
                _scavenger.audioSource.PlayOneShot(_scavenger.goToSound);
        }

        private IEnumerator ScavengerActions()
        {
            while (true)
            {
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
            _scavengerAgent.SetDestination(dest);
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
            _scavengerAgent.SetDestination(master.transform.position);
            
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