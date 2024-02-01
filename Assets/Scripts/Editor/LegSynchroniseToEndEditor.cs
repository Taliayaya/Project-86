using Gameplay;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(LegSynchroniseToEnd))]
    public class LegSynchroniseToEndEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Synchronise"))
            {
                var legSynchroniseToEnd = target as Gameplay.LegSynchroniseToEnd;
                if (legSynchroniseToEnd != null) legSynchroniseToEnd.Synchronise();
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}