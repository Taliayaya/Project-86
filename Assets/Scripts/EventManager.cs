using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DefaultNamespace
{
    public class TypedEvent : UnityEvent<object> { }
    /// <summary>
    /// A general event manager that can be used to trigger events.
    /// Using this EventManager is very important for the development of the code.
    /// Try avoiding as much as possible the use of dependencies between classes and instead, raise events.
    ///
    /// This class should be used as a gateway between classes.
    /// </summary>
    public class EventManager : Singleton<EventManager>
    {
        private Dictionary<string, UnityEvent> _events = new Dictionary<string, UnityEvent>();
        private Dictionary<string, TypedEvent> _typedEvents = new Dictionary<string, TypedEvent>();

        // We absolutely do not want to write manually in these dictionaries,
        // consider using the following methods
        public IReadOnlyDictionary<string, UnityEvent> Events => _events;
        public IReadOnlyDictionary<string, TypedEvent> TypedEvents => _typedEvents;


        #region Action events
        
        /// <summary>
        /// Add a listener to the event with the given name.
        /// When the event will be triggered, the listener will be called.
        /// </summary>
        /// <param name="eventName">The name of the event listening at</param>
        /// <param name="listener">The action to trigger during the event</param>
        /// 
        public static void AddListener(string eventName, UnityAction listener)
        {
            if (Instance._events.TryGetValue(eventName, out var evt))
                evt.AddListener(listener);
            else
            {
                // the unity event does not exist yet, we create it for the next time
                evt = new UnityEvent();
                evt.AddListener(listener);
                Instance._events.Add(eventName, evt);
            }
        }
        
        /// <summary>
        /// Stop listening at the event with the given name.
        /// </summary>
        /// <param name="eventName">The event name to stop listening at</param>
        /// <param name="listener">The Unity Action that stops listening</param>
        ///
        public static void RemoveListener(string eventName, UnityAction listener)
        {
            if (!HasInstance) // removing a listener from a non existing event is not an error. 
                return;
            if (Instance._events.TryGetValue(eventName, out var evt))
                evt.RemoveListener(listener);
        }

        /// <summary>
        /// Trigger the event with the given name.
        /// It will send the message to all the actions that are listening at the event.
        /// </summary>
        /// <param name="eventName">The event name to be triggered</param>
        /// <seealso cref="TriggerEvent(string, object)"/>
        public static void TriggerEvent(string eventName)
        {
            if (Instance._events.TryGetValue(eventName, out var evt))
                evt.Invoke();
        }
        
        #endregion

        #region Typed Events

        /// <summary>
        ///  Add a listener to the event with the given name.
        ///  When the event will be triggered, the listener will be called.
        /// </summary>
        /// <remarks>This override can receive an object</remarks>
        /// <param name="eventName">The name of the event to listen to</param>
        /// <param name="listener">The action that will be triggered</param>
        ///
        public static void AddListener(string eventName, UnityAction<object> listener)
        {
            if (Instance._typedEvents.TryGetValue(eventName, out var evt))
                evt.AddListener(listener);
            else
            {
                evt = new TypedEvent();
                evt.AddListener(listener);
                Instance._typedEvents.Add(eventName, evt);
            }
        }

        /// <summary>
        /// Stop listening at the event with the given name.
        /// </summary>
        /// <param name="eventName">The event name to stop listening at</param>
        /// <param name="listener">The Unity Action that stops listening</param>
        ///
        /// <remarks>This override can handle an object</remarks>
        ///
        /// <seealso cref="RemoveListener(string,UnityEngine.Events.UnityAction)"/>
        public static void RemoveListener(string eventName, UnityAction<object> listener)
        {
            if (!HasInstance)
                return;
            
            if (Instance._typedEvents.TryGetValue(eventName, out var evt))
                evt.RemoveListener(listener);
        }

        /// <summary>
        /// Trigger the event with the given name.
        /// It will send the message to all the actions that are listening at the event.
        /// </summary>
        /// <param name="eventName">The event name to be triggered</param>
        /// <param name="data">The data to send to all subscribers. It is a lazy type for more flexibility</param>
        /// <seealso cref="TriggerEvent(string)"/>
        public static void TriggerEvent(string eventName, object data)
        {
            if (Instance._typedEvents.TryGetValue(eventName, out var evt))
                evt.Invoke(data);
        }

        #endregion
    }
}