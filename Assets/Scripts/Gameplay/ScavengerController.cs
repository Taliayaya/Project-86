using System;
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
        [SerializeField] private float followDistance = 30;

        public ScavengerMaster master;
        private ScavengerState _state = ScavengerState.Idle;

        public ScavengerState State
        {
            get => _state;
            set
            {
                _state = value;
                switch (_state)
                {
                    case ScavengerState.Hide:
                        _scavengerAgent.isStopped = false;
                        break;
                    case ScavengerState.Idle:
                        _scavengerAgent.isStopped = true;
                        _scavengerAgent.ResetPath();
                        break;
                    case ScavengerState.Reloading:
                        _scavengerAgent.isStopped = true;
                        _scavengerAgent.ResetPath();
                        break;
                    case ScavengerState.Follow:
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

        private void Update()
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
        }
        
        private void Follow()
        {
            var distance = Vector3.Distance(transform.position, master.transform.position);
            if (!master || distance < followDistance)
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
        }

        #endregion

        #region Events

        

        #endregion
        
    }
}