using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class ScavengerWindow : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI scavengerName;
        
        [SerializeField] private Image healthBar;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private TextMeshProUGUI scavengerDistance;

        private void OnEnable()
        {
            EventManager.AddListener("OnScavengerStateChange", OnScavengerStateChange);
            EventManager.AddListener("OnScavengerHealthChange", OnScavengerHealthChange);
            EventManager.AddListener("OnScavengerNameChange", OnScavengerNameChange);
            EventManager.AddListener("OnScavengerDistanceChange", OnScavengerDistanceChange);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener("OnScavengerStateChange", OnScavengerStateChange);
            EventManager.RemoveListener("OnScavengerHealthChange", OnScavengerHealthChange);
            EventManager.RemoveListener("OnScavengerNameChange", OnScavengerNameChange);
            EventManager.RemoveListener("OnScavengerDistanceChange", OnScavengerDistanceChange);
        }

        private void OnScavengerNameChange(object arg0)
        {
            scavengerName.text = arg0.ToString();
        }

        private void OnScavengerDistanceChange(object arg0)
        {
            scavengerDistance.text = ((float)arg0).ToString("F0") + "m";
        }

        private void OnScavengerHealthChange(object arg0)
        {
            healthBar.fillAmount = (float) arg0;
        }

        private void OnScavengerStateChange(object arg0)
        {
            actionText.text = arg0.ToString();
        }
    }
}