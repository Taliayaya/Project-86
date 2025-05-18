using System;
using System.Collections.Generic;
using Networking.Widgets.Core.Base;
using Networking.Widgets.Core.Base.ChatService.Interfaces;
using Networking.Widgets.Core.Base.Widget;
using Networking.Widgets.Core.Base.Widget.Interfaces;
using Unity.Multiplayer.Widgets;
using Unity.Services.Multiplayer;
using UnityEngine;
using WidgetConfiguration = Networking.Widgets.Core.Configuration.WidgetConfiguration;

namespace Networking.Widgets.Vivox
{
    /// <summary>
    /// The VivoxManager handles the lifecycle of a Vivox chat session.
    /// </summary>
    [DefaultExecutionOrder(-99)]
    internal class VivoxManager : LazySingleton<VivoxManager>, IWidget, IWidgetConfigurationProvider, ISessionProvider, ISessionLifecycleEvents
    {
        enum ChatLifecycleState
        {
            Init,
            Joining,
            Joined,
            Leaving,
            Left
        }

        IChatService m_ChatService;

        string m_ChannelName;
        
        public bool IsInitialized { get; set; }
        
        public WidgetConfiguration WidgetConfiguration { get; set; }
        
        public ISession Session { get; set; }
        ChatLifecycleState m_ChatLifecycleState = ChatLifecycleState.Init;
        
        class ParticipantEventReceiver
        {
            IChatParticipant m_Participant;
            
            public ParticipantEventReceiver(IChatParticipant participant)
            {
                m_Participant = participant;
                m_Participant.OnMuteStateChanged += OnMuteStateChanged;
                m_Participant.OnSpeechDetected += OnSpeechDetected;
                m_Participant.OnAudioEnergyChanged += OnAudioEnergyChanged;
            }

            ~ParticipantEventReceiver()
            {
                m_Participant.OnMuteStateChanged -= OnMuteStateChanged;
                m_Participant.OnSpeechDetected -= OnSpeechDetected;
                m_Participant.OnAudioEnergyChanged -= OnAudioEnergyChanged;
            }

            void OnMuteStateChanged()
            {
                WidgetEventDispatcher.Instance.OnParticipantMuteStateChanged(m_Participant.Id, m_Participant.IsMuted);
            }
            
            void OnSpeechDetected()
            {
                WidgetEventDispatcher.Instance.OnParticipantSpeechDetected(m_Participant.Id);
            }
            
            void OnAudioEnergyChanged()
            {
                WidgetEventDispatcher.Instance.OnParticipantAudioEnergyChanged(m_Participant.Id);
            }
        }

        Dictionary<string, ParticipantEventReceiver> m_ParticipantEventReceivers = new();
        
        public void OnServicesInitialized()
        {
            RegisterVivoxEvents();
        }

        void OnEnable()
        {
            m_ChatService = WidgetDependencies.Instance.ChatService;

            WidgetEventDispatcher.Instance.RegisterWidget(this);
        }

        void OnDisable()
        {
            UnregisterVivoxEvents();
        }

        void RegisterVivoxEvents()
        {
            m_ChatService.OnChatJoined += OnChatJoined;
            m_ChatService.OnChatLeft += OnChatLeft;
            m_ChatService.OnChatMessageReceived += OnChatMessageReceived;
            m_ChatService.OnPlayerRemovedFromChat += OnPlayerRemovedFromChat;
        }

        void UnregisterVivoxEvents()
        {
            if (!IsInitialized)
                return;
            
            m_ChatService.OnChatJoined -= OnChatJoined;
            m_ChatService.OnChatLeft -= OnChatLeft;
            m_ChatService.OnChatMessageReceived -= OnChatMessageReceived;
            m_ChatService.OnPlayerRemovedFromChat -= OnPlayerRemovedFromChat;
        }
        
        public async void OnSessionJoined()
        {
            m_ChannelName = Session.Id;
            m_ChatLifecycleState = ChatLifecycleState.Joining;

            var chatOption = WidgetConfiguration.EnableVoiceChat ? ChatOption.TextAndAudio : ChatOption.TextOnly;
            await m_ChatService.JoinChatAsync(m_ChannelName, chatOption);
        }
        
        async void OnChatJoined(string chatId)
        {
            // we should immediately leave a chat that we shouldn't be in
            if (m_ChatLifecycleState != ChatLifecycleState.Joining ||
                (m_ChatLifecycleState == ChatLifecycleState.Joining && (Session == null || Session.Id != chatId)))
            {
                await m_ChatService.LeaveChatAsync(chatId);
                return;
            }
            
            m_ChatLifecycleState = ChatLifecycleState.Joined;
            WidgetEventDispatcher.Instance.OnChatJoined(chatId);
            
            Debug.Log($"Joined chat channel {chatId}");
            m_ChatService.OnPlayerAddedToChat += OnPlayerAddedToChat;
        }

        void OnPlayerAddedToChat(IChatParticipant participant)
        {
            if (participant.ChatId != m_ChannelName)
                return;

            m_ParticipantEventReceivers.Add(participant.Id, new ParticipantEventReceiver(participant));

            WidgetEventDispatcher.Instance.OnPlayerAddedToChat(participant);
        }
        
        void OnPlayerRemovedFromChat(IChatParticipant participant)
        {
            if (participant.ChatId != m_ChannelName)
                return;

            m_ParticipantEventReceivers.Remove(participant.Id, out var _);
        }
        
        void OnChatLeft(string channelId)
        {
            // we should not fire events for leaving a chat that we are not in
            if (m_ChatLifecycleState != ChatLifecycleState.Leaving || 
                (m_ChatLifecycleState == ChatLifecycleState.Leaving && channelId != m_ChannelName))
                return;
            
            m_ChannelName = null;
            
            m_ChatLifecycleState = ChatLifecycleState.Left;
            WidgetEventDispatcher.Instance.OnChatLeft(channelId);
            
            m_ParticipantEventReceivers.Clear();
            m_ChatService.OnPlayerAddedToChat -= OnPlayerAddedToChat;
            
            Debug.Log($"Left chat channel {channelId}");
        }
        
        void OnChatMessageReceived(IChatMessage message)
        {
            WidgetEventDispatcher.Instance.OnChatMessageReceived(message);
        }
        
        public async void OnSessionLeft()
        {
            // we left the session before we could join the chat
            // this means we can't leave the chat right now
            if (m_ChatLifecycleState != ChatLifecycleState.Joined)
                return;
            
            try
            {
                if (!Application.isPlaying)
                    return;

                if (m_ChatService.IsLoggedIn())
                {
                    m_ChatLifecycleState = ChatLifecycleState.Leaving;
                    await m_ChatService.LeaveChatAsync(m_ChannelName);
                }
            }
            catch(Exception exception)
            {
                Debug.LogError($"Failed to leave chat channel {m_ChannelName}: {exception.Message}");
            }   
        }
    }
}
