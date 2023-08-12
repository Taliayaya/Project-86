using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AI.BehaviourTree.BasicNodes
{
    public class Wander : ActionNode
    {
        private NavMeshAgent _agent;
        private bool _isSet = false;
        public int wanderRadius = 20;
        protected override void OnStart()
        {
            if (!_isSet)
            {
                _agent = blackBoard.GetValue<NavMeshAgent>("navMeshAgent");
                _isSet = true;
            }
            
        }

        protected override void OnStop()
        {
            
        }

        protected override State OnUpdate()
        {
            if (_agent.remainingDistance < 1f || _agent.velocity.magnitude < 1f || !_agent.hasPath)
                WanderAround();
            return State.Success;
        }
        
        private void WanderAround()
        {
            _agent.updateRotation = true;
            _agent.angularSpeed = 120;
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += _agent.transform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1);
            Vector3 finalPosition = hit.position;
            _agent.SetDestination(finalPosition);
        }
    }
}