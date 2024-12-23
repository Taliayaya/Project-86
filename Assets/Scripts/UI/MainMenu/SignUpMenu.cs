using System;
using System.Net.Mail;
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

        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;

        [SerializeField] private UnityEvent onRegisterSuccess;

        async Task SignUpWithUsernamePasswordAsync(string username, string password)
        {
            try
            {
                var res = await NetworkManager.AuthClient.CreateUserWithEmailAndPasswordAsync(username, password);
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
            Debug.Log("Register button clicked.");
            registerButton.interactable = false;
            try
            {

                if (string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(passwordInput.text) ||
                    string.IsNullOrEmpty(emailInput.text))
                {
                    feedbackText.text = "Personal Name, email and password cannot be empty.";
                    feedbackText.color = errorColor;
                }
                else
                {
                    MailAddress mailAddress = new MailAddress(emailInput.text);
                    if (mailAddress.Address != emailInput.text)
                    {
                        feedbackText.text = "Email is not valid.";
                        feedbackText.color = errorColor;
                    }
                    else
                    {
#if ALLOW_SIGNUP
                        feedbackText.text = "";
                        await NetworkManager.Instance.CreateUserWithEmailAndPasswordAsync(emailInput.text,
                            passwordInput.text, usernameInput.text);
#else
                        feedbackText.text = "Sign up is disabled.";
                        feedbackText.color = errorColor;
#endif
                    }
                }
            }
            catch (FormatException)
            {
                feedbackText.text = "Email is not valid.";
                feedbackText.color = errorColor;
            }
            finally
            {
                registerButton.interactable = true;
            }
        }
    }
}