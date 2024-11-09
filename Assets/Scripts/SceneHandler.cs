﻿using System;
using System.Collections;
using ScriptableObjects;
using ScriptableObjects.GameParameters;
using ScriptableObjects.UI;
using UI;
using UI.MainMenu;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : Singleton<SceneHandler>
{
    [SerializeField] private GraphicsParameters graphicsParameters;
    private static SceneData _activeSceneData;
    private class LoadingMonoBehaviour : MonoBehaviour
    {
    }

    private void OnEnable()
    {
        EventManager.AddListener("LoadingScene", OnLoadingScene);
        EventManager.AddListener($"UpdateGameParameter:{nameof(graphicsParameters.detailsDensity)}", UpdateGrassDensity);
        EventManager.AddListener("ReloadScene", ReloadScene);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener("LoadingScene", OnLoadingScene);
        EventManager.RemoveListener($"UpdateGameParameter:{nameof(graphicsParameters.detailsDensity)}", UpdateGrassDensity);
        EventManager.RemoveListener("ReloadScene", ReloadScene);
    }

    public static void ReloadScene()
    {
        WindowManager.CloseAll();
        EventManager.TriggerEvent("ReloadScene");
        EventManager.TriggerEvent("OnResume");
        LoadScene(_activeSceneData, GameManager.Mission);
    }
    
    private static Action onLoaderCallback;
    private static AsyncOperation _loadingAsyncOperation;

    public static void LoadScene(SceneData sceneData, object dataOnComplete = null)
    {
        _activeSceneData = sceneData;
        WindowManager.CloseAll();
        onLoaderCallback = () =>
        {
            GameObject loader = new GameObject("Loader");
            loader.AddComponent<LoadingMonoBehaviour>().StartCoroutine(LoadSceneAsync(sceneData, dataOnComplete));
        };
        EventManager.TriggerEvent("LoadingLoadingScene");
        SceneManager.LoadScene("LoadingScene");
    }

    
    private static IEnumerator LoadSceneAsync(SceneData sceneData, object dataOnComplete = null)
    {
        yield return null;
        _loadingAsyncOperation = SceneManager.LoadSceneAsync(sceneData.SceneName);
        EventManager.TriggerEvent("LoadingScene", (sceneData, _loadingAsyncOperation));
        _loadingAsyncOperation.completed += operation =>
        {
            Cursor.lockState = sceneData.cursorLockMode;
            if (sceneData.inputActionMap != "")
                InputManager.SwitchCurrentActionMap(sceneData.inputActionMap);
            // this line was added to fix an issue where somehow loaded settings were reset to default after a scene change
            DataHandler.LoadGameData();

            _loadingAsyncOperation = null;
            EventManager.TriggerEvent(Constants.TypedEvents.OnSceneLoadingCompleted, dataOnComplete);
            Debug.Log("Scene loaded");
        };

        while (!_loadingAsyncOperation.isDone)
        {
            yield return null;
        }
        
        yield return null;
    }
    
    public static float LoadProgress()
    {
        if (_loadingAsyncOperation != null)
            return _loadingAsyncOperation.progress;
        return 1f;
    }
    
    public static void LoaderCallback()
    {
        if (onLoaderCallback != null)
        {
            onLoaderCallback();
            onLoaderCallback = null;
        }
    }

    private void OnLoadingScene(object data)
    {
        Debug.Log("LoadingScene");
        if (data is (SceneData sceneData, AsyncOperation asyncOperation))
        {
            asyncOperation.completed += operation =>
            {
                Debug.Log("LoadingScene completed");
                var terrains =
                    GameObject.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                ApplyGrassSettings(terrains);
            };
        }
    }

    private static void ApplyGrassSettings(Terrain[] terrains)
    {
        foreach (var terrain in terrains)
        {
            terrain.detailObjectDensity = Instance.graphicsParameters.detailsDensity / 100f;
        }
    }

    private void OnReloadScene()
    {
        Debug.Log("ReloadingScene");
        var terrains = GameObject.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        ApplyGrassSettings(terrains);
    }

    private void UpdateGrassDensity(object data)
    {
        var terrains = GameObject.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        ApplyGrassSettings(terrains);
    }
}