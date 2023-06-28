using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public enum ModuleStatus
    {
        Ready,
        Cooldown,
        Disabled,
        Active,
    }
    [Serializable]
    public struct ModuleData
    {
        public string name;
        public float cooldown;
        public ModuleStatus status;
    }
    public class IconCooldown : MonoBehaviour
    {
        [SerializeField] private string eventName;
    
        [Header("References")]
        [SerializeField] private Image cooldownImage;
        [SerializeField] private RawImage iconImage;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private MaskableGraphic readyBorder;

        private void OnEnable()
        {
            EventManager.AddListener(eventName, OnEvent);
        }
    
        private void OnDisable()
        {
            EventManager.RemoveListener(eventName, OnEvent);
        }

        private void OnEvent(object arg0)
        {
            var data = (ModuleData) arg0;
            switch (data.status)
            {
                case ModuleStatus.Ready:
                    EnableIcon();
                    break;
                case ModuleStatus.Cooldown:
                    StartCoroutine(Cooldown(data.cooldown));
                    break;
                case ModuleStatus.Disabled:
                    DisableIcon();
                    break;
                case ModuleStatus.Active:
                    SetActiveIcon();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    
        IEnumerator Cooldown(float cooldownTime)
        {
            float timer = cooldownTime;
            DisableIcon();
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                cooldownImage.fillAmount = timer / cooldownTime;
                cooldownText.text = Mathf.Ceil(timer).ToString("F0");
                yield return null;
            }
            EnableIcon();
        }
    
        private void DisableIcon()
        {
            iconImage.color = Color.gray;
            readyBorder.enabled = false;
        }
    
        private void EnableIcon()
        {
            iconImage.color = Color.white;
            readyBorder.enabled = true;
            cooldownImage.fillAmount = 0;
            cooldownText.text = "";
        }
    
        private void SetActiveIcon()
        {
            iconImage.color = Color.green;
            readyBorder.enabled = false;
        }
    }
}