using System.Collections;
using System.Collections.Generic;
using Gameplay;
using Gameplay.Units;
using JetBrains.Annotations;
using ScriptableObjects.AI;
using UnityEngine;

namespace AI.BehaviourTree.BasicNodes
{
    /// <summary>
    /// This node will check if the AI can see any object of the given faction
    ///
    /// It requires the blackboard to have the following keys:
    /// - transform: The transform of the AI
    /// - agentSO: The agent scriptable object of the AI
    ///
    /// It will set the following keys:
    /// - closestTarget: The closest target the AI can see
    /// </summary>
    public class CanSeeObject : ActionNode
    {
        private Transform _transform;
        [Tooltip("The tag the AI will try to search for"), SerializeField] private Faction enemyFaction;
        
        [SerializeField] private LayerMask layerMask;
        [CanBeNull] private Unit _closestTarget;

        [CanBeNull]
        private Unit ClosestTarget
        {
            get => _closestTarget;
            set
            {
                _closestTarget = value;
                blackBoard.SetValue("closestTarget", _closestTarget ? _closestTarget.transform : null);
            }
        }
        private bool _isSet = false;

        private bool _isDone;
        private bool _canSeeTarget;
        
        private AgentSO _agentSo;
        
        private List<Unit> _targets;
        
        private List<Unit> _spottedTargets = new List<Unit>();
        private IEnumerator _canSeeTargetEnumerator;
        protected override void OnStart()
        {
            if (!_isSet)
            {
                _transform = blackBoard.GetValue<Transform>("transform");
                _agentSo = blackBoard.GetValue<AgentSO>("agentSO");
                _targets = Factions.GetMembers(enemyFaction);
                _isSet = true;
            }

            _isDone = false;
            _canSeeTarget = false;
            _canSeeTargetEnumerator = CanSeeTarget();
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            if (_isDone)
            {
                return _canSeeTarget ? State.Success : State.Failure;
            }

            if (ClosestTarget)
            {
                if (CanSeeSingleTarget(ClosestTarget))
                {
                    _canSeeTarget = true;
                    _isDone = true;
                    return State.Success;
                }

                ClosestTarget = null;
            }

            _canSeeTargetEnumerator.MoveNext();
            return State.Running;
        }


        /// <summary>
        /// We are using an IENumerator to check if we can see the target without too much performance loss
        /// We can't use a coroutine because we are in a class that doesn't inherit from MonoBehaviour
        /// but we know that the Node class ticks every frame so we can use this to our advantage
        /// </summary>
        private IEnumerator CanSeeTarget()
        {
            _spottedTargets.Clear();
            var minDistance = Mathf.Infinity;
            foreach (var target in new List<Unit>(_targets))
            {
                if (!target)
                    continue;
                var direction = target.transform.position - _transform.position;
                var angle = Vector3.Angle(direction, _transform.forward);
                
                Debug.DrawLine(_transform.position, target.transform.position, Color.red);
                if (angle < _agentSo.fieldOfViewAngle * 0.5f)
                {
                    Debug.DrawLine(_transform.position, target.transform.position, Color.blue);
                    if (PerformRaycast(direction, target) && (minDistance > direction.magnitude || (ClosestTarget && ClosestTarget.Priority > target.Priority)))
                    {
                        minDistance = direction.magnitude;
                        ClosestTarget = target;
                    }
                }

                yield return null;
            }

            _isDone = true;
        }

        private bool CanSeeSingleTarget(Unit target)
        {
            var direction = target.transform.position - _transform.position;
            var angle = Vector3.Angle(direction, _transform.forward);

            Debug.DrawLine(_transform.position, target.transform.position, Color.red);
            if (angle < _agentSo.fieldOfViewAngle * 0.5f)
            {
                Debug.DrawLine(_transform.position, target.transform.position, Color.blue);
                return PerformRaycast(direction, target);

            }
            return false;
        }
        
        public static bool CanSeeSingleTarget(Transform transform, AgentSO agentSo, GameObject target)
        {
            var direction = target.transform.position - transform.position;
            var angle = Vector3.Angle(direction, transform.forward);

            Debug.DrawLine(transform.position, target.transform.position, Color.red);
            if (angle < agentSo.fieldOfViewAngle * 0.5f)
            {
                Debug.DrawLine(transform.position, target.transform.position, Color.blue);
                return PerformRaycast(transform, agentSo, direction, target);

            }
            return false;
        }
        
        private static bool PerformRaycast(Transform transform, AgentSO agentSo, Vector3 direction, GameObject target)
        {
            RaycastHit hit;
            Debug.DrawRay(transform.position, direction.normalized * agentSo.viewDistance, Color.yellow);
            if (Physics.Raycast(transform.position, direction.normalized, out hit, agentSo.viewDistance))
            {
                if (hit.collider.gameObject == target)
                {
                    Debug.DrawLine(transform.position, target.transform.position, Color.green);
                    return true;
                }
            }

            return false;

        }

        private bool PerformRaycast(Vector3 direction, Unit target)
        {
            RaycastHit hit;
            Debug.DrawRay(_transform.position, direction.normalized * _agentSo.viewDistance, Color.yellow);
            if (Physics.Raycast(_transform.position, direction.normalized, out hit, _agentSo.viewDistance, layerMask))
            {
                if (hit.collider.gameObject == target.gameObject)
                {
                    Debug.DrawLine(_transform.position, target.transform.position, Color.green);
                    _canSeeTarget = true;
                    _spottedTargets.Add(target);
                    return true;
                }
            }

            return false;

        }
    }
}