using System;
using Gameplay.Units;
using UnityEngine;
using UnityEngine.AI;

namespace Gameplay
{
    public enum ScavengerState
    {
        Hide,
        Idle,
        Follow,
    }
    [RequireComponent(typeof(NavMeshAgent), typeof(Scavenger))]
    public class ScavengerController : MonoBehaviour
    {
        private NavMeshAgent _scavengerAgent;
        private Scavenger _scavenger;
        private float _lastHealth;
        
        [Header("Settings")]
        [SerializeField] private float followDistance = 30;

        public ScavengerMaster master;
        private ScavengerState _state;

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
                        break;
                    case ScavengerState.Follow:
                        _scavengerAgent.isStopped = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void Awake()
        {
            _scavengerAgent = GetComponent<NavMeshAgent>();
            _scavenger = GetComponent<Scavenger>();
            _scavenger.onHealthChange.AddListener(OnHealthChange);
        }

        private void OnHealthChange(float arg0, float arg1)
        {
            if (arg0 < _lastHealth)
                State = ScavengerState.Hide;
            _lastHealth = arg0;
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        #region States Methods
        
        private void Hide()
        {
            
        }
        
        private void Idle()
        {
            
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

        #endregion

        #region Events

        

        #endregion
        
    }
}