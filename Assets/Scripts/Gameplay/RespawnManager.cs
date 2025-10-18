using System;
using System.Linq;
using Armament.Shared;
using Cosmetic;
using Gameplay.Mecha;
using Networking;
using Networking.Widgets.Session.Session;
using Unity.Multiplayer.Widgets;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Gameplay
{
    public struct RespawnData
    {
        public Vector3 DeathPosition;
    }
    public class RespawnManager : Singleton<RespawnManager>
    {
        private GameObject[] _respawnPoints;
        
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private bool spawnAtStart = false;
        [SerializeField] private Transform spawnPoint;

        private void Awake()
        {
            _respawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        }

        private void Start()
        {
            //if (spawnAtStart)
            //{
            //    SpawnPlayer();
            //}
        }
        
        private void OnEnable()
        {
            EventManager.AddListener("OnRespawn", OnRespawn);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener("OnRespawn", OnRespawn);
        }

        private const bool DestroyWithScene = true;
        
        //[ServerRpc]
        public GameObject Respawn(GameObject prefab, GameObject spawnPoint, ulong clientId)
        {
            // random offset
            var offset = new Vector3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f));
            var go = Instantiate(prefab, spawnPoint.transform.position + offset, spawnPoint.transform.rotation);
            go.name = "Player" + clientId;

            if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("Giving control to client " + clientId );
                go.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, DestroyWithScene);
                //go.GetComponent<MechaController>().ReInit();
            }
            ApplyJuggernautConfig(go, clientId);
            
            return go;
        }

        void ApplyJuggernautConfig(GameObject go, ulong clientId)
        {
            var armamentSection = go.transform.GetChild(2);
            Debug.Assert(armamentSection.name == "Armament", "Armament section not found");
            
            MissionManager.Instance.GetPlayerByNetworkId(clientId, out var playerInfo);
            IReadOnlyPlayer player = SessionManager.Instance.ActiveSession.GetPlayer(playerInfo.Value.PlayerId.Value);
            Enum.TryParse(player.Properties[Constants.Properties.Session.JuggernautArmament].Value, out ArmamentType armament);
            
            var switcher1 = armamentSection.GetComponentInChildren<ArmamentComponentSwitcher>();
            var switcher2 = armamentSection.GetComponentInChildren<ArmamentSwitcher>();
            switcher1.ChangedArmament(armament);
            switcher2.ChangedArmament(armament);
        }

        public GameObject SpawnPlayer()
        {
            return SpawnPlayer(NetworkManager.Singleton.LocalClientId);
        }

        public GameObject SpawnPlayer(ulong clientId)
        {
            Debug.Log("spawnpoint: " + spawnPoint);
            return Respawn(playerPrefab, spawnPoint.gameObject, clientId);
        }

        public GameObject Respawn(GameObject prefab, int spawnPointIndex)
        {
            return Respawn(prefab, _respawnPoints[spawnPointIndex], NetworkManager.Singleton.LocalClientId);
        }

        public static Vector3 GetClosestRespawnPoint(Vector3 origin)
        {
            return Instance._GetClosestRespawnPointPos(origin);
        }

        private Vector3 _GetClosestRespawnPointPos(Vector3 origin)
        {
            var closestSpawnPoint = _respawnPoints.Min(x => Vector3.Distance(x.transform.position, origin));
            var spawnPoint = _respawnPoints.First(x => Math.Abs(Vector3.Distance(x.transform.position, origin) - closestSpawnPoint) < 0.5);
            return spawnPoint.transform.position;
        }

        public GameObject Respawn(GameObject prefab, Vector3 position)
        {
            if (_respawnPoints.Length == 0)
            {
                Debug.LogError("No respawn points found!");
                return null;
            }
            var closestSpawnPoint = _respawnPoints.Min(x => Vector3.Distance(x.transform.position, position));
            var spawnPoint = _respawnPoints.First(x => Math.Abs(Vector3.Distance(x.transform.position, position) - closestSpawnPoint) < 0.5);
            var player = Respawn(prefab, spawnPoint, NetworkManager.Singleton.LocalClientId);
            return player;
        }

        private void JuggernautConfig()
        {
            
        }

        public void OnRespawn(object arg)
        {
            if (_respawnCD) return;
            _respawnCD = true;
            Invoke(nameof(ResetCD), 1f);
            var respawnData = (RespawnData) arg;
            Respawn(playerPrefab, respawnData.DeathPosition);
        }

        private bool _respawnCD = false;

        private void ResetCD()
        {
            _respawnCD = false;
        }
    }
}