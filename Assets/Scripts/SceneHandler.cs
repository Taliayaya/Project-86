using System;
using System.Collections;
using Gameplay;
using Networking;
using Networking.Widgets.Session.Session;
using ScriptableObjects;
using ScriptableObjects.GameParameters;
using ScriptableObjects.UI;
using UI;
using UI.MainMenu;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHandler : Singleton<SceneHandler>
{
    [SerializeField] private GraphicsParameters graphicsParameters;
    [SerializeField] private GameObject loadingScreenPrefab;
    
    [SerializeField] private bool loadMainMenuOnStart = true;
    private static SceneData _activeSceneData;
    private Scene? _currentScene;
    private Scene _globalScene;

    private bool _init;
    private static bool _preloaded;
    

    private void OnEnable()
    {
        Debug.Log("[SceneHandler] OnEnable");
        EventManager.AddListener(Constants.TypedEvents.LoadingScene, OnLoadingScene);
        EventManager.AddListener($"UpdateGameParameter:{nameof(graphicsParameters.detailsDensity)}", UpdateGrassDensity);
        EventManager.AddListener("ReloadScene", ReloadScene);
        EventManager.AddListener("Play", OnPlay);
        EventManager.AddListener(Constants.Events.OnLeavingSession, OnLeavingSession);
        if (!_preloaded)
        {
            _preloaded = true;
            var loadingScreen = Instantiate(loadingScreenPrefab);
            DontDestroyOnLoad(loadingScreen);
        }

        if (_init)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= Instance.OnSceneEvent;
            NetworkManager.Singleton.SceneManager.OnSceneEvent += Instance.OnSceneEvent;
            NetworkManager.Singleton.SceneManager.PostSynchronizationSceneUnloading = true;
        }

    }

    private void Start()
    {
        if (loadMainMenuOnStart)
        {
            Debug.Log("[SceneHandler] Loading Main Menu");
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void OnLeavingSession()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= LateUserSpawn;
    }

    private void OnDisable()
    {
        if (this != Instance)
            return;
        Debug.Log("[SceneHandler] OnDisable");
        EventManager.RemoveListener("LoadingScene", OnLoadingScene);
        EventManager.RemoveListener($"UpdateGameParameter:{nameof(graphicsParameters.detailsDensity)}", UpdateGrassDensity);
        EventManager.RemoveListener("ReloadScene", ReloadScene);
        EventManager.RemoveListener("Play", OnPlay);
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsConnectedClient)
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
    }

    public static void Init()
    {
        if (Instance._init)
            return;
        Debug.Log("[SceneHandler] Init");
        NetworkManager.Singleton.SceneManager.OnSceneEvent += Instance.OnSceneEvent;
        NetworkManager.Singleton.SceneManager.PostSynchronizationSceneUnloading = true;
        // NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
        // NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
        // SceneManager.sceneLoaded += Instance.OnSinglePlayerLoad;
        Instance._init = true;
    }

    private void OnSinglePlayerLoad(Scene arg0, LoadSceneMode arg1)
    {
        StartCoroutine(PostSceneLoad(null, setActiveScene: false));
    }

    private bool _isReload;
    
    public void LoadGameScene(string newSceneName)
    {
        Init();
        Debug.Log($"Loading scene {newSceneName}");
        WindowManager.CloseAll();
        ShowLoadingUI();
        
        NetworkManager.Singleton.SceneManager.OnSceneEvent -= Instance.OnSceneEvent;
        NetworkManager.Singleton.SceneManager.OnSceneEvent += Instance.OnSceneEvent;
        if (!NetworkManager.Singleton.IsListening)
        {
            _loadingAsyncOperation = SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Single);
            EventManager.TriggerEvent(Constants.TypedEvents.LoadingScene, (_activeSceneData, _loadingAsyncOperation));
        }

        if (NetworkManager.Singleton.IsHost)
        {
            LoadSceneMode mode = _isReload ? LoadSceneMode.Single : LoadSceneMode.Additive;
            NetworkManager.Singleton.SceneManager.LoadScene(newSceneName, mode);
        }
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.Load:
                Debug.Log($"Loading scene {sceneEvent.SceneName}");
                WindowManager.CloseAll();
                _loadingAsyncOperation = sceneEvent.AsyncOperation;
                ShowLoadingUI();
                DisableSceneObjects(SceneManager.GetActiveScene());
                EventManager.TriggerEvent(Constants.TypedEvents.LoadingScene, (_activeSceneData, _loadingAsyncOperation));
                break;
            case SceneEventType.LoadComplete:
                // only handle ourselves here
                if (sceneEvent.ClientId != NetworkManager.Singleton.LocalClientId)
                    return;
                if (_currentScene.HasValue && _currentScene.Value.name == sceneEvent.SceneName)
                    return;
                Debug.Log($"Scene {sceneEvent.SceneName} loaded.");
                _currentScene = sceneEvent.Scene;
                StartCoroutine(PostSceneLoad(sceneEvent));
                break;
            case SceneEventType.SynchronizeComplete:
                Debug.Log("Synchronize event complete");
                StartCoroutine(SynchronizePostLoad());
                break;
            case SceneEventType.Synchronize:
                Debug.Log("Synchronize event");
                break;
            
            case SceneEventType.UnloadComplete:
                if (_activeSceneData && sceneEvent.SceneName == _activeSceneData.SceneName)
                {
                    Debug.Log($"Scene {sceneEvent.SceneName} unloaded.");
                    _currentScene = null;
                }
                break;
        }
        
    }

    void DisableSceneObjects(Scene scene)
    {
        foreach (GameObject rootObj in scene.GetRootGameObjects())
        {
            // Disable all root objects (and their children)
            rootObj.SetActive(false);
        }
    }

    void PreviousSceneCleanup()
    {
        var activeScene = SceneManager.GetActiveScene();
        Debug.Log($"Unloading scene {activeScene.name}");
        NetworkManager.Singleton.SceneManager.UnloadScene(activeScene);
    }

    IEnumerator SynchronizePostLoad()
    {
        yield return null;
        var terrains =
            GameObject.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        ApplyGrassSettings(terrains);
        yield return null;
        Debug.Log("Client is waiting for its player object to spawn");
        yield return null;
        OnPlay();
    }

    public static IEnumerator SpawnPlayers()
    {
        if (!NetworkManager.Singleton.IsHost)
            yield break;
        Debug.Log("Host is spawning itself");
        var respawnManager = FindAnyObjectByType<RespawnManager>();
        var playerObject = respawnManager.SpawnPlayer(NetworkManager.Singleton.LocalClientId);
        PlayerManager.PlayerObjects[NetworkManager.Singleton.LocalClientId] = playerObject;

        Debug.Log("Auto spawning for late users");
        NetworkManager.Singleton.OnClientConnectedCallback -= LateUserSpawn;
        NetworkManager.Singleton.OnClientConnectedCallback += LateUserSpawn;
    }

    IEnumerator PostSceneLoad(SceneEvent sceneEvent, bool setActiveScene = true)
    {
        if (!_isReload)
            PreviousSceneCleanup();
        yield return null;
        
        var terrains =
            GameObject.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        ApplyGrassSettings(terrains);
        yield return null;
        
        // SceneManager.SetActiveScene(sceneEvent.Scene);
        if (setActiveScene)
        {
            var scene = SceneManager.GetSceneByName(sceneEvent.SceneName);
            while (!scene.IsValid())
            {
                Debug.Log($"Scene {scene.name} is not valid and loaded={scene.isLoaded}.");
                scene = SceneManager.GetSceneByName(sceneEvent.SceneName);
                yield return null;
            }

            SceneManager.SetActiveScene(scene);
        }

        yield return null;

        yield return new WaitUntil(() =>
        {
            Debug.Log($"Waiting for GameManager.Mission...");
            return GameManager.Mission;
        });
        // Debug.Log($"Starting LoadingSceneCallback");
        // LoadingSceneCallback();
        yield return null;
        if (NetworkManager.Singleton.IsHost)
        {
            yield return SpawnPlayers();
        }
        else
        {
            Debug.Log("Client is waiting for its player object to spawn");
            var respawnManager = FindAnyObjectByType<RespawnManager>();
            // Debug.Log("Host is spawning player " + .ClientId);
            var playerObject = respawnManager.SpawnPlayer(sceneEvent.ClientId);
            PlayerManager.PlayerObjects[sceneEvent.ClientId] = playerObject;
        }
        yield return null;
        OnPlay();

        // HideLoadingUI();
    }

    public static void ShowLoadingUI()
    {
        EventManager.TriggerEvent("LoadingLoadingScene");
        EventManager.TriggerEvent("DisplayLoadingScreen", true);
    }

    public static void HideLoadingUI()
    {
        EventManager.TriggerEvent("DisplayLoadingScreen", false);
    }

    private void OnPlay()
    {
        Debug.Log("OnPlay");
        Cursor.lockState = GameManager.Mission.scene.cursorLockMode;
        if (GameManager.Mission.scene.inputActionMap != "")
            InputManager.SwitchCurrentActionMap(GameManager.Mission.scene.inputActionMap);
        DataHandler.LoadGameData();
        EventManager.TriggerEvent(Constants.TypedEvents.OnSceneLoadingCompleted, GameManager.Mission);
        HideLoadingUI();
    }

    public static void ReloadScene()
    {
        if (!NetworkManager.Singleton.IsHost)
            return;
        MissionManager.Instance.ReloadMission();
    }
    
    private static Action onLoaderCallback;
    private static AsyncOperation _loadingAsyncOperation;
    
    public static void LoadScene(SceneData sceneData, bool isReload = false)
    {
        Instance._isReload = isReload;
        _activeSceneData = sceneData;
        Instance.LoadGameScene(sceneData.SceneName);
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

    private static void LateUserSpawn(ulong clientId)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            var respawnManager = FindAnyObjectByType<RespawnManager>();
            PlayerManager.PlayerObjects[clientId] = respawnManager.SpawnPlayer(clientId);
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
