using Gameplay.Units;
using ScriptableObjects.AI;
using UnityEngine;

namespace AI.BehaviourTree.BasicNodes
{
    public class SummonStrike : ActionNode
    {
        private bool _isSet;
        private bool _isDone;

        [SerializeField] private float strikeYOffset = 700f;

        private AIAgent _agent;

        protected override void OnStart()
        {
            if (!_isSet)
            {
                _isSet = true;
                _agent = blackBoard.GetValue<AIAgent>("aiAgent");
            }
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            if (_agent.Target is null)
                return State.Failure;

            EventManager.TriggerEvent(Constants.TypedEvents.StrikeRequest, new StrikeRequest
            {
                strikePosition = _agent.Target.Position,
                strikeCallback = Callback
            });
            return State.Success;
        }

        private void Callback(StrikeResponse response)
        {
            Debug.Log("Strike requested: " + response);
        }
    }
}