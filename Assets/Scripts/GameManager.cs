using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using ScriptableObjects.GameParameters;
using UnityEngine;

/// <summary>
/// Game Manager is the main class of the game. It is a singleton that is not destroyed on load.
/// It is the main brain of the game, and it is responsible for the game flow.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    public static bool GameIsPaused { get; private set; } = false;

    #region Unity Callbacks

    protected override void OnAwake()
    {
        
        GameParameters[] gameParametersArray = Resources.LoadAll<GameParameters>("ScriptableObjects/Parameters");
        foreach (var parameter in gameParametersArray)
        {
            Debug.Log("[GameManager] Parameter: " + parameter.GetParametersName);
        }
    }

    private void OnEnable()
    {
        EventManager.AddListener("PauseGame", OnPauseGame);
        EventManager.AddListener("ResumeGame", OnResumeGame);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("PauseGame", OnPauseGame);
        EventManager.RemoveListener("ResumeGame", OnResumeGame);
    }

    #endregion

    #region Resume / Pause Game

    private void OnPauseGame()
    {
        GameIsPaused = true;
        Time.timeScale = 0;
    }

    private void OnResumeGame()
    {
        GameIsPaused = false;
        Time.timeScale = 1;
    }

    #endregion

    protected override void OnApplicationQuitting()
    {
#if !UNITY_EDITOR
        DataHandler.SaveGameData();
#endif
    }
}
