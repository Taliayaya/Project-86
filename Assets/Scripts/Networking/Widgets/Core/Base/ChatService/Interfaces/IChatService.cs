using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Networking.Widgets.Core.Base.ChatService.Interfaces
{
    /// <summary>
    /// The options for the chat
    /// </summary>
    internal enum ChatOption
    {
        TextOnly,
        AudioOnly,
        TextAndAudio
    }
    
    internal interface IChatService
    {
        /// <summary>
        /// Called when a Chat was joined
        /// </summary>
        event Action<string> OnChatJoined;
        
        /// <summary>
        /// Called when a Chat was left
        /// </summary>
        event Action<string> OnChatLeft;
        
        /// <summary>
        /// Called when a Player was added to the Chat
        /// </summary>
        event Action<IChatParticipant> OnPlayerAddedToChat;
        
        /// <summary>
        /// Called when a Player was removed from the Chat
        /// </summary>
        event Action<IChatParticipant> OnPlayerRemovedFromChat;

        /// <summary>
        /// Called when a new Message was received
        /// </summary>
        event Action<IChatMessage> OnChatMessageReceived;
        
        /// <summary>
        /// Volume of the InputDevice
        /// </summary>
        float InputDeviceVolume { get; }
        
        /// <summary>
        /// Volume of the OutputDevice
        /// </summary>
        float OutputDeviceVolume { get; }

        /// <summary>
        /// Initialize the Service
        /// </summary>
        Task InitializeAsync();
        
        /// <summary>
        /// Is the user logged in
        /// </summary>
        bool IsLoggedIn();
        
        /// <summary>
        /// Join a Chat
        /// </summary>
        /// <param name="channelId">Id of the chat</param>
        /// <param name="chatOption">Options for the chat</param>
        /// <returns></returns>
        Task JoinChatAsync(string channelId, ChatOption chatOption);
        
        /// <summary>
        /// Leave the specified Chat
        /// </summary>
        /// <param name="channelId">The Id of the chat</param>
        Task LeaveChatAsync(string channelId);
        
        /// <summary>
        /// Send a message to the specified Chat
        /// </summary>
        /// <param name="channelId">Id of the chat</param>
        /// <param name="message">Message that is send</param>
        /// <returns></returns>
        Task SendChatMessageAsync(string channelId, string message);
        
        /// <summary>
        /// Get the available Input Devices
        /// </summary>
        /// <returns>Input Devices</returns>
        ReadOnlyCollection<IAudioDevice> GetAvailableInputDevices();
        
        /// <summary>
        /// Get the available Output Devices
        /// </summary>
        /// <returns>Input Devices</returns>
        ReadOnlyCollection<IAudioDevice> GetAvailableOutputDevices();
        
        /// <summary>
        /// Get the active Input Device
        /// </summary>
        /// <returns>Active Input Device</returns>
        IAudioDevice GetActiveInputDevice();
        
        /// <summary>
        /// Get the active Output Device
        /// </summary>
        /// <returns>Active Output Device</returns>
        IAudioDevice GetActiveOutputDevice();
        
        /// <summary>
        /// Set the volume of the Output Device
        /// </summary>
        /// <param name="volume">The desired volume</param>
        void SetOutputDeviceVolume(float volume);
        
        /// <summary>
        /// Set the volume of the Input Device
        /// </summary>
        /// <param name="volume">The desired volume</param>
        void SetInputDeviceVolume(float volume);
    }
}
