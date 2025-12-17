using System;
using Networking.Widgets.Core.Base;
using Networking.Widgets.Core.Base.ChatService.Interfaces;
using Networking.Widgets.Core.Base.Session;
using Networking.Widgets.Core.Base.Widget;
using Networking.Widgets.Core.Base.Widget.Interfaces;
using TMPro;
using Unity.Multiplayer.Widgets;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Widgets.Vivox.Widgets.Text_Chat
{
    internal class TextChat : WidgetBehaviour, IChatEvents, ISessionProvider, ISessionLifecycleEvents
    {
        public ISession Session { get; set; }
        
        [Header("Text Chat References")]
        [SerializeField]
        Button m_SubmitButton;
        
        [SerializeField]
        TMP_InputField m_InputField;

        [Header("Text Message References")]
        [SerializeField]
        [Tooltip("The parent transform all TextMessage Items will be instantiated under.")]
        Transform m_ContentRoot;
        
        [SerializeField]
        [Tooltip("The Text Message Item that will be instantiated")]
        TextMessage m_TextMessagePrefab;

        IChatService m_ChatService;
        
        void Awake()
        {
            m_ChatService = WidgetDependencies.Instance.ChatService;
            m_SubmitButton.interactable = false;
            m_InputField.interactable = false;
        }

        public override void OnServicesInitialized()
        {
            m_SubmitButton.onClick.AddListener(SubmitMessage);
            m_InputField.onEndEdit.AddListener(_ =>
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    SubmitMessage();
                }
            });
            m_InputField.onValueChanged.AddListener(value =>
            {
                m_SubmitButton.interactable = !string.IsNullOrEmpty(value);
            });
        }
        
        async void SubmitMessage()
        {
            if (string.IsNullOrEmpty(m_InputField.text))
            {
                ClearInputField();
                return;
            }
                
            try
            {
                await m_ChatService.SendChatMessageAsync(Session.Id, m_InputField.text);
                ClearInputField();
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }

        void ClearInputField()
        {
            m_InputField.ActivateInputField();
            m_InputField.text = "";
        }

        public void OnChatJoined(string chatId)
        {
            m_InputField.interactable = true;
        }
        
        public void OnChatLeft(string chatId)
        {
            m_SubmitButton.interactable = false;
            m_InputField.interactable = false;
        }

        public void OnChatMessageReceived(IChatMessage message)
        {
            var textMessage = Instantiate(m_TextMessagePrefab, m_ContentRoot);
            textMessage.gameObject.SetActive(true);
            textMessage.Init(message, Session.GetPlayerName(message.SenderPlayerId));
        }

        public void OnSessionLeft()
        {
            // Clear the chat messages when leaving the session.
            if (m_ContentRoot != null)
            {
                foreach (Transform child in m_ContentRoot)
                {
                    // Keep the original TextMessage prefab.
                    if (child.gameObject == m_TextMessagePrefab.gameObject)
                        continue;
                    Destroy(child.gameObject);
                }
            }
        }
    }
}
