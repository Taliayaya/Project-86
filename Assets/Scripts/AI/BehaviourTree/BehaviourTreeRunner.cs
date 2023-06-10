using System;
using UnityEngine;

namespace AI.BehaviourTree
{
    public class BehaviourTreeRunner : MonoBehaviour
    {
        private BehaviourTree _tree;

        private void Start()
        {
            _tree = ScriptableObject.CreateInstance<BehaviourTree>();
            var log = ScriptableObject.CreateInstance<DebugLogNode>();
            var repeat = ScriptableObject.CreateInstance<RepeatNode>();
            
            log.message = "Hello World!";
            
            _tree.rootNode = repeat;
            repeat.child = log;
        }
        
        
        private void Update()
        {
            _tree.Update();
        }
    }
}