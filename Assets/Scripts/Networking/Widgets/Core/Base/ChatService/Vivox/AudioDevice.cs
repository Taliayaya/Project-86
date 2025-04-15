#if VIVOX_AVAILABLE
using System.Threading.Tasks;
using Unity.Services.Vivox;

namespace Unity.Multiplayer.Widgets
{
    /// <summary>
    /// Vivox output device implementation of IAudioDevice
    /// </summary>
    internal class VivoxAudioOutputDevice : IAudioDevice
    {
        VivoxOutputDevice m_VivoxOutputDevice;

        public string DeviceName => m_VivoxOutputDevice.DeviceName;
        public string DeviceID => m_VivoxOutputDevice.DeviceID;
        
        public VivoxAudioOutputDevice(VivoxOutputDevice vivoxOutputDevice)
        {
            m_VivoxOutputDevice = vivoxOutputDevice;
        }
        public async Task SetActiveDeviceAsync()
        {
            await VivoxService.Instance.SetActiveOutputDeviceAsync(m_VivoxOutputDevice);
        }
    }
    
    /// <summary>
    /// Vivox input device implementation of IAudioDevice
    /// </summary>
    internal class VivoxAudioInputDevice : IAudioDevice
    {
        VivoxInputDevice m_VivoxInputDevice;

        public string DeviceName => m_VivoxInputDevice.DeviceName;
        public string DeviceID => m_VivoxInputDevice.DeviceID;
        
        public VivoxAudioInputDevice(VivoxInputDevice vivoxInputDevice)
        {
            m_VivoxInputDevice = vivoxInputDevice;
        }
        public async Task SetActiveDeviceAsync()
        {
            await VivoxService.Instance.SetActiveInputDeviceAsync(m_VivoxInputDevice);
        }
    }
}
#endif
