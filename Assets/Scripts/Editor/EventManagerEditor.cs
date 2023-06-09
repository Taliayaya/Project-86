using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(EventManager))]
public class EventManagerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Event Manager is only available in play mode.", MessageType.Info);
            return;
        }
        TriggerEventButton();
        EditorGUILayout.Separator();
        TriggerEventTypedButton();
        
        
    }

    /// <summary>
    /// Allow the developer to trigger an event from the inspector by selecting the name
    /// from the dropdown list.
    /// The dropdown list contains only available events.
    /// </summary>
    private void TriggerEventButton()
    {
        var eventNames = EventManager.Instance.Events.Keys.ToArray();
        if (eventNames.Length == 0)
        {
            EditorGUILayout.HelpBox("No event found.", MessageType.Info);
            return;
        }
        int index = EditorGUILayout.Popup("Event Name", 0, eventNames);
        if (GUILayout.Button("Trigger Event"))
            EventManager.TriggerEvent(eventNames[index]);
    }

    /// <summary>
    /// Allow the developer to trigger a typed event from the inspector by selecting the name
    /// from the dropdown list.
    /// The dropdown list contains only available typed events.
    ///
    /// The developer can also select a data to send with the event.
    /// 
    /// </summary>
    private void TriggerEventTypedButton()
    {
        var typedEventNames = EventManager.Instance.TypedEvents.Keys.ToArray();
        if (typedEventNames.Length == 0)
        {
            EditorGUILayout.HelpBox("No typed event found.", MessageType.Info);
            return;
        }
        
        
        EditorGUILayout.BeginVertical();
        int index = EditorGUILayout.Popup("Typed Event Name", 0, typedEventNames);
        object data = EditorGUILayout.ObjectField("Typed Event Data", null, typeof(Object), true);
        EditorGUILayout.EndVertical();
        
        if (GUILayout.Button("Trigger Event with parameter"))
            EventManager.TriggerEvent(typedEventNames[index], data);
    }

}