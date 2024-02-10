using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ScriptableObjects.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScriptableObjects
{
    [System.Serializable]
    public class BinarySerializableData
    {
        // Only add here types that are serializable by default
        // Otherwise, add them to the SerializeValue method and the Deserialize method
        private static readonly List<Type> SerializableTypes = new List<Type>()
        {
            typeof(int),
            typeof(float),
            typeof(bool),
            typeof(string),
            typeof(InputBinding),
        };

        public Dictionary<string, object> Properties;

        public BinarySerializableData(ScriptableObject so, List<string> fieldsToSerialize, bool serializeAll = false)
        {
            Properties = new Dictionary<string, object>();
            Type T = so.GetType();
            foreach (var field in T.GetFields())
            {
                if (!serializeAll && !fieldsToSerialize.Contains(field.Name))
                    continue;
                if (Serialize(field, so, out var value))
                    Properties[field.Name] = value;
            }
        }

        private static bool IsTypeSerializable(Type tested) => SerializableTypes.Contains(tested) || tested.IsArray || tested.IsEnum || SerializableTypes.Contains(tested.GetElementType());

        public static bool Serialize(FieldInfo field, object data, out object value) =>
            SerializeValue(field.FieldType, field.GetValue(data), out value);
        
        public static bool Deserialize(FieldInfo field, object data, out object value)
        {
            Type T = field.FieldType;
            if (IsTypeSerializable(T))
            {
                value = data;
                return true;
            }

            if (IsOfType(T, typeof(Color)))
            {
                float[] c = (float[])data;
                value = new Color(c[0], c[1], c[2], c[3]);
                return true;
            }

            if (IsOfType(T, typeof(Vector2)))
            {
                float[] v = (float[])data;
                value = new Vector2(v[0], v[1]);
                return true;
            }
            value = null;
            return false;
        }
        
        private static bool IsOfType(Type tested, Type reference) => tested == reference || tested.IsArray && tested.GetElementType() == reference;

        public static Type GetSerializedType(FieldInfo field)
        {
            Type T = field.FieldType;
            if (IsTypeSerializable(T))
                return T;
            SerializeValue(T, T.IsValueType ? Activator.CreateInstance(T) : null, out var serialized);
            return serialized.GetType();
        }

        private static bool SerializeValue(Type T, object inValue, out object outValue)
        {
            if (IsTypeSerializable(T))
            {
                outValue = inValue;
                return true;
            }

            if (IsOfType(T, typeof(Color)))
            {
                Color c= (Color)inValue;
                outValue = new float[] { c.r, c.g, c.b, c.a };
                return true;
            }

            if (IsOfType(T, typeof(Vector2)))
            {
                var v = (Vector2)inValue;
                outValue = new float[] {v.x, v.y};
                return true;
            }
            
            outValue = null;
            return false;
        }
        
        
    }
}