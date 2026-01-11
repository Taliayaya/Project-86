/* TODO:
 * 1. Integrate the new resolution logic into the already existing dropdown
 * 2. Make Resolution list not get picked up by Awake() initialization (?)
 * 3. Clean up le code
 */

using DefaultNamespace;
using Firebase.Analytics;
using ScriptableObjects.GameParameters;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SettingsMenu : SettingsMenuCategory
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject sliderPrefab;
        [SerializeField] private GameObject togglePrefab;
        [SerializeField] private GameObject dropdownPrefab;
        [SerializeField] private GameObject contentParameterPrefab;

        [Header("References")]

        private Dictionary<string, GameParameters> _gameParametersMap;

        private void Awake()
        {
            // Loading game parameters into the dictionary with its name as a key and a reference to the object as a value
            _gameParametersMap = new Dictionary<string, GameParameters>();
            var gameParametersArray = Resources.LoadAll<GameParameters>("ScriptableObjects/Parameters");
            foreach (var parameter in gameParametersArray)
            {
                _gameParametersMap.Add(parameter.GetParametersName, parameter);
                if (parameter is GraphicsParameters graphics)
                {
                    graphics.Initialize();
                }
            }
        }

        #region Dynamic Creation of the UI

        public override void GenerateContent(Transform contentParent)
        {
            // not used, custom
        }

        public override void OnMenuCategoryGenerated(SettingsUI settingsUI)
        {
            SetupGameSettingsPanel(settingsUI);
        }

        public override void ResetSettings(SettingsUI settingsUI)
        {
            // not used, custom

        }

        public GameObject AddSettingsMenu(SettingsUI settingsUI, string menuContentName)
        {
            var menuButton = Instantiate(menuCategoryPrefab, settingsUI.menusParent);
            menuButton.GetComponentInChildren<TMP_Text>().text = menuContentName;
            menuButton.GetComponent<Button>().onClick.AddListener(() => SetGameSettingsContent(menuContentName, settingsUI));
            return menuButton;
        }

        private void SetupGameSettingsPanel(SettingsUI settingsUI)
        {
            List<string> availableMenus = new List<string>();
            foreach (var parametersValue in _gameParametersMap.Values)
            {
                // Ignore the parameters that have no parameters to show
                if (parametersValue.FieldsToShowInGame.Count == 0)
                    continue;
                var menuName = parametersValue.GetParametersName;
                AddSettingsMenu(settingsUI, menuName);
                // Set the button name
                availableMenus.Add(menuName);
            }

            if (availableMenus.Count > 0)
                SetGameSettingsContent(availableMenus[0], settingsUI);
        }

        private void SetGameSettingsContent(string menu, SettingsUI settingsUI)
        {
            settingsUI.menuContentName.text = menu;

            // Destroy the previous content
            foreach (Transform child in settingsUI.contentParent)
                Destroy(child.gameObject);

            var parameters = _gameParametersMap[menu];
            var parameterType = parameters.GetType();
            settingsUI.onResetButton.RemoveAllListeners();
            settingsUI.onResetButton.AddListener(
                (s) => ResetCategory(parameters, settingsUI));
            foreach (var fieldName in parameters.FieldsToShowInGame)
            {
                var paramWrapper = Instantiate(contentParameterPrefab, settingsUI.contentParent);
                var field = parameterType.GetField(fieldName);
                if (field == null)
                {
                    Debug.LogError($"[SettingsMenu] Field not found: {fieldName}");
                    continue;
                }

                paramWrapper.transform.GetComponentInChildren<TextMeshProUGUI>().text = fieldName;
                if (field.FieldType == typeof(bool))
                    AddToggle(field, parameters, fieldName, paramWrapper.transform);
                else if (field.FieldType == typeof(int) || field.FieldType == typeof(float))
                    AddSlider(field, parameters, fieldName, paramWrapper.transform);
                else if (field.FieldType.IsEnum)
                    AddDropdown(field, parameters, fieldName, paramWrapper.transform);
                else if (field.FieldType == typeof(ResolutionData))
                    AddResolutionDropdown(parameters as GraphicsParameters, paramWrapper.transform);
                else
                    Debug.LogError($"[SettingsMenu] Field type \"{field.FieldType}\" not supported");
            }

        }

        private void ResetCategory(GameParameters parameters, SettingsUI settingsUI)
        {
            parameters.ResetToDefault();
            SetGameSettingsContent(parameters.GetParametersName, settingsUI);
        }

        private void AddToggle(FieldInfo fieldInfo, GameParameters parameters, string parameter, Transform parent = null)
        {
            var gEditor = Instantiate(togglePrefab, parent);
            Toggle t = gEditor.GetComponent<Toggle>();
            t.isOn = (bool)fieldInfo.GetValue(parameters);
            t.onValueChanged.AddListener((isOn) => OnGameSettingsToggleValueChanged(parameters, fieldInfo, parameter, isOn));
        }

        private void AddSlider(FieldInfo fieldInfo, GameParameters parameters, string parameter, Transform parent = null)
        {
            bool isRange = Attribute.IsDefined(fieldInfo, typeof(RangeAttribute), false);

            if (!isRange)
                return;

            var rangeAttribute = (RangeAttribute)Attribute.GetCustomAttribute(fieldInfo, typeof(RangeAttribute));
            var slider = Instantiate(sliderPrefab, parent);
            var sliderComponent = slider.GetComponentInChildren<Slider>();
            sliderComponent.minValue = rangeAttribute.min;
            sliderComponent.maxValue = rangeAttribute.max;
            sliderComponent.wholeNumbers = fieldInfo.FieldType == typeof(int);
            sliderComponent.value = fieldInfo.FieldType == typeof(int)
                ? (int)fieldInfo.GetValue(parameters)
                : (float)fieldInfo.GetValue(parameters);
            sliderComponent.onValueChanged.AddListener((value) => OnGameSettingsSliderValueChanged(parameters, fieldInfo, parameter, value));
        }

        private void AddDropdown(
            FieldInfo fieldInfo,
            GameParameters parameters,
            string parameter,
            Transform parent = null
        )
        {
            var dropdownGo = Instantiate(dropdownPrefab, parent);
            var dropdown = dropdownGo.GetComponentInChildren<TMP_Dropdown>();
            dropdown.options.Clear();

            var fieldType = fieldInfo.FieldType;
            var enumNames = fieldType.GetEnumNames();

            foreach (var enumName in enumNames)
            {
                dropdown.options.Add(new TMP_Dropdown.OptionData(enumName));
            }

            dropdown.value = (int)fieldInfo.GetValue(parameters);

            dropdown.onValueChanged.AddListener(value =>
                OnGameSettingsDropdownValueChanged(parameters, fieldInfo, parameter, value)
            );

            return;
        }

        private void AddResolutionDropdown(GraphicsParameters graphics, Transform parent)
        {
            var dropdownGo = Instantiate(dropdownPrefab, parent);
            var dropdown = dropdownGo.GetComponentInChildren<TMP_Dropdown>();
            dropdown.options.Clear();

            int selectedIndex = 0;
            for (int i = 0; i < graphics.resolutions.Count; i++)
            {
                var res = graphics.resolutions[i];
                dropdown.options.Add(new TMP_Dropdown.OptionData($"{res.width}x{res.height}@{res.refresh_rate}Hz"));
                if (res.Equals(graphics.current_resolution))
                    selectedIndex = i;
            }

            dropdown.onValueChanged.AddListener(index =>
            {
                if (index >= 0 && index < graphics.resolutions.Count)
                {
                    graphics.current_resolution = graphics.resolutions[index];
                    EventManager.TriggerEvent("UpdateGameParameter:resolution", (object)graphics.current_resolution);
                }
            });
            dropdown.value = selectedIndex;
        }

        private void OnGameSettingsDropdownValueChanged(GameParameters parameters, FieldInfo fieldInfo, string parameter, int value)
        {
            fieldInfo.SetValue(parameters, value);
            EventManager.TriggerEvent($"UpdateGameParameter:{parameter}", value);
        }

        private void OnGameSettingsToggleValueChanged(GameParameters parameters, FieldInfo fieldInfo, string gameParameter, bool isOn)
        {
            fieldInfo.SetValue(parameters, isOn);
            EventManager.TriggerEvent($"UpdateGameParameter:{gameParameter}", isOn);
        }

        private void OnGameSettingsSliderValueChanged(GameParameters parameters, FieldInfo fieldInfo, string gameParameter, float value)
        {
            if (fieldInfo.FieldType == typeof(int))
            {
                fieldInfo.SetValue(parameters, (int)value);
            }
            else
                fieldInfo.SetValue(parameters, value);
            EventManager.TriggerEvent($"UpdateGameParameter:{gameParameter}", value);
        }

        #endregion
    }
}
