using System.Collections;
using Gameplay.Units;
using Unity.Collections;
using UnityEngine;

namespace AI.BehaviourTree.BasicNodes
{
    public class RotateTowardsEnemy : ActionNode
    {
        private Transform _enemyTransform;
        private AIAgent _aiAgent;

        [SerializeField] private float rotationSpeed = 1f;
        [SerializeField] private bool startRotating = true;

        private float _startTime;
#if UNITY_EDITOR
        public string[] requiredKeys = new string[] { "aiAgent (AIAgent.cs)", "closestTarget (Transform)" };
#endif

        private Quaternion _targetRotation;
        private bool _isSet = false;
        
        private bool _isRotating = false;

        protected override void OnStart()
        {
            if (!_isSet)
            {
                _aiAgent = blackBoard.GetValue<AIAgent>("aiAgent");
                _isSet = true;
            }
            _enemyTransform = blackBoard.GetValue<Transform>("closestTarget");
        }

        protected override void OnStop()
        {

        }

        protected override State OnUpdate()
        {
            if (!_enemyTransform)
                return State.Failure;
            if (startRotating && !_isRotating)
            {
                _aiAgent.RotateTowardsEnemy(_enemyTransform);
                _isRotating = true;
            }
            else
            {
                _aiAgent.StopRotating(_enemyTransform);
            }

            return State.Success;
        }
    }

}