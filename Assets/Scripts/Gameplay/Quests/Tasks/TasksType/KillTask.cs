using System;
using AI;
using Gameplay.Units;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gameplay.Quests.Tasks.TasksType
{
    [Serializable]
    public class KillTask : Task
    {
        public int enemyToKill;
        public UnitType type;
        
        private int _currentEnemyKilled = 0;

        private void Awake()
        {
            
            EventManager.AddListener("UnitDeath", OnEnemyDeath);
        }

        public void OnEnable()
        {
            Debug.Log("[KillTask] OnEnable()");
        }

        private void OnDisable()
        {
            Debug.Log("[KillTask] OnDisable()");
        }

        protected void OnEnemyDeath(object unitArg)
        {
            var unit = (Unit)unitArg;
            
            Debug.Log("[KillTask] OnEnemyDeath()" + unit.unitType);
            if (unit.unitType.HasFlag(type))
            {
                _currentEnemyKilled++;
                bool isComplete = Complete();
                OnTaskProgressChangedHandler(this);
            }
        }

        public override bool CanComplete()
        {
            return _currentEnemyKilled >= enemyToKill;
        }

        public override string ToString()
        {
            return $"Kill {enemyToKill} {type}{(enemyToKill > 1 ? "s" : "")}: {_currentEnemyKilled}/{enemyToKill}";
        }
    }
}