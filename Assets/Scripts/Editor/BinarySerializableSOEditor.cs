using System.Linq;
using System.Reflection;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(BinarySerializableSO), true ), CanEditMultipleObjects]
    public class BinarySerializableSOEditor : UnityEditor.Editor
    {
        private GUIStyle _buttonsStyle;
        private bool _showFieldsToSerialize;

        public override void OnInspectorGUI()
        {
            if (_buttonsStyle == null)
            {
                _buttonsStyle = new GUIStyle();
                _buttonsStyle.padding.right = 0;
                _buttonsStyle.padding.left = 0;
            }
            BinarySerializableSO tar = (BinarySerializableSO)target;

            serializedObject.Update();
        
            BinarySerializableSO[] hudWindowSettingsArray = targets.Cast<BinarySerializableSO>().ToArray();

            {


                if (GUILayout.Button("Debug fields to Serialize"))
                {
                    _showFieldsToSerialize = !_showFieldsToSerialize;
                }

                if (_showFieldsToSerialize)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("fieldsToSerialize"),
                        true); // Uncomment this line to display the fieldsToSerialize list in the inspector
                }

                System.Type parametersType = tar.GetType();
                FieldInfo[] fields = parametersType.GetFields();
                foreach (FieldInfo field in fields)
                {
                    //check for "hide in inspector" attribute:
                    // if there is one, cancel the display for this field
                    if (System.Attribute.IsDefined(field, typeof(HideInInspector), false))
                        continue;

                    // make a row
                    EditorGUILayout.BeginHorizontal();
                    // 1. display the custom toggle button
                    // (little trick to have the button stick to the bottom of the row
                    // if there is a header on this property...)
                    EditorGUILayout.BeginVertical(GUILayout.Width(20f));
                    EditorGUILayout.BeginHorizontal();
                    bool hasHeader = System.Attribute.IsDefined(field, typeof(HeaderAttribute), false);
                    if (hasHeader)
                        GUILayout.FlexibleSpace();

                    if (GUILayout.Button(
                            (tar.SerializeField(field.Name)
                                ? EditorGUIUtility.IconContent("animationvisibilitytoggleon@2x")
                                : EditorGUIUtility.IconContent("scenevis_hidden_hover@2x")), _buttonsStyle,
                            GUILayout.Width(20f),
                            GUILayout.Height(20f)))
                    {

                        foreach (var hudWindowSettings in hudWindowSettingsArray)
                        {
                            Debug.Log("GUI Button Pressed)" + hudWindowSettings.name);
                            hudWindowSettings.ToggleSerializeField(field.Name);
                            EditorUtility.SetDirty(hudWindowSettings);
                        }

                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    // 2. put some spacing between the button and the actual field display
                    GUILayout.Space(16);
                    // 3. display the field with a type-dependent input
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(field.Name), true);
                    EditorGUILayout.EndHorizontal();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}