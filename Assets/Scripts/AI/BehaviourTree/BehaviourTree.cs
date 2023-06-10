using UnityEngine;

namespace AI.BehaviourTree
{
    [CreateAssetMenu()]
    public class BehaviourTree : ScriptableObject
    {
        public Node rootNode;
        public Node.State treeState = Node.State.Running;

        public Node.State Update()
        {
            if (rootNode.state == Node.State.Running)
                treeState = rootNode.Update();

            return treeState;
        }
    }
}