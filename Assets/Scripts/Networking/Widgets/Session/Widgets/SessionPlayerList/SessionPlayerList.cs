using System.Collections.Generic;
using Armament.Shared;
using Networking.Widgets.Core.Base.ChatService.Interfaces;
using Networking.Widgets.Core.Base.Session;
using Networking.Widgets.Core.Base.Widget;
using Networking.Widgets.Core.Base.Widget.Interfaces;
using Unity.Multiplayer.Widgets;
using Unity.Services.Lobbies;
using Unity.Services.Multiplayer;
using UnityEngine;
using WidgetConfiguration = Networking.Widgets.Core.Configuration.WidgetConfiguration;

namespace Networking.Widgets.Session.Widgets.SessionPlayerList
{
    internal class SessionPlayerList : WidgetBehaviour, ISessionLifecycleEvents, ISessionProvider, IWidgetConfigurationProvider, ISessionEvents, IChatEvents
    {
        public ISession Session { get; set; }
        public WidgetConfiguration WidgetConfiguration { get; set; }
        
        /// <summary>
        /// If enabled the host will be able to kick players via the UI.
        /// </summary>
        [Header("Settings")]
        public bool HostCanKickPlayers = true;
        
        /// <summary>
        /// If enabled players can be muted via the UI.
        /// </summary>
        public bool PlayersCanBeMuted = true;
        
        /// <summary>
        /// The item that will be instantiated for each player in the session.
        /// </summary>
        [Header("References")]
        [Tooltip("The GameObject that will be instantiated for each player in the session.")]
        public GameObject ListItem;

        /// <summary>
        /// The root transform the Player List Item's will be instantiated under.
        /// </summary>
        [Tooltip("The parent transform all ListItems will be instantiated under.")]
        public Transform ContentRoot;

        Dictionary<string, SessionPlayerListItem> m_PlayerListItems = new();

        List<SessionPlayerListItem> m_CachedPlayerListItems = new();
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            UpdatePlayerList();
            
            EventManager.AddListener(Constants.Events.Session.SessionPlayerDataChanged, OnLobbyChanged);
        }

        private async void OnLobbyChanged()
        {
            Debug.Log("[SessionPlayerList] OnLobbyChanged");
            UpdatePlayerList();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            DisableAllPlayerListItems();
            EventManager.RemoveListener(Constants.Events.Session.SessionPlayerDataChanged, OnLobbyChanged);
        }

        public void OnSessionLeft()
        {
            DisableAllPlayerListItems();
        }
        
        public async void OnSessionJoined()
        {
            UpdatePlayerList();
        }

        public void OnPlayerJoinedSession(string playerId)
        {
            UpdatePlayerList();    
        }

        public void OnPlayerLeftSession(string playerId)
        {
            if (m_PlayerListItems.TryGetValue(playerId, out var playerListItem))
            {
                playerListItem.Reset();
                playerListItem.gameObject.SetActive(false);
                m_CachedPlayerListItems.Add(playerListItem);
                m_PlayerListItems.Remove(playerId);
            }
        }

        void UpdatePlayerList()
        {
            if (Session == null)
            {
                Debug.LogWarning("Session is null. Cannot update player list.");
                return;
            }

            Debug.Log($"[SessionPlayerList] Playersession has {Session.Players.Count} players");
            foreach (var player in Session.Players)
            {
                var playerId = player.Id;

                SessionPlayerListItem playerListItem;
                if (m_PlayerListItems.TryGetValue(playerId, out var item))
                {
                    playerListItem = item;
                }
                else
                {

                    playerListItem = GetPlayerListItem(playerId);
                    playerListItem.gameObject.SetActive(true);
                }

                var playerName = "Unknown";
                if (player.Properties.TryGetValue(SessionConstants.playerNamePropertyKey, out var playerNameProperty))
                    playerName = playerNameProperty.Value;
                Debug.Log($"[SessionPlayerList] Player {playerId} has a {SessionConstants.playerNamePropertyKey} property of {playerName}");

                var configuration = new SessionPlayerListItem.Configuration
                {
                    HostCanKickPlayers = HostCanKickPlayers,
                    PlayersCanBeMuted = (WidgetConfiguration?.EnableVoiceChat ?? false) && PlayersCanBeMuted,
                };
                string juggernautArmament = "MachineGun";
                if (playerId == Session.CurrentPlayer.Id)
                    juggernautArmament = ArmamentConfigManager.GetConfig().CurrentArmament.ToString();
                else if (player.Properties.TryGetValue(Constants.Properties.Session.JuggernautArmament,
                        out var juggernautArmamentProperty))
                {
                    juggernautArmament = juggernautArmamentProperty.Value;
                    Debug.Log($"[SessionPlayerList] Player {playerId} has a {Constants.Properties.Session.JuggernautArmament} property of {juggernautArmament}");
                }
                else
                {
                    Debug.LogWarning($"[SessionPlayerList] Player {playerId} does not have a {Constants.Properties.Session.JuggernautArmament} property. Using default value {juggernautArmament}");
                }

                string personalMark = "";
                if (player.Properties.TryGetValue(Constants.Properties.Session.PersonalMark, out var pmark))
                    personalMark = pmark.Value;
                Debug.Log($"[SessionPlayerList] Player {playerId} has a {Constants.Properties.Session.PersonalMark} property of {personalMark}");
                    
                playerListItem.Init(playerName, playerId, configuration, juggernautArmament, personalMark);
            }
        }

        SessionPlayerListItem GetPlayerListItem(string playerId)
        {
            if(m_PlayerListItems.TryGetValue(playerId, out var playerListItem))
                return playerListItem;
            
            if(m_CachedPlayerListItems.Count > 0)
            {
                playerListItem = m_CachedPlayerListItems[0];
                m_CachedPlayerListItems.RemoveAt(0);
            }
            else
            {
                playerListItem = Instantiate(ListItem, ContentRoot).GetComponent<SessionPlayerListItem>();
            }
            
            m_PlayerListItems.Add(playerId, playerListItem);
            return playerListItem;
        }

        void DisableAllPlayerListItems()
        {
            foreach (var playerListItem in m_PlayerListItems.Values)
            {
                playerListItem.Reset();
                playerListItem.gameObject.SetActive(false);
                m_CachedPlayerListItems.Add(playerListItem);
            }
            
            m_PlayerListItems.Clear();
        }

        public void OnPlayerAddedToChat(IChatParticipant participant)
        {
            if (!PlayersCanBeMuted)
                return;
            
            if(!m_PlayerListItems.TryGetValue(participant.Id, out var listItem))
                return;

            listItem.ChatParticipant = participant;
            
            listItem.SetMuteButtonInteractable(WidgetConfiguration.EnableVoiceChat);
            listItem.SetVoiceIndicatorEnabled(WidgetConfiguration.EnableVoiceChat);
        }
    }
}
