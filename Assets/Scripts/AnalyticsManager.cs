using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ScriptableObjects.GameParameters;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;

public class AnalyticsManager : Singleton<AnalyticsManager>
{
    private DataCollectionParameters _dataCollectionParameters;

    public static DataCollectionParameters.DataCollectionAgreement Agreement =>
        Instance._dataCollectionParameters.agreement;

    protected override void OnAwake()
    {
        base.OnAwake();
        _dataCollectionParameters =
            Resources.Load<DataCollectionParameters>("ScriptableObjects/Parameters/DataCollectionParameters");
        Debug.Log("[AnalyticsManager] _dataCollectionParameters: " + _dataCollectionParameters.agreement);
        _dataCollectionParameters.LoadFromFile();
        Debug.Log("[AnalyticsManager] Awake" + _dataCollectionParameters.agreement);
    }

    private void Start()
    {
        Debug.Log("[AnalyticsManager] Start");
        StartCoroutine(InitializeAnalyticsAsync());
    }

    private void OnEnable()
    {
        EventManager.AddListener("UpdateGameParameter:agreement", OnUpdateAgreement);
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            RegisterAnalyticsEvents();
        }
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("UpdateGameParameter:agreement", OnUpdateAgreement);
        UnregisterAnalyticsEvents();
    }

    private IEnumerator InitializeAnalyticsAsync()
    {
        yield return null;
        yield return UnityServices.InitializeAsync();
        Debug.Log("[AnalyticsManager] UnityServices.State: " + UnityServices.State);
        UpdateDataCollection();
        UnregisterAnalyticsEvents(); 
        RegisterAnalyticsEvents();
    }

    private void OnUpdateAgreement(object data)
    {
        var agreement = (DataCollectionParameters.DataCollectionAgreement)data;
        Debug.Log("[AnalyticsManager] Agreement updated: " + agreement);
        Debug.Log("[AnalyticsManager] Agreement parameter" + _dataCollectionParameters.agreement);
        _dataCollectionParameters.agreement = agreement;
        _dataCollectionParameters.SaveToFile();
        _dataCollectionParameters.agreement = DataCollectionParameters.DataCollectionAgreement.NotAsked;
        _dataCollectionParameters.LoadFromFile();
        Debug.Log("[AnalyticsManager] Agreement saved" + _dataCollectionParameters.agreement);
        UpdateDataCollection();
    }

    public void UpdateDataCollection()
    {
        switch (_dataCollectionParameters.agreement)
        {
            case DataCollectionParameters.DataCollectionAgreement.Agreed:
            {
                AnalyticsService.Instance.StartDataCollection();
                Debug.Log("[AnalyticsManager] Data collection started");
            }
                break;
            case DataCollectionParameters.DataCollectionAgreement.Disagreed:
                try
                {
                    if (_dataCollectionParameters.deleteDataOnDisagreement)
                    {
                        AnalyticsService.Instance.RequestDataDeletion();
                        Debug.Log("[AnalyticsManager] Data deleted");
                    }

                    Debug.Log("[AnalyticsManager] Data collection stopped");
                    AnalyticsService.Instance.StopDataCollection();
                }
                catch (NotSupportedException e)
                {
                    // this should be fired if you are trying to stop data collection without starting it first
                }
                catch (Exception e)
                {
                    Debug.LogError("[AnalyticsManager] " + e);
                }

                break;
            case DataCollectionParameters.DataCollectionAgreement.NotAsked:
                Debug.Log("[AnalyticsManager] Data collection not asked");
                EventManager.TriggerEvent(Constants.Events.OnAskDataCollectionAgreement);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }



    }

    # region Analytics Events

    public void RegisterAnalyticsEvents()
    {
        EventManager.AddListener(Constants.Events.Analytics.LevelFinished, OnLevelFinished);
        EventManager.AddListener(Constants.Events.Analytics.QuestCompleted, OnQuestCompleted);
    }
    
    public void UnregisterAnalyticsEvents()
    {
        EventManager.RemoveListener(Constants.Events.Analytics.LevelFinished, OnLevelFinished);
        EventManager.RemoveListener(Constants.Events.Analytics.QuestCompleted, OnQuestCompleted);
    }

    void OnLevelFinished()
    {
        //AnalyticsService.Instance.CustomData("levelFinished");
    }
    
    void OnQuestCompleted(object data)
    {
        var questName = (string)data;
        //AnalyticsService.Instance.CustomData("questComplete", new Dictionary<string, object>()
        //{
        //    {"questName", questName},
        //});
    }

    #endregion
}