using System;
using System.Collections;
using Networking.Widgets.Session.Session;
using Unity.Multiplayer.Widgets;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

namespace Networking.Communication.ChatService
{
    public class LobbyChatService : NetworkBehaviour
    {
        [SerializeField] private Transform chatContent;
        [SerializeField] private GameObject messagePrefab;

        public override void OnNetworkSpawn()
        {
            if (SessionManager.Instance.ActiveSession != null)
                OnSessionJoined(SessionManager.Instance.ActiveSession);
            Debug.Log("Chat Service Spawned");
            EventManager.AddListener(Constants.TypedEvents.Session.SessionJoined, OnSessionJoined);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            EventManager.RemoveListener(Constants.TypedEvents.Session.SessionJoined, OnSessionJoined);
        }

        private void OnMessageReceived(ChatMessage msg)
        {
            Debug.Log("Message Received: " + msg.message);
            if (string.IsNullOrWhiteSpace(msg.message)) return;
            var message = Instantiate(messagePrefab, chatContent);
            message.GetComponentInChildren<ChatMessageUI>().Init(msg);
        }

        [Rpc(SendTo.Everyone, RequireOwnership = false)]
        public void SendMsgRpc(ChatMessage msg)
        {
            OnMessageReceived(msg);
            SessionManager.Instance.AddPlayerName(msg.senderId, msg.sender);
        }
        
        IEnumerator WaitForPlayerName()
        {
            Debug.Log("Waiting for Player Name");
            yield return new WaitUntil(() => AuthenticationService.Instance.PlayerName != null);
            Debug.Log("Player Name Received");
            SendMsgRpc(new ChatMessage()
            {
                message = AuthenticationService.Instance.PlayerName + " joined the session",
                sender = AuthenticationService.Instance.PlayerName,
                senderId = AuthenticationService.Instance.PlayerId,
                type = ChatMessageType.System
            });
        }

        private void OnSessionJoined(object arg0)
        {
            Debug.Log("Session Joined");
            string playerName = AuthenticationService.Instance.PlayerName;
            SessionManager.Instance.ActiveSession.PlayerLeaving += ActiveSessionOnPlayerLeaving;
            if (playerName is null)
            {
                Debug.Log("Player Name is null");
                AuthenticationService.Instance.GetPlayerNameAsync();
                StartCoroutine(WaitForPlayerName());
                return;
            }
            
            SendMsgRpc(new ChatMessage()
            {
                message = playerName + " joined the session",
                sender = playerName,
                senderId = AuthenticationService.Instance.PlayerId,
                type = ChatMessageType.System
            });
        }

        // we cannot make the one leaving the session send the message
        // because we cannot assume that the message will be sent
        private void ActiveSessionOnPlayerLeaving(string clientId)
        {
            Debug.Log("Player Leaving " + clientId);
            if (!IsOwner)
            {
                Debug.Log("Not Owner");
                return;
            }

            string playerName = SessionManager.Instance.GetPlayerName(clientId) ?? "Someone";
            //string playerName = SessionManager.Instance.ActiveSession.GetPlayerName(clientId);
            SendMsgRpc(new ChatMessage()
            {
                message = playerName + " left the session",
                sender = AuthenticationService.Instance.PlayerName,
                senderId = AuthenticationService.Instance.PlayerId,
                type = ChatMessageType.System
            });
        }
    }
}