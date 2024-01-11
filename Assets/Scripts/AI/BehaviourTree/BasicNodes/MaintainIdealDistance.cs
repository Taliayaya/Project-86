using Gameplay.Units;
using ScriptableObjects.AI;
using UnityEngine;
using UnityEngine.AI;

namespace AI.BehaviourTree.BasicNodes
{
    public class MaintainIdealDistance : ActionNode
    {

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
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            if (!startMaintainingDistance)
                _aiAgent.StopMaintainIdealDistance();
            if (_aiAgent.Target is null || _aiAgent.Target.Visibility == TargetInfo.VisibilityStatus.NotVisible)
                return State.Failure;

            if (startMaintainingDistance)
                _aiAgent.StartMaintainIdealDistance(_aiAgent.Target.Unit.transform);

            return State.Success;
        }
    }
}