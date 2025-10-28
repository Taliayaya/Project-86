using System;
using Gameplay.Quests.Tasks.TaskHelper.TasksModules;
using Gameplay.Quests.Tasks.TasksType;
using Unity.Netcode;
using UnityEngine;

namespace Gameplay.Quests.Tasks
{
    public class Task : NetworkBehaviour
    {
        public delegate void TaskProgressChanged(Task task);
        public delegate void StatusChanged(TaskStatus newStatus, Task task);
        
        public event StatusChanged OnStatusChanged;
        public event TaskProgressChanged OnTaskProgressChanged;
        
        public TaskImportance importance;
        [NonSerialized]
        private NetworkVariable<TaskStatus> _status = new NetworkVariable<TaskStatus>(TaskStatus.Inactive);
        public TaskStatus Status
        {
            get => _status.Value;
            set
            {
                if (IsOwner)
                {
                    _status.Value = value;
                    OnStatusChanged?.Invoke(value, this);
                }
                // else
                //     Debug.LogWarning("[Task] Status: Task status can only be changed by the owner");
            }
        }

        private void Awake()
        {
        }

        // they need to be enabled during spawn or they wont be sync / taken into account
        protected override void OnNetworkPreSpawn(ref NetworkManager networkManager)
        {
            gameObject.SetActive(true);
            base.OnNetworkPreSpawn(ref networkManager);
        }

        protected override void OnNetworkPostSpawn()
        {
            base.OnNetworkPostSpawn();
            gameObject.SetActive(false);
        }

        public virtual void OnEnable()
        {
            _status.OnValueChanged += OnTaskStatusChanged;
            Debug.Log($"[Task] OnNetworkSpawn(): Task {name} spawned");

            if (Status != TaskStatus.Inactive)
            {
                OnTaskStatusChanged(TaskStatus.Inactive, Status);
            }
        }

        private void OnDisable()
        {
            _status.OnValueChanged -= OnTaskStatusChanged;
        }

        private void OnTaskStatusChanged(TaskStatus previousValue, TaskStatus newValue)
        {
            Debug.Log($"[Task] OnTaskStatusChanged(): Task {name} status changed to {newValue}");
            OnStatusChanged?.Invoke(previousValue, this);
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
            Debug.Log("[Task] Awake(): Task created" + name + " "+ Owner.Tasks.Count );
            if (Status == TaskStatus.Inactive)
            {
                _startTime = DateTime.Now;
                Status = TaskStatus.Active;
                ActivateTaskModules();
            }
            else if (Status == TaskStatus.Completed)
                Debug.Log("[Task] Activate(): Task is already completed");
        }
        
        private void ActivateTaskModules()
        {
            foreach (var taskModule in GetComponentsInChildren<TaskModule>())
            {
                taskModule.Activate(this);
            }
        }
        
        public virtual bool CanComplete()
        {
            return Status == TaskStatus.Active;
        }
        
        public virtual bool Complete(bool forceComplete = false)
        {
            if ((!CanComplete() && !forceComplete) || Status == TaskStatus.Failed)
                return false;
            Debug.Log("[Task] Complete(): Task completed");
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

        public void OnTaskProgressChangedHandler() => OnTaskProgressChangedHandler(this);
        public void OnTaskProgressChangedHandler(Task task)
        {
            OnTaskProgressChanged?.Invoke(task);
        }

        public virtual void RegisterEvents()
        {
            
        }
        
        public virtual void UnregisterEvents()
        {
            foreach (var taskModule in GetComponentsInChildren<TaskModule>())
            {
                taskModule.DeActivate(this);
            }
        } 


    }
}