using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class ScavengerWindow : MonoBehaviour
    {
        [Header("Alive References")]
        [SerializeField] private GameObject alivePanel;
        [SerializeField] private TextMeshProUGUI scavengerName;
        
        [SerializeField] private Image healthBar;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private TextMeshProUGUI scavengerDistance;
        
        [Header("Dead References")]
        [SerializeField] private GameObject deadPanel;

        private void OnEnable()
        {
            EventManager.AddListener("OnScavengerStateChange", OnScavengerStateChange);
            EventManager.AddListener("OnScavengerHealthChange", OnScavengerHealthChange);
            //EventManager.AddListener("OnScavengerNameChange", OnScavengerNameChange);
            EventManager.AddListener("OnScavengerDistanceChange", OnScavengerDistanceChange);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener("OnScavengerStateChange", OnScavengerStateChange);
            EventManager.RemoveListener("OnScavengerHealthChange", OnScavengerHealthChange);
            //EventManager.RemoveListener("OnScavengerNameChange", OnScavengerNameChange);
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
            if (arg0 is float health)
                healthBar.fillAmount = health;
            else
                return;

            if (health <= 0)
            {
                alivePanel.SetActive(false);
                deadPanel.SetActive(true);
            }
            else
            {
                alivePanel.SetActive(true);
                deadPanel.SetActive(false);
            }
        }

        private void OnScavengerStateChange(object arg0)
        {
            actionText.text = "Status: " + arg0;
        }
    }
}