using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScriptableObjects.Keybinds
{
    [Serializable]
    public struct Keybind
    {
        [TextArea(1, 3)]
        public string description;
        public InputActionReference inputActionReference;
        
        public string Name => inputActionReference.action.name;
        
        public int Count => inputActionReference.action.bindings.Count;
        public string EffectivePath => inputActionReference.action.bindings[0].effectivePath;
    }
    [CreateAssetMenu(fileName = "Keybinds", menuName = "Scriptable Objects/Keybinds" )]
    public class KeybindsSO : ScriptableObject
    {
        public List<Keybind> keybinds;
    }
}