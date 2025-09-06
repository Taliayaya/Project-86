using System;
using TMPro;
using UnityEngine;
using Unity.Netcode;

using Unity.Multiplayer.Widgets;
using Networking.Widgets.Session.Session;
using ScriptableObjects.GameParameters;
using Utility;

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
            keepRotation.enabled = !minimapParameters.rotateWithPlayer;
            try
            {
                var netobj = GetComponentInParent<NetworkObject>();
                _clientId = netobj.OwnerClientId;

                var username = SessionManager.Instance.ActiveSession.GetPlayerName(_clientId.ToString());
                SetUsername(username);;
            }
            catch 
            {
                SetUsername("Juggernaut");
            }

        }

        public void SetUsername(string username)
        {
            usernameText.text = username;
        }
    }
}
