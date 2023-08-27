using Gameplay.Quests.Tasks.TasksType;
using UnityEngine;

namespace Gameplay.Quests.Tasks.TaskHelper.Timer
{
    public class TimerOverride : ScriptableObject
    {
        public Task Task { get; set; }
        public TimerOverrideBehaviour behaviour;
        public virtual void RegisterEvents()
        {
            
        }
        
        public virtual void UnregisterEvents()
        {
            
        }
        
        public virtual void OnTimerEnd(TimerTask caller)
        {
            UnregisterEvents();
        }
        
        public virtual void OnTimerUpdate(float timeRemaining)
        {
            
        }
    }
}