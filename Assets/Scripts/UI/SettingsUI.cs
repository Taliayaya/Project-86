using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Prefabs")] 
        [SerializeField] private GameObject menuButtonPrefab;
        
        [Header("References")] [SerializeField]
        private GameObject gameSettingsPanel;
        public TMP_Text menuContentName;
        public Transform contentParent;
        public Transform menusParent;
        
        public UnityEvent<SettingsUI> onMenuCategoryGenerated = new UnityEvent<SettingsUI>();
        public UnityEvent<SettingsUI> onResetButton = new UnityEvent<SettingsUI>();


        public void GenerateMenuCategory(SettingsMenuCategory menuCategory)
        {
            ClearContent();
            menuContentName.text = menuCategory.MenuContentName;
            menuCategory.GenerateContent(contentParent);
            onResetButton.RemoveAllListeners();
            onResetButton.AddListener((s) => menuCategory.ResetSettings());
        }

        public void ResetButton()
        {
            onResetButton.Invoke(this);
        }


        private void Start()
        {
            onMenuCategoryGenerated.Invoke(this);
            CloseGameSettingsPanel();
        }

        public void OpenGameSettingsPanel()
        {
            WindowManager.Open(() => gameSettingsPanel.transform.GetChild(0).gameObject.SetActive(true),
                CloseGameSettingsPanel);
        }

        // Use Window.Close to close the window instead of this method
        private void CloseGameSettingsPanel()
        {
            DataHandler.SaveGameData(); // Saving the settings
            gameSettingsPanel.transform.GetChild(0).gameObject.SetActive(false);
        }
        
        public void ClearContent()
        {
            foreach (Transform c in contentParent)
            {
                Destroy(c.gameObject);
            }
        }
    }
}