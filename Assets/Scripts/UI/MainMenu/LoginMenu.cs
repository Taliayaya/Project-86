using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using Firebase.Auth;
using Networking;
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

        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;

        [SerializeField] private UnityEvent onLoginSuccess;

        async Task SignInWithUsernamePasswordAsync(string username, string password)
        {
            try
            {
                await AuthManager.Instance.LoginUserWithEmailAndPasswordAsync(username, password);
                feedbackText.text = "Log in successful.";
                feedbackText.color = successColor;
                onLoginSuccess.Invoke();
            }
            catch (FirebaseAuthException ex)
            {
                Debug.LogException(ex);
                feedbackText.color = errorColor;
                feedbackText.text = AuthManager.GetAuthErrorReason(ex);
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
            try
            {
                loginButton.interactable = false;
                if (string.IsNullOrEmpty(emailInput.text) || string.IsNullOrEmpty(passwordInput.text))
                {
                    feedbackText.text = "Email and password are required.";
                    feedbackText.color = errorColor;
                }
                else if (new MailAddress(emailInput.text).Address != emailInput.text)
                {
                    feedbackText.text = "Invalid email address.";
                    feedbackText.color = errorColor;
                }
                else
                {
                    feedbackText.text = "";
                    await SignInWithUsernamePasswordAsync(emailInput.text, passwordInput.text);
                }
            }
            catch (FormatException)
            {
                feedbackText.text = "Invalid email address.";
                feedbackText.color = errorColor;
            }
            finally
            {
                loginButton.interactable = true;
            }
        }
        
        [ContextMenu("Auto Login")]
        public async void AutoLogin()
        {
            bool sessionActive = await AuthManager.Instance.OldSessionStillActive();
            Debug.Log("AutoLogin " + sessionActive);
            if (sessionActive)
            {
                onLoginSuccess.Invoke();
            }
        }
    }
}