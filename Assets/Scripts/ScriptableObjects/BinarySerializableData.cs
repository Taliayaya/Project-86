using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScriptableObjects
{
    [System.Serializable]
    public class BinarySerializableData
    {
        private static readonly List<Type> SerializableTypes = new List<Type>()
        {
            typeof(int),
            typeof(float),
            typeof(bool),
            typeof(string),
            typeof(InputBinding),
        };

        public Dictionary<string, object> Properties;

        public BinarySerializableData(ScriptableObject so, List<string> fieldsToSerialize)
        {
            Properties = new Dictionary<string, object>();
            Type T = so.GetType();
            foreach (var field in T.GetFields())
            {
                if (!fieldsToSerialize.Contains(field.Name))
                    continue;
                if (Serialize(field, so, out var value))
                    Properties[field.Name] = value;
            }
        }

        private static bool IsTypeSerializable(Type tested) => SerializableTypes.Contains(tested) || tested.IsArray || SerializableTypes.Contains(tested.GetElementType());

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
            
            outValue = null;
            return false;
        }
        
        
    }
}