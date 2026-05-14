using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    [CustomEditor(typeof(StudioListener))]
    [CanEditMultipleObjects]
    public class StudioListenerEditor : Editor
    {
        private SerializedProperty attenuationObject;
        private SerializedProperty nonRigidbodyVelocity;

        private void OnEnable()
        {
            attenuationObject = serializedObject.FindProperty("attenuationObject");
            nonRigidbodyVelocity = serializedObject.FindProperty("nonRigidbodyVelocity");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(true);
            int index = ((StudioListener)serializedObject.targetObject).ListenerNumber;
            EditorGUILayout.IntSlider(L10n.Tr("Listener Index"), index, 0, FMOD.CONSTANTS.MAX_LISTENERS - 1);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(attenuationObject, new GUIContent(L10n.Tr("Attenuation Object")));
            EditorGUILayout.PropertyField(nonRigidbodyVelocity, new GUIContent(L10n.Tr("Non-Rigidbody Velocity")));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
