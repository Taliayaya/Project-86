using System;
using Gameplay.Quests.Tasks.TasksType;
using ScriptableObjects.Quests;
using UnityEngine;

namespace Gameplay.Quests.Tasks
{
    public class Task : ScriptableObject
    {
        public delegate void TaskProgressChanged(Task task);
        public delegate void StatusChanged(TaskStatus newStatus, Task task);
        
        public event StatusChanged OnStatusChanged;
        public event TaskProgressChanged OnTaskProgressChanged;
        
        private TaskStatus _status = TaskStatus.Inactive;
        public TaskStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                    OnStatusChanged?.Invoke(_status, this);
                _status = value;
            }
        }

        [NonSerialized]
        private DateTime? _startTime;
        public DateTime? StartTime
        {
            get => _startTime;
            protected set => _startTime = value;
        }
        
        public bool IsCompleted => Status == TaskStatus.Completed;
        
        public virtual Quest Owner { get; set; }

        public virtual void Activate()
        {
            if (Status == TaskStatus.Inactive)
            {
                _startTime = DateTime.Now;
                Status = TaskStatus.Active;
            }
            else if (Status == TaskStatus.Completed)
                Debug.Log("[Task] Activate(): Task is already completed");
        }
        
        public virtual bool CanComplete()
        {
            return Status == TaskStatus.Active;
        }
        
        public virtual bool Complete(bool forceComplete = false)
        {
            if (!CanComplete() && !forceComplete)
                return false;
            Status = TaskStatus.Completed;

            return IsCompleted;
        }

        public virtual void Fail()
        {
            if (Status != TaskStatus.Completed)
                Status = TaskStatus.Failed;
        }
        
        public virtual void NotifyQuestCompleted()
        {
        }
        
        public void OnTaskProgressChangedHandler(Task task)
        {
            OnTaskProgressChanged?.Invoke(task);
        }


    }
}