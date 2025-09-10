using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class LoadingScene : MonoBehaviour
    {
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Button playButton;

        private void Start()
        {
            EventManager.AddListener("DisplayLoadingScreen", DisplayLoadingScreen);
            Debug.Log("[LoadingScene] Start");
        }

        private void OnEnable()
        {
            Debug.Log("[LoadingScene] OnEnable");
            EventManager.RemoveListener("DisplayLoadingScreen", DisplayLoadingScreen);
            EventManager.AddListener("DisplayLoadingScreen", DisplayLoadingScreen);
        }

        public void OnDisable()
        {
            Debug.Log("[LoadingScene] OnDisable");
            EventManager.RemoveListener("DisplayLoadingScreen", DisplayLoadingScreen);
        }

        private void DisplayLoadingScreen(object arg0)
        {
            Debug.Log("DisplayLoadingScreen " + arg0);
            if (arg0 is bool show)
                loadingScreen.SetActive(show);
        }

    }
}
