using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class RebindingDisplay : MonoBehaviour
{
    private InputActionReference _actionReference;

    public InputActionReference ActionReference
    {
        get => _actionReference;
            
         set
         {
             _actionReference = value;
             Init();
         }
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text bindingDisplayName;
    [SerializeField] private GameObject startRebindButton;
    [SerializeField] private TMP_Text inputNameText;


    public void Init()
    {
        inputNameText.text = ActionReference.action.name;
        if (ActionReference.action.bindings.Count > 0)
            UpdateKeyText();
    }

    private void UpdateKeyText()
    {
        bindingDisplayName.text = InputControlPath.ToHumanReadableString(ActionReference.action.bindings[0].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }
    
    public void StartRebind()
    {
        EventManager.TriggerEvent("RebindStarted", true);
        startRebindButton.SetActive(false);
        ActionReference.action.PerformInteractiveRebinding().WithCancelingThrough("Escape").OnCancel(operation =>
        {
            EventManager.TriggerEvent("RebindStarted", false);
            startRebindButton.SetActive(true);
        }).OnMatchWaitForAnother(0.1f).OnComplete(operation =>
        {
            UpdateKeyText();
            startRebindButton.SetActive(true);
            operation.Dispose();
            EventManager.TriggerEvent("RebindStarted", false);
        }).Start();
    }
}
