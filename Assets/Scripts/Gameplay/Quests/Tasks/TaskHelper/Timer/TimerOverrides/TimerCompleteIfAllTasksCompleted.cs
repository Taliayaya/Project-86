using System.Linq;

namespace Gameplay.Quests.Tasks.TaskHelper.Timer.TimerOverrides
{
    public class TimerCompleteIfAllTasksCompleted : TimerOverride
    {
        public override void OnTimerUpdate(float timeRemaining)
        {
            base.OnTimerUpdate(timeRemaining);
            if (Task.Owner.Tasks.All(task => task.IsCompleted || task == Task))
            {
                Task.Complete(true);
                Task.Owner.Complete(true);
            }
        }
    }
}