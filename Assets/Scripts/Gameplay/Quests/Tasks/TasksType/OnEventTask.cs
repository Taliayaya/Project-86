using UnityEngine;

namespace Gameplay.Quests.Tasks.TasksType
{
    public class OnEventTask : Task
    {
        [SerializeField] private string eventName;
        [SerializeField] private string taskDescription;
        
        [SerializeField] private int eventCount = 1;
        [SerializeField] private bool typedEvent = false;
        [Tooltip("Fail the task instead of completing it when the event count is reached (e.g. lose conditions).")]
        [SerializeField] private bool failOnEvent = false;

        private int _currentEventCount = 0;
        private void OnEventTypedTriggered(object arg)
        {
            _currentEventCount++;
            Resolve();
            OnTaskProgressChangedHandler(this);
        }

        private void OnEventTriggered()
        {
            Debug.Log("[OnEventTask] OnEventTriggered()");
            _currentEventCount++;
            Resolve();
            OnTaskProgressChangedHandler(this);
        }

        private void Resolve()
        {
            if (failOnEvent)
            {
                if (_currentEventCount >= eventCount)
                    Fail();
            }
            else
                Complete();
        }

        public override void RegisterEvents()
        {
            base.RegisterEvents();
            if (typedEvent)
                EventManager.AddListener(eventName, OnEventTypedTriggered);
            else
            {
                Debug.Log("[OnEventTask] RegisterEvents() " + eventName);
                EventManager.AddListener(eventName, OnEventTriggered);
            }
        }

        public override void Activate()
        {
            base.Activate();
            _currentEventCount = 0; 
        }

        public override bool CanComplete()
        {
            return _currentEventCount >= eventCount;
        }

        public override void UnregisterEvents()
        {
            base.UnregisterEvents();
            if (typedEvent)
                EventManager.RemoveListener(eventName, OnEventTypedTriggered);
            else
                EventManager.RemoveListener(eventName, OnEventTriggered);
        }

        public override string ToString()
        {
            return taskDescription + $" {_currentEventCount}/{eventCount}";
        }
    }
}