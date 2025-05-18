using Networking.Widgets.Core.Base;
using Networking.Widgets.Core.Base.ChatService.Interfaces;
using TMPro;
using Unity.Multiplayer.Widgets;
using UnityEngine.UI;

namespace Networking.Widgets.Vivox.Widgets.Base
{
    /// <summary>
    /// Abstract base class for selecting audio devices.
    /// </summary>
    internal abstract class SelectAudioDevice : WidgetBehaviour
    {
        protected TMP_Dropdown m_Dropdown;
        
        protected Slider m_Slider;
        
        protected bool m_IsInitialized;
        
        protected IChatService m_ChatService;

        protected override void OnEnable()
        {
            m_ChatService = WidgetDependencies.Instance.ChatService;
            base.OnEnable();
        }

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
            if (m_IsInitialized)
                return;
            
            m_Dropdown = GetComponentInChildren<TMP_Dropdown>();
            m_Dropdown.onValueChanged.AddListener(OnValueChanged);
            m_Dropdown.interactable = false;
            m_Dropdown.ClearOptions();

            m_Slider = GetComponentInChildren<Slider>();
            m_Slider.onValueChanged.AddListener(OnVolumeChanged);
            m_Slider.interactable = false;

            m_IsInitialized = true;
        }

        public override void OnServicesInitialized()
        {
            Initialize();
            PopulateUI();
        }

        void PopulateUI()
        {
            SetupDevices();
            SetupVolume();
            m_Dropdown.interactable = true;
            m_Slider.interactable = true;
        }
        
        protected abstract void SetupDevices();
        
        protected abstract void SetupVolume();

        protected abstract void OnValueChanged(int selectedDevice);
        
        protected abstract void OnVolumeChanged(float volume);
        
    }
}
