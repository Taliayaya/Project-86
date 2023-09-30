using System;
using System.Collections.Generic;
using Gameplay.Quests.Tasks.TaskHelper;
using Gameplay.Quests.Tasks.TaskHelper.Timer;
using UnityEngine;
using UnityEngine.Events;

namespace Gameplay.Quests.Tasks.TasksType
{
    [Serializable]
    public struct TimerAction
    {
        public float time;
        public UnityEvent action;
    }
    public class TimerTask : Task
    {
        public enum TimerEndBehaviour
        {
            Complete,
            Fail,
        }
        [NonSerialized]
        private bool _timerCompleted = false;
        [Header("Settings")]
        
        public TimerEndBehaviour timerEndBehaviour;
        public string timerName = "Timer";
        public bool showTimerInSecondsOnly = true;

        [Tooltip("Timer duration in seconds")]
        public float timerDuration;
        
        public UnityEvent<float> onTimerUpdate;
        public UnityEvent<TimerTask> onTimerEnd;
        public List<TimerOverride> timerOverrides = new List<TimerOverride>();

        public List<TimerAction> timerActions = new List<TimerAction>();


        public override void Activate()
        {
            base.Activate();
            _timerCompleted = false;
            _lastTimeRemaining = timerDuration;
            TimerPlayer.PlayTimer(timerDuration, this);
        }

        public override bool CanComplete()
        {
            return _timerCompleted;
        }

        private void OnTimerEnd(TimerTask arg0)
        {
            if (timerEndBehaviour == TimerEndBehaviour.Complete)
            {
                _timerCompleted = true;
                Complete();
            }
            else
                Fail();
        }
        
        [NonSerialized]
        private float _lastTimeRemaining = 0;
        
        private void OnTimerUpdate(float time)
        {
            _lastTimeRemaining = time;
            OnTaskProgressChangedHandler();
            foreach (var action in timerActions)
            {
                if (time <= action.time)
                    action.action?.Invoke();
            }
        }

        public override void RegisterEvents()
        {
            
            onTimerEnd.AddListener(OnTimerEnd);
            onTimerUpdate.AddListener(OnTimerUpdate);
            timerOverrides.ForEach(x =>
            {
                x.Task = this;
                x.RegisterEvents();
                onTimerUpdate.AddListener(x.OnTimerUpdate);
                onTimerEnd.AddListener(x.OnTimerEnd);
            });
            base.RegisterEvents();
        }

        public override void UnregisterEvents()
        {
            onTimerEnd.RemoveListener(OnTimerEnd);
            onTimerUpdate.RemoveListener(OnTimerUpdate);
            timerOverrides.ForEach(x =>
            {
                x.Task = this;
                x.UnregisterEvents();
                onTimerUpdate.RemoveListener(x.OnTimerUpdate);
                onTimerEnd.RemoveListener(x.OnTimerEnd);
            });
            base.UnregisterEvents();
        }

        public override string ToString()
        {
            int minutes = Mathf.FloorToInt(_lastTimeRemaining / 60);
            int seconds = Mathf.FloorToInt(_lastTimeRemaining % 60);
            if (showTimerInSecondsOnly)
                return $"{timerName}: {_lastTimeRemaining:F0}s";
            return $"{timerName}: {minutes}min {seconds}s";
        }
    }
}