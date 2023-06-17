using UnityEngine;

namespace AI.BehaviourTree.CoreNodes
{
    public class WaitNode : ActionNode
    {
        public float duration = 1f;
        private float _startTime;
        protected override void OnStart()
        {
            _startTime = Time.time;
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            if (Time.time - _startTime >= duration)
            {
                return State.Success;
            }
            return State.Running;
        }
    }
}
