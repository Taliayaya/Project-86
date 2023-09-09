using System.Collections;
using DefaultNamespace;
using Gameplay.Quests.Tasks.TasksType;
using UnityEngine;

namespace Gameplay.Quests.Tasks.TaskHelper.Timer
{
    public class TimerPlayer : Singleton<TimerPlayer>
    {
        private IEnumerator PlayerTimer(float duration, TimerTask caller, float interval = 1f)
        {
            while (duration > 0 && caller.Status is not TaskStatus.Failed and not TaskStatus.Completed)
            {
                duration -= interval;
                caller.onTimerUpdate?.Invoke(duration);
                yield return new WaitForSeconds(interval);
            }
            caller.onTimerEnd?.Invoke(caller);
        }
        
        public static void PlayTimer(float duration, TimerTask caller, float interval = 1f)
        {
            Instance.StartCoroutine(Instance.PlayerTimer(duration, caller, interval));
        }
    }
}