using DefaultNamespace;
using Gameplay;
using ScriptableObjects.UI;
using UI;
using UI.MainMenu;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The InputManager catches the input events of the player
/// and transmit them to the whole codebase by triggering their respective events.
///
/// By doing this, it avoids having to use the InputSystem in every class that needs it.
/// It removes dependencies and makes the code more readable.
///
/// Consider adding the new input to this class
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class InputManager : Singleton<InputManager>
{
    private DeathData _deathData;
    private PlayerInput _playerInput;
    private CursorSO _cursorSo;
    protected override void OnAwake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _cursorSo = Resources.Load<CursorSO>("ScriptableObjects/UI/Cursor/Cursor");
        var keybindsSave = PlayerPrefs.GetString("keybinds");
        if (keybindsSave != "")
            _playerInput.actions.LoadBindingOverridesFromJson(keybindsSave);
    }

    private void OnEnable()
    {
        EventManager.AddListener("OnDeath", OnDeath);
        EventManager.AddListener("RebindStarted", OnRebindStarted);
        EventManager.AddListener("DeleteSave", OnDeleteSave);
        EventManager.AddListener("OnResume", OnResumeFollow);
        EventManager.AddListener(Constants.Events.Session.ReturnToMainMenu, OnReturnToMainMenu);
    }


    private void OnDisable()
    {
        EventManager.RemoveListener("OnDeath", OnDeath);
        EventManager.RemoveListener("RebindStarted", OnRebindStarted);
        EventManager.RemoveListener("DeleteSave", OnDeleteSave);
        EventManager.RemoveListener("OnResume", OnResumeFollow);
        EventManager.RemoveListener(Constants.Events.Session.ReturnToMainMenu, OnReturnToMainMenu);
    }

    #region Juggernaut Action Map

    private void OnMove(InputValue inputValue)
    {
        var data = inputValue.Get<Vector2>();
        EventManager.TriggerEvent("OnMove", data);
    }

    private void OnRun(InputValue inputValue)
    {
        EventManager.TriggerEvent("OnRun", inputValue.isPressed);
    }
    
    
    private void OnJump(InputValue inputValue)
    {
        Debug.Log("OnJump");
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.OnJump, inputValue.isPressed);
    }

    private void OnPrimaryFire(InputValue inputValue)
    {
        if (_isOrderingScavenger)
        {
            _isOrderingScavenger = false;
            EventManager.TriggerEvent("OnOrderScavengerSubmit");
            return;
        }
        EventManager.TriggerEvent("OnPrimaryFire");
    }

    private void OnSecondaryFire(InputValue inputValue)
    {
        EventManager.TriggerEvent("OnSecondaryFire");
    }

    private void OnLookAround(InputValue inputValue)
    {
        var data = inputValue.Get<Vector2>();
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.OnLookAround, data);
    }

    private void OnZoomIn(InputValue inputValue)
    {
        var data = inputValue.Get<float>();
        if (data > 0)
        {
            Debug.Log("OnZoomIn");
            EventManager.TriggerEvent("OnZoomIn", data);
        }
    }
        
    private void OnZoomOut(InputValue inputValue)
    {
        var data = inputValue.Get<float>();
        if (data < 0)
            EventManager.TriggerEvent("OnZoomOut", data);
        //EventManager.TriggerEvent("OnZoomOut", data);
    }

    private void OnPause()
    {
        if (_pauseCd)
            return;
        EventManager.TriggerEvent("OnPause");
        _playerInput.SwitchCurrentActionMap("PauseMenu");
        _pauseCd = true;
        Invoke(nameof(ResetPauseCd), _pauseDelay);
    }
    
    private void OnGrapplingThrow(InputValue inputValue)
    {
        EventManager.TriggerEvent("OnGrapplingThrow", inputValue.isPressed);
    }
    
    private void OnReload(InputValue inputValue)
    {
        EventManager.TriggerEvent("OnReload");
    }

    private void OnFreeLook(InputValue inputValue)
    {
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.OnFreeLook, inputValue.isPressed);
    }

    private void OnChangeView(InputValue inputValue)
    {
        EventManager.TriggerEvent(Constants.Events.Inputs.OnChangeView);
    }

    private void OnToggleMap(InputValue inputValue)
    {
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.OnToggleMap, inputValue.isPressed);
    }
    
    private void OnToggleObjective(InputValue inputValue)
    {
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.OnToggleObjective, inputValue.isPressed);
    }
    
    private void OnToggleHUD(InputValue inputValue)
    {
        EventManager.TriggerEvent(Constants.Events.Inputs.OnToggleHUD);
    }
    
    private void OnPhotoMode(InputValue inputValue)
    {
        SwitchCurrentActionMap("FreeCamera");
        EventManager.TriggerEvent(Constants.Events.Inputs.OnToggleHUD, false);
        EventManager.TriggerEvent(Constants.Events.Inputs.Juggernaut.OnPhotoMode);
        HealthBar.IsVisible = false;
    }

    #region Scavenger Inputs

    private void OnCallScavenger(InputValue inputValue)
    {
        EventManager.TriggerEvent("OnCallScavenger");
    }
    
    private void OnStopScavenger(InputValue inputValue)
    {
        EventManager.TriggerEvent("OnStopScavenger");
    }
    
    private bool _isOrderingScavenger = false;
    private void OnOrderScavenger(InputValue inputValue)
    {
        _isOrderingScavenger = !_isOrderingScavenger;
        EventManager.TriggerEvent("OnOrderScavenger");
    }


    #endregion

    private bool _isEditingHUD = false;
    private void OnEditHUD(InputValue inputValue)
    {
        return; // disabled for now
        _isEditingHUD = !_isEditingHUD;
        if (_isEditingHUD)
        {
            Cursor.lockState = CursorLockMode.Confined;
            _playerInput.SwitchCurrentActionMap("HUDEdit");
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            _playerInput.SwitchCurrentActionMap("Juggernaut");
            Cursor.SetCursor(_cursorSo.defaultTexture, Vector2.zero, CursorMode.Auto);
        }

        EventManager.TriggerEvent("OnHUDEdit", _isEditingHUD);
    }
        
    #endregion
    
    private void ResetPauseCd()
    {
        _pauseCd = false;
    }

    #region Pause Action Map
        
    [SerializeField] private float _pauseDelay = 0.2f;
    private bool _pauseCd = false;
    public void OnResume()
    {
        EventManager.TriggerEvent("OnResume");
    }
    
    private void OnResumeFollow()
    {
        WindowManager.Close();
        if (WindowManager.WindowOpenedCount > 0)
            return;
        _playerInput.SwitchCurrentActionMap("Juggernaut");
    }

    private void OnReturnToMainMenu()
    {
        _playerInput.SwitchCurrentActionMap("MainMenu");
    }
    #endregion

    #region Death Action Map

    private void OnRespawn()
    {
        var respawnData = new RespawnData
        {
            DeathPosition = _deathData.DeathPosition,
        };
        
        _playerInput.SwitchCurrentActionMap("Juggernaut");
        EventManager.TriggerEvent("OnRespawn", respawnData);
    }
        

    #endregion

    private void OnDeath(object deathData)
    {
        _deathData = (DeathData) deathData;
        if (_isEditingHUD)
            OnEditHUD(null);
        _playerInput.SwitchCurrentActionMap("Death");
    }
    
    private void OnRebindStarted(object started)
    {
        if ((bool)started)
            _playerInput.SwitchCurrentActionMap("Rebinding");
        else
        {
            var keybindsSave = _playerInput.actions.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString("keybinds", keybindsSave);
            _playerInput.SwitchCurrentActionMap("PauseMenu");
        }
    }

    private void OnDeleteSave(object obj)
    {
        switch ((string)obj)
        {
            case "Inputs":
                PlayerPrefs.DeleteKey("keybinds");
                _playerInput.actions.RemoveAllBindingOverrides();
                break;
        }
    }

    #region FreeCamera
    
    private void OnMoveFreeCamera(InputValue inputValue)
    {
        var data = inputValue.Get<Vector2>();
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.FreeCamera.OnMoveFreeCamera, data);
    }
    
    private void OnSpeedFreeCamera(InputValue inputValue)
    {
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.FreeCamera.OnSpeedFreeCamera, inputValue.isPressed);
    }
    
    private void OnLookAroundFreeCamera(InputValue inputValue)
    {
        var data = inputValue.Get<Vector2>();
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.FreeCamera.OnLookAroundFreeCamera, data);
    }
    
    private void OnGoDownFreeCamera(InputValue inputValue)
    {
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.FreeCamera.OnGoDownFreeCamera, inputValue.isPressed);
    }
    
    private void OnGoUpFreeCamera(InputValue inputValue)
    {
        EventManager.TriggerEvent(Constants.TypedEvents.Inputs.FreeCamera.OnGoUpFreeCamera, inputValue.isPressed);
    }

    private void OnExitPhotoMode(InputValue inputValue)
    {
        SwitchCurrentActionMap("Juggernaut");
        EventManager.TriggerEvent(Constants.Events.Inputs.OnToggleHUD, true);
        EventManager.TriggerEvent(Constants.Events.Inputs.FreeCamera.OnExitPhotoMode);
        HealthBar.IsVisible = true;
    }

    #endregion

    #region MainMenu Action Map

    private void OnClose()
    {
        EventManager.TriggerEvent("OnClose");
        if (WindowManager.WindowOpenedCount > 0)
            WindowManager.Close();
        else
            MainMenuManager.Instance.ToMainMenu();
    }
    
    private void OnToMainMenu()
    {
        EventManager.TriggerEvent("OnToMainMenu");
        MainMenuManager.Instance.ToMainMenu();
    }
    
    private void OnToSettingsMode()
    {
        EventManager.TriggerEvent("OnToSettings");
        MainMenuManager.Instance.ToSettingsMode();
    }
    
    private void OnToGameMode()
    {
        EventManager.TriggerEvent("OnToGameMode");
        MainMenuManager.Instance.ToGameMode();
    }

    #endregion

    public static void SwitchCurrentActionMap(string actionMap)
    {
        Instance._playerInput.SwitchCurrentActionMap(actionMap);
    }

}