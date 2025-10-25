using Networking.Widgets.Core.Base;
using Networking.Widgets.Session.Session;
using Networking.Widgets.Session.Widgets.Base;
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

public class ConnectionManager : EnterSessionBase
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
        EventManager.AddListener(Constants.TypedEvents.Session.SessionJoined, OnSessionJoinedEvent);
        await UnityServices.InitializeAsync();

    }

    private void OnSessionJoinedEvent(object arg0)
    {
        Debug.Log("Session Joined");
        StartCoroutine(SceneHandler.SpawnPlayers());
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

#if UNITY_EDITOR
            EnterSession();
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
    
    [SerializeField]
    bool m_AutoCreateSession = true;

    protected override EnterSessionData GetSessionData()
    {
        return new EnterSessionData
        {
            SessionAction = SessionAction.Create,
            SessionName = _profileName,
            WidgetConfiguration = WidgetConfiguration,
            AdditionalOptions = new AdditionalOptions
            {
                AutoCreateSession = m_AutoCreateSession,
            }
        };
    }
}
}