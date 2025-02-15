using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace UI.HUD
{
    public struct HUDWarningData
    {
        public string main;
        public string sub;
        public float duration;
    }
    public class HUDWarningTrigger : MonoBehaviour
    {
        [SerializeField] private Transform warningPanel;
        [SerializeField] private TMP_Text mainText;
        [SerializeField] private TMP_Text subText;
        
        private void Awake()
        {
            warningPanel.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            EventManager.AddListener(Constants.TypedEvents.ShowHUDWarning, OnShowHUDWarning);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.TypedEvents.ShowHUDWarning, OnShowHUDWarning);
        }

        private void OnShowHUDWarning(object data)
        {
            if (data is HUDWarningData hudWarningData)
            {
                StartCoroutine(ShowWarning(hudWarningData.main, hudWarningData.sub, hudWarningData.duration));
            }
        }

        IEnumerator ShowWarning(string main, string sub, float duration)
        {
            mainText.text = main;
            subText.text = sub;
            warningPanel.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            warningPanel.gameObject.SetActive(false);
        }
    }
}