using System;
using Discord.Sdk;
using Networking.Widgets.Session.Session;
using UnityEngine;

namespace Integrations
{
    public class RichPresence : MonoBehaviour
    {
        public struct LobbyDetails
        {
            public string LobbyId;
            public string LobbySecret;
            public int MaxLobbySize;
            public string State;
            public string Details;
        }
        [SerializeField] private string details = "In Unity";

        [SerializeField] private string state = "Building a game";
        
        private static ulong startTimestamp;

        static RichPresence()
        {
            startTimestamp = (ulong)System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private void Start()
        {
            PublishRichPresence();
        }

        public void PublishRichPresence()
        {
            EventManager.TriggerEvent(Constants.TypedEvents.Discord.UpdateActivity, this);
        }
        
        public void UpdateRichPresence(Client client)
        {
            Activity activity = new Activity();

            activity.SetType(ActivityTypes.Playing);
            activity.SetDetails(details);
            activity.SetState(state);

            var activityTimestamp = new ActivityTimestamps();
            activityTimestamp.SetStart(startTimestamp);
            activity.SetTimestamps(activityTimestamp);

            client.UpdateRichPresence(activity, OnUpdateRichPresence);
        }

        public void UpdateRichPresenceLobby(Client client, LobbyDetails lobbyDetails)
        {
            UpdateRichPresenceLobby(client, lobbyDetails.State, lobbyDetails.Details, lobbyDetails.LobbySecret, lobbyDetails.LobbyId, lobbyDetails.MaxLobbySize);
        }
        
        public void UpdateRichPresenceLobby(Client client, string newState, string newDetails, string lobbySecret, string lobbyId, int maxLobbySize)
        {
            Activity activity = new Activity();

            state = newState;
            details = newDetails;
            activity.SetType(ActivityTypes.Playing);
            activity.SetState(newState);
            activity.SetDetails(newDetails);

            var activityTimestamp = new ActivityTimestamps();
            activityTimestamp.SetStart(startTimestamp);
            activity.SetTimestamps(activityTimestamp);
    
            var activityParty = new ActivityParty();
            activityParty.SetId(lobbyId);
            activityParty.SetCurrentSize(SessionManager.Instance.ActiveSession.Players.Count);
            activityParty.SetMaxSize(maxLobbySize);
            activity.SetParty(activityParty);

            var activitySecrets = new ActivitySecrets();
            activitySecrets.SetJoin(lobbySecret);
            activity.SetSecrets(activitySecrets);

            client.UpdateRichPresence(activity, OnUpdateRichPresence);
            client.RegisterLaunchCommand(client.GetApplicationId(), Constants.Properties.AppLaunchCmd);
        }

        private void OnUpdateRichPresence(ClientResult result)
        {
            if (result.Successful())
            {
                Debug.Log("Rich presence updated!");
            }
            else
            {
                Debug.LogError($"Failed to update rich presence {result.Error()}");
            }
        }

        public void SetDetails(string newDetails)
        {
            details = newDetails;
            PublishRichPresence();
        }
        
        public void SetState(string newState)
        {
            state = newState;
            PublishRichPresence();
        }
    }
}