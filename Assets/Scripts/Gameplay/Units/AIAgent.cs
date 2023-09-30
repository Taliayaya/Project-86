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
    
    [RequireComponent(typeof(NavMeshAgent), typeof(BehaviourTreeRunner), typeof(AudioSource))]
    public class AIAgent : Unit
    {
        private NavMeshAgent _agent;
        private BehaviourTreeRunner _behaviourTreeRunner;
        private AudioSource _audioSource;

        [SerializeField] private AgentSO agentSo;

        [SerializeField] private DebugAgent debugAgent;
        [SerializeField] [CanBeNull] private List<Transform> patrolWaypoints = null;
        private WeaponModule[] _weaponModules;
        public BehaviourTree Tree => _behaviourTreeRunner.tree;
        public DemoParameters demoParameters;
        
        private Coroutine _rotateCoroutine;

        public override void Awake()
        {
            base.Awake();
            _agent = GetComponent<NavMeshAgent>();
            _behaviourTreeRunner = GetComponent<BehaviourTreeRunner>();
            _weaponModules = GetComponentsInChildren<WeaponModule>();
            _audioSource = GetComponent<AudioSource>();
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
        public void RotateTowardsEnemy(Transform closestTarget)
        {
            StopRotating();
            isRotating = true;
            _agent.updateRotation = false;
            _rotateCoroutine = StartCoroutine(RotateTowardsEnemyCoroutine(closestTarget));
        }

        public void StopRotating()
        {
            if (_rotateCoroutine != null)
                StopCoroutine(_rotateCoroutine);
            isRotating = false;
            _agent.updateRotation = true;
        }
        
        IEnumerator RotateTowardsEnemyCoroutine(Transform closestTarget)
        {
            while (true)
            {
                if (!closestTarget)
                    yield break;
                var direction = (closestTarget.position - transform.position).normalized;
                var lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * agentSo.rotationSpeed);
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
                        _agent.SetDestination(closestTarget.position);
                    }
                    else if (distance < agentSo.idealDistanceFromEnemy - 3)
                    {
                        if (NavMesh.SamplePosition(transform.position - transform.forward * agentSo.idealDistanceFromEnemy, out NavMeshHit hit, 10,
                                NavMesh.AllAreas))
                            _agent.SetDestination(hit.position);
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