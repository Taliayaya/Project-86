    using System.Reflection;
    using ScriptableObjects.GameParameters;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(GameParameters), true)]
    public class GameParameterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GameParameters parameters = (GameParameters)target;

            EditorGUILayout.LabelField($"Name: {parameters.GetParametersName}", EditorStyles.boldLabel);

            System.Type parametersType = parameters.GetType();
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
                bool hasHeader = System.Attribute.IsDefined(field, typeof(HeaderAttribute), false);
                if (hasHeader)
                    GUILayout.FlexibleSpace();
                if (GUILayout.Button(parameters.ShowsField(field.Name) ? "-" : "+", GUILayout.Width(20f)))
                {
                    parameters.ToggleShowField(field.Name);
                    EditorUtility.SetDirty(parameters);
                    AssetDatabase.SaveAssets();
                }

                EditorGUILayout.EndVertical();
                // 2. put some spacing between the button and the actual field display
                GUILayout.Space(16);
                // 3. display the field with a type-dependent input
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field.Name), true);
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }