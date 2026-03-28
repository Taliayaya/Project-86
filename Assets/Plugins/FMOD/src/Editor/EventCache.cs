using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FMODUnity
{
    public class EventCache : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        public List<EditorBankRef> EditorBanks;
        [SerializeField]
        public List<EditorEventRef> EditorEvents;
        public Dictionary<string, int> EditorEventsDict;
        [SerializeField]
        public List<EditorParamRef> EditorParameters;
        [SerializeField]
        public List<EditorBankRef> MasterBanks;
        [SerializeField]
        public List<EditorBankRef> StringsBanks;
        [SerializeField]
        public int cacheVersion;
        [SerializeField]
        private Int64 cacheTime;
        [SerializeField]
        private List<DictionaryEntry> SerializableEventsDict;
        [Serializable]
        private struct DictionaryEntry
        {
            [SerializeField]
            public string key;
            [SerializeField]
            public int index;
        }

        public DateTime CacheTime
        {
            get { return new DateTime(cacheTime); }
            set { cacheTime = value.Ticks; }
        }

        public EventCache()
        {
            EditorBanks = new List<EditorBankRef>();
            EditorEvents = new List<EditorEventRef>();
            SerializableEventsDict = new List<DictionaryEntry>();
            EditorEventsDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            EditorParameters = new List<EditorParamRef>();
            MasterBanks = new List<EditorBankRef>();
            StringsBanks = new List<EditorBankRef>();
            cacheTime = 0;
        }

        public void OnBeforeSerialize()
        {
            if (SerializableEventsDict.Count == 0)
            {
                SerializableEventsDict = EditorEventsDict.Select(item => new DictionaryEntry { key = item.Key, index = item.Value}).ToList();
            }
        }

        public void OnAfterDeserialize()
        {
            if (SerializableEventsDict.Count > 0)
            {
                SerializableEventsDict.ForEach((item) =>
                {
                    EditorEventsDict.Add(item.key, item.index);
                });
                SerializableEventsDict.Clear();
            }
        }

        public void BuildDictionary()
        {
            EditorEventsDict.Clear();
            int index = 0;

            EditorEvents.ForEach((eventRef) => {
                if (!EditorEventsDict.ContainsKey(eventRef.Path))
                {
                    EditorEventsDict.Add(eventRef.Path, index);
                }
                index++;
            });
        }
    }
}
