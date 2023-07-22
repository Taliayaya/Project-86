using System.Collections.Generic;
using ScriptableObjects.Keybinds;
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
        public KeybindsSO keybindsSO;

        public override string MenuContentName => "Keybinds";
        // Start is called before the first frame update

        public void GenerateKeybinds(Transform contentParent)
        {
            foreach (Keybind keybind in keybindsSO.keybinds)
            {
                if (keybind.Count > 0)
                {
                    GameObject rebindDisplay = Instantiate(inputRebindingPrefab, contentParent);
                    rebindDisplay.GetComponent<RebindingDisplay>().Keybind = keybind;
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
        

        public override void ResetSettings(SettingsUI settingsUI)
        {
            EventManager.TriggerEvent("DeleteSave", "Inputs");
            settingsUI.ClearContent();
            GenerateKeybinds(settingsUI.contentParent);
        }
    }
}
