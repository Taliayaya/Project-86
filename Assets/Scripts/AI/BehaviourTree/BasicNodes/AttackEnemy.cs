using System.Collections;
using Gameplay.Mecha;
using Gameplay.Units;
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
        
        private AIAgent _aiAgent;
        
        [SerializeField] private float timeBeforeSpotExpired = 5f;
        public float angleToShoot = 15f;
        
        private WeaponModule[] _weaponModules;
        private Coroutine[] _coroutines;
        
        private bool _isSet = false;
        private int startcount = 0;

        protected override void OnStart()
        {
            if (!_isSet)
            {
                _transform = blackBoard.GetValue<Transform>("transform");
                _weaponModules = blackBoard.GetValue<WeaponModule[]>("weaponModules");
                _aiAgent = blackBoard.GetValue<AIAgent>("aiAgent");
                _coroutines = new Coroutine[_weaponModules.Length];
                _isSet = true;
            }

            // closest target is set in CanSeeObject and can change
            if (_coroutines.Length > 0 && _coroutines[0] is not null)
                for (int i = 0; i < _weaponModules.Length; i++)
                {
                    _weaponModules[i].StopCoroutine(_coroutines[i]);
                }

            startcount++;
            for (int i = 0; i < _weaponModules.Length; i++)
                _coroutines[i] = ShootAtEnemy(_weaponModules[i]);
        }

        protected override void OnStop()
        {
            
        }

        protected override State OnUpdate()
        {
            if (_aiAgent.Target?.Visibility == TargetInfo.VisibilityStatus.NotVisible)
                return State.Failure;
            
            return State.Success;
        }

        private Coroutine ShootAtEnemy(WeaponModule weaponModule)
        {
            if (weaponModule.HoldFire)
            {
                return weaponModule.StartCoroutine(weaponModule.ShootHoldDuringTime(timeBeforeSpotExpired,
                    EnemyInRange));
            }
            
            return weaponModule.StartShootDuringTime(timeBeforeSpotExpired, EnemyInRange);
        }

        public bool EnemyInRange()
        {
            if (_aiAgent.Target?.Visibility != TargetInfo.VisibilityStatus.Visible)
                return false;
            var direction = _aiAgent.Target.Position - _transform.position;
            var angle = Vector3.Angle(direction, _transform.forward);

            return angle < angleToShoot * 0.5f;
        }
    }
}