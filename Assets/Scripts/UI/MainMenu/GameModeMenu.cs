using System;
using System.Collections.Generic;
using DefaultNamespace.Sound;
using Gameplay;
using Networking;
using Networking.RpcRequestStructs;
using ScriptableObjects.UI;
using TMPro;
using Unity.Netcode;
using Unity.Services.Analytics;
using Unity.Services.Lobbies;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace UI.MainMenu
{
    public class GameModeMenu : NetworkBehaviour
    {
        private List<RegionPoint> _regionPointObjects = new List<RegionPoint>();

        [SerializeField] private Transform regionPointsTransform;
        [SerializeField] private GameObject regionPointPrefab;

        [SerializeField] private GameObject missionManagerPrefab;
        [Header("Mission Window")] [SerializeField]
        private AudioClip onClickSound;

        [SerializeField] private Button closingBackground;
        [SerializeField] private GameObject windowPanel;
        [SerializeField] private TMP_Text regionNameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text enemyTypeText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button multiplayerModeButton;
        [SerializeField] private Button startMultiplayerButton;

        private RegionPointsSO _selectedRegionPointsSo;

        private void Awake()
        {
            RegionPointsSO[] regionPointsSos = Resources.LoadAll<RegionPointsSO>("ScriptableObjects/UI/Main Menu/");
            foreach (var regionPointsSo in regionPointsSos)
            {
                CreateRegionPoint(regionPointsSo);
            }

            closingBackground.onClick.AddListener(WindowManager.Close);
            startMultiplayerButton.gameObject.SetActive(false);
        }

        public void CreatedSession()
        {
            startMultiplayerButton.gameObject.SetActive(true);
            startMultiplayerButton.onClick.RemoveAllListeners();
            startMultiplayerButton.onClick.AddListener(() => StartSession(true));
            SetMission(_selectedRegionPointsSo);
        }

        private void CreateRegionPoint(RegionPointsSO regionPointsSo)
        {
            GameObject regionPoint = Instantiate(regionPointPrefab, regionPointsTransform);
            RectTransform regionPointTransform = regionPoint.GetComponent<RectTransform>();
            regionPointTransform.anchoredPosition = regionPointsSo.position;
            RegionPoint regionPointComponent = regionPoint.GetComponent<RegionPoint>();
            regionPointComponent.Init(regionPointsSo);
            regionPointComponent.SetClick((rp) =>
            {
                SetWindow(rp);
                WindowManager.Open(OpenWindow, CloseWindow);
            });
            _regionPointObjects.Add(regionPointComponent);
        }

        private void OpenWindow()
        {
            windowPanel.SetActive(true);
        }

        private void OpenWindow(RegionPointsSO regionPointsSo)
        {
            SetWindow(regionPointsSo);
            windowPanel.SetActive(true);
        }

        private void SetWindow(RegionPointsSO regionPointsSo)
        {
            regionNameText.text = regionPointsSo.regionName;
            descriptionText.text = regionPointsSo.description;
            enemyTypeText.text = regionPointsSo.enemyType.ToString();
            startButton.onClick.RemoveAllListeners();
            _selectedRegionPointsSo = regionPointsSo;
            startButton.onClick.AddListener(() => StartSession());
            multiplayerModeButton.gameObject.SetActive(regionPointsSo.isMultiplayer);
        }

        private void CloseWindow()
        {
            windowPanel.SetActive(false);
        }

        public void StartSession(bool isMultiplayer = false)
        {
            if (!isMultiplayer)
                NetworkManager.StartHost();
            // SendUserInfoToLobby();
            startButton.interactable = false;
            startMultiplayerButton.interactable = false;
            SoundManager.PlayOneShot(onClickSound);
            // notify all clients and the host to trigger the scene change
            Debug.Log("Creating missionManager");
            var missionManager = Instantiate(missionManagerPrefab);
            DontDestroyOnLoad(missionManager);
            missionManager.GetComponent<NetworkObject>().Spawn();
            missionManager.GetComponent<MissionManager>().missionName.Value = _selectedRegionPointsSo.name;
            
            //AnalyticsService.Instance.CustomData("levelStarted", new Dictionary<string, object>()
            //{
            //    {"levelName", _selectedRegionPointsSo.regionName}
            //});
        }
        async void SetMission(RegionPointsSO mission)
        {
            if (!IsHost)
                return;
            Debug.Log("Set mission for lobby");
            var lobbyIds = await LobbyService.Instance.GetJoinedLobbiesAsync();
            Debug.Assert(lobbyIds.Count == 1, "expected only one lobby, not parties implemented yet");

            UpdateLobbyOptions options = new UpdateLobbyOptions();
            options.HostId = AuthenticationService.Instance.PlayerId;


            options.Data = new Dictionary<string, DataObject>()
            {
                {
                    "missionName", new DataObject(visibility: DataObject.VisibilityOptions.Public,
                            mission.name)
                }
            };
            await LobbyService.Instance.UpdateLobbyAsync(lobbyIds[0], options);
        }

        // async void SendUserInfoToLobby()
        // {
        //     try
        //     {
        //         UpdatePlayerOptions options = new UpdatePlayerOptions();

        //         options.Data = new Dictionary<string, PlayerDataObject>()
        //         {
        //             {
        //                 "username", new PlayerDataObject(
        //                     visibility: PlayerDataObject.VisibilityOptions.Public,
        //                     value: AuthManager.PlayerName)
        //             }
        //         };

        //         string playerId = AuthenticationService.Instance.PlayerId;

        //         var lobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, options);
        //         Debug.Log("Sent userinfo to lobby");
        //     }
        //     catch (LobbyServiceException e)
        //     {
        //         Debug.Log(e);
        //     }
        // }
    }
}