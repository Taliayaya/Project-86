using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Button = UnityEngine.UI.Button;

namespace UI.MainMenu
{
    public class SignUpMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Color successColor;
        [SerializeField] private Color errorColor;

        [SerializeField] private Button registerButton;

        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField passwordConfInput;

        [SerializeField] private UnityEvent onRegisterSuccess;

        async Task SignUpWithUsernamePasswordAsync(string username, string password)
        {
            try
            {
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(username, password);
                feedbackText.text = "Account created successfully.";
                feedbackText.color = successColor;
                Debug.Log("SignUp is successful.");
                onRegisterSuccess.Invoke();
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

        public async void OnRegisterButtonClicked()
        {
            registerButton.interactable = false;
            if (passwordInput.text != passwordConfInput.text)
            {
                feedbackText.text = "Passwords do not match.";
                feedbackText.color = errorColor;
            }
            else if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text))
            {
                feedbackText.text = "Username and password cannot be empty.";
                feedbackText.color = errorColor;
            }
            else
            {
#if ALLOW_SIGNUP
                await SignUpWithUsernamePasswordAsync(usernameInput.text, passwordInput.text);
#else
                feedbackText.text = "Sign up is disabled.";
                feedbackText.color = errorColor;
#endif
            }

            registerButton.interactable = true;
        }
    }
}