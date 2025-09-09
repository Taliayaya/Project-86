using System;
using System.Collections;
using Networking.Widgets.Session.Session;
using ScriptableObjects;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
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
            StartCoroutine(QuitSession());
        }
        
        IEnumerator QuitSession()
        {
            WindowManager.CloseAll();
            Debug.Log("IsSignedIn: " + AuthenticationService.Instance.IsSignedIn);
            var task = SessionManager.Instance.LeaveSession();
            yield return new WaitUntil(() => task.IsCompleted);
            Debug.Log("IsSignedIn: " + AuthenticationService.Instance.IsSignedIn);
            GameManager.Instance.Pause(false);
            // NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId, "Return to main menu");
            SceneManager.LoadScene(mainMenuSceneData.SceneName);
        }
    }
}