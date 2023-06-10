using System.Collections.Generic;
using UnityEngine;

namespace AI.BehaviourTree
{
    public abstract class CompositeNode : Node
    {
        public List<Node> children = new List<Node>();

    }
}