using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    [CustomEditor(typeof(StudioGlobalParameterTrigger))]
    public class StudioGlobalParameterTriggerEditor : Editor
    {
        private SerializedProperty param;
        private SerializedProperty trigger;
        private SerializedProperty tag;
        private SerializedProperty value;

        private SerializedProperty data1, data2;

        private static GUIContent NotFoundWarning;

        private string currentPath;

        [SerializeField]
        private EditorParamRef editorParamRef;

        private void OnEnable()
        {
            param = serializedObject.FindProperty("Parameter");
            trigger = serializedObject.FindProperty("TriggerEvent");
            tag = serializedObject.FindProperty("CollisionTag");
            value = serializedObject.FindProperty("Value");
        }

        public override void OnInspectorGUI()
        {
            if (NotFoundWarning == null)
            {
                Texture warningIcon = EditorUtils.LoadImage("NotFound.png");
                NotFoundWarning = new GUIContent(L10n.Tr("Parameter Not Found"), warningIcon);
            }

            EditorGUILayout.PropertyField(trigger, new GUIContent(L10n.Tr("Trigger")));
            if (trigger.enumValueIndex >= (int)EmitterGameEvent.TriggerEnter && trigger.enumValueIndex <= (int)EmitterGameEvent.TriggerExit2D)
            {
                tag.stringValue = EditorGUILayout.TagField("Collision Tag", tag.stringValue);
            }

            EditorGUILayout.PropertyField(param, new GUIContent(L10n.Tr("Parameter")));

            if (param.stringValue != currentPath)
            {
                currentPath = param.stringValue;

                if (string.IsNullOrEmpty(param.stringValue))
                {
                    editorParamRef = null;
                }
                else
                {
                    editorParamRef = EventManager.ParamFromPath(param.stringValue);
                    value.floatValue = Mathf.Clamp(value.floatValue, editorParamRef.Min, editorParamRef.Max);
                }
            }

            if (editorParamRef != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(L10n.Tr("Override Value"));
                    value.floatValue = EditorUtils.DrawParameterValueLayout(value.floatValue, editorParamRef);
                }
            }
            else
            {
                Rect rect = EditorGUILayout.GetControlRect();
                rect.xMin += EditorGUIUtility.labelWidth;

                GUI.Label(rect, NotFoundWarning);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
