using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.Widgets.Core.Base;
using Networking.Widgets.Core.Base.Session;
using Networking.Widgets.Core.Base.Widget;
using Networking.Widgets.Core.Configuration;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Networking.Widgets.Session.Session
{
    /// <summary>
    /// The SessionManager handles the lifecycle of a Session.
    ///
    /// When the application is quit the SessionManager will leave the active session.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SessionManager : LazySingleton<SessionManager>
    {
        bool m_Initialized;

        QuerySessionsResults m_SessionQueryResults;

        WidgetEventDispatcher m_WidgetEventDispatcher;
        
        Dictionary<string, string> m_PlayerIdToName = new Dictionary<string, string>();

        ISession m_ActiveSession;

        internal ISession ActiveSession
        {
            get => m_ActiveSession;

#if UNITY_EDITOR
            set
#else
            private set
#endif
            {
                m_PlayerIdToName.Clear();
                if (value != null)
                {
                    m_ActiveSession = value;
                    RegisterSessionEvents();
                    Debug.Log($"Joined Session {m_ActiveSession.Id}");
                    m_WidgetEventDispatcher.OnSessionJoined(m_ActiveSession, EnterSessionData.WidgetConfiguration);
                    EventManager.TriggerEvent("SessionJoined", m_ActiveSession);
                }
                else if (m_ActiveSession != null)
                {
                    m_ActiveSession = null;
                    m_WidgetEventDispatcher.OnSessionLeft();
                }
            }
        }
        
        public void AddPlayerName(string playerId, string playerName)
        {
            m_PlayerIdToName[playerId] = playerName;
        }
        
        public string GetPlayerName(string playerId)
        {
            return m_PlayerIdToName.TryGetValue(playerId, out var playerName) ? playerName : null;
        }

        EnterSessionData EnterSessionData { get; set; }

        async void Start()
        {
            if (!m_Initialized)
            {
                var widgetDependencies = WidgetDependencies.Instance;

                WidgetEventDispatcher.Instance.OnServicesInitializedEvent.AddListener(OnServicesInitialized);
                Debug.Log("Initializing Services...");
                await widgetDependencies.ServiceInitialization.InitializeAsync();
            }
            m_WidgetEventDispatcher = WidgetEventDispatcher.Instance;
        }

        void OnServicesInitialized()
        {
            m_Initialized = true;
        }
        
        internal async Task EnterSession(EnterSessionData enterSessionData)
        {
            try
            {
                if (false && !m_Initialized)
                    throw new InvalidOperationException($"Services are not initialized. If you manually initialize Services please call {nameof(WidgetServiceInitialization.ServicesInitialized)} afterwards.");

                EnterSessionData = enterSessionData;

                if (m_ActiveSession != null)
                {
                    await LeaveSession();
                }

                var playerProperties = await GetPlayerProperties();

                Debug.Log("Joining Session...");
                m_WidgetEventDispatcher.OnSessionJoining();

                var joinSessionOptions = new JoinSessionOptions
                {
                    PlayerProperties = playerProperties
                };

                var sessionOptions = new SessionOptions
                {
                    MaxPlayers = enterSessionData.WidgetConfiguration.MaxPlayers,
                    IsLocked = false,
                    IsPrivate = false,
                    PlayerProperties = playerProperties,
                    Name = enterSessionData.SessionAction == SessionAction.Create ? enterSessionData.SessionName : Guid.NewGuid().ToString()
                };
                
                SetConnection(ref sessionOptions, enterSessionData.WidgetConfiguration);

                if (enterSessionData.WidgetConfiguration.ConnectionType != ConnectionType.None && enterSessionData.WidgetConfiguration.NetworkHandler != null)
                {
                    sessionOptions.WithNetworkHandler(enterSessionData.WidgetConfiguration.NetworkHandler);
                    joinSessionOptions.WithNetworkHandler(enterSessionData.WidgetConfiguration.NetworkHandler);
                }

                switch (enterSessionData.SessionAction)
                {
                    case SessionAction.Create:
                        ActiveSession = await WidgetDependencies.Instance.MultiplayerService.CreateSessionAsync(sessionOptions);
                        break;
                    case SessionAction.StartMatchmaking:
                        ActiveSession = await WidgetDependencies.Instance.MultiplayerService.MatchmakeSessionAsync(enterSessionData.AdditionalOptions.MatchmakerOptions, sessionOptions);
                        break;
                    case SessionAction.QuickJoin:
                        var quickJoinOptions = new QuickJoinOptions
                        {
                            CreateSession = enterSessionData.AdditionalOptions.AutoCreateSession
                        };
                        ActiveSession = await WidgetDependencies.Instance.MultiplayerService.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);
                        break;
                    case SessionAction.JoinByCode:
                        ActiveSession = await WidgetDependencies.Instance.MultiplayerService.JoinSessionByCodeAsync(enterSessionData.JoinCode, joinSessionOptions);
                        break;
                    case SessionAction.JoinById:
                        ActiveSession = await WidgetDependencies.Instance.MultiplayerService.JoinSessionByIdAsync(enterSessionData.Id, joinSessionOptions);
                        break;
                    case SessionAction.Invalid:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (SessionException sessionException)
            {
                HandleSessionException(sessionException);
            }
            catch (AggregateException ae)
            {
                ae.Handle(ex =>
                {
                    if (ex is SessionException sessionException)
                    {
                        HandleSessionException(sessionException);
                        return true;
                    }

                    return false;
                });
            }
        }

        void HandleSessionException(SessionException sessionException)
        {
            Debug.LogException(sessionException);
            m_WidgetEventDispatcher.OnSessionFailedToJoin(sessionException);
            ActiveSession = null;
        }

        async Task<Dictionary<string, PlayerProperty>> GetPlayerProperties()
        {
            var playerName = await WidgetDependencies.Instance.AuthenticationService.GetPlayerNameAsync();
            var playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);
            var playerProperties = new Dictionary<string, PlayerProperty> { { SessionConstants.playerNamePropertyKey, playerNameProperty } };
            return playerProperties;
        }

        static void SetConnection(ref SessionOptions options, WidgetConfiguration config)
        {
            switch (config.ConnectionType)
            {
                case ConnectionType.None:
                    break;
                case ConnectionType.Direct:
                    options.WithDirectNetwork(config.ListenIpAddress, 
                        config.ConnectionMode == ConnectionMode.Listen ? config.ListenIpAddress : config.PublishIpAddress, 
                        config.Port);
                    break;
#if NGO_2_AVAILABLE                  
                case ConnectionType.DistributedAuthority:
                    options.WithDistributedAuthorityNetwork();
                    break;
#endif
                case ConnectionType.Relay:
                default:
                    options.WithRelayNetwork();
                    break;
            }
        }
        
        internal async Task<IList<ISessionInfo>> QuerySessions()
        {
            var sessionQueryOptions = new QuerySessionsOptions();
            m_SessionQueryResults = await WidgetDependencies.Instance.MultiplayerService.QuerySessionsAsync(sessionQueryOptions);
            return m_SessionQueryResults.Sessions;
        }
        
        internal async Task LeaveSession()
        {
            if (ActiveSession != null)
            {
                UnregisterPlayerEvents();
                EventManager.TriggerEvent(Constants.Events.OnLeavingSession);
                
                try
                {
                    await ActiveSession.LeaveAsync();    
                }
                catch
                {
                    // Ignored as we are exiting the game
                }
                finally
                {
                    ActiveSession = null;
                }
            }
        }

        internal async void KickPlayer(string playerId)
        {
            if (!ActiveSession.IsHost)
                return;

            await ActiveSession.AsHost().RemovePlayerAsync(playerId);
        }
        
        void RegisterSessionEvents()
        {
            if (ActiveSession == null || m_WidgetEventDispatcher == null)
            {
                Debug.LogWarning("ActiveSession is null. Cannot register events.");
                return;
            }

            ActiveSession.Changed += m_WidgetEventDispatcher.OnSessionChanged;
            ActiveSession.StateChanged += m_WidgetEventDispatcher.OnSessionStateChanged;
            ActiveSession.PlayerJoined += m_WidgetEventDispatcher.OnPlayerJoinedSession;
            ActiveSession.PlayerLeaving += m_WidgetEventDispatcher.OnPlayerLeftSession;
            ActiveSession.SessionPropertiesChanged += m_WidgetEventDispatcher.OnSessionPropertiesChanged;
            ActiveSession.PlayerPropertiesChanged += m_WidgetEventDispatcher.OnPlayerPropertiesChanged;
            ActiveSession.RemovedFromSession += m_WidgetEventDispatcher.OnRemovedFromSession;
            ActiveSession.Deleted += m_WidgetEventDispatcher.OnSessionDeleted;
            
            ActiveSession.RemovedFromSession += OnRemovedFromSession;
        }
        
        void UnregisterPlayerEvents()
        {
            ActiveSession.Changed -= m_WidgetEventDispatcher.OnSessionChanged;
            ActiveSession.StateChanged -= m_WidgetEventDispatcher.OnSessionStateChanged;
            ActiveSession.PlayerJoined -= m_WidgetEventDispatcher.OnPlayerJoinedSession;
            ActiveSession.PlayerLeaving -= m_WidgetEventDispatcher.OnPlayerLeftSession;
            ActiveSession.SessionPropertiesChanged -= m_WidgetEventDispatcher.OnSessionPropertiesChanged;
            ActiveSession.PlayerPropertiesChanged -= m_WidgetEventDispatcher.OnPlayerPropertiesChanged;
            ActiveSession.RemovedFromSession -= m_WidgetEventDispatcher.OnRemovedFromSession;
            ActiveSession.Deleted -= m_WidgetEventDispatcher.OnSessionDeleted;
            
            ActiveSession.RemovedFromSession -= OnRemovedFromSession;
        }

        async void OnRemovedFromSession()
        {
            await LeaveSession();
        }
    }
}
