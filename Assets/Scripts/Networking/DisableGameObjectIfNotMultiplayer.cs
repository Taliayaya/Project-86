using UnityEngine;

namespace Networking
{
    public class DisableGameObjectIfNotMultiplayer : MonoBehaviour
    {
        private void Awake()
        {
            EventManager.AddListener(Constants.TypedEvents.Auth.OnLoginSuccess, OnAuthStatusSuccess);
            EventManager.AddListener(Constants.TypedEvents.Auth.OnSignUpSuccess, OnAuthStatusSuccess);
        }


        private void OnDestroy()
        {
            EventManager.RemoveListener(Constants.TypedEvents.Auth.OnLoginSuccess, OnAuthStatusSuccess);
            EventManager.RemoveListener(Constants.TypedEvents.Auth.OnSignUpSuccess, OnAuthStatusSuccess);
        }

        private void OnAuthStatusSuccess(object arg0)
        {
            UpdateGameObjectState();
        }

        private void UpdateGameObjectState()
        {
            if (AuthManager.Instance.IsSignedIn)
            {
                gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}