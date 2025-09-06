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
                gameObject.SetActive(IsVisible);
            }
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
        }
        
        private void OnToggleHealthBar(object arg0)
        {
            gameObject.SetActive((bool) arg0);
        }
    }
}
