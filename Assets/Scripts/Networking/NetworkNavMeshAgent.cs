using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Networking
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NetworkNavMeshAgent : NetworkBehaviour
    {
        public NavMeshAgent Agent { get; private set; }

        public float sprintSpeedMultiplier = 1.2f;
        private float _defaultSpeed;
        private bool _isSprinting;
        
        public NetworkVariable<bool> isEngineOn = new NetworkVariable<bool>(true);

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            isEngineOn.OnValueChanged += OnEngineStatusChanged;
        }

        private void OnEngineStatusChanged(bool previousValue, bool newValue)
        {
            if (previousValue == newValue)
                return;
            if (newValue)
            {
                Agent.isStopped = false;
            }
            else
            {
                Agent.isStopped = true;
            }
        }

        public void SetDestination(Vector3 destination)
        {
            if (!NetworkManager.IsListening)
                Agent.SetDestination(destination);
            else
                SetDestinationRpc(destination);
        }
        
        public void SetEngine(bool enable)
        {
            if (!IsOwner)
                return;
            isEngineOn.Value = enable;
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void SetDestinationRpc(Vector3 destination)
        {
            if (!isEngineOn.Value)
                return;
            Agent.SetDestination(destination);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void ResumeRpc()
        {
            if (!isEngineOn.Value)
                return;
            Agent.isStopped = false;
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        public void StopRpc()
        {
            Agent.isStopped = true;
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void WarpRpc(Vector3 position)
        {
            Agent.Warp(position);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void ResetPathRpc()
        {
            Agent.ResetPath();
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        public void SetStopDistanceRpc(float stopDistance)
        {
            Agent.stoppingDistance = stopDistance;
        }

        public void Sprint(bool sprinting = true)
        {
            if (sprinting && !_isSprinting)
            {
                _defaultSpeed = Agent.speed;
                SetSpeedRpc(Agent.speed * sprintSpeedMultiplier);
            }

            if (!sprinting && _isSprinting)
            {
                SetSpeedRpc(_defaultSpeed);
            }
            _isSprinting = sprinting;
                
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        public void SetSpeedRpc(float speed)
        {
            Agent.speed = speed;
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        public void UpdateRotationRpc(bool rotate)
        {
            Agent.updateRotation = rotate;
        }
    }
}