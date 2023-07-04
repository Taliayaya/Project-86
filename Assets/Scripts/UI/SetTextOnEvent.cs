using System;
using TMPro;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class SetTextOnEvent : MonoBehaviour
    {
        private TextMeshProUGUI _textMeshProUGUI;

        private void Awake()
        {
            _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        }

        public void SetText(string text)
        {
            if (!_textMeshProUGUI) _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            _textMeshProUGUI.text = text;
        }
        
        public void SetText(float text)
        {
            if (!_textMeshProUGUI) _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
            _textMeshProUGUI.text = text.ToString("F0");
        }
        
    }
}