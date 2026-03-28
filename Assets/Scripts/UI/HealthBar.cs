using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private bool toggleable = true;
        private Transform _mainCamera;

        private static bool _isVisible = true;
        public static bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                EventManager.TriggerEvent(Constants.TypedEvents.OnToggleHealthBar, _isVisible);
            }
        }

        private void Awake()
        {
            if (toggleable)
            {
                EventManager.AddListener(Constants.TypedEvents.OnToggleHealthBar, OnToggleHealthBar);  
            }
            gameObject.SetActive(false);
        }
        
        private void OnDestroy()
        {
            EventManager.RemoveListener(Constants.TypedEvents.OnToggleHealthBar, OnToggleHealthBar);    
        }

        public void OnHealthChange(float health, float maxHealth)
        {
            if (healthText)
                healthText.text = $"{health:F0} / {maxHealth}"; 
            healthSlider.value = health / maxHealth;
            
            // enable health bar if they took damage
            if (!Mathf.Approximately(health, maxHealth))
                gameObject.SetActive(IsVisible);
        }
        
        private void OnToggleHealthBar(object arg0)
        {
            bool toggle = (bool) arg0;
            // if hp bar is full, hide
            if (Mathf.Approximately(healthSlider.value, healthSlider.maxValue))
                toggle = false;

            gameObject.SetActive(toggle);
        }
    }
}
