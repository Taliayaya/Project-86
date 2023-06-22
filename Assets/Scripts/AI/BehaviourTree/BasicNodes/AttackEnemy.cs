using System.Collections;
using Gameplay.Mecha;
using ScriptableObjects.AI;
using UnityEngine;

namespace AI.BehaviourTree.BasicNodes
{
    
    /// <summary>
    /// This is a basic node that will attack the enemy.
    /// It requires the blackboard to have the following keys:
    /// - transform: The transform of the AI
    /// - agentSO: The agent scriptable object of the AI
    /// - closestTarget: The closest target the AI can see
    /// </summary>
    public class AttackEnemy : ActionNode
    {
        private Transform _transform;
        private Transform _closestTarget;
        
        [SerializeField] private float timeBeforeSpotExpired = 5f;
        
        private WeaponModule[] _weaponModules;
        private Coroutine[] _coroutines;
        
        private bool _isSet = false;
        protected override void OnStart()
        {
            if (!_isSet)
            {
                _transform = blackBoard.GetValue<Transform>("transform");
                _weaponModules = blackBoard.GetValue<WeaponModule[]>("weaponModules");
                _isSet = true;
            }
            // closest target is set in CanSeeObject and can change
            _closestTarget = blackBoard.GetValue<Transform>("closestTarget");

            if (_coroutines != null)
                for (int i = 0; i < _weaponModules.Length; i++)
                {
                    _weaponModules[i].StopCoroutine(_coroutines[i]);
                }
        }

        protected override void OnStop()
        {
            
        }

        protected override State OnUpdate()
        {
            if (!_closestTarget)
                return State.Failure;
            _coroutines = new Coroutine[_weaponModules.Length];
            for (int i = 0; i < _weaponModules.Length; i++)
                _coroutines[i] =  ShootAtEnemy(_weaponModules[i]);
            return State.Success;
        }

        private Coroutine ShootAtEnemy(WeaponModule weaponModule)
        {
            if (weaponModule.HoldFire)
            {
                return weaponModule.StartCoroutine(weaponModule.ShootHoldDuringTime(_transform, timeBeforeSpotExpired));
            }
            else
            {
                return weaponModule.StartCoroutine(weaponModule.ShootDuringTime(_transform, timeBeforeSpotExpired));
            }
        }
    }
}