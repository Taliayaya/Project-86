using System;
using Gameplay;
using Unity.Netcode;

namespace Networking
{
    using UnityEngine;

    public class QuickConnect : NetworkBehaviour
    {
        private bool _spawned;
        private void Start()
        {
            Debug.Log("QuickConnect Start");
            NetworkManager.Singleton.OnClientConnectedCallback += StartMultiplayer;
            //NetworkManager.Singleton.OnClientStarted += () =>
            //{
            //    Debug.Log("Client started");
            //    StartMultiplayer(NetworkManager.Singleton.LocalClientId);
            //};
            //NetworkManager.Singleton.OnServerStarted += () =>
            //{
            //    Debug.Log("Server started");
            //    StartMultiplayer(NetworkManager.Singleton.LocalClientId);
            //};
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            //StartMultiplayer(NetworkManager.LocalClientId);
        }

        void StartMultiplayer(ulong clientId)
        {
            if (_spawned) return;
            _spawned = true;
            Debug.Log("Started as Client");
            FindAnyObjectByType<RespawnManager>().SpawnPlayer();
            NetworkManager.Singleton.OnClientConnectedCallback -= StartMultiplayer;
        }
    }

}