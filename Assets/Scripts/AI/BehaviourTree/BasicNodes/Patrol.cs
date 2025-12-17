using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Units;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace AI.BehaviourTree.BasicNodes
{
    [Serializable]
    public struct CustomPatrolConfig
    {
        public int steps;
        public int distanceBetweenSteps;
    }
    public class Patrol : ActionNode
    {
        private AIAgent _aiAgent;
        private bool _isSet = false;
        public CustomPatrolConfig customPatrolConfig;
        
        private IEnumerator<Vector3> _patrolPoints;
        [CanBeNull] private List<Transform> _waypoints;

        private Vector3? _goal;
        protected override void OnStart()
        {
            if (!_isSet)
            {
                _aiAgent = blackBoard.GetValue<AIAgent>("aiAgent");
                blackBoard.TryGetValue("waypoints", out _waypoints);
                _isSet = true;
            }
            //_agent.updateRotation = true;
            //_agent.angularSpeed = 120;
        }

        protected override void OnStop()
        {
            
        }

        protected override State OnUpdate()
        {
            if (_aiAgent.Agent.Agent.remainingDistance < 1f || _aiAgent.Agent.Agent.velocity.magnitude < 1f || !_aiAgent.Agent.Agent.hasPath)
            {
                if (_patrolPoints != null && _patrolPoints.MoveNext())
                    _aiAgent.SetDestination(_patrolPoints.Current);
                else if (_waypoints is { Count: > 0 })
                    WaypointToEnumerator();
                else
                    GenerateRandomPatrol();
            }
            return State.Success;
        }

        private void WaypointToEnumerator()
        {
            if (_waypoints is null)
                return;
            List<Vector3> points = new List<Vector3>();
            foreach (Transform transform in _waypoints)
            {
                points.Add(transform.position);
            }
            _patrolPoints = points.GetEnumerator();
        }
        private void GenerateRandomPatrol()
        {
            List<Vector3> points = new List<Vector3>();
            Vector3 previousPosition = _aiAgent.transform.position;
            for (int i = 0; i < customPatrolConfig.steps; i++)
            {
                Vector3 randomDirection = Random.insideUnitSphere * customPatrolConfig.distanceBetweenSteps;
                randomDirection += previousPosition;
                NavMeshHit hit;
                NavMesh.SamplePosition(randomDirection, out hit, customPatrolConfig.distanceBetweenSteps, 1);
                Vector3 finalPosition = hit.position;
                
                previousPosition = finalPosition;
                points.Add(finalPosition);
            }
            _patrolPoints = points.GetEnumerator();
        }
    }
}