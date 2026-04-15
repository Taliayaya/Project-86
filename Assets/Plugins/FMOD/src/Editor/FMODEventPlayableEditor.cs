#if UNITY_TIMELINE_EXIST

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using System;
using System.Linq;
using System.Reflection;

namespace FMODUnity
{
    [CustomEditor(typeof(FMODEventPlayable))]
    public class FMODEventPlayableEditor : Editor
    {
        private FMODEventPlayable eventPlayable;
        private EditorEventRef editorEventRef;
        private List<EditorParamRef> missingInitialParameterValues = new List<EditorParamRef>();
        private List<EditorParamRef> missingParameterAutomations = new List<EditorParamRef>();

        private SerializedProperty parametersProperty;
        private SerializedProperty parameterLinksProperty;
        private SerializedProperty parameterAutomationProperty;

        private ListView parameterLinksView;
        private ListView initialParameterValuesView;

        private string eventPath;

        public void OnEnable()
        {
            eventPlayable = target as FMODEventPlayable;

            parametersProperty = serializedObject.FindProperty("Parameters");
            parameterLinksProperty = serializedObject.FindProperty("Template.ParameterLinks");
            parameterAutomationProperty = serializedObject.FindProperty("Template.ParameterAutomation");

            parameterLinksView = new ListView(parameterLinksProperty);
            parameterLinksView.drawElementWithLabelCallback = DrawParameterLink;
            parameterLinksView.onCanAddCallback = (list) => missingParameterAutomations.Count > 0;
            parameterLinksView.onAddDropdownCallback = DoAddParameterLinkMenu;
            parameterLinksView.onRemoveCallback = (list) => DeleteParameterAutomation(list.index);

            initialParameterValuesView = new ListView(parametersProperty);
            initialParameterValuesView.drawElementWithLabelCallback = DrawInitialParameterValue;
            initialParameterValuesView.onCanAddCallback = (list) => missingInitialParameterValues.Count > 0;
            initialParameterValuesView.onAddDropdownCallback = DoAddInitialParameterValueMenu;
            initialParameterValuesView.onRemoveCallback = (list) => DeleteInitialParameterValue(list.index);

            RefreshEventRef();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void OnDestroy()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            RefreshMissingParameterLists();

            // This is in case the undo/redo modified any curves on the Playable's clip
            RefreshTimelineEditor();
        }

        private void RefreshEventRef()
        {
            if (eventPath != eventPlayable.EventReference.Path)
            {
                eventPath = eventPlayable.EventReference.Path;

                if (!string.IsNullOrEmpty(eventPath))
                {
                    editorEventRef = EventManager.EventFromPath(eventPath);
                }
                else
                {
                    editorEventRef = null;
                }

                if (editorEventRef != null)
                {
                    eventPlayable.UpdateEventDuration(
                        editorEventRef.IsOneShot ? editorEventRef.Length : float.PositiveInfinity);
                }

                ValidateParameterSettings();
                RefreshMissingParameterLists();
            }
        }

        private void ValidateParameterSettings()
        {
            if (editorEventRef != null)
            {
                List<string> namesToDelete = new List<string>();

                for (int i = 0; i < parametersProperty.arraySize; ++i)
                {
                    SerializedProperty current = parametersProperty.GetArrayElementAtIndex(i);
                    SerializedProperty name = current.FindPropertyRelative("Name");

                    EditorParamRef paramRef = editorEventRef.LocalParameters.FirstOrDefault(p => p.Name == name.stringValue);

                    if (paramRef != null)
                    {
                        SerializedProperty value = current.FindPropertyRelative("Value");
                        value.floatValue = Mathf.Clamp(value.floatValue, paramRef.Min, paramRef.Max);
                    }
                    else
                    {
                        namesToDelete.Add(name.stringValue);
                    }
                }

                foreach(string name in namesToDelete)
                {
                    DeleteInitialParameterValue(name);
                }

                namesToDelete.Clear();

                for (int i = 0; i < parameterLinksProperty.arraySize; ++i)
                {
                    SerializedProperty current = parameterLinksProperty.GetArrayElementAtIndex(i);
                    SerializedProperty name = current.FindPropertyRelative("Name");

                    if (!editorEventRef.LocalParameters.Any(p => p.Name == name.stringValue))
                    {
                        namesToDelete.Add(name.stringValue);
                    }
                }

                foreach(string name in namesToDelete)
                {
                    DeleteParameterAutomation(name);
                }
            }
        }

        private void RefreshMissingParameterLists()
        {
            if (editorEventRef != null)
            {
                serializedObject.Update();

                missingInitialParameterValues =
                    editorEventRef.LocalParameters.Where(p => !InitialParameterValueExists(p.Name)).ToList();
                missingParameterAutomations =
                    editorEventRef.LocalParameters.Where(p => !ParameterLinkExists(p.Name)).ToList();
            }
            else
            {
                missingInitialParameterValues.Clear();
                missingParameterAutomations.Clear();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            RefreshEventRef();

            var eventReference = serializedObject.FindProperty("EventReference");
            var stopType = serializedObject.FindProperty("StopType");

            const string EventReferenceLabel = "Event";

            EditorUtils.DrawLegacyEvent(serializedObject.FindProperty("eventName"), EventReferenceLabel);

            EditorGUILayout.PropertyField(eventReference, new GUIContent(EventReferenceLabel));
            EditorGUILayout.PropertyField(stopType, new GUIContent("Stop Mode"));

            DrawInitialParameterValues();
            DrawParameterAutomations();

            eventPlayable.OnValidate();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInitialParameterValues()
        {
            if (editorEventRef != null)
            {
                parametersProperty.isExpanded =
                    EditorGUILayout.Foldout(parametersProperty.isExpanded, "Initial Parameter Values", true);

                if (parametersProperty.isExpanded)
                {
                    initialParameterValuesView.DrawLayout();
                }
            }
        }

        private void DoAddInitialParameterValueMenu(Rect rect, UnityEditorInternal.ReorderableList list)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("All"), false, () =>
                {
                    foreach (EditorParamRef parameter in missingInitialParameterValues)
                    {
                        AddInitialParameterValue(parameter);
                    }
                });

            menu.AddSeparator(string.Empty);

            foreach (EditorParamRef parameter in missingInitialParameterValues)
            {
                string text = parameter.Name;

                if (ParameterLinkExists(parameter.Name))
                {
                    text += " (automated)";
                }

                menu.AddItem(new GUIContent(text), false,
                    (userData) =>
                    {
                        AddInitialParameterValue(userData as EditorParamRef);
                    },
                    parameter);
            }

            menu.DropDown(rect);
        }

        private void DrawInitialParameterValue(Rect rect, float labelRight, int index, bool active, bool focused)
        {
            if (editorEventRef == null)
            {
                return;
            }

            SerializedProperty property = parametersProperty.GetArrayElementAtIndex(index);

            string name = property.FindPropertyRelative("Name").stringValue;

            EditorParamRef paramRef = editorEventRef.LocalParameters.FirstOrDefault(p => p.Name == name);

            if (paramRef == null)
            {
                return;
            }

            Rect nameLabelRect = rect;
            nameLabelRect.xMax = labelRight;

            Rect sliderRect = rect;
            sliderRect.xMin = nameLabelRect.xMax;

            SerializedProperty valueProperty = property.FindPropertyRelative("Value");

            GUI.Label(nameLabelRect, name);

            using (new NoIndentScope())
            {
                valueProperty.floatValue =
                    EditorGUI.Slider(sliderRect, valueProperty.floatValue, paramRef.Min, paramRef.Max);
            }
        }

        private void DrawParameterAutomations()
        {
            if (editorEventRef != null)
            {
                parameterLinksProperty.isExpanded =
                    EditorGUILayout.Foldout(parameterLinksProperty.isExpanded, "Parameter Automations", true);

                if (parameterLinksProperty.isExpanded)
                {
                    parameterLinksView.DrawLayout();
                }
            }
        }

        private void DoAddParameterLinkMenu(Rect rect, UnityEditorInternal.ReorderableList list)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("All"), false, () =>
                {
                    foreach (EditorParamRef parameter in missingParameterAutomations)
                    {
                        AddParameterAutomation(parameter.Name);
                    }
                });

            menu.AddSeparator(string.Empty);

            foreach (EditorParamRef parameter in missingParameterAutomations)
            {
                string text = parameter.Name;

                if (InitialParameterValueExists(parameter.Name))
                {
                    text += " (has initial value)";
                }

                menu.AddItem(new GUIContent(text), false,
                    (userData) =>
                    {
                        AddParameterAutomation(userData as string);
                    },
                    parameter.Name);
            }

            menu.DropDown(rect);
        }

        private void DrawParameterLink(Rect rect, float labelRight, int index, bool active, bool focused)
        {
            if (editorEventRef == null)
            {
                return;
            }

            SerializedProperty linkProperty = parameterLinksProperty.GetArrayElementAtIndex(index);

            string name = linkProperty.FindPropertyRelative("Name").stringValue;

            EditorParamRef paramRef = editorEventRef.LocalParameters.FirstOrDefault(p => p.Name == name);

            if (paramRef == null)
            {
                return;
            }

            int slot = linkProperty.FindPropertyRelative("Slot").intValue;

            string slotName = string.Format("Slot{0:D2}", slot);
            SerializedProperty valueProperty = parameterAutomationProperty.FindPropertyRelative(slotName);

            GUIStyle slotStyle = GUI.skin.label;

            Rect slotRect = rect;
            slotRect.width = slotStyle.CalcSize(new GUIContent("slot 00:")).x;

            Rect nameRect = rect;
            nameRect.xMin = slotRect.xMax;
            nameRect.xMax = labelRight;

            Rect valueRect = rect;
            valueRect.xMin = nameRect.xMax;

            using (new EditorGUI.PropertyScope(rect, GUIContent.none, valueProperty))
            {
                GUI.Label(slotRect, string.Format("slot {0:D2}:", slot), slotStyle);
                GUI.Label(nameRect, name);

                using (new NoIndentScope())
                {
                    valueProperty.floatValue =
                        EditorGUI.Slider(valueRect, valueProperty.floatValue, paramRef.Min, paramRef.Max);
                }
            }
        }

        private bool InitialParameterValueExists(string name)
        {
            return parametersProperty.ArrayContains("Name", p => p.stringValue == name);
        }

        private bool ParameterLinkExists(string name)
        {
            return parameterLinksProperty.ArrayContains("Name", p => p.stringValue == name);
        }

        private void AddInitialParameterValue(EditorParamRef editorParamRef)
        {
            serializedObject.Update();

            if (!InitialParameterValueExists(editorParamRef.Name))
            {
                DeleteParameterAutomation(editorParamRef.Name);

                parametersProperty.ArrayAdd(p => {
                    p.FindPropertyRelative("Name").stringValue = editorParamRef.Name;
                    p.FindPropertyRelative("Value").floatValue = editorParamRef.Default;
                });

                serializedObject.ApplyModifiedProperties();

                RefreshMissingParameterLists();
            }
        }

        private void DeleteInitialParameterValue(string name)
        {
            serializedObject.Update();

            int index = parametersProperty.FindArrayIndex("Name", p => p.stringValue == name);

            if (index >= 0)
            {
                DeleteInitialParameterValue(index);
            }
        }

        private void DeleteInitialParameterValue(int index)
        {
            serializedObject.Update();

            parametersProperty.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();
            RefreshMissingParameterLists();
        }

        private void AddParameterAutomation(string name)
        {
            serializedObject.Update();

            if (!ParameterLinkExists(name))
            {
                int slot = -1;

                for (int i = 0; i < AutomatableSlots.Count; ++i)
                {
                    if (!parameterLinksProperty.ArrayContains("Slot", p => p.intValue == i))
                    {
                        slot = i;
                        break;
                    }
                }

                if (slot >= 0)
                {
                    DeleteInitialParameterValue(name);

                    parameterLinksProperty.ArrayAdd(p => {
                        p.FindPropertyRelative("Name").stringValue = name;
                        p.FindPropertyRelative("Slot").intValue = slot;
                    });

                    serializedObject.ApplyModifiedProperties();

                    RefreshMissingParameterLists();
                    RefreshTimelineEditor();
                }
            }
        }

        private void DeleteParameterAutomation(string name)
        {
            serializedObject.Update();

            int index = parameterLinksProperty.FindArrayIndex("Name", p => p.stringValue == name);

            if (index >= 0)
            {
                DeleteParameterAutomation(index);
            }
        }

        private void DeleteParameterAutomation(int index)
        {
            serializedObject.Update();

            if (eventPlayable.OwningClip.hasCurves)
            {
                SerializedProperty linkProperty = parameterLinksProperty.GetArrayElementAtIndex(index);
                SerializedProperty slotProperty = linkProperty.FindPropertyRelative("Slot");

                AnimationClip curvesClip = eventPlayable.OwningClip.curves;

                Undo.RecordObject(curvesClip, string.Empty);
                AnimationUtility.SetEditorCurve(curvesClip, GetParameterCurveBinding(slotProperty.intValue), null);
            }

            parameterLinksProperty.DeleteArrayElementAtIndex(index);

            serializedObject.ApplyModifiedProperties();

            RefreshMissingParameterLists();

            RefreshTimelineEditor();
        }

        private static EditorCurveBinding GetParameterCurveBinding(int index)
        {
            EditorCurveBinding result = new EditorCurveBinding() {
                path = string.Empty,
                type = typeof(FMODEventPlayable),
                propertyName = string.Format("parameterAutomation.slot{0:D2}", index),
            };

            return result;
        }

        private static void RefreshTimelineEditor()
        {
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        }
    }
}
#endif
