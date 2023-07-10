using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    public class MenuKeybinds : SettingsMenuCategory
    {
        [SerializeField] private GameObject inputRebindingPrefab;

        [Header("Rebindable Actions")]
        public List<InputActionReference> actionReferences;

        public override string MenuContentName => "Keybinds";
        // Start is called before the first frame update

        public void GenerateKeybinds(Transform contentParent)
        {
            foreach (InputActionReference inputRef in actionReferences)
            {
                if (inputRef.action.bindings.Count > 0)
                {
                    GameObject rebindDisplay = Instantiate(inputRebindingPrefab, contentParent);
                    rebindDisplay.GetComponent<RebindingDisplay>().ActionReference = inputRef;
                }
            }
        
        }

        public override void GenerateContent(Transform contentParent)
        {
            GenerateKeybinds(contentParent);
        }
        
        public override void OnMenuCategoryGenerated(SettingsUI settingsUI)
        {
            CreateMenu(settingsUI);
        }
        

        public override void ResetSettings()
        {
            
        }
    }
}
