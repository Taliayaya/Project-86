using System.Collections;
using System.Collections.Generic;
using Gameplay;
using Gameplay.Units;
using JetBrains.Annotations;
using ScriptableObjects.AI;
using UnityEngine;
using UnityEngine.Events;

namespace AI
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
    public class AutoDetectEnemy : MonoBehaviour
    {
        [Tooltip("The tag the AI will try to search for"), SerializeField]
        private Faction enemyFaction;

        [SerializeField] private LayerMask layerMask;
        [CanBeNull] public Unit _closestTarget;
        public UnityEvent<Unit> onTargetSpotted;

        public float fieldOfViewAngle = 150f;
        public float viewDistance = 50f;

        [CanBeNull]
        private Unit ClosestTarget
        {
            get => _closestTarget;
            set
            {
                _closestTarget = value;
                onTargetSpotted.Invoke(_closestTarget);
            }
        }

        private bool _isSet = false;

        private bool _isDone;
        private bool _canSeeTarget;
        private bool _sharingPosition;

        private List<Unit> _targets;

        private List<Unit> _spottedTargets = new List<Unit>();
        private IEnumerator _canSeeTargetEnumerator;

        private void Start()
        {
            _targets = Factions.GetMembers(enemyFaction);
            _isSet = true;
            StartCoroutine(CanSeeTarget());
        }

        private float _minDistance = Mathf.Infinity;
        /// <summary>
        /// We are using an IENumerator to check if we can see the target without too much performance loss
        /// We can't use a coroutine because we are in a class that doesn't inherit from MonoBehaviour
        /// but we know that the Node class ticks every frame so we can use this to our advantage
        /// </summary>
        private IEnumerator CanSeeTarget()
        {
            while (true)
            {
                _spottedTargets.Clear();
                _minDistance = Mathf.Infinity;
                if (ClosestTarget != null && CanSeeTargetAux(ClosestTarget))
                {
                    yield return new WaitForSeconds(2f);
                    continue;
                }

                ClosestTarget = null;

                Debug.Log("Start seeing " + _targets.Count);
                foreach (var target in new List<Unit>(_targets))
                {
                    Debug.Log("see has target");
                    if (!target)
                        continue;
                    Debug.Log("try see: " + target.gameObject.name);
                    if (CanSeeTargetAux(target))
                    {
                        Debug.Log("see: " + target.gameObject.name);
                        break;
                    }

                    yield return new WaitForSeconds(0.3f);
                }

                yield return new WaitForSeconds(1.5f);

            }
        }

        private bool CanSeeTargetAux(Unit target)
        {
            var direction = target.transform.position - transform.position;
            var angle = Vector3.Angle(direction, transform.forward);
            
            Debug.Log("angle: " + angle + " " + fieldOfViewAngle * 0.5f);

            if (angle < fieldOfViewAngle * 0.5f)
            {
                Debug.DrawLine(transform.position, target.transform.position, Color.blue);
                if (PerformRaycast(direction, target) && (_minDistance > direction.magnitude ||
                                                          (ClosestTarget && ClosestTarget.Priority >
                                                              target.Priority)))
                {
                    _minDistance = direction.magnitude;
                    ClosestTarget = target;
                    return true;
                }
            }
            return false;
        }

        private bool CanSeeSingleTarget(Unit target)
        {
            var direction = target.transform.position - transform.position;
            var angle = Vector3.Angle(direction, transform.forward);

            Debug.DrawLine(transform.position, target.transform.position, Color.red);
            if (angle < fieldOfViewAngle * 0.5f)
            {
                Debug.DrawLine(transform.position, target.transform.position, Color.blue);
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
            Debug.DrawRay(transform.position, direction.normalized * viewDistance, Color.yellow, 2);
            if (Physics.Raycast(transform.position, direction.normalized, out hit, viewDistance, layerMask))
            {
                bool isDirectTarget = hit.collider.gameObject == target.gameObject;
                if (isDirectTarget || (hit.rigidbody != null && hit.rigidbody.gameObject == target.gameObject))
                {
                    Debug.DrawLine(transform.position, target.transform.position, Color.green);
                    _canSeeTarget = true;
                    _spottedTargets.Add(target);
                    return true;
                }
            }

            return false;

        }
    }
}