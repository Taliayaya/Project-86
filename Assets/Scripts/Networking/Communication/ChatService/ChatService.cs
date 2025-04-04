using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Networking.Widgets.Session.Session;
using TMPro;
using Unity.Multiplayer.Widgets;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Communication.ChatService
{
    public enum ChatMessageType
    {
        System,
        Player,
    }

    public struct ChatMessage : INetworkSerializable
    {
        public ChatMessageType type;
        public string message;
        public string sender;
        public string senderId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref type);
            serializer.SerializeValue(ref message);
            serializer.SerializeValue(ref sender);
            serializer.SerializeValue(ref senderId);
        }
    }

    public class ChatService : MonoBehaviour
    {
        private IList<KeyValuePair<string, MessageObjectUI>> m_MessageObjPool =
            new List<KeyValuePair<string, MessageObjectUI>>();

        ScrollRect m_TextChatScrollRect;

        public GameObject ChatContentObj;
        public GameObject MessageObject;
        public Button EnterButton;
        public InputField MessageInputField;
        public Button SendTTSMessageButton;
        public Toggle ToggleTTS;
        public GameObject ChannelEffectPanel;
        public Dropdown ChannelEffectDropdown;
        
        [SerializeField] private ChatCapability chatCapability = ChatCapability.TextOnly;

        private Task FetchMessages = null;
#pragma warning disable CS0414 // Field is assigned but its value is never used
        private DateTime? oldestMessage = null;
#pragma warning restore CS0414 // Field is assigned but its value is never used

        public string LobbyChannelName => SessionManager.Instance.ActiveSession.Id;

        async void Start()
        {
             AuthenticationService.Instance.SignedIn += async () =>
             {
                 await VivoxService.Instance.InitializeAsync();
                 await VivoxService.Instance.LoginAsync(new LoginOptions()
                 {
                     DisplayName = AuthenticationService.Instance.PlayerName,
                     PlayerId = AuthenticationService.Instance.PlayerId
                 });
             };           
             
            VivoxService.Instance.ChannelJoined += OnChannelJoined;
            VivoxService.Instance.DirectedMessageReceived += OnDirectedMessageReceived;
            VivoxService.Instance.ChannelMessageReceived += OnChannelMessageReceived;
            VivoxService.Instance.ChannelMessageEdited += OnChannelMessageEdited;
            VivoxService.Instance.ChannelMessageDeleted += OnChannelMessageDeleted;
            
            
            EventManager.AddListener(Constants.TypedEvents.Session.SessionJoined, OnSessionJoined);
            m_TextChatScrollRect = GetComponent<ScrollRect>();

#if !(UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL)
        MessageInputField.gameObject.SetActive(false);
        EnterButton.gameObject.SetActive(false);
        SendTTSMessageButton.gameObject.SetActive(false);
#else
            //EnterButton.onClick.AddListener(SendMessage);
            //MessageInputField.onEndEdit.AddListener((string text) => { EnterKeyOnTextField(); });
            //SendTTSMessageButton.onClick.AddListener(SubmitTTSMessageToVivox);
            //ToggleTTS.onValueChanged.AddListener(TTSToggleValueChanged);
            //ChannelEffectDropdown.onValueChanged.AddListener(ChannelEffectValueChanged);
            AudioTapsManager.Instance.OnTapsFeatureChanged += OnAudioTapsManagerFeatureChanged;

            //ChannelEffectPanel.gameObject.SetActive(AudioTapsManager.Instance.IsFeatureEnabled);
#endif
            m_TextChatScrollRect.onValueChanged.AddListener(ScrollRectChange);
        }

        private void OnSessionJoined(object arg0)
        {
            Debug.Log("Session joined");
            if (SessionManager.Instance.ActiveSession != null)
            {
                Debug.Log("Session joined event " + LobbyChannelName);
                SessionManager.Instance.ActiveSession.PlayerJoined += ActiveSessionOnPlayerJoined;
                VivoxService.Instance.JoinGroupChannelAsync(LobbyChannelName, chatCapability);
            }
        }
        
        private void ActiveSessionOnPlayerJoined(string playerId)
        {
            Debug.Log($"Player joined: {playerId}");
            
            string playerName = SessionManager.Instance.ActiveSession.GetPlayerName(playerId);
            // search playername
            Debug.Log("Session owner: " + NetworkManager.Singleton.CurrentSessionOwner);
            if (NetworkManager.Singleton.CurrentSessionOwner == NetworkManager.Singleton.LocalClientId)
            {
                VivoxService.Instance.SendChannelTextMessageAsync(LobbyChannelName,
                    $"{playerName} has joined the session.");
            }
        }

        private void OnEnable()
        {
            ClearTextField();
        }

        private void OnDisable()
        {
            if (m_MessageObjPool.Count > 0)
            {
                ClearMessageObjectPool();
            }

            oldestMessage = null;
        }

        private void ScrollRectChange(Vector2 vector)
        {
            // Scrolled near end and check if we are fetching history already
            if (m_TextChatScrollRect.verticalNormalizedPosition >= 0.95f && FetchMessages != null &&
                (FetchMessages.IsCompleted || FetchMessages.IsFaulted || FetchMessages.IsCanceled))
            {
                m_TextChatScrollRect.normalizedPosition = new Vector2(0, 0.8f);
                FetchMessages = FetchHistory(false);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task FetchHistory(bool scrollToBottom = false)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
#if !UNITY_WEBGL
            try
            {
                var chatHistoryOptions = new ChatHistoryQueryOptions()
                {
                    TimeEnd = oldestMessage
                };
                var historyMessages =
                    await VivoxService.Instance.GetChannelTextMessageHistoryAsync(LobbyChannelName,
                        10,
                        chatHistoryOptions);
                var reversedMessages = historyMessages.Reverse();
                foreach (var historyMessage in reversedMessages)
                {
                    AddMessageToChat(historyMessage, true, scrollToBottom);
                }

                // Update the oldest message ReceivedTime if it exists to help the next fetch get the next batch of history
                oldestMessage = historyMessages.FirstOrDefault()?.ReceivedTime;
            }
            catch (TaskCanceledException e)
            {
                Debug.Log(
                    $"Chat history request was canceled, likely because of a logout or the data is no longer needed: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Tried to fetch chat history and failed with error: {e.Message}");
                Debug.LogException(e);
            }
#endif
        }

        void OnDestroy()
        {
            VivoxService.Instance.ChannelJoined -= OnChannelJoined;
            VivoxService.Instance.DirectedMessageReceived -= OnDirectedMessageReceived;
            VivoxService.Instance.ChannelMessageReceived -= OnChannelMessageReceived;
            VivoxService.Instance.ChannelMessageEdited -= OnChannelMessageEdited;
            VivoxService.Instance.ChannelMessageDeleted -= OnChannelMessageDeleted;

#if UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
//            EnterButton.onClick.RemoveAllListeners();
//            MessageInputField.onEndEdit.RemoveAllListeners();
//            SendTTSMessageButton.onClick.RemoveAllListeners();
//            ToggleTTS.onValueChanged.RemoveAllListeners();
#endif
            m_TextChatScrollRect.onValueChanged.RemoveAllListeners();
        }

        void TTSToggleValueChanged(bool toggleTTS)
        {
            if (!ToggleTTS.isOn)
            {
                VivoxService.Instance.TextToSpeechCancelMessages(TextToSpeechMessageType.LocalPlayback);
            }
        }

        private void OnAudioTapsManagerFeatureChanged(bool enabled)
        {
            ChannelEffectPanel.gameObject.SetActive(enabled);
        }

        private void ChannelEffectValueChanged(int value)
        {
            AudioTapsManager.Instance.AddChannelAudioEffect((AudioTapsManager.Effects)value);
        }

        void ClearMessageObjectPool()
        {
            foreach (KeyValuePair<string, MessageObjectUI> keyValuePair in m_MessageObjPool)
            {
                Destroy(keyValuePair.Value.gameObject);
            }

            m_MessageObjPool.Clear();
        }

        void ClearTextField()
        {
            //MessageInputField.text = string.Empty;
            //MessageInputField.Select();
            //MessageInputField.ActivateInputField();
        }

        void EnterKeyOnTextField()
        {
            if (!Input.GetKeyDown(KeyCode.Return))
            {
                return;
            }

            SendMessage();
        }

        void SendMessage()
        {
            if (string.IsNullOrEmpty(MessageInputField.text))
            {
                return;
            }
            if (SessionManager.Instance.ActiveSession == null)
            {
                return;
            }

            VivoxService.Instance.SendChannelTextMessageAsync(SessionManager.Instance.ActiveSession.Name,
                MessageInputField.text);
            ClearTextField();
        }

        void SubmitTTSMessageToVivox()
        {
            if (string.IsNullOrEmpty(MessageInputField.text))
            {
                return;
            }

            VivoxService.Instance.TextToSpeechSendMessage(MessageInputField.text,
                TextToSpeechMessageType.RemoteTransmissionWithLocalPlayback);
            ClearTextField();
        }

        IEnumerator SendScrollRectToBottom()
        {
            yield return new WaitForEndOfFrame();

            // We need to wait for the end of the frame for this to be updated, otherwise it happens too quickly.
            m_TextChatScrollRect.normalizedPosition = new Vector2(0, 0);

            yield return null;
        }

        void OnDirectedMessageReceived(VivoxMessage message)
        {
            AddMessageToChat(message, false, true);
        }

        void OnChannelJoined(string channelName)
        {
            FetchMessages = FetchHistory(true);
        }

        void OnChannelMessageReceived(VivoxMessage message)
        {
            AddMessageToChat(message, false, true);
        }

        private void OnChannelMessageEdited(VivoxMessage message)
        {
            var editedMessage = m_MessageObjPool?.FirstOrDefault(x => x.Key == message.MessageId).Value;
            // If we have the message that's been edited we will update if not we do nothing.
            editedMessage?.SetTextMessage(message);
        }

        private void OnChannelMessageDeleted(VivoxMessage message)
        {
            var editedMessage = m_MessageObjPool?.FirstOrDefault(x => x.Key == message.MessageId).Value;
            // If we have the message that's been deleted we will destroy it if not we do nothing.
            editedMessage?.SetTextMessage(message, true);
        }

        void AddMessageToChat(VivoxMessage message, bool isHistory = false, bool scrollToBottom = false)
        {
            var newMessageObj = Instantiate(MessageObject, ChatContentObj.transform);
            var newMessageTextObject = newMessageObj.GetComponent<MessageObjectUI>();
            if (isHistory)
            {
                m_MessageObjPool.Insert(0,
                    new KeyValuePair<string, MessageObjectUI>(message.MessageId, newMessageTextObject));
                newMessageObj.transform.SetSiblingIndex(0);
            }
            else
            {
                m_MessageObjPool.Add(
                    new KeyValuePair<string, MessageObjectUI>(message.MessageId, newMessageTextObject));
            }

            newMessageTextObject.SetTextMessage(message);
            if (scrollToBottom)
            {
                StartCoroutine(SendScrollRectToBottom());
            }

            if (!message.FromSelf)
            {
                if (ToggleTTS && ToggleTTS.isOn)
                {
                    VivoxService.Instance.TextToSpeechSendMessage(
                        $"{message.SenderDisplayName} said, {message.MessageText}",
                        TextToSpeechMessageType.LocalPlayback);
                }
            }
        }
    }
}
