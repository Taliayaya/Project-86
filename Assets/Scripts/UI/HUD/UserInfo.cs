using System;
using Gameplay;
using Gameplay.Units;
using Networking;
using TMPro;
using UnityEngine;
using Unity.Netcode;

using Networking.Widgets.Session.Session;
using ScriptableObjects.GameParameters;
using Utility;
using PlayerInfo = Networking.PlayerInfo;

namespace UI.HUD
{
    public class UserInfo : NetworkBehaviour
    {
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private KeepRotationTo keepRotation;
        [SerializeField] private MinimapParameters minimapParameters;

        private ulong _clientId;

        private void Start()
        {
            Debug.Log("[UserInfo] OnNetworkSpawn");
            if (PlayerManager.Player)
            {
                keepRotation.targetRotation = minimapParameters.rotateWithPlayer ? PlayerManager.Player.transform.GetComponentInChildren<UserInfo>().transform : null;
            }

            MissionManager.Instance.Players.OnListChanged += OnListChanged;

            var netobj = GetComponentInParent<NetworkObject>();
            _clientId = netobj.OwnerClientId;
            UpdateUsername();
            
        }

        private void OnListChanged(NetworkListEvent<PlayerInfo> changeEvent)
        {
            UpdateUsername();
        }

        public void UpdateUsername()
        {
            if (!MissionManager.Instance.GetPlayerByNetworkId(_clientId, out PlayerInfo? info))
            {
                Debug.Log("[UserInfo] No player");
                SetUsername("Juggernaut");
            }
            else
            {
                Debug.Log($"[UserInfo] username: {info.Value.PlayerName}");
                // removing #ID
                String username = info.Value.PlayerName.ToString().Split("#")[0];
                SetUsername(username);
            }
        }

        private void OnEnable()
        {
            EventManager.AddListener(Constants.TypedEvents.OnPlayerChanged, OnPlayerChanged);
        }

        private void OnPlayerChanged(object arg0)
        {
            Unit player = (Unit)arg0;
            keepRotation.targetRotation = minimapParameters.rotateWithPlayer ? player.transform.GetComponentInChildren<UserInfo>().transform : null;
        }

        public void SetUsername(string username)
        {
            usernameText.text = username;
        }
    }
}
