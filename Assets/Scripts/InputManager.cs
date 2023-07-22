using DefaultNamespace;
using Gameplay;
using ScriptableObjects.UI;
using UI;
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
    }
        
    private void OnDisable()
    {
        EventManager.RemoveListener("OnDeath", OnDeath);
        EventManager.RemoveListener("RebindStarted", OnRebindStarted);
        EventManager.RemoveListener("DeleteSave", OnDeleteSave);
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
        EventManager.TriggerEvent("OnLookAround", data);
    }

    private void OnZoomIn(InputValue inputValue)
    {
        var data = inputValue.Get<float>();
        if (data > 0)
            EventManager.TriggerEvent("OnZoomIn", data);
    }
        
    private void OnZoomOut(InputValue inputValue)
    {
        var data = inputValue.Get<float>();
        Debug.Log(data);
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
        WindowManager.Close();
        if (WindowManager.WindowOpenedCount > 0)
            return;
        Debug.Log("OnResume");

        EventManager.TriggerEvent("OnResume");
        _playerInput.SwitchCurrentActionMap("Juggernaut");
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
        
}