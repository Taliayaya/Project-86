using System;
using System.Collections;
using AI;
using Gameplay.Units;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Unit = Gameplay.Units.Unit;

namespace Gameplay.Quests.Tasks.TasksType
{
    [Serializable]
    public class KillTask : Task
    {
        public int enemyToKill;
        public UnitType type;
        
        [SerializeField] private string taskName = "Kill";
        
        [SerializeField] NetworkVariable<int> _currentEnemyKilled = new NetworkVariable<int>(0);

        public override void OnEnable()
        {
            if (IsOwner)
                _currentEnemyKilled.Value = 0;
            _currentEnemyKilled.OnValueChanged += ValueChanged;
        }

        private void ValueChanged(int previousValue, int newValue)
        {
            bool isComplete = Complete();
            OnTaskProgressChangedHandler(this);
        }

        protected void OnEnemyDeath(object unitArg)
        {
            var unit = (Unit)unitArg;
            
            Debug.Log("[KillTask] OnEnemyDeath()" + unit.unitType + " has type " + type.HasFlag(unit.unitType));
            if (type.HasFlag(unit.unitType))
            {
                if (IsOwner)
                {
                    _currentEnemyKilled.Value++;
                    Complete();
                    OnTaskProgressChangedHandler(this);
                }
            }
        }

        public override bool CanComplete()
        {
            Debug.Log($"[CanComplete]2 currentEnemyKilled {_currentEnemyKilled.Value}");
            return _currentEnemyKilled.Value >= enemyToKill;
        }

        public override string ToString()
        {
            return $"{taskName} {enemyToKill} {type}{(enemyToKill > 1 ? "s" : "")}: {_currentEnemyKilled.Value}/{enemyToKill}";
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            if (IsOwner)
                EventManager.AddListener("UnitDeath", OnEnemyDeath);
        }
        
        public override void UnregisterEvents()
        {
            base.UnregisterEvents();
            if (IsOwner)
                EventManager.RemoveListener("UnitDeath", OnEnemyDeath);
        }
    }
    
}