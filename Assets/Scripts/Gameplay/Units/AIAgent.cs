using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI.BehaviourTree;
using Gameplay.Mecha;
using JetBrains.Annotations;
using Networking;
using ScriptableObjects.AI;
using ScriptableObjects.GameParameters;
using Unity.Netcode;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEditor;
using Utility;

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
    
    public class TargetInfo : INetworkSerializable, IEquatable<TargetInfo>
    {
        public NetworkObjectReference UnitReference;
        private Unit _unit;
        public float RemainingTimeBeforeExpiring;
        
        public enum VisibilityStatus
        {
            Visible,
            Network, // Not visible but known by the legion network
            NotVisible,
        }
        
        public VisibilityStatus Visibility;

        public Unit Unit
        {
            get
            {
                if (_unit == null && UnitReference.TryGet(out NetworkObject unitNetworkObject))
                {
                    _unit = unitNetworkObject.GetComponent<Unit>();
                }

                return _unit;
            }
        }
        
        public TargetInfo()
        {
        }
        
        public TargetInfo(Unit unit, VisibilityStatus visibilityStatus, float remainingTimeBeforeExpiring)
        {
            UnitReference = unit.NetworkObject;
            Visibility = visibilityStatus;
            RemainingTimeBeforeExpiring = remainingTimeBeforeExpiring;
        }
        
        public Vector3 Position => Unit.transform.position;
        public Vector3 AimPosition => Unit.transform.position + Vector3.up * Unit.aimiYOffset;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref UnitReference);
            serializer.SerializeValue(ref RemainingTimeBeforeExpiring);
            serializer.SerializeValue(ref Visibility);
        }

        public bool Equals(TargetInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return UnitReference.Equals(other.UnitReference) && RemainingTimeBeforeExpiring.Equals(other.RemainingTimeBeforeExpiring) && Visibility == other.Visibility;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TargetInfo)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(UnitReference, RemainingTimeBeforeExpiring, (int)Visibility);
        }
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

        private NetworkVariable<TargetInfo> _target = new NetworkVariable<TargetInfo>();
        [CanBeNull]
        public TargetInfo Target
        {
            get => _target.Value;
            set
            {
                _target.Value = value;
                onTargetChanged?.Invoke(_target.Value);
                if (_target.Value?.Unit != null)
                    _target.Value.Unit.onUnitDeath.AddListener((_) => Target = null);
            }
        }
        
        public NetworkNavMeshAgent Agent => _agent;
        public override void Awake()
        {
            base.Awake();
            _agent = GetComponent<NetworkNavMeshAgent>();
            graphAgent = GetComponent<BehaviorGraphAgent>();
            _behaviourTreeRunner = GetComponent<BehaviourTreeRunner>();
            _weaponModules = GetComponentsInChildren<WeaponModule>().ToList().FindAll(module => !module.aiIgnore).ToArray();
            _audioSource = GetComponent<AudioSource>();
            _rb = GetComponent<Rigidbody>();
            
            if (_firstChild == null)
                _firstChild = transform.GetChild(0);
            if (_sensor == null)
                _sensor = _firstChild;
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
            Tree.blackBoard.SetValue("sensor", _sensor);
            Tree.blackBoard.SetValue("agentSO", agentSo);
            Tree.blackBoard.SetValue("weaponModules", _weaponModules);
            Tree.blackBoard.SetValue("aiAgent", this);
            if (patrolWaypoints != null)
                Tree.blackBoard.SetValue("waypoints", patrolWaypoints);
            
            // if we paused, dont start doing anything
            if (Factions.IsPaused(Faction)) return;
            
            if (isAutonomous)
                _behaviourTreeRunner.StartAI();
            if (rotateMainBodyTowardsEnemy)
                RotateTowardsEnemy();
            else
                _agent.Agent.updateRotation = true;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            // _agent.enabled = IsSpawned;
            if (IsOwner)
            {
                _target.Value = null;
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            onCollisionEnterEvent?.Invoke(other);
        }

        public override void OnGainedOwnership()
        {
            base.OnGainedOwnership();
        }

        public override void OnLostOwnership()
        {
            base.OnLostOwnership();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventManager.AddListener(Constants.TypedEvents.FactionPause, OnFactionPause);
        }

        private void OnFactionPause(object arg0)
        {
            if (arg0 is not Gameplay.Faction faction || faction != Faction)
                return;

            if (Factions.IsPaused(Faction))
            {
                if (isAutonomous)
                    _behaviourTreeRunner.StopAI();
                StopAllCoroutines();
                _agent.StopRpc();
            }
            else
            {
                _agent.ResumeRpc();
                if (isAutonomous)
                    _behaviourTreeRunner.StartAI();
                if (rotateMainBodyTowardsEnemy)
                    StartCoroutine(RotateTowardsEnemyCoroutine());
            }
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
            SetDestinationRpc(destination);
        }

        [Rpc(SendTo.ClientsAndHost)]
        public void SetDestinationRpc(Vector3 destination)
        {
            if (NavMesh.SamplePosition(destination, out var hit, 15f, -1))
                _agent.SetDestination(hit.position);
        }

        public void SetDestination(Transform destination)
        {
            SetDestination(destination.position);
        }

        public void ChangeEnemyDetection(bool canDetect)
        {
            graphAgent.SetVariableValue("CanDetect", canDetect);
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
        [Tooltip("The transform from which raycast are made")][SerializeField] private Transform _sensor;
        private Vector3 _lastPosition;
        IEnumerator RotateTowardsEnemyCoroutine()
        {
            while (true)
            {
                Vector3 direction;
                if (Target == null || Target?.Unit == null || Target.Visibility == TargetInfo.VisibilityStatus.Network)
                {
                    Vector3 velocity = _agent.Agent.velocity.normalized;
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
                if (!closestTarget || !IsSpawned || !IsOwner)
                    yield break;
                if (agentSo.idealDistanceFromEnemy > 0)
                {
                    float distance = Vector3.Distance(transform.position, closestTarget.position);
                    if (distance > agentSo.idealDistanceFromEnemy + 3)
                    {
                        //_agent.isStopped = false;
                        _agent.ResumeRpc();
                        SetDestination(closestTarget.position);
                    }
                    else if (distance < agentSo.idealDistanceFromEnemy - 3)
                    {
                        _agent.StopRpc();
                        //_agent.isStopped = true;
                    }
                }

                yield return new WaitForSeconds(0.5f);

            }

        }

        #endregion

        public float torqueDamageForce;
        public override void TakeSlowEffect(DamagePackage damagePackage)
        {
        }

        public override void OnTakeDamage(DamagePackage damagePackage)
        {
            base.OnTakeDamage(damagePackage);
            if (damagePackage.Audio != null)
                _audioSource.PlayOneShot(damagePackage.Audio);
            // var direction = damagePackage.HitPoint - damagePackage.DamageSourcePosition;
            // direction.Normalize();
            // _rb.AddTorque(direction * torqueDamageForce, ForceMode.Impulse);
        }

        public override void Die()
        {
            if (Died || Singleton.Quitting)
                return;
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
            float halfAngle = agentSo.fieldOfViewAngle / 2f;
            Transform viewpoint = _firstChild ?? transform;
            
            var frontPoint = transform.position + viewpoint.forward * agentSo.viewDistance;
            
            var rightPoint = transform.position + Quaternion.AngleAxis(agentSo.fieldOfViewAngle / 2, Vector3.up) *
                viewpoint.forward * agentSo.viewDistance;
            var leftPoint = transform.position + Quaternion.AngleAxis(-agentSo.fieldOfViewAngle / 2, Vector3.up) *
                viewpoint.forward * agentSo.viewDistance;
            
            Gizmos.color = debugAgent.frontViewColor;
            Gizmos.DrawLine(transform.position, frontPoint);
            
            Gizmos.color = debugAgent.sideViewColor;
            Gizmos.DrawLine(transform.position, rightPoint);
            Gizmos.DrawLine(transform.position, leftPoint);

            Handles.color = debugAgent.sideViewColor;
            Handles.DrawWireArc(transform.position, Vector3.up, viewpoint.forward, halfAngle, agentSo.viewDistance);
            Handles.DrawWireArc(transform.position, Vector3.up, viewpoint.forward, -halfAngle, agentSo.viewDistance);
            // Gizmos.DrawLine(rightPoint, frontPoint);
            // Gizmos.DrawLine(leftPoint, frontPoint);

        }
        
        #endregion
    }
}