using UnityEngine;

namespace Gameplay.Quests.Tasks.TaskHelper.Timer.TimerOverrides
{
    public class TimerPlayerDeath : TimerOverride
    {
        public override void RegisterEvents()
        {
            base.RegisterEvents();
            EventManager.AddListener("OnDeath", OnDeath);
            Debug.Log("Player death event registered");
        }

        public override void UnregisterEvents()
        {
            base.UnregisterEvents();
            EventManager.RemoveListener("OnDeath", OnDeath);
        }

        private void OnDeath(object deathData)
        {
            if (behaviour == TimerOverrideBehaviour.Fail)
                Task.Fail(); 
            else if (behaviour == TimerOverrideBehaviour.Success)
                Task.Complete(true);
        }
    }
}