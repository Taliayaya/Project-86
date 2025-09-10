using Networking.Widgets.Session.Session;
using Unity.Multiplayer.Widgets;

namespace Networking
{
   using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class ConnectionManager : Singleton<MonoBehaviour>
{
   private string _profileName;
   private string _sessionName = "Test";
   private int _maxPlayers = 10;
   private ConnectionState _state = ConnectionState.Disconnected;
   private ISession _session;
   private NetworkManager m_NetworkManager;
   [SerializeField] private bool autoConnect = true;

   private enum ConnectionState
   {
       Disconnected,
       Connecting,
       Connected,
   }

    private async void Awake()
    {
        m_NetworkManager = FindAnyObjectByType<NetworkManager>();
        m_NetworkManager.OnClientConnectedCallback += OnClientConnectedCallback;
        m_NetworkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
        _profileName = $"Player-{UnityEngine.Random.Range(0, 1000)}";
        await UnityServices.InitializeAsync();
        
    }

    private async void Start()
    {
        // if (autoConnect)
        // {
        //     await CreateOrJoinSessionAsync();
        // }
    }

    private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
    {
        if (m_NetworkManager.LocalClient.IsSessionOwner)
        {
            Debug.Log($"Client-{m_NetworkManager.LocalClientId} is the session owner!");
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (m_NetworkManager.LocalClientId == clientId)
        {
            Debug.Log($"Client-{clientId} is connected and can spawn {nameof(NetworkObject)}s.");
        }
    }

    #if UNITY_EDITOR
   private void OnGUI()
   {
       if (_state == ConnectionState.Connected)
           return;

       GUI.enabled = _state != ConnectionState.Connecting;

       using (new GUILayout.HorizontalScope(GUILayout.Width(250)))
       {
           GUILayout.Label("Profile Name", GUILayout.Width(100));
           _profileName = GUILayout.TextField(_profileName);
       }

       using (new GUILayout.HorizontalScope(GUILayout.Width(250)))
       {
           GUILayout.Label("Session Name", GUILayout.Width(100));
           _sessionName = GUILayout.TextField(_sessionName);
       }

       GUI.enabled = GUI.enabled && !string.IsNullOrEmpty(_profileName) && !string.IsNullOrEmpty(_sessionName);

       if (GUILayout.Button("Create or Join Session"))
       {
           CreateOrJoinSessionAsync();
       }
   }
   #endif

   protected override void OnDestroy()
   {
       base.OnDestroy();
       _session?.LeaveAsync();
   }

   private async Task CreateOrJoinSessionAsync()
   {
       _state = ConnectionState.Connecting;

       try
       {
           Debug.Log($"Switching profile to {_profileName}");
           AuthenticationService.Instance.SignOut(true);
           AuthenticationService.Instance.SwitchProfile(_profileName);
           if (!AuthenticationService.Instance.IsSignedIn)
               await AuthenticationService.Instance.SignInAnonymouslyAsync();


           var options = new SessionOptions()
           {
               Name = _sessionName,
               MaxPlayers = _maxPlayers
           }.WithDistributedAuthorityNetwork();

           _session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(_sessionName, options);
#if UNITY_EDITOR
           
           SessionManager.Instance.ActiveSession = _session;
#endif
           Debug.Log($"Connected to session {_session.Name} with id {_session.Id}");

           _state = ConnectionState.Connected;
       }
       catch (Exception e)
       {
           _state = ConnectionState.Disconnected;
           Debug.LogException(e);
       }
   }
} 
}