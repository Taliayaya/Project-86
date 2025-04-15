using System.Collections.Generic;
using Networking.Widgets.Core.Base.ChatService.Interfaces;
using Networking.Widgets.Core.Base.ManagerInitialization;
using Networking.Widgets.Core.Base.Widget.Interfaces;
using Networking.Widgets.Core.Configuration;
using Unity.Services.Multiplayer;
using UnityEngine.Events;

namespace Networking.Widgets.Core.Base.Widget
{
    /// <summary>
    /// The WidgetEventDispatcher collects all widgets and calls relevant events when they occur.
    /// </summary>
    internal class WidgetEventDispatcher : LazySingleton<WidgetEventDispatcher>
    {
        internal UnityEvent OnServicesInitializedEvent = new ();
        
        List<IWidget> m_Widgets = new();

        List<ISessionProvider> m_SessionProviders = new();
        List<IWidgetConfigurationProvider> m_WidgetConfigurationProviders = new();
        List<ISessionLifecycleEvents> m_SessionLifecycleListeners = new();
        List<ISessionEvents> m_SessionEventListeners = new();
        List<IChatParticipantEvents> m_VoiceChatParticipantEvents = new();
        List<IChatEvents> m_ChatEvents = new();

        ISession m_Session;
        WidgetConfiguration m_WidgetConfiguration;

        internal void OnServicesInitialized()
        {
            for (var index = m_Widgets.Count - 1; index >= 0; index--)
            {
                var widget = m_Widgets[index];
                widget.IsInitialized = true;
                widget.OnServicesInitialized();
            }

            OnServicesInitializedEvent?.Invoke();
        }
        
        /// <summary>
        /// Used to register <see cref="Networking.Widgets.Core.Base.Widget.Interfaces.IWidget"/> to the <see cref="WidgetEventDispatcher"/>.
        /// </summary>
        /// <param name="widget">The widget to register.</param>
        internal void RegisterWidget(IWidget widget)
        {
            if(!ManagerFactory.IsInitialized)
                ManagerFactory.Initialize();
            
            if (widget is ISessionProvider sessionAccessor)
            {
                m_SessionProviders.Add(sessionAccessor);
                sessionAccessor.Session = m_Session;
            }

            if (widget is IWidgetConfigurationProvider widgetConfigurationProvider)
            {
                m_WidgetConfigurationProviders.Add(widgetConfigurationProvider);
                widgetConfigurationProvider.WidgetConfiguration = m_WidgetConfiguration;
            }

            if (widget is ISessionLifecycleEvents sessionLifecycleEvents)
            {
                m_SessionLifecycleListeners.Add(sessionLifecycleEvents);
            }
            
            if (widget is ISessionEvents sessionEvents)
            {
                m_SessionEventListeners.Add(sessionEvents);
            }
            
            if (widget is IChatParticipantEvents voiceChatParticipantEvents)
            {
                m_VoiceChatParticipantEvents.Add(voiceChatParticipantEvents);
            }
            
            if (widget is IChatEvents chatEvents)
            {
                m_ChatEvents.Add(chatEvents);
            }
            
            m_Widgets.Add(widget);

            widget.IsInitialized = global::Networking.Widgets.Core.Base.WidgetServiceInitialization.IsInitialized;
            if(global::Networking.Widgets.Core.Base.WidgetServiceInitialization.IsInitialized)
                widget.OnServicesInitialized();
        }

        
        /// <summary>
        /// Used to unregister <see cref="Networking.Widgets.Core.Base.Widget.Interfaces.IWidget"/> from the <see cref="WidgetEventDispatcher"/>.
        /// </summary>
        /// <param name="widget">The widget to unregister.</param>
        internal void UnregisterWidget(IWidget widget)
        {
            if (widget is ISessionProvider sessionAccessor)
            {
                sessionAccessor.Session = null;
                m_SessionProviders.Remove(sessionAccessor);
            }

            if (widget is IWidgetConfigurationProvider widgetConfigurationProvider)
            {
                widgetConfigurationProvider.WidgetConfiguration = null;
                m_WidgetConfigurationProviders.Remove(widgetConfigurationProvider);
            }
            
            if (widget is ISessionLifecycleEvents sessionLifecycleEvents)
            {
                m_SessionLifecycleListeners.Remove(sessionLifecycleEvents);
            }

            if (widget is ISessionEvents sessionEvents)
            {
                m_SessionEventListeners.Remove(sessionEvents);
            }
            
            if (widget is IChatParticipantEvents voiceChatParticipantEvents)
            {
                m_VoiceChatParticipantEvents.Remove(voiceChatParticipantEvents);
            }
            
            if (widget is IChatEvents chatEvents)
            {
                m_ChatEvents.Remove(chatEvents);
            }
            
            m_Widgets.Remove(widget);
        }

        internal void OnSessionJoined(ISession session, WidgetConfiguration widgetConfiguration)
        {
            m_Session = session;
            m_WidgetConfiguration = widgetConfiguration;

            for (var index = m_SessionProviders.Count - 1; index >= 0; index--)
            {
                var sessionAccessor = m_SessionProviders[index];
                sessionAccessor.Session = m_Session;
            }

            for (var index = m_WidgetConfigurationProviders.Count - 1; index >= 0; index--)
            {
                var widgetConfigurationProvider = m_WidgetConfigurationProviders[index];
                widgetConfigurationProvider.WidgetConfiguration = m_WidgetConfiguration;
            }

            for (var index = m_SessionLifecycleListeners.Count - 1; index >= 0; index--)
            {
                var sessionLifecycleListeners = m_SessionLifecycleListeners[index];
                sessionLifecycleListeners.OnSessionJoined();
            }
        }

        internal void OnSessionLeft()
        {
            m_Session = null;
            m_WidgetConfiguration = null;

            for (var index = m_SessionProviders.Count - 1; index >= 0; index--)
            {
                var sessionAccessor = m_SessionProviders[index];
                sessionAccessor.Session = null;
            }

            for (var index = m_WidgetConfigurationProviders.Count - 1; index >= 0; index--)
            {
                var widgetConfigurationProvider = m_WidgetConfigurationProviders[index];
                widgetConfigurationProvider.WidgetConfiguration = null;
            }

            for (var index = m_SessionLifecycleListeners.Count - 1; index >= 0; index--)
            {
                var sessionLifecycleListener = m_SessionLifecycleListeners[index];
                sessionLifecycleListener.OnSessionLeft();
            }
        }
        
        internal void OnSessionJoining()
        {
            for (var index = m_SessionLifecycleListeners.Count - 1; index >= 0; index--)
            {
                var sessionLifecycleListener = m_SessionLifecycleListeners[index];
                sessionLifecycleListener.OnSessionJoining();
            }
        }
        
        internal void OnSessionFailedToJoin(SessionException sessionException)
        {
            for (var index = m_SessionLifecycleListeners.Count - 1; index >= 0; index--)
            {
                var sessionLifecycleListener = m_SessionLifecycleListeners[index];
                sessionLifecycleListener.OnSessionFailedToJoin(sessionException);
            }
        }

        internal void OnSessionChanged()
        {
            for (var index = m_SessionEventListeners.Count - 1; index >= 0; index--)
            {
                var sessionEventListener = m_SessionEventListeners[index];
                sessionEventListener.OnSessionChanged();
            }
        }

        internal void OnSessionStateChanged(SessionState sessionState)
        {
            for (var index = m_SessionEventListeners.Count - 1; index >= 0; index--)
            {
                var sessionEventListener = m_SessionEventListeners[index];
                sessionEventListener.OnSessionStateChanged(sessionState);
            }
        }
        
        internal void OnPlayerJoinedSession(string playerId)
        {
            for (var index = m_SessionEventListeners.Count - 1; index >= 0; index--)
            {
                var sessionEventListener = m_SessionEventListeners[index];
                sessionEventListener.OnPlayerJoinedSession(playerId);
            }
        }
        
        internal void OnPlayerLeftSession(string playerId)
        {
            for (var index = m_SessionEventListeners.Count - 1; index >= 0; index--)
            {
                var sessionEventListener = m_SessionEventListeners[index];
                sessionEventListener.OnPlayerLeftSession(playerId);
            }
        }
        
        internal void OnSessionPropertiesChanged()
        {
            for (var index = m_SessionEventListeners.Count - 1; index >= 0; index--)
            {
                var sessionEventListener = m_SessionEventListeners[index];
                sessionEventListener.OnSessionPropertiesChanged();
            }
        }
        
        internal void OnPlayerPropertiesChanged()
        {
            for (var index = m_SessionEventListeners.Count - 1; index >= 0; index--)
            {
                var sessionEventListener = m_SessionEventListeners[index];
                sessionEventListener.OnPlayerPropertiesChanged();
            }
        }
        
        internal void OnRemovedFromSession()
        {
            for (var index = m_SessionEventListeners.Count - 1; index >= 0; index--)
            {
                var sessionEventListener = m_SessionEventListeners[index];
                sessionEventListener.OnRemovedFromSession();
            }
        }
        
        internal void OnSessionDeleted()
        {
            for (var index = m_SessionEventListeners.Count - 1; index >= 0; index--)
            {
                var sessionEventListener = m_SessionEventListeners[index];
                sessionEventListener.OnSessionDeleted();
            }
        }

        internal void OnParticipantMuteStateChanged(string playerId, bool participantIsMuted)
        {
            for (var index = m_VoiceChatParticipantEvents.Count - 1; index >= 0; index--)
            {
                var playerIdListener = m_VoiceChatParticipantEvents[index];
                if (playerIdListener.PlayerId == playerId)
                {
                    playerIdListener.OnPlayerMuteStateChanged(participantIsMuted);
                }
            }
        }
        
        internal void OnParticipantSpeechDetected(string playerId)
        {
            for (var index = m_VoiceChatParticipantEvents.Count - 1; index >= 0; index--)
            {
                var playerIdListener = m_VoiceChatParticipantEvents[index];
                if (playerIdListener.PlayerId == playerId)
                {
                    playerIdListener.OnPlayerSpeechDetected();
                }
            }
        }
        
        internal void OnParticipantAudioEnergyChanged(string playerId)
        {
            for (var index = m_VoiceChatParticipantEvents.Count - 1; index >= 0; index--)
            {
                var playerIdListener = m_VoiceChatParticipantEvents[index];
                if (playerIdListener.PlayerId == playerId)
                {
                    playerIdListener.OnPlayerAudioEnergyChanged();
                }
            }
        }
        
        internal void OnChatJoined(string channelId)
        {
            for (var index = m_ChatEvents.Count - 1; index >= 0; index--)
            {
                var chatEventListener = m_ChatEvents[index];
                chatEventListener.OnChatJoined(channelId);
            }
        }

        internal void OnChatLeft(string channelId)
        {
            for (var index = m_ChatEvents.Count - 1; index >= 0; index--)
            {
                var chatEventListener = m_ChatEvents[index];
                chatEventListener.OnChatLeft(channelId);
            }
        }

        internal void OnPlayerAddedToChat(IChatParticipant participant)
        {
            for (var index = m_ChatEvents.Count - 1; index >= 0; index--)
            {
                var chatEventListener = m_ChatEvents[index];
                chatEventListener.OnPlayerAddedToChat(participant);
            }
        }

        internal void OnChatMessageReceived(IChatMessage chatMessage)
        {
            for (var index = m_ChatEvents.Count - 1; index >= 0; index--)
            {
                var chatEventListener = m_ChatEvents[index];
                chatEventListener.OnChatMessageReceived(chatMessage);
            }
        }
    }
}
