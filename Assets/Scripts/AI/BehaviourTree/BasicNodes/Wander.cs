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
        
        private Vector3? _goal;
        protected override void OnStart()
        {
            if (!_isSet)
            {
                _agent = blackBoard.GetValue<NavMeshAgent>("navMeshAgent");
                _isSet = true;
            }
            
            blackBoard.TryGetValue("goal", out _goal);
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
            _agent.angularSpeed = 120;
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            if (_goal.HasValue)
            {
                Debug.Log("Wander: " + _goal.Value);
                Debug.DrawRay(_goal.Value, Vector3.up * 100, Color.red, 10);
                randomDirection += _goal.Value;
                //blackBoard.RemoveValue("goal"); // we don't want to be stuck on the same goal
                //_goal = null;
            }
            else
                randomDirection += _agent.transform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1);
            Vector3 finalPosition = hit.position;
            _agent.SetDestination(finalPosition);
        }
    }
}