using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    [CustomEditor(typeof(StudioParameterTrigger))]
    public class StudioParameterTriggerEditor : Editor
    {
        private StudioEventEmitter targetEmitter;
        private SerializedProperty emitters;
        private SerializedProperty trigger;
        private SerializedProperty tag;

        private bool[] expanded;

        private void OnEnable()
        {
            emitters = serializedObject.FindProperty("Emitters");
            trigger = serializedObject.FindProperty("TriggerEvent");
            tag = serializedObject.FindProperty("CollisionTag");
            targetEmitter = null;
            for (int i = 0; i < emitters.arraySize; i++)
            {
                targetEmitter = emitters.GetArrayElementAtIndex(i).FindPropertyRelative("Target").objectReferenceValue as StudioEventEmitter;
                if (targetEmitter != null)
                {
                    expanded = new bool[targetEmitter.GetComponents<StudioEventEmitter>().Length];
                    break;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var newTargetEmitter = EditorGUILayout.ObjectField(L10n.Tr("Target"), targetEmitter, typeof(StudioEventEmitter), true) as StudioEventEmitter;
            if (newTargetEmitter != targetEmitter)
            {
                emitters.ClearArray();
                targetEmitter = newTargetEmitter;

                if (targetEmitter == null)
                {
                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                List<StudioEventEmitter> newEmitters = new List<StudioEventEmitter>();
                targetEmitter.GetComponents(newEmitters);
                expanded = new bool[newEmitters.Count];
                foreach (var emitter in newEmitters)
                {
                    emitters.InsertArrayElementAtIndex(0);
                    emitters.GetArrayElementAtIndex(0).FindPropertyRelative("Target").objectReferenceValue = emitter;
                }
            }

            if (targetEmitter == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(trigger, new GUIContent(L10n.Tr("Trigger")));

            if (trigger.enumValueIndex >= (int)EmitterGameEvent.TriggerEnter && trigger.enumValueIndex <= (int)EmitterGameEvent.TriggerExit2D)
            {
                tag.stringValue = EditorGUILayout.TagField("Collision Tag", tag.stringValue);
            }

            var localEmitters = new List<StudioEventEmitter>();
            targetEmitter.GetComponents(localEmitters);

            int emitterIndex = 0;
            foreach (var emitter in localEmitters)
            {
                SerializedProperty emitterProperty = null;
                for(int i = 0; i < emitters.arraySize; i++)
                {
                    if (emitters.GetArrayElementAtIndex(i).FindPropertyRelative("Target").objectReferenceValue == emitter)
                    {
                        emitterProperty = emitters.GetArrayElementAtIndex(i);
                        break;
                    }
                }

                // New emitter component added to game object since we last looked
                if (emitterProperty == null)
                {
                    emitters.InsertArrayElementAtIndex(0);
                    emitterProperty = emitters.GetArrayElementAtIndex(0);
                    emitterProperty.FindPropertyRelative("Target").objectReferenceValue = emitter;
                }

                if (!emitter.EventReference.IsNull)
                {
                    expanded[emitterIndex] = EditorGUILayout.Foldout(expanded[emitterIndex], emitter.EventReference.Path);
                    if (expanded[emitterIndex])
                    {
                        var eventRef = EventManager.EventFromGUID(emitter.EventReference.Guid);

                        foreach (var paramRef in eventRef.LocalParameters)
                        {
                            bool set = false;
                            int index = -1;
                            for (int i = 0; i < emitterProperty.FindPropertyRelative("Params").arraySize; i++)
                            {
                                if (emitterProperty.FindPropertyRelative("Params").GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue == paramRef.Name)
                                {
                                    index = i;
                                    set = true;
                                    break;
                                }
                            }
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.PrefixLabel(paramRef.Name);
                            bool newSet = GUILayout.Toggle(set, "");
                            if (!set && newSet)
                            {
                                index = 0;
                                emitterProperty.FindPropertyRelative("Params").InsertArrayElementAtIndex(0);
                                emitterProperty.FindPropertyRelative("Params").GetArrayElementAtIndex(0).FindPropertyRelative("Name").stringValue = paramRef.Name;
                                emitterProperty.FindPropertyRelative("Params").GetArrayElementAtIndex(0).FindPropertyRelative("Value").floatValue = 0;
                            }
                            if (set && !newSet)
                            {
                                emitterProperty.FindPropertyRelative("Params").DeleteArrayElementAtIndex(index);
                            }
                            set = newSet;

                            if (set)
                            {
                                var valueProperty = emitterProperty.FindPropertyRelative("Params")
                                    .GetArrayElementAtIndex(index).FindPropertyRelative("Value");
                                valueProperty.floatValue =
                                    EditorUtils.DrawParameterValueLayout(valueProperty.floatValue, paramRef);
                            }
                            else
                            {
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    EditorUtils.DrawParameterValueLayout(0, paramRef);
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                emitterIndex++;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
