using System.Collections.Generic;
using Gameplay;
using Networking.Widgets.Core.Base;
using Networking.Widgets.Core.Base.Session;
using Networking.Widgets.Session.Session;
using Networking.Widgets.Session.Widgets.Base;
using Unity.Services.Lobbies;
using UnityEditor;

namespace Networking
{
   using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class ConnectionManager : EnterSessionBase
{
    #if UNITY_EDITOR
    private string _profileName;
    private string _sessionName = "Test";
    private string _joinCode = "";
    private int _maxPlayers = 10;
    private ConnectionState _state = ConnectionState.Disconnected;
    private ISession _session;
    private NetworkManager m_NetworkManager;
    [SerializeField] private bool autoConnect = true;

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public enum ConnectionState
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
        _sessionName = $"Session{DateTime.Now.Minute}";
        EventManager.AddListener(Constants.TypedEvents.Session.SessionJoined, OnSessionJoinedEvent);
        await UnityServices.InitializeAsync();
        // auto create
        var session = SessionManager.Instance;

    }

    private void OnSessionJoinedEvent(object arg0)
    {
        Debug.Log("Session Joined");
        GUIUtility.systemCopyBuffer = SessionManager.Instance.ActiveSession.Code;
        var respawnManager = FindAnyObjectByType<RespawnManager>();
        var playerObject = respawnManager.SpawnPlayer(NetworkManager.Singleton.LocalClientId);
        PlayerManager.PlayerObjects[NetworkManager.Singleton.LocalClientId] = playerObject;
        // StartCoroutine(SceneHandler.SpawnPlayers());
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

        using (new GUILayout.HorizontalScope(GUILayout.Width(250)))
        {
            GUILayout.Label("Join Code", GUILayout.Width(100));
            _joinCode = GUILayout.TextField(_joinCode);
            
        }

        GUI.enabled = GUI.enabled && !string.IsNullOrEmpty(_profileName) && !string.IsNullOrEmpty(_sessionName);

        if (GUILayout.Button("JoinSession"))
        {
            CreateSessionAsync();
        }
    }
#endif

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _session?.LeaveAsync();
    }

    public async Task CreateOrJoinSessionAsync(string sessionName, string profileName)
    {
        if (string.IsNullOrEmpty(profileName) || string.IsNullOrEmpty(sessionName))
        {
            Debug.LogError("Please provide a player and session name, to login.");
            return;
        }

        // Only sign in if not already signed in.
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SwitchProfile(profileName);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Set the session options.
        var options = new SessionOptions()
        {
            PlayerProperties = await GetPlayerProperties(),
            Name = sessionName,
            MaxPlayers = 5
        }.WithDistributedAuthorityNetwork();

        // Join a session if it already exists, or create a new one.
        SessionManager.Instance.ActiveSession =
            await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionName, options);
    }

    private async Task CreateSessionAsync()
    {
        _state = ConnectionState.Connecting;

        try
        {
            Debug.Log($"Switching profile to {_profileName}");
            AuthenticationService.Instance.SignOut(true);
            AuthenticationService.Instance.SwitchProfile(_profileName);
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

#if UNITY_EDITOR
            CreateOrJoinSessionAsync(_sessionName, _profileName);
#endif
            // Debug.Log($"Connected to session {_session.Name} with id {_session.Id}");

            _state = ConnectionState.Connected;
        }
        catch (Exception e)
        {
            _state = ConnectionState.Disconnected;
            Debug.LogException(e);
        }
    }
    
    async Task<Dictionary<string, PlayerProperty>> GetPlayerProperties()
    {
        var playerName = await WidgetDependencies.Instance.AuthenticationService.GetPlayerNameAsync();
        var playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);
        var playerProperties = new Dictionary<string, PlayerProperty> { { SessionConstants.playerNamePropertyKey, playerNameProperty } };
        return playerProperties;
    }
    
    [SerializeField]
    bool m_AutoCreateSession = true;

    protected override EnterSessionData GetSessionData()
    {
        if (_joinCode == String.Empty)
        {
            return new EnterSessionData
            {
                SessionAction = SessionAction.Create,
                SessionName = _sessionName,
                WidgetConfiguration = WidgetConfiguration,
                AdditionalOptions = new AdditionalOptions
                {
                    AutoCreateSession = m_AutoCreateSession,
                }
            };
        }
        else
        {
            return new EnterSessionData
            {
                SessionAction = SessionAction.JoinByCode,
                WidgetConfiguration = WidgetConfiguration,
                JoinCode = _joinCode
            };
        }
    }
#endif
}
}