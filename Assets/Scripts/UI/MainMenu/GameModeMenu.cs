using System;
using System.Collections.Generic;
using DefaultNamespace.Sound;
using ScriptableObjects.UI;
using TMPro;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public class GameModeMenu : MonoBehaviour
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
        
        private void Awake()
        {
            RegionPointsSO[] regionPointsSos = Resources.LoadAll<RegionPointsSO>("ScriptableObjects/UI/Main Menu/");
            foreach (var regionPointsSo in regionPointsSos)
            {
                CreateRegionPoint(regionPointsSo);
            }
            closingBackground.onClick.AddListener(WindowManager.Close);
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
            startButton.onClick.AddListener(() =>
            {
                SoundManager.PlayOneShot(onClickSound);
                SceneHandler.LoadScene(regionPointsSo.scene, regionPointsSo);
                GameManager.Mission = regionPointsSo;
                AnalyticsService.Instance.CustomData("levelStarted", new Dictionary<string, object>()
                {
                    {"levelName", regionPointsSo.regionName}
                });
            });
        }

        private void CloseWindow()
        {
            windowPanel.SetActive(false);
        }
    }
}