#if VIVOX_AVAILABLE
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Unity.Services.Vivox;
using UnityEngine;

namespace Unity.Multiplayer.Widgets
{
    internal class VivoxChatService : IChatService
    {
        public event Action<string> OnChatJoined;
        public event Action<string> OnChatLeft;
        public event Action<IChatParticipant> OnPlayerAddedToChat;
        public event Action<IChatParticipant> OnPlayerRemovedFromChat;
        public event Action<IChatMessage> OnChatMessageReceived;

        public float InputDeviceVolume => VivoxService.Instance.InputDeviceVolume;
        public float OutputDeviceVolume => VivoxService.Instance.OutputDeviceVolume;
        
        Dictionary<string, Collection<IChatParticipant>> m_ActiveChannels = new();
        
        public async Task InitializeAsync()
        {
            await VivoxService.Instance.InitializeAsync();
            
            VivoxService.Instance.ChannelJoined += (channelId) =>
            {
                OnChatJoined?.Invoke(channelId);
                m_ActiveChannels.Add(channelId, new Collection<IChatParticipant>());
            };
            
            VivoxService.Instance.ChannelLeft += (channelId) =>
            {
                OnChatLeft?.Invoke(channelId);
                m_ActiveChannels.Remove(channelId);
            };
            
            VivoxService.Instance.ParticipantAddedToChannel += (participant) =>
            {
                var vivoxChatParticipant = new VivoxChatParticipant(participant);
                m_ActiveChannels[participant.ChannelName].Add(vivoxChatParticipant);
                
                OnPlayerAddedToChat?.Invoke(vivoxChatParticipant);
            };

            VivoxService.Instance.ParticipantRemovedFromChannel += (participant) =>
            {
                var participants = m_ActiveChannels[participant.ChannelName];
                for (var i = participants.Count - 1; i >= 0; i--)
                {
                    if (participants[i].Id == participant.PlayerId)
                    {
                        OnPlayerRemovedFromChat?.Invoke(participants[i]);
                        m_ActiveChannels[participant.ChannelName].Remove(participants[i]);
                        break;
                    }       
                }
            };

            VivoxService.Instance.ChannelMessageReceived += (message) =>
            {
                OnChatMessageReceived?.Invoke(new VivoxChatMessage(message));
            };
        }

        public bool IsLoggedIn()
        {
            return VivoxService.Instance.IsLoggedIn;
        }
        
        public async Task JoinChatAsync(string channelId, ChatOption chatOption)
        {
            await VivoxService.Instance.JoinGroupChannelAsync(channelId, (ChatCapability)chatOption);
        }

        public async Task LeaveChatAsync(string channelId)
        {
            await VivoxService.Instance.LeaveChannelAsync(channelId);
        }

        public ReadOnlyCollection<IAudioDevice> GetAvailableInputDevices()
        {
            var availableInputDevices = VivoxService.Instance.AvailableInputDevices;
            
            var audioDevices = new Collection<IAudioDevice>();
            foreach (var device in availableInputDevices)
            {
                audioDevices.Add(new VivoxAudioInputDevice(device));
            }

            return new ReadOnlyCollection<IAudioDevice>(audioDevices);
        }

        public ReadOnlyCollection<IAudioDevice> GetAvailableOutputDevices()
        {
            var availableOutputDevices = VivoxService.Instance.AvailableOutputDevices;
            
            var audioDevices = new Collection<IAudioDevice>();
            foreach (var device in availableOutputDevices)
            {
                audioDevices.Add(new VivoxAudioOutputDevice(device));
            }

            return new ReadOnlyCollection<IAudioDevice>(audioDevices);
        }

        public IAudioDevice GetActiveInputDevice()
        {
            var activeInputDevice = VivoxService.Instance.ActiveInputDevice;
            return new VivoxAudioInputDevice(activeInputDevice);
        }

        public IAudioDevice GetActiveOutputDevice()
        {
            var activeOutputDevice = VivoxService.Instance.ActiveOutputDevice;
            return new VivoxAudioOutputDevice(activeOutputDevice);
        }

        public async Task SendChatMessageAsync(string channelId, string message)
        {
            await VivoxService.Instance.SendChannelTextMessageAsync(channelId, message);
        }

        /// <inheritdoc cref="IVivoxService.SetOutputDeviceVolume"/>
        public void SetOutputDeviceVolume(float volume)
        {
            VivoxService.Instance.SetOutputDeviceVolume((int)volume);
        }
        
        /// <inheritdoc cref="IVivoxService.SetInputDeviceVolume"/>
        public void SetInputDeviceVolume(float volume)
        {
            VivoxService.Instance.SetInputDeviceVolume((int)volume);
        }
    }
}
#endif
