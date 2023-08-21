using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Quests;
using Gameplay.Quests.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace ScriptableObjects.Quests
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Quests/Quest")]
    public class Quest : ScriptableObject
    {
        #region Events
        
        public delegate void StatusChanged(QuestStatus oldStatus, Quest quest);
        public delegate void TaskStatusChanged(TaskStatus oldStatus, Task task);
        
        public event Task.TaskProgressChanged OnTaskProgressChanged;
        public event StatusChanged OnStatusChanged;
        public event TaskStatusChanged OnTaskStatusChanged;

        #endregion
        
        [Header("General Info")]
        
        [TextArea]
        public string description;
        
        
        public TaskOrder taskOrder;
        [NonSerialized]
        private QuestStatus _status = QuestStatus.Inactive;

        public QuestStatus Status
        {
            get => _status;
            set
            {
                var oldStatus = _status;
                _status = value;
                if (oldStatus != value)
                    OnStatusChanged?.Invoke(_status, this);

            }
        }

        [SerializeField]
        private List<Task> tasks = new List<Task>();

        public List<Task> Tasks
        {
            get => tasks;
            set
            {
                tasks = value;
            }
            
            
        }
        
        [Header("Conditions")]
        public Quest[] requiredFinishedQuests = Array.Empty<Quest>();
        
        
        public bool IsCompleted => Status == QuestStatus.Completed;
        
        public bool CanActivate()
        {
            foreach (var quest in requiredFinishedQuests)
            {
                if (!quest.IsCompleted)
                    return false;
            }

            return !IsCompleted && Status != QuestStatus.Locked;
        }

        public bool CanComplete()
        {
            if (_status != QuestStatus.Active)
                return false;

            foreach (var task in tasks)
            {
                var canComplete = task.CanComplete();
                if (!canComplete)
                    return false;
            }

            return true;
        }
        
        public void CompleteCompletableTasks(bool forceComplete = false)
        {
            foreach (var task in Tasks)
            {
                if (!task.IsCompleted && task.Complete(forceComplete))
                    task.UnregisterEvents();
            }
        }
        
        public void Complete(bool forceComplete = false)
        {
            if (!CanComplete() && !forceComplete)
                return;
            Debug.Log($"[Quest] Complete(): Quest {name} completed");
            Status = QuestStatus.Completed;
            CompleteCompletableTasks(forceComplete);

        }
        
        
        private void ActivateTasks()
        {
            RegisterTaskEvents();
            switch (taskOrder)
            {
                case TaskOrder.Sequential:
                    foreach (var task in Tasks)
                    {
                        if (task.Status == TaskStatus.Inactive)
                        {
                            task.Activate();
                            task.RegisterEvents();
                            task.Owner = this;
                            break;
                        }
                    }
                    break;
                case TaskOrder.Parallel:
                    foreach (var task in Tasks)
                    {
                        task.Activate();
                        task.Owner = this;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool Activate()
        {
            if (!CanActivate())
                return false;
            Status = QuestStatus.Active;
            ActivateTasks();
            return true;
        }

        #region Tasks

        public void NotifyTasksQuestCompleted()
        {
            foreach (var task in tasks)
            {
                task.NotifyQuestCompleted();
            }
        }

        public void NotifyTaskProgressChanged(Task task)
        {
            OnTaskProgressChanged?.Invoke(task);
        }
        public void NotifyTaskStatusChanged(TaskStatus oldStatus, Task task)
        {
            Debug.Log($"[Quest] NotifyTaskStatusChanged(): Task {task.name} status changed to {task.Status}");
            Complete();
            OnTaskStatusChanged?.Invoke(oldStatus, task);
        }
        
        private void RegisterTaskEvents()
        {
            foreach (var task in Tasks)
            {
                task.OnStatusChanged += NotifyTaskStatusChanged;
                task.OnTaskProgressChanged += NotifyTaskProgressChanged;
                task.RegisterEvents();
            }
        }
        
        private void UnregisterTaskEvents()
        {
            foreach (var task in Tasks)
            {
                task.OnStatusChanged -= NotifyTaskStatusChanged;
                task.OnTaskProgressChanged -= NotifyTaskProgressChanged;
            }
        }

        #region Filters
        
        public List<Task> GetFailedTasks()
        {
            return Tasks.Where(task => task.Status == TaskStatus.Failed).ToList();
        }

        public List<Task> GetInActiveTasks()
        {
            return Tasks.Where(task => task.Status == TaskStatus.Inactive).ToList();
        }

        public List<Task> GetActiveTasks()
        {
            return Tasks.Where(task => task.Status == TaskStatus.Active).ToList();
        }

        public List<Task> GetActiveAndCompletedTasks()
        {
            return Tasks.Where(task => task.Status == TaskStatus.Active || task.Status == TaskStatus.Completed).ToList();
        }

        public List<Task> GetCompletedTasks()
        {
            return Tasks.Where(task => task.Status == TaskStatus.Completed).ToList();
        }

        public List<Task> GetTasks(TaskFilter filter)
        {
            switch (filter)
            {
                case TaskFilter.Inactive:
                    return GetInActiveTasks();
                case TaskFilter.Active:
                    return GetActiveTasks();
                case TaskFilter.ActiveAndCompleted:
                    return GetActiveAndCompletedTasks();
                case TaskFilter.Failed:
                    return GetFailedTasks();
                case TaskFilter.All:
                    return Tasks.ToList();
                default:
                    throw new ArgumentOutOfRangeException("filter", filter, null);
            }
        }
        

        #endregion
        

        #endregion
    }
}