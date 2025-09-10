using System;
using ScriptableObjects.UI;
using UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Networking
{
    public struct PlayerInfo : INetworkSerializable, IEquatable<PlayerInfo>
    {
        public FixedString128Bytes PlayerId;
        public FixedString32Bytes PlayerName;
        public ulong NetworkId;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref NetworkId);
        }

        public bool Equals(PlayerInfo other)
        {
            return PlayerId.Equals(other.PlayerId) && PlayerName.Equals(other.PlayerName) && NetworkId == other.NetworkId;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PlayerId, PlayerName, NetworkId);
        }
    }
    public class MissionManager : NetworkBehaviour
    {
        public NetworkVariable<FixedString64Bytes> missionName = new NetworkVariable<FixedString64Bytes>();
        
        public NetworkList<PlayerInfo> Players = new NetworkList<PlayerInfo>();
        
        public static MissionManager Instance;

        private void Awake()
        {
            Instance = this;
            EventManager.AddListener(Constants.Events.OnLeavingSession, OnLeavingSession);
        }

        private void OnLeavingSession()
        {
            Instance = null;
            Destroy(this);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log("MissionManager OnNetworkSpawn");
            missionName.OnValueChanged += OnNewMission;
            if (!missionName.Value.IsEmpty)
                OnNewMission("", missionName.Value);
        }

        public bool GetPlayerById(FixedString128Bytes playerId, out PlayerInfo? playerInfo)
        {
            foreach (var player in Players)
            {
                if (player.PlayerId.Equals(playerId))
                {
                    playerInfo = player;
                    return true;
                }
            }
            playerInfo = null;
            return false;
        }
        
        public bool GetPlayerByNetworkId(ulong networkId, out PlayerInfo? playerInfo)
        {
            foreach (var player in Players)
            {
                if (player.NetworkId == networkId)
                {
                    playerInfo = player;
                    return true;
                }
            }
            playerInfo = null;
            return false;
        }

        [Rpc(SendTo.Owner)]
        public void RegisterPlayerRpc(FixedString128Bytes playerId, FixedString32Bytes playerName, ulong networkId)
        {
            if (GetPlayerById(playerId, out PlayerInfo? info) && info.Value.NetworkId == networkId)
            {
                Debug.Log($"Player {playerName} with id {playerId} already registered with network id {networkId}, removing: " +
                Players.Remove(info.Value));
            }

            Debug.Log($"Registering player {playerName} with id {playerId} and network id {networkId}");
            Players.Add(new PlayerInfo()
            {
                PlayerId = playerId,
                PlayerName = playerName,
                NetworkId = networkId
            });
        }

        protected override void OnNetworkSessionSynchronized()
        {
            base.OnNetworkSessionSynchronized();
            Debug.Log("MissionManager OnNetworkSessionSynchronized");
            missionName.OnValueChanged += OnNewMission;
            if (!missionName.Value.IsEmpty)
            {
                GameManager.Mission = Resources.Load<RegionPointsSO>($"ScriptableObjects/UI/Main Menu/{missionName.Value}");
            }
        }

        private void OnNewMission(FixedString64Bytes previousValue, FixedString64Bytes newValue)
        {
            GameManager.Mission = Resources.Load<RegionPointsSO>($"ScriptableObjects/UI/Main Menu/{newValue}");
            SceneHandler.LoadScene(GameManager.Mission.scene);
            Debug.Log($"New mission: {GameManager.Mission.name}");
        }

        public void ReloadMission()
        {
            SceneHandler.LoadScene(GameManager.Mission.scene, true);
            ReloadMissionRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReloadMissionRpc()
        {
            WindowManager.CloseAll();
            EventManager.TriggerEvent("ReloadScene");
            EventManager.TriggerEvent("OnResume");
        }
    }
}
