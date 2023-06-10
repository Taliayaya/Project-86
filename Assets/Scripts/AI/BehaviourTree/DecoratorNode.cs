using UnityEngine;

namespace AI.BehaviourTree
{
    public abstract class DecoratorNode : Node
    {
        public Node child;
    }
}