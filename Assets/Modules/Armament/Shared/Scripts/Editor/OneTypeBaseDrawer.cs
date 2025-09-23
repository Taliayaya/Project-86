using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace Armament.Shared.Editor
{
	[CustomPropertyDrawer(typeof(OneTypeBase), true)]
	public class OneTypeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Calculate rects for the Type and Info fields
			Rect typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			string typeName = GetDirectAccess<string>(property, "TypeName");
			string valueName = GetDirectAccess<string>(property, "ValueName");
			SerializedProperty typeProp = property.FindPropertyRelative(typeName);

			// Get the existing SoundTypes from the parent array
			SerializedProperty parentArray = property.serializedObject.FindProperty(GetPropertyName(property));
			List<Enum> usedTypes = new List<Enum>();


			int current = int.Parse(label.ToString().Replace("Element ", ""));

			List<Enum> allTypes = new List<Enum>();
			allTypes.AddRange(Enum.GetValues(typeProp.GetFieldType()).Cast<Enum>());

			for (int i = 0; i < parentArray.arraySize; i++)
			{
				int normalizedRelativeIndex = parentArray.GetArrayElementAtIndex(i).FindPropertyRelative(typeName).enumValueIndex;
				int normalzedCurrentIndex = typeProp.enumValueIndex;

				normalizedRelativeIndex.Cap(0, allTypes.Count - 1);
				normalzedCurrentIndex.Cap(0, allTypes.Count - 1);


				if (normalizedRelativeIndex == normalzedCurrentIndex && i < current)
				{
					usedTypes.Add(allTypes.ElementAt(normalizedRelativeIndex));
				}
				else if (normalizedRelativeIndex != normalzedCurrentIndex)
				{
					usedTypes.Add(allTypes.ElementAt(normalizedRelativeIndex));
				}
			}

			
			List<Enum> availableTypes = allTypes.Except(usedTypes).ToList();

			bool areAllUsed = false;
			if (availableTypes.Count > 0)
			{
				int selectedIndex = Mathf.Max(0, availableTypes.IndexOf(allTypes.ElementAt(typeProp.enumValueIndex)));
				selectedIndex = EditorGUI.Popup(typeRect, typeName, selectedIndex,
					availableTypes.Select(t => t.ToString()).ToArray());
				Enum type = availableTypes[selectedIndex];
				typeProp.enumValueIndex = type.IndexOf(allTypes);
			}
			else
			{
				EditorGUI.LabelField(typeRect, typeName, $"All {typeName} were used");
				areAllUsed = true;
			}

			if (!areAllUsed)
			{
				SerializedProperty infoProp = property.FindPropertyRelative(valueName);
				if (infoProp.isArray)
				{
					EditorGUI.PropertyField(GetPropertyPosition(position, infoProp, 1), infoProp, true);
				}
				else
				{
					EditorGUI.PropertyField(GetPropertyPosition(position, 1), infoProp, GUIContent.none, true);
				}
			}

			EditorGUI.EndProperty();
		}

		private static string GetPropertyName(SerializedProperty property)
		{
			return property.propertyPath.Substring(0, property.propertyPath.IndexOf("."));
		}

		private Rect GetPropertyPosition(Rect position, int count)
		{
			return new Rect(position.x,
				position.y + EditorGUIUtility.singleLineHeight * count + EditorGUIUtility.standardVerticalSpacing,
				position.width, EditorGUIUtility.singleLineHeight);
		}

		private Rect GetPropertyPosition(Rect position, SerializedProperty relative, int count)
		{
			return new Rect(position.x,
				position.y + EditorGUIUtility.singleLineHeight * count + EditorGUIUtility.standardVerticalSpacing,
				position.width, GetInfoPropertyHeight(relative));
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			string valueName = GetDirectAccess<string>(property, "ValueName");
			if (string.IsNullOrEmpty(valueName)) throw new InvalidOperationException($"ValueName not found in {property.type}");
			SerializedProperty infoProp = property.FindPropertyRelative(valueName);
			height += GetInfoPropertyHeight(infoProp);
			return height;
		}

		private float GetInfoPropertyHeight(SerializedProperty property)
		{
			return EditorGUI.GetPropertyHeight(property, true);
		}

		private T GetDirectAccess<T>(SerializedProperty property, string name)
		{
			object parent = property.serializedObject.targetObject;
			object valueInstance = fieldInfo.GetValue(parent);
			if (valueInstance != null)
			{
				Type type = valueInstance.GetType();
				if (valueInstance.GetType().IsArray)
				{
					type = type.GetElementType();
					valueInstance = ((Array)valueInstance).GetValue(0);
				}

				var propInfo = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
				if (propInfo != null)
				{
					object propValue = propInfo.GetValue(valueInstance, null);
					return (T)propValue;
				}
			}
			return default;
		}
	}
}