using System;
using System.Text;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Random = UnityEngine.Random;
using Task = System.Threading.Tasks.Task;

namespace Networking
{
    public class AuthManager : Singleton<AuthManager>
    {
        [SerializeField] private string providerName = "oidc-firebase";
        public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;

        public FirebaseAuthClient Client { get; private set; }
        public static FirebaseAuthClient AuthClient => Instance.Client;
        
        public string PlayerName => AuthenticationService.Instance.PlayerName;

        protected override void OnAwake()
        {
            base.OnAwake();
            UnityServices.InitializeAsync();

            Debug.Log("[NetworkManager] Awake");
            var config = new FirebaseAuthConfig()
            {
                ApiKey = "AIzaSyCW-PJ1X-hqYt_od8xa9fptDgrpb9RcqIQ",
                AuthDomain = "project-86-unity.firebaseapp.com",
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider()
                },
                UserRepository = new FileUserRepository("Project-86-Community/Project-86")
            };
            Client = new FirebaseAuthClient(config);
            UnityServices.Initialized += async () =>
            {
                Debug.Log("[NetworkManager] UnityServices Initialized");
                await OldSessionStillActive();
            };
        }

        string DecodeFirebaseIdToken(string idToken)
        {
            var parts = idToken.Split('.');
            if (parts.Length != 3) return "Invalid token format";

            var payload = parts[1];
            var decoded =
                Encoding.UTF8.GetString(
                    Convert.FromBase64String(payload + new string('=', (4 - payload.Length % 4) % 4)));

            return decoded;
        }

        public async Task CreateUserWithEmailAndPasswordAsync(string email, string password, string username)
        {
            var res = await AuthClient.CreateUserWithEmailAndPasswordAsync(email, password, username);
            Debug.LogError("aud: " + DecodeFirebaseIdToken(res.User.Credential.IdToken));
            await AuthenticationService.Instance.SignInWithOpenIdConnectAsync(providerName,
                res.User.Credential.IdToken,
                new SignInOptions() { CreateAccount = true });
            if (AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(username);
                EventManager.TriggerEvent(Constants.TypedEvents.Auth.OnSignUpSuccess, AuthClient.User);
                Debug.Log("SignUp is successful.");
                Debug.Log("name=" + AuthenticationService.Instance.PlayerName);
            }
            else
            {
                Debug.LogError("SignUp failed.");
            }
        }

        public async Task LoginUserWithEmailAndPasswordAsync(string email, string password)
        {
            var res = await AuthClient.SignInWithEmailAndPasswordAsync(email, password);
            await AuthenticationService.Instance.SignInWithOpenIdConnectAsync(providerName,
                res.User.Credential.IdToken,
                new SignInOptions() { CreateAccount = false });
            if (AuthenticationService.Instance.IsSignedIn)
            {
                EventManager.TriggerEvent(Constants.TypedEvents.Auth.OnLoginSuccess, AuthClient.User);
            }
            else
            {
                Debug.LogError("Login failed.");
            }
        }

        [ContextMenu("RefreshSession")]
        public async Task<bool> OldSessionStillActive()
        {
            Debug.Log("Checking session " + AuthenticationService.Instance.IsSignedIn);
#if UNITY_EDITOR
            AuthenticationService.Instance.SignOut();
            string profile = "Test" + Random.Range(0, 1000);
            AuthenticationService.Instance.SwitchProfile(profile);
            await AuthenticationService.Instance.SignInAnonymouslyAsync(new SignInOptions() { CreateAccount = true });
            await AuthenticationService.Instance.UpdatePlayerNameAsync(profile);
            return true;
#endif
            Debug.Log("Shall not be there: " + AuthenticationService.Instance.IsSignedIn);
            if (AuthenticationService.Instance.IsSignedIn)
            {
                EventManager.TriggerEvent(Constants.TypedEvents.Auth.OnLoginSuccess, AuthClient.User);
                return true;
            }
            else if (AuthClient.User != null)
            {
                var token = await AuthClient.User.GetIdTokenAsync(true);
                await AuthenticationService.Instance.SignInWithOpenIdConnectAsync(providerName,
                    token,
                    new SignInOptions() { CreateAccount = false });
                if (!AuthenticationService.Instance.IsSignedIn) return false;
                EventManager.TriggerEvent(Constants.TypedEvents.Auth.OnLoginSuccess, AuthClient.User);
                return true;
            }

            return false;
        }


        public static string GetAuthErrorReason(FirebaseAuthException ex)
        {
            if (ex is FirebaseAuthHttpException httpEx)
            {
                if (httpEx.ResponseData.Contains("TOO_MANY_ATTEMPTS_TRY_LATER"))
                {
                    return "Too many attempts. Try again later.";
                }
            }

            return ex.Reason switch
            {
                // server related errors
                AuthErrorReason.SystemError => "A system error occurred.",
                AuthErrorReason.InvalidApiKey or AuthErrorReason.InvalidProviderID or AuthErrorReason.InvalidIdentifier
                    or AuthErrorReason.MissingRequestURI or AuthErrorReason.Undefined
                    or AuthErrorReason.MissingRequestType
                    or AuthErrorReason.InvalidIDToken =>
                    "The API configuration has an error. Contact support. Reason: " +
                    ex.Reason,

                // user related errors
                AuthErrorReason.UserDisabled => "This account has been disabled.",
                AuthErrorReason.LoginCredentialsTooOld => "The login credentials have expired.",
                AuthErrorReason.TooManyAttemptsTryLater => "Too many attempts. Try again later.",
                AuthErrorReason.ResetPasswordExceedLimit => "Password reset limit exceeded.",
                AuthErrorReason.WeakPassword => "Password is too weak. It must be at least 6 characters long.",
                AuthErrorReason.AlreadyLinked => "This account is already linked.",
                AuthErrorReason.EmailExists => "This email is already in use.",
                _ => "Email or password is incorrect.",
            };
        }
    }
}