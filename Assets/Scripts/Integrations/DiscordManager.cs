using System;
using System.Diagnostics;
using System.IO;
using Discord.Sdk;
using Networking.Widgets.Core.Base;
using Networking.Widgets.Session.Session;
using UnityEngine;
using Debug = UnityEngine.Debug;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using Microsoft.Win32;
#endif

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

        // Set from the "project-86:///_discord/join?secret=..." command line arg Discord launches us
        // with when a friend clicks Join while the game isn't already running, consumed once connected.
        private static string _pendingJoinSecret;

        public event Action<Client.Status> onStatusChanged;
        public event Action<ulong> onUserUpdated;

        public Client Client => _client;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            RegisterUriProtocolHandler();
            _pendingJoinSecret = ExtractJoinSecretFromCommandLine();
        }

        private void Start()
        {
            Debug.Log("Starting DiscordManager");
            _client = new Client();
            _client.AddLogCallback(OnLog, LoggingSeverity.Error);
            _client.SetStatusChangedCallback(OnStatusChanged);
            _client.SetUserUpdatedCallback(OnUserUpdated);
            _client.SetActivityInviteCreatedCallback(OnActivityInvite);
            _client.SetActivityJoinCallback(OnActivityJoin);
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

            if (status != Client.Status.Ready)
                return;

            // we try to relaunch rich presence if it failed
            if (_richPresence != null)
                _richPresence.UpdateRichPresence(_client);

            // we were launched (or already running) with a pending Discord join link, join it now
            if (_pendingJoinSecret != null)
            {
                var secret = _pendingJoinSecret;
                _pendingJoinSecret = null;
                JoinSessionByCode(secret);
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
                JoinSessionByCode(joinSecret);
            }
            else
            {
                Debug.LogError($"Failed to accept invite: {result.Error()}");
            }
        }

        // Fires when a user clicks "Join" on a friend's Rich Presence (the secret set via ActivitySecrets.SetJoin)
        private void OnActivityJoin(string joinSecret)
        {
            Debug.Log($"Joining via Rich Presence secret: {joinSecret}");
            JoinSessionByCode(joinSecret);
        }

        private async void JoinSessionByCode(string joinSecret)
        {
            await SessionManager.Instance.EnterSession(new EnterSessionData()
            {
                SessionAction = SessionAction.JoinByCode,
                JoinCode = joinSecret,
            });
        }

        // Discord launches us with e.g. "project-86:///_discord/join?secret=XYZ" as a command line arg
        // when a friend clicks Join and the game wasn't already running.
        private static string ExtractJoinSecretFromCommandLine()
        {
            const string secretKey = "secret=";
            foreach (var arg in Environment.GetCommandLineArgs())
            {
                if (!arg.StartsWith(Constants.Properties.AppLaunchCmd, StringComparison.OrdinalIgnoreCase))
                    continue;

                var secretIndex = arg.IndexOf(secretKey, StringComparison.OrdinalIgnoreCase);
                if (secretIndex < 0)
                    continue;

                var secret = arg[(secretIndex + secretKey.Length)..];
                var ampersandIndex = secret.IndexOf('&');
                if (ampersandIndex >= 0)
                    secret = secret[..ampersandIndex];
                return Uri.UnescapeDataString(secret);
            }

            return null;
        }

        // We ship as a plain zip (no installer), so there's nothing else that registers the
        // "project-86://" URI scheme with the OS for us. Re-registering on every launch is cheap
        // and keeps the handler pointed at the right exe if the player re-extracts to a new folder.
        private static void RegisterUriProtocolHandler()
        {
            try
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                RegisterUriProtocolHandlerWindows();
#elif UNITY_STANDALONE_LINUX && !UNITY_EDITOR
                RegisterUriProtocolHandlerLinux();
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to register {Constants.Properties.AppLaunchCmd} URI protocol handler: {e}");
            }
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private static void RegisterUriProtocolHandlerWindows()
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                return;

            var protocol = Constants.Properties.AppLaunchCmd.Replace("://", "");
            using var protocolKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{protocol}");
            protocolKey.SetValue("", $"URL:{protocol} Protocol");
            protocolKey.SetValue("URL Protocol", "");
            using var commandKey = protocolKey.CreateSubKey(@"shell\open\command");
            commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
        }
#elif UNITY_STANDALONE_LINUX && !UNITY_EDITOR
        private static void RegisterUriProtocolHandlerLinux()
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
                return;

            var protocol = Constants.Properties.AppLaunchCmd.Replace("://", "");
            var appsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "applications");
            Directory.CreateDirectory(appsDir);

            const string desktopFileName = "project-86.desktop";
            var desktopFilePath = Path.Combine(appsDir, desktopFileName);
            File.WriteAllText(desktopFilePath,
                "[Desktop Entry]\n" +
                "Type=Application\n" +
                "Name=Project 86\n" +
                $"Exec=\"{exePath}\" %u\n" +
                $"MimeType=x-scheme-handler/{protocol};\n" +
                "NoDisplay=true\n");

            RunTool("xdg-mime", $"default {desktopFileName} x-scheme-handler/{protocol}", appsDir);
            RunTool("update-desktop-database", appsDir, appsDir);
        }

        private static void RunTool(string fileName, string arguments, string workingDirectory)
        {
            using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            process?.WaitForExit();
        }
#endif
    }
}