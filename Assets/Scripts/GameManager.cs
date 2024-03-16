using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using ScriptableObjects.GameParameters;
using ScriptableObjects.UI;
using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Analytics;

/// <summary>
/// Game Manager is the main class of the game. It is a singleton that is not destroyed on load.
/// It is the main brain of the game, and it is responsible for the game flow.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    public static bool GameIsPaused { get; private set; } = false;
    private RegionPointsSO _mission;
    public static RegionPointsSO Mission
    {
        get => Instance._mission;
        set => Instance._mission = value;
    }

    #region Unity Callbacks

    protected override void OnAwake()
    {
        Debug.Log("[GameManager] Awake");
        GameParameters[] gameParametersArray = Resources.LoadAll<GameParameters>("ScriptableObjects/Parameters");
        foreach (var parameter in gameParametersArray)
        {
            Debug.Log("[GameManager] Parameter: " + parameter.GetParametersName);
        }
        DataHandler.LoadGameData();
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        
        //Cursor.lockState = CursorLockMode.Locked;

        Application.quitting += () => EventManager.TriggerEvent("OnApplicationQuit");
    }

    private void OnEnable()
    {
        EventManager.AddListener("OnPause", OnPauseGame);
        EventManager.AddListener("OnResume", OnResumeGame);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("OnPause", OnPauseGame);
        EventManager.RemoveListener("OnResume", OnResumeGame);
    }

    #endregion

    #region Resume / Pause Game

    private void OnPauseGame()
    {
        GameIsPaused = true;
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void Pause(bool pause)
    {
        GameIsPaused = pause;
        Time.timeScale = pause ? 0 : 1;
    }

    private void OnResumeGame()
    {
        GameIsPaused = false;
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
    }

    #endregion

    protected override void OnApplicationQuitting()
    {
#if !UNITY_EDITOR
        DataHandler.SaveGameData();
#endif
    }
    
    
}
