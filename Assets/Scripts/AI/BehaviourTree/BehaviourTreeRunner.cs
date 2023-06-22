using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AI.BehaviourTree
{
    public class BehaviourTreeRunner : MonoBehaviour
    {
        public BehaviourTree tree;

        private void Awake()
        {
            tree = tree.Clone();
            tree.Bind();
        }
        
        
        private void Update()
        {
            tree.Update();
        }
    }
}