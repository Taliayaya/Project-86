using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuKeybinds : MonoBehaviour
{
    [SerializeField] private GameObject inputRebindingPrefab;
    [SerializeField] private GameObject keybindsParent;
    
    
    [Header("Rebindable Actions")]
    [SerializeField] private List<InputActionReference> actionReferences;

    // Start is called before the first frame update

    public void GenerateKeybinds()
    {
        foreach (Transform c in keybindsParent.transform)
        {
            Destroy(c.gameObject);
        }
        foreach (InputActionReference inputRef in actionReferences)
        {
            
            if (inputRef.action.bindings.Count > 0)
            {
                GameObject rebindDisplay = Instantiate(inputRebindingPrefab, keybindsParent.transform);
                rebindDisplay.GetComponent<RebindingDisplay>().ActionReference = inputRef;
            }
        }
        
    }

}
