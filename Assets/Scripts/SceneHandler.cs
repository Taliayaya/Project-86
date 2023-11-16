using System;
using System.Collections;
using ScriptableObjects;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : Singleton<SceneHandler>
{
    private class LoadingMonoBehaviour : MonoBehaviour
    {
    }
    public static void ReloadScene()
    {
        WindowManager.CloseAll();
        EventManager.TriggerEvent("ReloadScene");
        EventManager.TriggerEvent("OnResume");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    private static Action onLoaderCallback;
    private static AsyncOperation _loadingAsyncOperation;

    public static void LoadScene(SceneData sceneData)
    {
        WindowManager.CloseAll();
        onLoaderCallback = () =>
        {
            GameObject loader = new GameObject("Loader");
            loader.AddComponent<LoadingMonoBehaviour>().StartCoroutine(LoadSceneAsync(sceneData));
        };
        EventManager.TriggerEvent("LoadingLoadingScene");
        SceneManager.LoadScene("LoadingScene");
    }
    
    private static IEnumerator LoadSceneAsync(SceneData sceneData)
    {
        yield return null;
        _loadingAsyncOperation = SceneManager.LoadSceneAsync(sceneData.SceneName);
        EventManager.TriggerEvent("LoadingScene", (sceneData, _loadingAsyncOperation));
        _loadingAsyncOperation.completed += operation =>
        {
            Cursor.lockState = sceneData.cursorLockMode;
            if (sceneData.inputActionMap != "")
                InputManager.SwitchCurrentActionMap(sceneData.inputActionMap);

            _loadingAsyncOperation = null;
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
}