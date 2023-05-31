using System.IO;
using UnityEngine;

namespace ScriptableObjects
{
    /// <summary>
    /// JsonSerializableSO is a ScriptableObject that can be saved and loaded from a json file.
    /// It is used to save data from the game in a persistent way, for usage between game sessions.
    /// 
    /// </summary>
    /// <remarks>Scriptable Objects are saved by default in the Editor, this class must be inherited only to keep user changes in a session (such as settings)</remarks>
    public class JsonSerializableSO : ScriptableObject
    {
        #if UNITY_EDITOR
        private const string ScriptableObjectsDataDirectory = "ScriptableObjects_dev";
        #else
        private const string ScriptableObjectsDataDirectory = "ScriptableObjects";
        #endif

        /// <summary>
        /// Saves the ScriptableObject to a file in the persistent data path.
        /// It writes all the serialized fields to a json file.
        /// </summary>
        /// <param name="filename">The name of the output file</param>
        public void SaveToFile(string filename)
        {
             string dirPath = Path.Combine(Application.persistentDataPath, ScriptableObjectsDataDirectory);
             string filePath = Path.Combine(dirPath, $"{filename}.json");
            
             if (!Directory.Exists(dirPath))
                 Directory.CreateDirectory(dirPath);
             
             if (!File.Exists(filePath))
                 File.Create(filePath).Dispose();

             var jsonContent = JsonUtility.ToJson(this);
             File.WriteAllText(filePath, jsonContent);
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
        /// It writes all the serialized fields from a json file.
        /// </summary>
        /// <remarks>Any serialized field will be overriden by the json content if it exists. It may cause unexpected issues.</remarks>
        /// <param name="filename">The input file name</param>
        public void LoadFromFile(string filename)
        {
            string dirPath = Path.Combine(Application.persistentDataPath, ScriptableObjectsDataDirectory);
            string filePath = Path.Combine(dirPath, $"{filename}.json");

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[JsonSerializableSO] LoadFromFile: File \"{filePath}\" not found! Getting default values.", this);
                return;
            }
            
            var jsonContent = File.ReadAllText(filePath);
            JsonUtility.FromJsonOverwrite(jsonContent, this);
        }
        
        /// <summary>
        /// Load the file with the same name as the ScriptableObject.
        /// See <see cref="LoadFromFile(string)"/> for more information.
        /// </summary>
        public void LoadFromFile()
        {
            LoadFromFile(name);   
        }
    }
}