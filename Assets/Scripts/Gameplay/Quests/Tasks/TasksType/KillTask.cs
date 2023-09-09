using System;
using System.Collections;
using AI;
using Gameplay.Units;
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
        
        [NonSerialized]
        private int _currentEnemyKilled = 0;

        public void OnEnable()
        {
            _currentEnemyKilled = 0;
        }
        protected void OnEnemyDeath(object unitArg)
        {
            var unit = (Unit)unitArg;
            
            Debug.Log("[KillTask] OnEnemyDeath()" + unit.unitType + " has type " + type.HasFlag(unit.unitType));
            if (type.HasFlag(unit.unitType))
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

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            EventManager.AddListener("UnitDeath", OnEnemyDeath);
        }
        
        public override void UnregisterEvents()
        {
            base.UnregisterEvents();
            EventManager.RemoveListener("UnitDeath", OnEnemyDeath);
        }
    }
    
}