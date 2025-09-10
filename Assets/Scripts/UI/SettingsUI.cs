using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private bool closeOnStart = true;
        [Header("Prefabs")] [SerializeField] private GameObject menuButtonPrefab;

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
            AddListener(menuCategory);
        }

        private void AddListener(SettingsMenuCategory menuCategory)
        {
            onResetButton.AddListener((s) => menuCategory.ResetSettings(s));
        }

        public void ResetButton()
        {
            onResetButton.Invoke(this);
        }


        private void Start()
        {
            onMenuCategoryGenerated.Invoke(this);
            if (closeOnStart)
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

        public void Logout()
        {
            AuthenticationService.Instance.SignOut(true);
            SceneManager.LoadScene("MainMenu");
        }

        public void ReloadMission()
        {
            SceneHandler.ReloadScene();
        }
}
}