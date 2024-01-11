using System;
using System.Collections;
using System.Collections.Generic;
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
        
        public OrderPriority orderPriority = OrderPriority.Low;

        [SerializeField] private DebugAgent debugAgent;
        [SerializeField] [CanBeNull] private List<Transform> patrolWaypoints = null;
        private WeaponModule[] _weaponModules;
        public BehaviourTree Tree => _behaviourTreeRunner.tree;
        public DemoParameters demoParameters;
        
        private Coroutine _rotateCoroutine;

        [CanBeNull] private TargetInfo _target;
        [CanBeNull]
        public TargetInfo Target
        {
            get => _target;
            set
            {
                _target = value;
                _target?.Unit.onUnitDeath.AddListener((_) => Target = null);
            }
        }
        public override void Awake()
        {
            base.Awake();
            _agent = GetComponent<NavMeshAgent>();
            _behaviourTreeRunner = GetComponent<BehaviourTreeRunner>();
            _weaponModules = GetComponentsInChildren<WeaponModule>();
            _audioSource = GetComponent<AudioSource>();
            
            _firstChild = transform.GetChild(0);
            if (name.Contains("Lowe"))
            {
                Health = demoParameters.loweHealth;
                MaxHealth = demoParameters.loweHealth;
            }
            else
            {
                Health = demoParameters.ameiseHealth;
                MaxHealth = demoParameters.ameiseHealth;
            }
            _xRotation = _firstChild.localRotation.eulerAngles.x;
            _yRotation = transform.rotation.eulerAngles.y;


        }

        protected override void Start()
        {
            base.Start();
            Tree.blackBoard.SetValue("navMeshAgent", _agent);
            Tree.blackBoard.SetValue("transform", transform);
            Tree.blackBoard.SetValue("agentSO", agentSo);
            Tree.blackBoard.SetValue("weaponModules", _weaponModules);
            Tree.blackBoard.SetValue("aiAgent", this);
            if (patrolWaypoints != null)
                Tree.blackBoard.SetValue("waypoints", patrolWaypoints);
        }
        
        public void AddDestinationGoal(Vector3 destination)
        {
            Tree.blackBoard.SetValue("goal", destination);
        }


        #region AI Coroutines

        

        [HideInInspector] public bool isRotating;
        public void RotateTowardsEnemy()
        {
            StopRotating();
            isRotating = true;
            _agent.updateRotation = true;
            _rotateCoroutine = StartCoroutine(RotateTowardsEnemyCoroutine());
        }

        public void StopRotating()
        {
            if (_rotateCoroutine != null)
                StopCoroutine(_rotateCoroutine);
            isRotating = false;
            _agent.updateRotation = true;
        }

        private Transform _firstChild;
        private float _xRotation;
        private float _yRotation;
        IEnumerator RotateTowardsEnemyCoroutine()
        {
            while (true)
            {
                if (Target == null)
                {
                    _agent.updateRotation = true;
                    yield break;
                }
                if (Target.Visibility == TargetInfo.VisibilityStatus.Network)
                {
                    _agent.updateRotation = true;
                    yield return null;
                }

                var direction = (Target.AimPosition - transform.position).normalized;
                var newRotation = Quaternion.LookRotation(direction).eulerAngles;
                
                //Debug.Log("Rotation: " + newRotation);
                
                _yRotation -= (_yRotation - newRotation.y) * Time.deltaTime * agentSo.rotationSpeed;
                _xRotation -= (_xRotation - newRotation.x) * Time.deltaTime * agentSo.rotationSpeed;
                
                _xRotation = Mathf.Clamp(_xRotation, agentSo.minXRotation, agentSo.maxXRotation);
                transform.rotation = Quaternion.Euler(0, newRotation.y, 0);
                _firstChild.localRotation = Quaternion.Euler(newRotation.x, 0, 0);
                //Debug.DrawRay(transform.position, direction * 100, Color.magenta);
                
                yield return null;
            }
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