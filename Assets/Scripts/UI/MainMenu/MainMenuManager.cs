using Cinemachine;
using UnityEngine;
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
        
        private CinemachineVirtualCamera _currentCamera;

        protected override void OnAwake()
        {
            base.OnAwake();
            _currentCamera = mainMenuCamera;
            gameModeCamera.enabled = false;
            settingsCamera.enabled = false;
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