using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    [CustomEditor(typeof(StudioBankLoader))]
    [CanEditMultipleObjects]
    public class StudioBankLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var load = serializedObject.FindProperty("LoadEvent");
            var unload = serializedObject.FindProperty("UnloadEvent");
            var tag = serializedObject.FindProperty("CollisionTag");
            var banks = serializedObject.FindProperty("Banks");
            var preload = serializedObject.FindProperty("PreloadSamples");

            EditorGUILayout.PropertyField(load, new GUIContent(L10n.Tr("Load")));
            EditorGUILayout.PropertyField(unload, new GUIContent(L10n.Tr("Unload")));

            if ((load.enumValueIndex >= 3 && load.enumValueIndex <= 6) ||
                (unload.enumValueIndex >= 3 && unload.enumValueIndex <= 6))
            {
                tag.stringValue = EditorGUILayout.TagField("Collision Tag", tag.stringValue);
            }

            EditorGUILayout.PropertyField(preload, new GUIContent(L10n.Tr("Preload Sample Data")));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(L10n.Tr("Banks"));
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button(L10n.Tr("Add Bank"), GUILayout.ExpandWidth(false)))
            {
                banks.InsertArrayElementAtIndex(banks.arraySize);
                SerializedProperty newBank = banks.GetArrayElementAtIndex(banks.arraySize - 1);
                newBank.stringValue = "";

                EventBrowser browser = CreateInstance<EventBrowser>();

                browser.titleContent = new GUIContent(L10n.Tr("Select FMOD Bank"));

                browser.ChooseBank(newBank);
                browser.ShowUtility();
            }

            Texture deleteTexture = EditorUtils.LoadImage("Delete.png");
            GUIContent deleteContent = new GUIContent(deleteTexture, "Delete Bank");

            var buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding.top = buttonStyle.padding.bottom = 1;
            buttonStyle.margin.top = 2;
            buttonStyle.padding.left = buttonStyle.padding.right = 4;
            buttonStyle.fixedHeight = GUI.skin.textField.CalcSize(new GUIContent()).y;

            for (int i = 0; i < banks.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(banks.GetArrayElementAtIndex(i), GUIContent.none);

                if (GUILayout.Button(deleteContent, buttonStyle, GUILayout.ExpandWidth(false)))
                {
                    banks.DeleteArrayElementAtIndex(i);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            Event e = Event.current;
            if (e.type == EventType.DragPerform)
            {
                if (DragAndDrop.objectReferences.Length > 0 &&
                    DragAndDrop.objectReferences[0] != null &&
                    DragAndDrop.objectReferences[0].GetType() == typeof(EditorBankRef))
                {
                    int pos = banks.arraySize;
                    banks.InsertArrayElementAtIndex(pos);
                    var pathProperty = banks.GetArrayElementAtIndex(pos);

                    pathProperty.stringValue = ((EditorBankRef)DragAndDrop.objectReferences[0]).Name;

                    e.Use();
                }
            }
            if (e.type == EventType.DragUpdated)
            {
                if (DragAndDrop.objectReferences.Length > 0 &&
                    DragAndDrop.objectReferences[0] != null &&
                    DragAndDrop.objectReferences[0].GetType() == typeof(EditorBankRef))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    DragAndDrop.AcceptDrag();
                    e.Use();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
