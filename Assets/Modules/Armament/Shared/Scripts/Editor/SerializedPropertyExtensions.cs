using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
namespace Armament.Shared.Editor
{
	public static class SerializedPropertyExtensions
	{
		public static Type GetFieldType(this SerializedProperty property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			Type parentType = property.serializedObject.targetObject.GetType();
			FieldInfo field = GetFieldInfoFromPropertyPath(parentType, property.propertyPath);
			if (field != null)
			{
				Type fieldType = field.FieldType;
				if (fieldType.IsArray)
				{
					return fieldType.GetElementType();
				}

				if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
				{
					return fieldType.GetGenericArguments()[0];
				}

				return fieldType;
			}

			return null;
		}

		private static FieldInfo GetFieldInfoFromPropertyPath(Type type, string path)
		{
			string[] elements = path.Split('.');
			FieldInfo field = null;

			for (int i = 0; i < elements.Length; i++)
			{
				string element = elements[i];
				if (element == "Array")
				{
					i++; // Skip the "data[x]" part
					continue;
				}

				field = type.GetField(element, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (field == null)
					return null;

				type = field.FieldType;
				if (type.IsArray)
					type = type.GetElementType();
				else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
					type = type.GetGenericArguments()[0];
			}

			return field;
		}
	}
}