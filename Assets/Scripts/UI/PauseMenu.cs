using System;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private SceneData mainMenuSceneData;

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
            GameManager.Instance.Pause(false);
            SceneHandler.LoadScene(mainMenuSceneData);
            return;

        }
    }
}