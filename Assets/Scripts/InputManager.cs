using DefaultNamespace;
using Gameplay;
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
    protected override void OnAwake()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        EventManager.AddListener("OnDeath", OnDeath);
    }
        
    private void OnDisable()
    {
        EventManager.RemoveListener("OnDeath", OnDeath);
    }

    #region Juggernaut Action Map

    private void OnMove(InputValue inputValue)
    {
        var data = inputValue.Get<Vector2>();
        EventManager.TriggerEvent("OnMove", data);
    }
        
    private void OnPrimaryFire(InputValue inputValue)
    {
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
        EventManager.TriggerEvent("OnZoomIn", data);
    }
        
    private void OnZoomOut(InputValue inputValue)
    {
        var data = inputValue.Get<float>();
        EventManager.TriggerEvent("OnZoomOut", data);
    }

    private void OnPause()
    {
        EventManager.TriggerEvent("OnPause");
        _playerInput.SwitchCurrentActionMap("PauseMenu");
    }
        
    #endregion

    #region Pause Action Map
        

    private void OnResume()
    {
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
        
}