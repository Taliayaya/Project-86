using Networking.Widgets.Core.Base.Widget.Interfaces;
using TMPro;
using Unity.Multiplayer.Widgets;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Networking.Widgets.Session.Widgets
{
    internal class ShowJoinCode : WidgetBehaviour, ISessionLifecycleEvents, ISessionProvider
    {
        const string k_NoCode = "–";

        public ISession Session { get; set; }
        
        [SerializeField]
        TMP_Text m_Text;
        [SerializeField]
        Button m_CopyCodeButton;

        void Start()
        {
            if(m_Text == null)
                m_Text = GetComponentInChildren<TMP_Text>();
            if(m_CopyCodeButton == null)
                m_CopyCodeButton = GetComponentInChildren<Button>();
            
            m_CopyCodeButton.onClick.AddListener(CopySessionCodeToClipboard);
        }

        public override void OnServicesInitialized()
        {
            m_CopyCodeButton.interactable = false;
        }

        public void OnSessionJoined()
        {
            m_Text.text = Session?.Code ?? k_NoCode;
            m_CopyCodeButton.interactable = true;
        }

        public void OnSessionLeft()
        {
            m_Text.text = k_NoCode;
            m_CopyCodeButton.interactable = false;
        }

        void CopySessionCodeToClipboard()
        {
            // Deselect the button when clicked.
            EventSystem.current.SetSelectedGameObject(null);
            
            var code = m_Text.text;

            if (Session?.Code == null || string.IsNullOrEmpty(code))
            {
                return;
            }

            // Copy the text to the clipboard.
            GUIUtility.systemCopyBuffer = code;
        }
    }
}
