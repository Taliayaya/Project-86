using System;
using System.Collections.Generic;
using Cinemachine;
using Firebase.Auth;
using Networking;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public class MainMenuManager : Singleton<MainMenuManager>
    {
        [SerializeField] private CinemachineVirtualCamera mainMenuCamera;
        [SerializeField] private CinemachineVirtualCamera gameModeCamera;
        [SerializeField] private CinemachineVirtualCamera settingsCamera;
        [SerializeField] private Button toMainLeftButton;
        [SerializeField] private Button toMainRightButton;
        

        [SerializeField] private PersonalMarksMenu personalMarksMenu;
        
        [SerializeField] private List<TMP_Text> multiplayerTexts = new List<TMP_Text>();
        
        private CinemachineVirtualCamera _currentCamera;

        protected override void OnAwake()
        {
            base.OnAwake();
            _currentCamera = mainMenuCamera;
            gameModeCamera.enabled = false;
            settingsCamera.enabled = false;
        }

        private void OnEnable()
        {
            EventManager.AddListener(Constants.TypedEvents.Auth.OnLoginSuccess, OnAuthentication);
            EventManager.AddListener(Constants.TypedEvents.Auth.OnSignUpSuccess, OnAuthentication);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.TypedEvents.Auth.OnSignUpSuccess, OnAuthentication);
            EventManager.RemoveListener(Constants.TypedEvents.Auth.OnLoginSuccess, OnAuthentication);
        }

        private void OnAuthentication(object arg0)
        {
            if (arg0 is not User user)
                return;
            if (AuthManager.Instance.IsSignedIn)
            {
                foreach (var tmpText in multiplayerTexts)
                {
                    tmpText.text = AuthManager.Instance.PlayerName;
                }
            }
            else
            {
                foreach (var tmpText in multiplayerTexts)
                {
                    tmpText.text = "Handler";
                }
            }
        }

        public void ToGameMode()
        {
            toMainLeftButton.gameObject.SetActive(false);
            toMainRightButton.gameObject.SetActive(true);
            _currentCamera.enabled = false;
            gameModeCamera.enabled = true;
            _currentCamera = gameModeCamera;
        }
        
        public void ToSettingsMode()
        {
            toMainLeftButton.gameObject.SetActive(true);
            toMainRightButton.gameObject.SetActive(false);
            _currentCamera.enabled = false;
            settingsCamera.enabled = true;
            _currentCamera = settingsCamera;
        }
        
        public void ToMainMenu()
        {
            toMainLeftButton.gameObject.SetActive(false);
            toMainRightButton.gameObject.SetActive(false);
            _currentCamera.enabled = false;
            mainMenuCamera.enabled = true;
            _currentCamera = mainMenuCamera;
        }

        public void OpenCosmetic()
        {
            WindowManager.Open(personalMarksMenu.Open, personalMarksMenu.Close, true);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}