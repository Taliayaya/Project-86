using System;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using Gameplay.Quests.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

public class NetworkManager : Singleton<NetworkManager>
{
    [SerializeField] private string providerName = "oidc-firebase";
    public bool IsSignedIn => AuthenticationService.Instance.IsSignedIn;
    
    public FirebaseAuthClient Client { get; private set; }
    public static FirebaseAuthClient AuthClient => Instance.Client;

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
    }

    public async Task CreateUserWithEmailAndPasswordAsync(string email, string password, string username)
    {
        try
        {
            var res = await AuthClient.CreateUserWithEmailAndPasswordAsync(email, password, username);
            await AuthenticationService.Instance.SignInWithOpenIdConnectAsync(providerName,
                res.User.Credential.IdToken,
                new SignInOptions() { CreateAccount = true });
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }
}