using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Utility
{
    [Serializable]
    public struct EventData
    {
        public bool typed;
        public string eventName;
        public UnityEvent repeatEvent;
        public UnityEvent<bool> boolEvent;
        public bool isToggleBool;
        
        public void Invoke()
        {
            repeatEvent.Invoke();
        }

        private bool _toggleOn;
        public void InvokeTyped(object value)
        {
            if (value is bool boolValue)
            {
                if (isToggleBool)
                {
                    _toggleOn = !_toggleOn;
                    boolEvent.Invoke(_toggleOn);
                }
                else
                    boolEvent.Invoke(boolValue);
            }
        }
    }
    public class OnEventRepeater : MonoBehaviour
    {
        public List<EventData> events = new List<EventData>();

        private void OnEnable()
        {
            foreach (EventData eventData in events)
            {
                if (eventData.typed)
                    EventManager.AddListener(eventData.eventName, eventData.InvokeTyped);
                else
                    EventManager.AddListener(eventData.eventName, eventData.Invoke);
            }
        }

        private void OnDisable()
        {
            foreach (EventData eventData in events)
            {
                if (eventData.typed)
                    EventManager.RemoveListener(eventData.eventName, eventData.InvokeTyped);
                else
                    EventManager.RemoveListener(eventData.eventName, eventData.Invoke);
            }
        }
        
    }
}