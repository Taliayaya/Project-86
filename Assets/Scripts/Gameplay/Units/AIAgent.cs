using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI.BehaviourTree;
using Gameplay.Mecha;
using JetBrains.Annotations;
using ScriptableObjects.AI;
using ScriptableObjects.GameParameters;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Gameplay.Units
{
    [Serializable]
    internal class DebugAgent
    {
        public bool debug = true;
        public bool debugSelected = true;

        [Header("Settings")] public bool debugSpot = true;
        
        [Header("Colors")]
        public Color frontViewColor = Color.blue;
        public Color sideViewColor = Color.cyan;
    }

    public enum OrderPriority
    {
        Low,
        Medium,
        High
    }
    
    public class TargetInfo
    {
        public Unit Unit;
        public float RemainingTimeBeforeExpiring;
        
        public enum VisibilityStatus
        {
            Visible,
            Network, // Not visible but known by the legion network
            NotVisible,
        }
        
        public VisibilityStatus Visibility;
        
        public TargetInfo(Unit unit, VisibilityStatus visibilityStatus, float remainingTimeBeforeExpiring)
        {
            Unit = unit;
            Visibility = visibilityStatus;
            RemainingTimeBeforeExpiring = remainingTimeBeforeExpiring;
        }
        
        public Vector3 Position => Unit.transform.position;
        public Vector3 AimPosition => Unit.transform.position + Vector3.up * Unit.aimiYOffset;
    }
    
    [RequireComponent(typeof(NavMeshAgent), typeof(BehaviourTreeRunner), typeof(AudioSource))]
    public class AIAgent : Unit
    {
        private NavMeshAgent _agent;
        private BehaviourTreeRunner _behaviourTreeRunner;
        private AudioSource _audioSource;

        [SerializeField] private AgentSO agentSo;
        [SerializeField] private bool isAutonomous = true;
        public OrderPriority orderPriority = OrderPriority.Low;

        [SerializeField] private DebugAgent debugAgent;
        [SerializeField] [CanBeNull] private List<Transform> patrolWaypoints = null;
        [SerializeField] private bool rotateMainBodyTowardsEnemy = true;
        private WeaponModule[] _weaponModules;
        public BehaviourTree Tree => _behaviourTreeRunner.tree;
        public DemoParameters demoParameters;

        private Coroutine _rotateCoroutine;
        
        [SerializeField] protected UnityEvent<TargetInfo> onTargetChanged;

        [CanBeNull] private TargetInfo _target;
        [CanBeNull]
        public TargetInfo Target
        {
            get => _target;
            set
            {
                _target = value;
                onTargetChanged?.Invoke(_target);
                _target?.Unit.onUnitDeath.AddListener((_) => Target = null);
            }
        }
        public override void Awake()
        {
            base.Awake();
            _agent = GetComponent<NavMeshAgent>();
            _behaviourTreeRunner = GetComponent<BehaviourTreeRunner>();
            _weaponModules = GetComponentsInChildren<WeaponModule>().ToList().FindAll(module => !module.aiIgnore).ToArray();
            _audioSource = GetComponent<AudioSource>();
            
            if (_firstChild == null)
                _firstChild = transform.GetChild(0);
            if (name.Contains("Lowe"))
            {
                Health = demoParameters.loweHealth;
                MaxHealth = demoParameters.loweHealth;
            }
            else if (name.Contains("Dinosauria"))
            {
                Health = demoParameters.dinosauriaHealth;
                MaxHealth = demoParameters.dinosauriaHealth;
            }
            else
            {
                Health = demoParameters.ameiseHealth;
                MaxHealth = demoParameters.ameiseHealth;
            }
        }

        protected override void Start()
        {
            base.Start();
            Tree.blackBoard.SetValue("navMeshAgent", _agent);
            Tree.blackBoard.SetValue("transform", _firstChild);
            Tree.blackBoard.SetValue("agentSO", agentSo);
            Tree.blackBoard.SetValue("weaponModules", _weaponModules);
            Tree.blackBoard.SetValue("aiAgent", this);
            if (patrolWaypoints != null)
                Tree.blackBoard.SetValue("waypoints", patrolWaypoints);
            if (isAutonomous)
                _behaviourTreeRunner.StartAI();
            if (rotateMainBodyTowardsEnemy)
                RotateTowardsEnemy();
        }
        
        public void AddDestinationGoal(Vector3 destination)
        {
            Tree.blackBoard.SetValue("goal", destination);
        }
        
        public void AddDestinationGoal(Transform destination)
        {
            Tree.blackBoard.SetValue("goal", destination.position);
        }
        
        public void SetDestination(Vector3 destination)
        {
            if (NavMesh.SamplePosition(destination, out var hit, 15f, -1))
                _agent.SetDestination(hit.position);
        }
        
        public void SetDestination(Transform destination)
        {
            SetDestination(destination.position);
        }


        #region AI Coroutines

        

        [HideInInspector] public bool isRotating;
        public void RotateTowardsEnemy()
        {
            StopRotating();
            isRotating = true;
            _rotateCoroutine = StartCoroutine(RotateTowardsEnemyCoroutine());
        }

        public void StopRotating()
        {
            if (_rotateCoroutine != null)
                StopCoroutine(_rotateCoroutine);
            isRotating = false;
        }

        [SerializeField] private Transform _firstChild;
        private Vector3 _lastPosition;
        IEnumerator RotateTowardsEnemyCoroutine()
        {
            while (true)
            {
                Vector3 direction;
                if (Target == null || Target.Visibility == TargetInfo.VisibilityStatus.Network)
                {
                    Vector3 velocity = _agent.velocity.normalized;
                    if (velocity == Vector3.zero)
                        direction = _firstChild.forward;
                    else
                        direction = velocity;
                }
                else
                    direction = (Target.AimPosition - transform.position).normalized;
                var newRotation = Quaternion.LookRotation(direction);

                Quaternion current = _firstChild.localRotation;
                _firstChild.localRotation = Quaternion.Slerp(current, newRotation, Time.deltaTime * agentSo.rotationSpeed);
                transform.localRotation = Quaternion.Euler(0, 0, 0);
                
                yield return new WaitForFixedUpdate();
            }
        }
        
        public void StartAI()
        {
            _behaviourTreeRunner.StartAI();
        }

        public void StartMaintainIdealDistance(Transform closestTarget)
        {
            _maintainIdealDistanceCoroutine = StartCoroutine(MaintainIdealDistanceCoroutine(closestTarget));
        }
        public void StopMaintainIdealDistance()
        {
            if (_maintainIdealDistanceCoroutine != null)
                StopCoroutine(_maintainIdealDistanceCoroutine);
        }
        
        private Coroutine _maintainIdealDistanceCoroutine;
        IEnumerator MaintainIdealDistanceCoroutine(Transform closestTarget)
        {
            while (true)
            {
                if (!closestTarget)
                    yield break;
                if (agentSo.idealDistanceFromEnemy > 0)
                {
                    float distance = Vector3.Distance(transform.position, closestTarget.position);
                    if (distance > agentSo.idealDistanceFromEnemy + 3)
                    {
                        _agent.isStopped = false;
                        _agent.SetDestination(closestTarget.position);
                    }
                    else if (distance < agentSo.idealDistanceFromEnemy - 3)
                    {
                        _agent.isStopped = true;
                    }
                }

                yield return new WaitForSeconds(0.5f);

            }

        }

        #endregion

        public override void OnTakeDamage(DamagePackage damagePackage)
        {
            base.OnTakeDamage(damagePackage);
            if (damagePackage.DamageAudioClip != null)
                _audioSource.PlayOneShot(damagePackage.DamageAudioClip);
            
        }

        public override void Die()
        {
            if (agentSo.deathEffect != null)
                Instantiate(agentSo.deathEffect, transform.position, Quaternion.identity);
            base.Die();
            if (agentSo.deadPrefab != null)
            {
                // TODO: use rotation of the rotating object (first child)
                var dead = Instantiate(agentSo.deadPrefab, transform.position, transform.rotation);
                dead.transform.GetChild(0).rotation = transform.GetChild(0).rotation;
            }
        }

        #region Debug

        private void OnDrawGizmos()
        {
            //if (Application.isPlaying)
            if (!debugAgent.debug)
                return;
            if (debugAgent.debugSelected)
                return;
            DebugGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugAgent.debug)
                return;
            if (!debugAgent.debugSelected)
                return;
            DebugGizmos();
        }

        private void DebugGizmos()
        {
            if (debugAgent.debugSpot)
                DebugGizmosSpot();
        }

        private void DebugGizmosSpot()
        {
            var frontPoint = transform.position + transform.forward * agentSo.viewDistance;
            var rightPoint = transform.position + Quaternion.AngleAxis(agentSo.fieldOfViewAngle / 2, Vector3.up) *
                transform.forward * agentSo.viewDistance;
            var leftPoint = transform.position + Quaternion.AngleAxis(-agentSo.fieldOfViewAngle / 2, Vector3.up) *
                transform.forward * agentSo.viewDistance;
            Gizmos.color = debugAgent.frontViewColor;
            Gizmos.DrawLine(transform.position, frontPoint);
            
            Gizmos.color = debugAgent.sideViewColor;
            Gizmos.DrawLine(transform.position, rightPoint);
            Gizmos.DrawLine(transform.position, leftPoint);
            
            Gizmos.DrawLine(rightPoint, frontPoint);
            Gizmos.DrawLine(leftPoint, frontPoint);

        }
        
        #endregion
    }
}