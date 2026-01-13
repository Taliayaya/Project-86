using System;
using System.Collections.Generic;
using System.Reflection;
using DefaultNamespace;
using ScriptableObjects.GameParameters;
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
            GraphicsParameters graphics = parameters as GraphicsParameters;
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
                else if (field.FieldType.IsEnum || field.FieldType == typeof(ResolutionData) || field.FieldType == typeof(DisplayData))
                    AddDropdown(field, parameters, graphics, fieldName, paramWrapper.transform);
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
            GraphicsParameters graphics,
            string parameter,
            Transform parent = null
        )
        {
            var dropdownGo = Instantiate(dropdownPrefab, parent);
            var dropdown = dropdownGo.GetComponentInChildren<TMP_Dropdown>();
            dropdown.options.Clear();

            var fieldType = fieldInfo.FieldType;

            if (fieldType.IsEnum)
            {
                var enumNames = fieldType.GetEnumNames();

                foreach (var enumName in enumNames)
                {
                    dropdown.options.Add(new TMP_Dropdown.OptionData(enumName));
                }

                dropdown.value = (int)fieldInfo.GetValue(parameters);

                dropdown.onValueChanged.AddListener(value =>
                    OnGameSettingsDropdownValueChanged(parameters, fieldInfo, parameter, value)
                );
            }
            else if (fieldType == typeof(ResolutionData))
                ListDropdown(dropdown, fieldInfo, graphics, parameter, graphics.resolutions, graphics.resolution);
            else if (fieldType == typeof(DisplayData))
                ListDropdown(dropdown, fieldInfo, graphics, parameter, graphics.displays, graphics.display);
            return;
        }

        // helper function to unify list behaviour, also for any future use
        private void ListDropdown<T>(
            TMP_Dropdown dropdown,
            FieldInfo fieldInfo,
            GraphicsParameters graphics,
            string parameter,
            List<T> data_list,
            T current_value
        ) where T : IEquatable<T>
        {
            dropdown.options.Clear();

            int selectedIndex = 0;
            for (int i = 0; i < data_list.Count; i++)
            {
                var new_value = data_list[i];
                dropdown.options.Add(new TMP_Dropdown.OptionData(new_value.ToString()));
                if (new_value.Equals(current_value))
                    selectedIndex = i;
            }

            dropdown.value = selectedIndex;

            dropdown.onValueChanged.AddListener(index =>
            {
                var value = data_list[index];
                fieldInfo.SetValue(graphics, value);
                OnGameSettingsDropdownValueChanged(graphics, fieldInfo, parameter, index);
            });
        }

        private void OnGameSettingsDropdownValueChanged(GameParameters parameters, FieldInfo fieldInfo, string parameter, int value)
        {
            var graphics = parameters as GraphicsParameters;
            if (fieldInfo.FieldType == typeof(ResolutionData))
            {
                graphics.resolution = graphics.resolutions[value];
                EventManager.TriggerEvent($"UpdateGameParameter:{parameter}", graphics.resolution);
            }

            else if (fieldInfo.FieldType == typeof(DisplayData))
            {
                graphics.display = graphics.displays[value];
                EventManager.TriggerEvent($"UpdateGameParameter:{parameter}", graphics.display);
            }

            else
            {
                fieldInfo.SetValue(parameters, value);
                EventManager.TriggerEvent($"UpdateGameParameter:{parameter}", value);
            }
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
