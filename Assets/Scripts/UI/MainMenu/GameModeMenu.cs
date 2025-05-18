using System;
using System.Collections.Generic;
using DefaultNamespace.Sound;
using Gameplay;
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

namespace UI.MainMenu
{
    public class GameModeMenu : NetworkBehaviour
    {
        private List<RegionPoint> _regionPointObjects = new List<RegionPoint>();

        [SerializeField] private Transform regionPointsTransform;
        [SerializeField] private GameObject regionPointPrefab;

        [Header("Mission Window")] [SerializeField]
        private AudioClip onClickSound;

        [SerializeField] private Button closingBackground;
        [SerializeField] private GameObject windowPanel;
        [SerializeField] private TMP_Text regionNameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text enemyTypeText;
        [SerializeField] private Button startButton;
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
            NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
            {
                startMultiplayerButton.gameObject.SetActive(true);
                startMultiplayerButton.onClick.RemoveAllListeners();
                startMultiplayerButton.onClick.AddListener(() => StartSession(true));
            };
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
        }

        private void CloseWindow()
        {
            windowPanel.SetActive(false);
        }

        public void StartSession(bool isMultiplayer = false)
        {
            startButton.interactable = false;
            startMultiplayerButton.interactable = false;
            SoundManager.PlayOneShot(onClickSound);
            // notify all clients and the host to trigger the scene change
            SceneHandler.LoadScene(_selectedRegionPointsSo.scene, _selectedRegionPointsSo, isMultiplayer);
            StartSessionRpc(new MissionData()
            {
                missionName = _selectedRegionPointsSo.name
            });
            //AnalyticsService.Instance.CustomData("levelStarted", new Dictionary<string, object>()
            //{
            //    {"levelName", _selectedRegionPointsSo.regionName}
            //});
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void StartSessionRpc(MissionData missionData)
        {
            RegionPointsSO regionPointsSo = Resources.Load<RegionPointsSO>($"ScriptableObjects/UI/Main Menu/{missionData.missionName}");
            Debug.Log("StartSessionRpc " + regionPointsSo);
            Debug.Log("SceneData " + regionPointsSo.scene);
            GameManager.Mission = regionPointsSo;
            NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManagerOnOnLoadComplete;
        }

        private void SceneManagerOnOnLoadComplete(ulong clientid, string scenename, LoadSceneMode loadscenemode)
        {
            if (scenename == "LoadingScene") return;
            if (clientid != NetworkManager.Singleton.LocalClientId) return;
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= SceneManagerOnOnLoadComplete;
            Cursor.lockState = _selectedRegionPointsSo.scene.cursorLockMode;
            if (_selectedRegionPointsSo.scene.inputActionMap != "")
                InputManager.SwitchCurrentActionMap(_selectedRegionPointsSo.scene.inputActionMap);
            DataHandler.LoadGameData();
            EventManager.TriggerEvent(Constants.TypedEvents.OnSceneLoadingCompleted, _selectedRegionPointsSo);
            FindAnyObjectByType<RespawnManager>().SpawnPlayer(clientid);
            
            Debug.Log("Scene loaded");
        }
    }
}