using System.Collections.Generic;
using Gameplay.Units;
using UnityEngine;
using UnityEngine.AI;

namespace AI.BehaviourTree.BasicNodes
{
    public class Wander : ActionNode
    {
        private AIAgent _agent;
        private bool _isSet = false;
        public int wanderRadius = 20;
        
        private Vector3? _goal;
        protected override void OnStart()
        {
            if (!_isSet)
            {
                _agent = blackBoard.GetValue<AIAgent>("aiAgent");
                _isSet = true;
            }
            
            blackBoard.TryGetValue("goal", out _goal);
        }

        protected override void OnStop()
        {
            
        }

        protected override State OnUpdate()
        {
            if (_agent.Agent.remainingDistance < 1f || _agent.Agent.velocity.magnitude < 1f || !_agent.Agent.hasPath)
                WanderAround();
            return State.Success;
        }
        
        private void WanderAround()
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            if (_goal.HasValue)
            {
                randomDirection += _goal.Value;
                //blackBoard.RemoveValue("goal"); // we don't want to be stuck on the same goal
                //_goal = null;
            }
            else
                randomDirection += _agent.transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, 1))
            {
                Vector3 finalPosition = hit.position;
                _agent.SetDestination(finalPosition);
                if (_agent.Agent.pathStatus == NavMeshPathStatus.PathComplete)
                {
                    _goal = null;
                    blackBoard.RemoveValue("goal");
                }

            }
        }
    }
}