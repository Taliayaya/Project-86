using System;
using Discord.Sdk;
using Networking.Widgets.Core.Base;
using Networking.Widgets.Session.Session;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Integrations
{
    public class DiscordManager : Singleton<DiscordManager>
    {
        [SerializeField] 
        private ulong clientId; // Set this in the Unity Inspector from the dev portal
        
        private Client _client;
        private string _codeVerifier;

        private string _lobbySecret;
        private ulong _lobbyId;
        private RichPresence.LobbyDetails _lobbyDetails;
        
        private RichPresence _richPresence;
        
        public event Action<Client.Status> onStatusChanged;
        public event Action<ulong> onUserUpdated;
        
        public Client Client => _client;

        private void Start()
        {
            Debug.Log("Starting DiscordManager");
            _client = new Client();
            _client.AddLogCallback(OnLog, LoggingSeverity.Error);
            _client.SetStatusChangedCallback(OnStatusChanged);
            _client.SetUserUpdatedCallback(OnUserUpdated);
            _client.SetActivityInviteCreatedCallback(OnActivityInvite);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _client.Disconnect();
        }

        private void OnEnable()
        {
            EventManager.AddListener(Constants.TypedEvents.Auth.OnLoginSuccess, OnLoginSuccess);
            EventManager.AddListener(Constants.TypedEvents.Discord.UpdateActivity, UpdateRichPresence);
            EventManager.AddListener(Constants.TypedEvents.Discord.UpdateLobby, UpdateLobby);
        }

        private void OnUserUpdated(ulong id)
        {
            onUserUpdated?.Invoke(id);
        }

        private void UpdateRichPresence(object arg0)
        {
            if (arg0 is not RichPresence richPresence)
                return;

            if (_client.GetStatus() == Client.Status.Ready)
            {
                if (_lobbyId == 0)
                    richPresence.UpdateRichPresence(_client);
                else
                    richPresence.UpdateRichPresenceLobby(_client, _lobbyDetails);
            }

            _richPresence = richPresence;
        }
        
        private void UpdateLobby(object arg0)
        {
            if (arg0 is not RichPresence.LobbyDetails lobbyDetails)
                return;
            
            _lobbyDetails = lobbyDetails;
            if (_richPresence && _client.GetStatus() == Client.Status.Ready)
                _richPresence.UpdateRichPresenceLobby(_client, lobbyDetails);
        }

        private void OnLog(string message, LoggingSeverity severity)
        {
            Debug.Log($"[{severity}]: {message}");
        }

        private void OnStatusChanged(Client.Status status, Client.Error error, int errorCode)
        {
            Debug.Log($"Status changed: {status})");
            if (error != Client.Error.None)
                Debug.LogError($"Error: {error}, Code: {errorCode}");
            onStatusChanged?.Invoke(status);
            
            // we try to relaunch rich presence if it failed
            if (status == Client.Status.Ready && _richPresence != null)
            {
                _richPresence.UpdateRichPresence(_client);
            }
        }

        public void OnLoginSuccess(object data)
        {
            StartOAuthFlow();
        }
        
        public void StartOAuthFlow()
        {
            Debug.Log("Starting OAuth flow");
            var authorizationVerifier = _client.CreateAuthorizationCodeVerifier();
            _codeVerifier = authorizationVerifier.Verifier();

            var args = new AuthorizationArgs();
            args.SetClientId(clientId);
            args.SetScopes(Client.GetDefaultCommunicationScopes());
            args.SetCodeChallenge(authorizationVerifier.Challenge());
            _client.Authorize(args, OnAuthorizeResult);
        }
        
        private void OnAuthorizeResult(ClientResult result, string code, string redirectUri)
        {
            if (!result.Successful())
            {
                Debug.Log($"Authorization result: [{result.Error()}]");
                return;
            }
            GetTokenFromCode(code, redirectUri);
        }

        private void GetTokenFromCode(string code, string redirectUri)
        {
            _client.GetToken(clientId, code, _codeVerifier, redirectUri, OnGetToken);
        }

        private void OnGetToken(ClientResult result, string token, string refreshToken, AuthorizationTokenType tokenType, int expiresIn, string scope)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.Log("Failed to retrieve token");
            }
            else
            {
                _client.UpdateToken(AuthorizationTokenType.Bearer, token, OnUpdateToken);
            }
        }

        private void OnUpdateToken(ClientResult result)
        {
            if (result.Successful())
            {
                _client.Connect();
            }
            else
            {
                Debug.LogError($"Failed to update token: {result.Error()}");
            }
        }

        private Action<ulong> _callback;
        public void CreateLobby(Action<ulong> callback)
        {
            _callback = callback;
            _lobbySecret = Guid.NewGuid().ToString();
            Client.CreateOrJoinLobby(_lobbySecret, OnCreateOrJoinLobby);
        }

        public void LeaveLobby()
        {
            _client.LeaveLobby(_lobbyId, OnLeaveLobby);
        }

        private void OnCreateOrJoinLobby(ClientResult clientResult, ulong lobbyId)
        {
            if (clientResult.Successful())
            {
                Debug.Log($"Lobby created with ID: {lobbyId}");
                _lobbyId = lobbyId;
                _callback?.Invoke(lobbyId);
            }
            else
            {
                Debug.LogError($"Failed to create lobby: {clientResult.Error()}");
            }
        }
        
        private void OnLeaveLobby(ClientResult clientResult)
        {
            if (clientResult.Successful())
            {
                _lobbyId = 0;
                _lobbySecret = string.Empty;
                UpdateRichPresence(_richPresence);

                Debug.Log($"Successfully left lobby");
            }
            else
            {
                Debug.LogError($"Failed to leave lobby: {clientResult}");
            }
        }
        
        public void SendInvite(ulong targetUserId)
        {
            Client.SendActivityInvite(targetUserId, "Join my game!", OnSendInvite);
        }

        private void OnSendInvite(ClientResult result)
        {
            if (result.Successful())
            {
                Debug.Log("Successfully sent invite");
            }
            else
            {
                Debug.LogError($"Failed to send invite: {result.Error()}");
            }
        }
        
        private void OnActivityInvite(ActivityInvite invite)
        {
            Debug.Log($"Received invite from user {invite.SenderId()}");
            _client.AcceptActivityInvite(invite, OnAcceptInvite);
        }

        private void OnAcceptInvite(ClientResult result, string joinSecret)
        {
            if (result.Successful())
            {
                Debug.Log($"Accepted invite with lobby secret: {joinSecret}");
                var joining = SessionManager.Instance.EnterSession(new EnterSessionData()
                {
                    SessionAction = SessionAction.JoinByCode,
                    JoinCode = joinSecret,
                });
                joining.Start();
            }
            else
            {
                Debug.LogError($"Failed to accept invite: {result.Error()}");
            }
        }
    }
}