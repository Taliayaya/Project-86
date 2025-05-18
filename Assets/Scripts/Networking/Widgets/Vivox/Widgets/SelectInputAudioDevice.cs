using System.Linq;
using Networking.Widgets.Vivox.Widgets.Base;
using Unity.Multiplayer.Widgets;

namespace Networking.Widgets.Vivox.Widgets
{
    internal class SelectInputAudioDevice : SelectAudioDevice
    {
        protected override void SetupDevices()
        {
            var devices = m_ChatService.GetAvailableInputDevices();
            m_Dropdown.AddOptions(devices.Select(device => device.DeviceName).ToList());
            var activeDevice = m_ChatService.GetActiveInputDevice();
            m_Dropdown.value = devices.IndexOf(activeDevice);
        }
        
        protected override void OnValueChanged(int selectedDevice)
        {
            var devices = m_ChatService.GetAvailableInputDevices();
            devices[selectedDevice].SetActiveDeviceAsync();
        }

        protected override void SetupVolume()
        {
            m_Slider.SetValueWithoutNotify(m_ChatService.InputDeviceVolume);
        }
        
        protected override void OnVolumeChanged(float volume)
        {
            m_ChatService.SetInputDeviceVolume(volume);
        }
    }
}
