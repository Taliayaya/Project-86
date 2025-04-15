using Networking.Widgets.Core.Base;
using Networking.Widgets.Core.Base.Widget.Interfaces;
using Networking.Widgets.Session.Session;
using Unity.Multiplayer.Widgets;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WidgetConfiguration = Networking.Widgets.Core.Configuration.WidgetConfiguration;

namespace Networking.Widgets.Session.Widgets.Base
{
    /// <summary>
    /// Base class for joining a session via a button.
    /// </summary>
    internal class EnterSessionBase : WidgetBehaviour, ISessionLifecycleEvents, ISessionProvider
    {
        [Header("Widget Configuration")]
        [Tooltip("General Widget Configuration.")]
        public WidgetConfiguration WidgetConfiguration;
        
        [Header("Join Session Events")]
        [Tooltip("Event invoked when the user is attempting to join a session.")]
        public UnityEvent JoiningSession = new();
        [Tooltip("Event invoked when the user has successfully joined a session.")]
        public UnityEvent<ISession> JoinedSession = new();
        [Tooltip("Event invoked when the user has failed to join a session.")]
        public UnityEvent<SessionException> FailedToJoinSession = new();

        [SerializeField, HideInInspector]
        protected Button m_EnterSessionButton;
        
        public ISession Session { get; set; }
        
        protected virtual void Awake()
        {
            m_EnterSessionButton ??= GetComponentInChildren<Button>();
            m_EnterSessionButton.onClick.AddListener(EnterSession);
            m_EnterSessionButton.interactable = false;
        }

        public override void OnServicesInitialized()
        {
            m_EnterSessionButton.interactable = true;
        }

        protected virtual void OnDestroy()
        {
            m_EnterSessionButton.onClick.RemoveListener(EnterSession);
        }

        public void OnSessionJoining()
        {
            JoiningSession?.Invoke();
            m_EnterSessionButton.interactable = false;
        }

        public void OnSessionFailedToJoin(SessionException sessionException)
        {
            FailedToJoinSession?.Invoke(sessionException);
            m_EnterSessionButton.interactable = true;
        }

        public void OnSessionJoined()
        {
            JoinedSession?.Invoke(Session);
            m_EnterSessionButton.interactable = Session == null;
        }

        public void OnSessionLeft()
        {
            m_EnterSessionButton.interactable = true;
        }

        protected virtual EnterSessionData GetSessionData()
        {
            return new EnterSessionData { SessionAction = SessionAction.Invalid };
        }

        protected async void EnterSession()
        {
            await SessionManager.Instance.EnterSession(GetSessionData());
        }
    }
}
