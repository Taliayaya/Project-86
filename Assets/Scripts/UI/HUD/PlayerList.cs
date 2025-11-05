using System;
using System.Collections.Generic;
using Networking;
using Networking.Widgets.Session.Session;
using ScriptableObjects.Skins;
using TMPro;
using Unity.Multiplayer.Widgets;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class PlayerList : MonoBehaviour
    {
        [SerializeField] private Transform playerList;

        private void OnEnable()
        {
            UpdateList();
            NetworkManager.Singleton.OnConnectionEvent += SingletonOnOnConnectionEvent;
        }

        private void OnDisable()
        {
            NetworkManager.Singleton.OnConnectionEvent -= SingletonOnOnConnectionEvent;
        }

        private void SingletonOnOnConnectionEvent(NetworkManager arg1, ConnectionEventData arg2)
        {
            UpdateList();
        }

        private void UpdateList()
        {
            foreach (Transform child in playerList)
            {
                child.gameObject.SetActive(false);
            }

            foreach (var networkClient in NetworkManager.Singleton.ConnectedClientsList)
            {
                MissionManager.Instance.GetPlayerByNetworkId(networkClient.ClientId, out PlayerInfo? player);
                IReadOnlyPlayer playerInfo =
                    SessionManager.Instance.ActiveSession.GetPlayer(player.Value.PlayerId.Value);
                var personalMarkFileName = playerInfo.Properties[Constants.Properties.Session.PersonalMark].Value;
                var personalMarkSo = Resources.Load<PersonalMarkSO>("ScriptableObjects/Skins/PersonalMarks/" + personalMarkFileName);
                
                var go = playerList.GetChild((int)networkClient.ClientId);
                go.GetComponentInChildren<TMP_Text>().text = player.Value.PlayerName.Value;
                go.GetComponentInChildren<RawImage>().texture = personalMarkSo.image;
                go.gameObject.SetActive(true);
            }
        }
    }
}