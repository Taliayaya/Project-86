using System;
using UnityEngine;

namespace Gameplay.Quests.Tasks.TaskHelper.TasksModules
{
    public abstract class TaskModule : MonoBehaviour
    {
        
        public virtual void Activate(Task task)
        {
            task.OnStatusChanged -= OnStatusChanged;
            task.OnStatusChanged += OnStatusChanged;
        }

        private void OnStatusChanged(TaskStatus oldStatus, Task task)
        {
            switch (task.Status)
            {
                case TaskStatus.Active:
                    break;
                case TaskStatus.Inactive:
                    break;
                case TaskStatus.Completed:
                    OnComplete(task);
                    break;
                case TaskStatus.Failed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(oldStatus), oldStatus, null);
            }
        }

        public virtual void OnComplete(Task task)
        {
            task.OnStatusChanged -= OnStatusChanged;

        }
    }
}