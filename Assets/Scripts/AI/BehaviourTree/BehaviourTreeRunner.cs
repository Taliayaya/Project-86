using System;
using System.Collections;
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
        
        private IEnumerator AIUpdate()
        {
            while (true)
            {
                if (!GameManager.GameIsPaused)
                    tree.Update();
                yield return new WaitForSeconds(0.4f);
            }
        }
        
        public void StartAI()
        {
            StartCoroutine(AIUpdate());
        }
        
        public void StopAI()
        {
            StopAllCoroutines();
        }
       
    }
}