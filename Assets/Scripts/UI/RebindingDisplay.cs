using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects.Keybinds;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RebindingDisplay : MonoBehaviour
{
    private Keybind _keybind;
    
    public Keybind Keybind
    {
        get => _keybind;
            
         set
         {
             _keybind = value;
             Init();
         }
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text bindingDisplayName;
    [SerializeField] private GameObject startRebindButton;
    [SerializeField] private TMP_Text inputNameText;
    [SerializeField] private TMP_Text inputDescriptionText;
    [SerializeField] private GameObject helperBox;


    public void Init()
    {
        inputNameText.text = Keybind.Name;
        if (Keybind.Count > 0)
            UpdateKeyText();
    }

    private void UpdateKeyText()
    {
        bindingDisplayName.text = InputControlPath.ToHumanReadableString(Keybind.EffectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
        inputDescriptionText.text = Keybind.description;
    }
    
    public void StartRebind()
    {
        EventManager.TriggerEvent("RebindStarted", true);
        startRebindButton.SetActive(false);
        Keybind.inputActionReference.action.PerformInteractiveRebinding().WithCancelingThrough("Escape").OnCancel(operation =>
        {
            EventManager.TriggerEvent("RebindStarted", false);
            startRebindButton.SetActive(true);
        }).OnMatchWaitForAnother(0.1f).OnComplete(operation =>
        {
            UpdateKeyText();
            EventManager.TriggerEvent(Constants.TypedEvents.RebindKey + Keybind.Name, Keybind);
            startRebindButton.SetActive(true);
            operation.Dispose();
            EventManager.TriggerEvent("RebindStarted", false);
        }).Start();
    }
}
