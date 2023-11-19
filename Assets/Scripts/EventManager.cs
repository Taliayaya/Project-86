using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Events;

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

    private static Dictionary<string, UnityEvent> _tmpEvents = new Dictionary<string, UnityEvent>();
    private static Dictionary<string, TypedEvent> _tmpTypedEvents = new Dictionary<string, TypedEvent>();

    // We absolutely do not want to write manually in these dictionaries,
    // consider using the following methods
    public IReadOnlyDictionary<string, UnityEvent> Events => _events;
    public IReadOnlyDictionary<string, TypedEvent> TypedEvents => _typedEvents;

    protected new static bool AllowAutoCreation => false;
    
    public new static EventManager Instance
    {
        get
        {
            // lock for thread safety
            lock (Lock)
            {
                if (_instance)
                    return _instance;
    
                if (!AllowAutoCreation)
                    return null;
                Debug.Log($"[{nameof(Singleton)}<{typeof(EventManager)}>] An instance is needed in the scene and no existing instances were found, so a new instance will be created.");
                return _instance = new GameObject($"({nameof(Singleton)}){typeof(EventManager)}")
                    .AddComponent<EventManager>();
            }
        }
    }
    protected override void OnAwake()
    {
        base.OnAwake();
        foreach (KeyValuePair<string, UnityEvent> tmpEvent in _tmpEvents)
        {
            if (_events.ContainsKey(tmpEvent.Key))
                _events[tmpEvent.Key] = tmpEvent.Value;
            else
                _events.Add(tmpEvent.Key, tmpEvent.Value);
        }

        foreach (KeyValuePair<string,TypedEvent> typedEvent in _typedEvents)
        {
            if (_typedEvents.ContainsKey(typedEvent.Key))
                _typedEvents[typedEvent.Key] = typedEvent.Value;
            else
                _typedEvents.Add(typedEvent.Key, typedEvent.Value);
        }
        _tmpEvents.Clear();
        _tmpTypedEvents.Clear();
        Debug.Log("[EventManager] Awake");
    }


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
        if (!Instance)
        {
            if (EventManager._tmpEvents.TryGetValue(eventName, out var e))
                e.AddListener(listener);
            else
            {
                e = new UnityEvent();
                e.AddListener(listener);
                EventManager._tmpEvents.Add(eventName, e);
            }

            return;
        }

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
        {
            if (_tmpEvents.TryGetValue(eventName, out var e))
                e.RemoveListener(listener);
            return;
        }

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
        if (!Instance)
        {
            if (EventManager._tmpTypedEvents.TryGetValue(eventName, out var e))
                e.AddListener(listener);
            else
            {
                e = new TypedEvent();
                e.AddListener(listener);
                EventManager._tmpTypedEvents.Add(eventName, e);
            }
            return;
        }
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
        {
            if (_tmpTypedEvents.TryGetValue(eventName, out var e))
                e.RemoveListener(listener);
        }

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