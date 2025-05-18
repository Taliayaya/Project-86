using Networking.Widgets.Core.Base.Widget.Interfaces;
using Networking.Widgets.Session.Session;
using Unity.Multiplayer.Widgets;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Networking.Widgets.Session.Widgets
{
    [RequireComponent(typeof(Button))]
    internal class LeaveSession : WidgetBehaviour, ISessionLifecycleEvents, ISessionProvider
    {
        [FormerlySerializedAs("ExitedSession")]
        [Tooltip("Event invoked when the user has successfully left a session.")]
        public UnityEvent SessionLeft = new();
     
        public ISession Session { get; set; }
        
        Button m_Button;
        
        void Start()
        {
            m_Button = GetComponent<Button>();
            m_Button.onClick.AddListener(Leave);
            SetButtonActive();
        }

        public void OnSessionLeft()
        {
            SessionLeft.Invoke();
            SetButtonActive();
        }

        public void OnSessionJoined()
        {
            SetButtonActive();
        }

        
        void SetButtonActive()
        {
            m_Button.interactable = Session != null;
        }
        
        async void Leave()
        {
            await SessionManager.Instance.LeaveSession();
        }
    }
}
