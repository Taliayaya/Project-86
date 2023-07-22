using System.Linq;
using ScriptableObjects.UI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HUDWindow)), CanEditMultipleObjects]
public class HUDWindowEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        HUDWindow[] hudWindows = targets.Cast<HUDWindow>().ToArray();
        if (GUILayout.Button("Set Settings To Current Layout"))
        {
            foreach (var hudWindow in hudWindows)
                hudWindow.SetPreset();
        }

        if (GUILayout.Button("Delete Save"))
        {
            foreach (var hudWindow in hudWindows)
                hudWindow.DeleteSave();
        }

        serializedObject.ApplyModifiedProperties();
    }
}