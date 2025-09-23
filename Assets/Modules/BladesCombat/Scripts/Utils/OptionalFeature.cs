using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace BladesCombat.Utils
{
    public class OptionalFeature<T>
    {
        private List<T> _components;
        private bool _hasComponents;
        private Action<T> _onInvoke;
        public OptionalFeature(List<T> components, Action<T> onInvoke)
        {
            _components = components;
            _hasComponents = components != null && components.Any();
            _onInvoke = onInvoke;
        }

        public OptionalFeature(List<T> components)
        {
            _components = components;
            _hasComponents = components != null && components.Any();
            _onInvoke = obj => { Debug.Log($"Processing {obj.GetType().Name} is skipped. Make sure on invoke is set"); };
        }

        public void Invoke()
        {
            if (!_hasComponents) return;
            foreach (T component in _components)
            {
                _onInvoke(component);
            }
        }

        public void Invoke(Action<T> onInvoke)
        {
            if (!_hasComponents) return;
            foreach (T component in _components)
            {
                onInvoke(component);
            }
        }
    }
}