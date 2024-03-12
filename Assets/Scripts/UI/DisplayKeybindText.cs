using System;
using ScriptableObjects.Keybinds;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    public class DisplayKeybindText : MonoBehaviour
    {
        [SerializeField] private Keybind keybind;
        [SerializeField] private TMP_Text textInput;

        private void Start()
        {
            textInput.text = keybind.KeyValue;
        }

        private void OnEnable()
        {
            EventManager.AddListener(Constants.TypedEvents.RebindKey + keybind.Name, OnKeybindChanged);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.TypedEvents.RebindKey + keybind.Name, OnKeybindChanged);
        }

        private void OnKeybindChanged(object arg0)
        {
            textInput.text = keybind.KeyValue;
        }
    }
}