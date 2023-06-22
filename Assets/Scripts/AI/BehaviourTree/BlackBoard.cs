using System;
using System.Collections.Generic;
using UnityEngine;

namespace AI.BehaviourTree
{
    [Serializable]
    public class BlackBoard
    {
        private Dictionary<string, object> _values = new Dictionary<string, object>();
        
        public void SetValue<T>(string key, T value)
        {
            _values[key] = value;
        }
        public void SetValue(string key, object value)
        {
            _values[key] = value;
        }
        
        public T GetValue<T>(string key)
        {
            return (T)_values[key];
        }
        public object GetValue(string key)
        {
            return _values[key];
        }
        
        public void RemoveValue(string key)
        {
            _values.Remove(key);
        }
        
        public bool HasValue(string key)
        {
            return _values.ContainsKey(key);
        }
        
        public void Clear()
        {
            _values.Clear();
        }
        
        public BlackBoard Clone()
        {
            var clone = new BlackBoard();
            foreach (var pair in _values)
            {
                clone.SetValue(pair.Key, pair.Value);
            }
            return clone;
        }
        
        public object Pop(string key)
        {
            var value = GetValue(key);
            RemoveValue(key);
            return value;
        }
        
        public T Pop<T>(string key)
        {
            var value = GetValue<T>(key);
            RemoveValue(key);
            return value;
        }
            
    }
}
