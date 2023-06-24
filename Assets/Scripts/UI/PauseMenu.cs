using System;
using UnityEngine;

namespace UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenuPanel;

        private void OnEnable()
        {
            EventManager.AddListener("OnPause", OnPause);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener("OnPause", OnPause);
        }

        private void Open()
        {
            pauseMenuPanel.SetActive(true);
        }

        private void Close()
        {
            pauseMenuPanel.SetActive(false);
        }
        
        public void OnPause()
        {
            WindowManager.Open(Open, Close);
        }
        

        public void Resume()
        {
            EventManager.TriggerEvent("OnResume");
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