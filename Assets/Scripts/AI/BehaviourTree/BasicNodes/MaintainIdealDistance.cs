using Gameplay.Units;
using ScriptableObjects.AI;
using UnityEngine;
using UnityEngine.AI;

namespace AI.BehaviourTree.BasicNodes
{
    public class MaintainIdealDistance : ActionNode
    {

        private Transform _closestTarget;
        private bool _isSet = false;
        private AIAgent _aiAgent;
        
        [SerializeField] private bool startMaintainingDistance = true;
        protected override void OnStart()
        {
            if (!_isSet)
            {
                _aiAgent = blackBoard.GetValue<AIAgent>("aiAgent");
                _isSet = true;
            }
            blackBoard.TryGetValue<Transform>("closestTarget", out _closestTarget);
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            if (!startMaintainingDistance)
                _aiAgent.StopMaintainIdealDistance();
            if (!_closestTarget)
                return State.Failure;
            
            if (startMaintainingDistance)
                _aiAgent.StartMaintainIdealDistance(_closestTarget);

            return State.Success;
        }
    }
}