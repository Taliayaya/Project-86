using System.Collections;
using Gameplay.Units;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace AI.BehaviourTree.BasicNodes
{
    public class RotateTowardsEnemy : ActionNode
    {
        private Transform _enemyTransform;
        private NavMeshAgent _navMeshAgent;
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
                _navMeshAgent = blackBoard.GetValue<NavMeshAgent>("navMeshAgent");
                _isSet = true;
                
                _angularSpeed = _navMeshAgent.angularSpeed;
                
                _navMeshAgent.updateRotation = false;
            }
            if (!blackBoard.TryGetValue("closestTarget", out _enemyTransform))
                return;
        }

        protected override void OnStop()
        {

        }

        private float _angularSpeed;
        protected override State OnUpdate()
        {
            _navMeshAgent.angularSpeed = _angularSpeed;
            if (startRotating && !_enemyTransform)
                return State.Failure;
            if (startRotating)
            {
                if (!_aiAgent.isRotating)
                {
                    _navMeshAgent.angularSpeed = 0;
                    _aiAgent.RotateTowardsEnemy();
                }
            }
            else
            {
                _navMeshAgent.angularSpeed = _angularSpeed;
                _aiAgent.StopRotating();
            }

            return State.Success;
        }
    }

}