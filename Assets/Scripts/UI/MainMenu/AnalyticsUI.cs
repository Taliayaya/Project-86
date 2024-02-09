using System;
using ScriptableObjects.GameParameters;
using UnityEngine;

namespace UI.MainMenu
{
    public class AnalyticsUI : MonoBehaviour
    {
        [SerializeField] private GameObject analyticsPanel;
        private void Awake()
        {
            analyticsPanel.SetActive(false);
            
            
        }

        private void Start()
        {
            if (AnalyticsManager.HasInstance && AnalyticsManager.Agreement == DataCollectionParameters.DataCollectionAgreement.NotAsked)
            {
                OpenAnalyticsPanel();
            }
        }

        private void OnEnable()
        {
            EventManager.AddListener(Constants.Events.OnAskDataCollectionAgreement, OpenAnalyticsPanel);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.Events.OnAskDataCollectionAgreement, OpenAnalyticsPanel);
        }

        public void OpenAnalyticsPanel()
        {
            analyticsPanel.SetActive(true);
        }
        
        public void CloseAnalyticsPanel()
        {
            analyticsPanel.SetActive(false);
        }
        
        public void OnAgree()
        {
            EventManager.TriggerEvent("UpdateGameParameter:agreement", DataCollectionParameters.DataCollectionAgreement.Agreed);
            CloseAnalyticsPanel();
        }
        
        public void OnDisagree()
        {
            EventManager.TriggerEvent("UpdateGameParameter:agreement", DataCollectionParameters.DataCollectionAgreement.Disagreed);
            CloseAnalyticsPanel();
        }
    }
}