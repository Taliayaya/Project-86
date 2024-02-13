using System;
using TMPro;
using UI.HUD.HUDWindowSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

namespace UI.HUD
{
    public class HUDWindowEdit : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private HUDWindow _hudWindow;
        public MoveWindowArea moveWindowArea;
        public ResizeWindowArea resizeWindowArea;

        private bool IsContextActive => contextMenu.activeSelf;
        [SerializeField] private GameObject contextMenu;
        [SerializeField] private TMP_Text contextMenuTitle;
        [SerializeField] private Slider activeOpacitySlider;
        [SerializeField] private Slider zoomOpacitySlider;
        [SerializeField] private Toggle activeWindowToggle;
        
        public void Init(HUDWindow hudWindow)
        {
            _hudWindow = hudWindow;
            moveWindowArea.hudWindow = hudWindow;
            resizeWindowArea.hudWindow = hudWindow;
            contextMenuTitle.text = hudWindow.settings.title;
            activeOpacitySlider.value = hudWindow.settings.activeOpacity * 100;
            zoomOpacitySlider.value = hudWindow.settings.zoomOpacity * 100;
            activeWindowToggle.isOn = hudWindow.settings.isActive;
            
            zoomOpacitySlider.interactable = hudWindow.settings.isOpacityEditable;
            activeOpacitySlider.interactable = hudWindow.settings.isOpacityEditable;
            activeWindowToggle.interactable = hudWindow.settings.isClosable;
            
            hudWindow.onWindowModeChanged.AddListener(OnWindowModeChanged);
            
        }

        private void OnWindowModeChanged(HUDWindow arg0)
        {
            if (arg0.WindowState == HUDWindow.HUDWindowState.Play)
            {
                arg0.Opacity = arg0.settings.activeOpacity;
            }
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            contextMenu.SetActive(!contextMenu.activeSelf);
            if (!IsContextActive)
            {
                _hudWindow.Opacity = 1;
                _hudWindow.transform.SetAsLastSibling();
                _hudWindow.settings.orderInLayer = _hudWindow.transform.GetSiblingIndex();
            }
        }
        
        public void SetActiveOpacity(float value)
        {
            _hudWindow.Opacity = value/100;
            _hudWindow.settings.activeOpacity = value/100;
        }
        
        public void SetZoomOpacity(float value)
        {
            _hudWindow.Opacity = value/100;
            _hudWindow.settings.zoomOpacity = value/100;
        }
        
        public void SetActiveWindow(bool value)
        {
            _hudWindow.IsActivated = value;
        }
        
        public void SendBackward()
        {
            _hudWindow.transform.SetSiblingIndex(_hudWindow.transform.GetSiblingIndex() - 1);
            _hudWindow.settings.orderInLayer = _hudWindow.transform.GetSiblingIndex();
        }

        public void ResetWindow()
        {
            _hudWindow.ResetSettings();
            Init(_hudWindow);
        }
        
    }
}