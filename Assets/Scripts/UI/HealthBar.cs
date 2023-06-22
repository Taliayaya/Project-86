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
        private Transform _mainCamera;

        

        public void OnHealthChange(float health, float maxHealth)
        {
            healthText.text = $"{health} / {maxHealth}"; 
            healthSlider.value = health / maxHealth;
        }
    }
}