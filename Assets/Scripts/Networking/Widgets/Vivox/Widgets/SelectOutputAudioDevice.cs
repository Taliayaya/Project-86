using System.Linq;
using Networking.Widgets.Vivox.Widgets.Base;
using Unity.Multiplayer.Widgets;

namespace Networking.Widgets.Vivox.Widgets
{
    internal class SelectOutputAudioDevice : SelectAudioDevice
    {
        protected override void SetupDevices()
        {
            var devices = m_ChatService.GetAvailableOutputDevices();
            m_Dropdown.AddOptions(devices.Select(device => device.DeviceName).ToList());
            var activeDevice = m_ChatService.GetActiveOutputDevice();
            m_Dropdown.value = devices.IndexOf(activeDevice);
        }
        
        protected override void OnValueChanged(int selectedDevice)
        {
            var devices = m_ChatService.GetAvailableOutputDevices();
            devices[selectedDevice].SetActiveDeviceAsync();
        }

        protected override void SetupVolume()
        {
            m_Slider.SetValueWithoutNotify(m_ChatService.OutputDeviceVolume);
        }
        
        protected override void OnVolumeChanged(float volume)
        {
            m_ChatService.SetOutputDeviceVolume(volume);
        }
    }
}
