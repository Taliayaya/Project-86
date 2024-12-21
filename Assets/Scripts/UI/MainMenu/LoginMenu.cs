using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public class LoginMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Color successColor;
        [SerializeField] private Color errorColor;

        [SerializeField] private Button loginButton;

        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        
        [SerializeField] private UnityEvent onLoginSuccess;

        async Task SignInWithUsernamePasswordAsync(string username, string password)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password);
                feedbackText.text = "Log in successful.";
                feedbackText.color = successColor;
                onLoginSuccess.Invoke();
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                feedbackText.text = ex.Message;
                feedbackText.color = errorColor;
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                feedbackText.text = ex.Message;
                feedbackText.color = errorColor;
                Debug.LogException(ex);
            }
        }

        public async void OnLoginButtonClicked()
        {
            loginButton.interactable = false;
            await SignInWithUsernamePasswordAsync(usernameInput.text, passwordInput.text);
            loginButton.interactable = true;
        }
    }
}