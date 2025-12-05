using System;
using System.Collections.Generic;
using System.Linq;
using Gameplay.Quests.Tasks;
using Unity.Netcode;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.Events;
using Task = Gameplay.Quests.Tasks.Task;
using TaskStatus = Gameplay.Quests.Tasks.TaskStatus;

namespace Gameplay.Quests
{
    public class Quest : NetworkBehaviour
    {
        #region Events
        
        public delegate void StatusChanged(QuestStatus oldStatus, Quest quest);
        public delegate void TaskStatusChanged(TaskStatus oldStatus, Task task);
        
        public event Task.TaskProgressChanged OnTaskProgressChanged;
        public event StatusChanged OnStatusChanged;
        public event TaskStatusChanged OnTaskStatusChanged;

        #endregion
        
        [Header("General Info")]
        
        public string questName;
        [TextArea]
        public string description;
        
        
        public TaskOrder taskOrder;
        [NonSerialized]
        private NetworkVariable<QuestStatus> _status = new NetworkVariable<QuestStatus>(QuestStatus.Inactive);

        public QuestStatus Status
        {
            get => _status.Value;
            set
            {
                if (IsOwner)
                    _status.Value = value;
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
        
        [Header("Events")]
        public UnityEvent<Quest> onActivate;
        public UnityEvent<Quest> onComplete;
        public bool IsCompleted => Status == QuestStatus.Completed;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _status.OnValueChanged += NetworkStatusChanged;
        }

        private void NetworkStatusChanged(QuestStatus previousValue, QuestStatus newValue)
        {
            if (!IsOwner && newValue == QuestStatus.Completed)
                Complete(true);
                
            if (previousValue != newValue)
                OnStatusChanged?.Invoke(newValue, this);
        }

        public bool CanActivate()
        {
            foreach (var quest in requiredFinishedQuests)
            {
                if (!quest.IsCompleted)
                    return false;
            }

            Debug.Log($"[Quest] CanActivate(): Quest {name} can be activated");
            return !IsCompleted && Status != QuestStatus.Locked;
        }

        public bool CanComplete()
        {
            Debug.Log($"[Quest] CanComplete(): Quest {name} {Status}");
            if (Status != QuestStatus.Active)
                return false;

            foreach (var task in GetPrincipaleTasks())
            {
                var canComplete = task.CanComplete();
                Debug.Log($"[Quest] CanComplete(): Quest {name} | Task {task.name} {canComplete}");
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
            
            onComplete?.Invoke(this);
            Status = QuestStatus.Completed;
            CompleteCompletableTasks(forceComplete);
            UnregisterTaskEvents();
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }

            EventManager.TriggerEvent(Constants.Events.Analytics.QuestCompleted, questName);
        }
        
        
        private void ActivateTasks()
        {
            foreach (Transform child in transform)
            {
                Debug.Log($"[Quest] ActivateTasks(): Activating task {child.name}");
                child.gameObject.SetActive(true);
            }
            RegisterTaskEvents();
            switch (taskOrder)
            {
                case TaskOrder.Sequential:
                    foreach (var task in Tasks)
                    {
                        if (task.Status == TaskStatus.Inactive)
                        {
                            task.Owner = this;
                            task.Activate();
                            task.RegisterEvents();
                            break;
                        }
                    }
                    break;
                case TaskOrder.Parallel:
                    foreach (var task in Tasks)
                    {
                        task.Owner = this;
                        task.Activate();
                        task.RegisterEvents();
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
            onActivate?.Invoke(this);
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
            if (task.Status == TaskStatus.Failed && task.importance == TaskImportance.Principal)
            {
                Status = QuestStatus.Cancelled;
                CompleteCompletableTasks();
            }
            else
                Complete();
            OnTaskStatusChanged?.Invoke(oldStatus, task);
        }
        
        private void RegisterTaskEvents()
        {
            foreach (var task in Tasks)
            {
                task.OnStatusChanged += NotifyTaskStatusChanged;
                task.OnTaskProgressChanged += NotifyTaskProgressChanged;
            }
        }
        
        private void UnregisterTaskEvents()
        {
            foreach (var task in Tasks)
            {
                task.OnStatusChanged -= NotifyTaskStatusChanged;
                task.OnTaskProgressChanged -= NotifyTaskProgressChanged;
                task.UnregisterEvents();
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
        
        public List<Task> GetPrincipaleTasks()
        {
            return Tasks.Where(task => task.importance == TaskImportance.Principal).ToList();
        }
        
        public List<Task> GetOptionalTasks()
        {
            return Tasks.Where(task => task.importance == TaskImportance.Optional).ToList();
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
                case TaskFilter.Principal:
                    return GetPrincipaleTasks();
                case TaskFilter.Optional:
                    return GetOptionalTasks();
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