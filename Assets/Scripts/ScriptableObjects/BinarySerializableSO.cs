﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace ScriptableObjects
{
    public class BinarySerializableSO : ScriptableObject
    {
#if UNITY_EDITOR
        private const string ScriptableObjectsDataDirectory = "ScriptableObjects_dev";
#else
                private const string ScriptableObjectsDataDirectory = "ScriptableObjects";
#endif

        [SerializeField] protected List<string> fieldsToSerialize = new List<string>();
        public List<string> FieldsToSerialize => fieldsToSerialize;
        /// <summary>
        /// Saves the ScriptableObject to a file in the persistent data path.
        /// It writes all the serialized fields in a binary format, unreadable.
        /// This is more secure than <see cref="JsonSerializableSO"/> but it does not mean it is completely secured.
        /// The goal is to discourage the user to modify the file.
        /// </summary>
        /// <seealso cref="JsonSerializableSO.SaveToFile(string)"/>
        /// <param name="filename">The name of the output file</param>
        public void SaveToFile(string filename)
        {
            string dirPath = Path.Combine(Application.persistentDataPath, ScriptableObjectsDataDirectory);
            string filePath = Path.Combine(dirPath, $"{filename}.data");

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(filePath, FileMode.Create);
            
            BinarySerializableData data = new BinarySerializableData(this, fieldsToSerialize);
            Debug.Log($"[BinarySerializableSO] SaveToFile: Saving to {filePath}");
            try
            {
                formatter.Serialize(stream, data.Properties);
                Debug.Log($"[BinarySerializableSO] SaveToFile: Successfully serialized");
            }
            catch (SerializationException e)
            {
                Debug.LogError($"[BinarySerializableSO] SaveToFile: Failed to serialize. Reason: {e.Message}");
            }
            finally
            {
                stream.Close();
            }

        }

        /// <summary>
        /// Save the file with the same name as the ScriptableObject.
        /// See <see cref="SaveToFile(string)"/> for more information.
        /// </summary>
        public void SaveToFile()
        {
            SaveToFile(name);
        }

        /// <summary>
        /// Loads the ScriptableObject from a file in the persistent data path.
        /// It writes all the serialized fields from a binary file.
        /// </summary>
        /// <remarks>Any serialized field will be overriden by the content if it exists. It may cause unexpected issues.</remarks>
        /// <seealso cref="JsonSerializableSO.LoadFromFile(string)"/>
        /// <param name="filename">The input file name</param>
        public void LoadFromFile(string filename)
        {
            string dirPath = Path.Combine(Application.persistentDataPath, ScriptableObjectsDataDirectory);
            string filePath = Path.Combine(dirPath, $"{filename}.data");

            if (!File.Exists(filePath))
            {
                Debug.LogWarning(
                    $"[BinarySerializableSO] LoadFromFile: File \"{filePath}\" not found! Getting default values.", this);
                return;
            }

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(filePath, FileMode.Open);

            Dictionary<string, object> properties = null;
            try
            {
                properties = formatter.Deserialize(stream) as Dictionary<string, object>;
                Debug.Log($"[BinarySerializableSO] LoadFromFile: Successfully deserialized");
            }
            catch (SerializationException e)
            {
                Debug.LogError($"[BinarySerializableSO] LoadFromFile: Failed to deserialize. Reason: {e.Message}");
            }
            finally
            {
                stream.Close();
            }
            
            if (properties == null)
                return;

            Type T = GetType();
            foreach (var pair in properties)
            {
                var field = T.GetField(pair.Key);
                if (BinarySerializableData.Deserialize(field, pair.Value, out var deserializedValue))
                    field.SetValue(this, deserializedValue);
            }
        }

        /// <summary>
        /// Load the file with the same name as the ScriptableObject.
        /// See <see cref="LoadFromFile(string)"/> for more information.
        /// </summary>
        public void LoadFromFile()
        {
            LoadFromFile(name);
        }
        
        public bool SerializeField(string fieldName)
        {
            return fieldsToSerialize.Contains(fieldName);
        }

        public void ToggleSerializeField(string fieldName)
        {
            if (SerializeField(fieldName))
                fieldsToSerialize.Remove(fieldName);
            else
                fieldsToSerialize.Add(fieldName);
        }

        public void ResetToDefault()
        {
            Type T = GetType();
            var s = "";
            foreach (var fieldName in fieldsToSerialize)
            {
                s += fieldName + ", ";
            }
            Debug.Log("[BinarySerializableSO] ResetToDefault: Resetting to default values: " + s);
            foreach (var fieldName in fieldsToSerialize)
            {
                var field = T.GetField(fieldName);
                DefaultValueAttribute defaultValue = field.GetCustomAttribute(typeof(DefaultValueAttribute)) as DefaultValueAttribute;
                if (defaultValue != null)
                {
                    var val = defaultValue.GetDefaultValue();
                    field.SetValue(this, val);
                    Debug.Log($"[BinarySerializableSO] ResetToDefault: Reset {fieldName} to {val}, {val.GetType()}");
                    EventManager.TriggerEvent($"UpdateGameParameter:{fieldName}", val);
                }
                else
                    Debug.LogWarning($"[BinarySerializableSO] ResetToDefault: No default value found for {fieldName}");
            }
        }
    }
}


public class DefaultValueAttribute : Attribute
{
    public object Value { get; }

    public DefaultValueAttribute(object value)
    {
        Value = value;
    }

    public object GetDefaultValue()
    {
        return Value;
    }
}