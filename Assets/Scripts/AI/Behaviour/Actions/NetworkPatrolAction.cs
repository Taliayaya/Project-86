using System;
using System.Collections.Generic;
using System.Linq;
using Networking;
using ScriptableObjects.AI;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.AI;

namespace Unity.Behavior
{

    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Network Patrol", story: "[Agent] patrols along [Waypoints] on network", category: "Action/Navigation", id: "5d22455d98fca864187bf7a737530d7e")]
    internal partial class NetworkPatrolAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<List<GameObject>> Waypoints;
        [SerializeReference] public BlackboardVariable<AgentSO> AgentSo = new(null);
        [SerializeReference] public BlackboardVariable<float> WaypointWaitTime = new(1.0f);
        [SerializeReference] public BlackboardVariable<float> DistanceThreshold = new(0.2f);

        [Tooltip("Should patrol restart from the latest point?")] [SerializeReference]
        public BlackboardVariable<bool> PreserveLatestPatrolPoint = new(false);

        private NavMeshAgent m_NavMeshAgent;
        private NetworkNavMeshAgent m_NetworkNavMeshAgent;
        [CreateProperty] private Vector3 m_CurrentTarget;
        [CreateProperty] private float m_OriginalStoppingDistance = -1f;
        [CreateProperty] private float m_OriginalSpeed = -1f;
        [CreateProperty] private float m_WaypointWaitTimer;
        private float m_CurrentSpeed;
        [CreateProperty] private int m_CurrentPatrolPoint = 0;
        [CreateProperty] private bool m_Waiting;

        protected override Status OnStart()
        {
            if (Agent.Value == null)
            {
                LogFailure("No agent assigned.");
                return Status.Failure;
            }

            if (Waypoints.Value == null || Waypoints.Value.Count == 0)
            {
                Waypoints.Value = GameObject.FindGameObjectsWithTag("PatrolPoint").ToList();
                LogFailure("No waypoints to patrol assigned.");
                return Status.Failure;
            }

            Initialize();

            m_Waiting = false;
            m_WaypointWaitTimer = 0.0f;

            MoveToNextWaypoint();
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (Agent.Value == null || Waypoints.Value == null)
            {
                return Status.Failure;
            }

            if (m_Waiting)
            {
                if (m_WaypointWaitTimer > 0.0f)
                {
                    m_WaypointWaitTimer -= Time.deltaTime;
                }
                else
                {
                    m_WaypointWaitTimer = 0f;
                    m_Waiting = false;
                    MoveToNextWaypoint();
                }
            }
            else
            {
                float distance = GetDistanceToWaypoint();
                bool destinationReached = distance <= DistanceThreshold;

                // Check if we've reached the waypoint (ensuring NavMeshAgent has completed path calculation if available)
                if (destinationReached && (m_NavMeshAgent == null || !m_NavMeshAgent.pathPending))
                {
                    m_WaypointWaitTimer = WaypointWaitTime.Value;
                    m_Waiting = true;
                    m_CurrentSpeed = 0;

                    return Status.Running;
                }
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (m_NavMeshAgent != null)
            {
                if (m_NavMeshAgent.isOnNavMesh)
                {
                    m_NavMeshAgent.ResetPath();
                }

                m_NavMeshAgent.speed = m_OriginalSpeed;
                m_NavMeshAgent.stoppingDistance = m_OriginalStoppingDistance;
            }
        }

        protected override void OnDeserialize()
        {
            // If using a navigation mesh, we need to reset default value before Initialize.
            m_NavMeshAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            m_NetworkNavMeshAgent = Agent.Value.GetComponentInChildren<NetworkNavMeshAgent>();
            if (m_NavMeshAgent != null)
            {
                if (m_OriginalSpeed >= 0f)
                    m_NavMeshAgent.speed = m_OriginalSpeed;
                if (m_OriginalStoppingDistance >= 0f)
                    m_NavMeshAgent.stoppingDistance = m_OriginalStoppingDistance;

                m_NetworkNavMeshAgent.WarpRpc(Agent.Value.transform.position);
            }

            int patrolPoint = m_CurrentPatrolPoint - 1;
            Initialize();
            // During deserialization, bypass PreserveLatestPatrolPoint.
            m_CurrentPatrolPoint = patrolPoint;
        }

        private void Initialize()
        {
            m_NavMeshAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            m_NetworkNavMeshAgent = Agent.Value.GetComponentInChildren<NetworkNavMeshAgent>();
            if (m_NavMeshAgent != null)
            {
                if (m_NavMeshAgent.isOnNavMesh)
                {
                    m_NetworkNavMeshAgent.ResetPathRpc();
                }

                m_OriginalSpeed = m_NavMeshAgent.speed;
                m_NavMeshAgent.speed = AgentSo.Value.speed;
                m_OriginalStoppingDistance = m_NavMeshAgent.stoppingDistance;
                m_NavMeshAgent.stoppingDistance = DistanceThreshold;
            }

            m_CurrentPatrolPoint = PreserveLatestPatrolPoint.Value ? m_CurrentPatrolPoint - 1 : -1;
        }

        private float GetDistanceToWaypoint()
        {
            if (m_NavMeshAgent != null)
            {
                return m_NavMeshAgent.remainingDistance;
            }

            Vector3 targetPosition = m_CurrentTarget;
            Vector3 agentPosition = Agent.Value.transform.position;
            agentPosition.y = targetPosition.y; // Ignore y for distance check.
            return Vector3.Distance(agentPosition, targetPosition);
        }

        private void MoveToNextWaypoint()
        {
            m_CurrentPatrolPoint = (m_CurrentPatrolPoint + 1) % Waypoints.Value.Count;

            m_CurrentTarget = Waypoints.Value[m_CurrentPatrolPoint].transform.position;
            if (m_NetworkNavMeshAgent != null)
            {
                m_NetworkNavMeshAgent.SetDestination(m_CurrentTarget);
            }
        }
    }
}
