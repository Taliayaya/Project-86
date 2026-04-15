using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace FMODUnity
{
    public class FindAndReplace : EditorWindow
    {
        private bool levelScope = true;
        private bool prefabScope;
        private string findText;
        private string replaceText;
        private string message = "";
        private MessageType messageType = MessageType.None;
        private int lastMatch = -1;
        private List<StudioEventEmitter> emitters;

        private bool first = true;

        [MenuItem("FMOD/Find and Replace", priority = 2)]
        private static void ShowFindAndReplace()
        {
            var window = CreateInstance<FindAndReplace>();
            window.titleContent = new GUIContent("FMOD Find and Replace");
            window.OnHierarchyChange();
            var position = window.position;
            window.maxSize = window.minSize = position.size = new Vector2(400, 170);
            window.position = position;
            window.ShowUtility();
        }

        private void OnHierarchyChange()
        {
            emitters = new List<StudioEventEmitter>(Resources.FindObjectsOfTypeAll<StudioEventEmitter>());

            if (!levelScope)
            {
                emitters.RemoveAll(x => PrefabUtility.GetPrefabAssetType(x) == PrefabAssetType.NotAPrefab);
            }

            if (!prefabScope)
            {
                emitters.RemoveAll(x => PrefabUtility.GetPrefabAssetType(x) != PrefabAssetType.NotAPrefab);
            }
        }

        private void OnGUI()
        {
            bool doFind = false;
            if ((Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                Event.current.Use();
                doFind = true;
            }

            GUI.SetNextControlName("find");
            EditorGUILayout.PrefixLabel("Find:");
            EditorGUI.BeginChangeCheck();
            findText = EditorGUILayout.TextField(findText);
            if (EditorGUI.EndChangeCheck())
            {
                lastMatch = -1;
                message = null;
            }
            EditorGUILayout.PrefixLabel("Replace:");
            replaceText = EditorGUILayout.TextField(replaceText);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            levelScope = EditorGUILayout.ToggleLeft("Current Level", levelScope, GUILayout.ExpandWidth(false));
            prefabScope = EditorGUILayout.ToggleLeft("Prefabs", prefabScope, GUILayout.ExpandWidth(false));
            if (EditorGUI.EndChangeCheck())
            {
                OnHierarchyChange();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Find") || doFind)
            {
                message = "";
                {
                    FindNext();
                }
                if (lastMatch == -1)
                {
                    message = "Finished Search";
                    messageType = MessageType.Warning;
                }
            }
            if (GUILayout.Button("Replace"))
            {
                message = "";
                if (lastMatch == -1)
                {
                    FindNext();
                }
                else
                {
                    Replace();
                }
                if (lastMatch == -1)
                {
                    message = "Finished Search";
                    messageType = MessageType.Warning;
                }
            }
            if (GUILayout.Button("Replace All"))
            {
                if (EditorUtility.DisplayDialog("Replace All", "Are you sure you wish to replace all in the current hierachy?", "yes", "no"))
                {
                    ReplaceAll();
                }
            }
            GUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(message))
            {
                EditorGUILayout.HelpBox(message, messageType);
            }
            else
            {
                EditorGUILayout.HelpBox("\n\n", MessageType.None);
            }

            if (first)
            {
                first = false;
                EditorGUI.FocusTextInControl("find");
            }
        }

        private void FindNext()
        {
            for (int i = lastMatch + 1; i < emitters.Count; i++)
            {
                if (emitters[i].EventReference.Path.IndexOf(findText, 0, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    lastMatch = i;
                    EditorGUIUtility.PingObject(emitters[i]);
                    Selection.activeGameObject = emitters[i].gameObject;
                    message = "Found object";
                    messageType = MessageType.Info;
                    return;
                }
            }
            lastMatch = -1;
        }

        private void ReplaceAll()
        {
            int replaced = 0;
            for (int i = 0; i < emitters.Count; i++)
            {
                if (ReplaceText(emitters[i]))
                {
                    replaced++;
                }
            }

            message = string.Format("{0} replaced", replaced);
            messageType = MessageType.Info;
        }

        private bool ReplaceText(StudioEventEmitter emitter)
        {
            int findLength = findText.Length;
            int replaceLength = replaceText.Length;
            int position = 0;
            var serializedObject = new SerializedObject(emitter);
            var eventReferenceProperty = serializedObject.FindProperty("EventReference");
            var pathProperty = eventReferenceProperty.FindPropertyRelative("Path");
            string path = pathProperty.stringValue;
            position = path.IndexOf(findText, position, StringComparison.CurrentCultureIgnoreCase);
            while (position >= 0)
            {
                path = path.Remove(position, findLength).Insert(position, replaceText);
                position += replaceLength;
                position = path.IndexOf(findText, position, StringComparison.CurrentCultureIgnoreCase);
            }
            EventReference newEventReference = EventReference.Find(path);
            eventReferenceProperty.SetEventReference(newEventReference.Guid, newEventReference.Path);
            return serializedObject.ApplyModifiedProperties();
        }

        private void Replace()
        {
            ReplaceText(emitters[lastMatch]);
            FindNext();
        }
    }
}
