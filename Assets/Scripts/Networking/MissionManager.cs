using System;
using ScriptableObjects.UI;
using UI;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Networking
{
    public class MissionManager : NetworkBehaviour
    {
        public NetworkVariable<FixedString64Bytes> missionName = new NetworkVariable<FixedString64Bytes>();

        public static MissionManager Instance;

        private void Awake()
        {
            Instance = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Debug.Log("MissionManager OnNetworkSpawn");
            missionName.OnValueChanged += OnNewMission;
            if (!missionName.Value.IsEmpty)
                OnNewMission("", missionName.Value);
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
