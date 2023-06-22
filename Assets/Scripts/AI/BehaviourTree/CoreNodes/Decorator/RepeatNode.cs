using UnityEngine;

namespace AI.BehaviourTree.CoreNodes.Decorator
{
    public class RepeatNode : DecoratorNode
    {
        [SerializeField] private uint repeatCount;
        [SerializeField] private bool infinite;
        protected override void OnStart()
        {
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            var childState = child.Update();
            
            if (childState == State.Running)
                return State.Running;
            
            if (infinite)
                return State.Running;
            if (repeatCount > 0)
            {
                repeatCount--;
                return State.Running;
            }
            return childState;
        }
    }
}
